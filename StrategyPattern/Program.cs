using System;
using System.Collections.Generic;

namespace StrategyPattern
{
    public class Order
    {
        private PaymentStrategy _currentPayment;
        private readonly List<OrderPosition> _positions = new();

        // можно менять стратегию на лету
        public PaymentStrategy PaymentStrategy
        {
            set { _currentPayment = value; }
        }

        public void AddProduct(string productTitle, decimal unitPrice, int count)
        {
            _positions.Add(new OrderPosition(productTitle, unitPrice, count));
        }

        public void ProcessOrder()
        {
            if (_currentPayment == null)
            {
                Console.WriteLine("Не выбран способ оплаты. Сначала выберите вариант из меню.\n");
                return;
            }

            decimal total = CalculateTotal();
            Console.WriteLine($"\nЗаказ сформирован. Сумма к оплате: {total} руб.");

            _currentPayment.ProcessPayment(total);

            Console.WriteLine("Готово: заказ оформлен.\n");
        }

        private decimal CalculateTotal()
        {
            decimal total = 0;
            foreach (var pos in _positions)
                total += pos.UnitPrice * pos.Quantity;
            return total;
        }

        private sealed class OrderPosition
        {
            public string ProductTitle { get; }
            public decimal UnitPrice { get; }
            public int Quantity { get; }

            public OrderPosition(string productTitle, decimal unitPrice, int quantity)
            {
                ProductTitle = productTitle;
                UnitPrice = unitPrice;
                Quantity = quantity;
            }
        }
    }

    // общий интерфейс для всех способов оплаты
    public abstract class PaymentStrategy
    {
        public abstract void ProcessPayment(decimal amount);
    }

    // 1) Безналичный расчёт (карта)
    public sealed class CardPaymentStrategy : PaymentStrategy
    {
        private readonly string _cardNumber;
        private readonly string _expiry;
        private readonly string _cvv;

        public CardPaymentStrategy(string cardNumber, string expiry, string cvv)
        {
            _cardNumber = cardNumber;
            _expiry = expiry;
            _cvv = cvv;
        }

        public override void ProcessPayment(decimal amount)
        {
            Console.WriteLine($"[Карта] Платёж на {amount} руб.");
            Console.WriteLine($"[Карта] Карта: {Mask(_cardNumber)}, срок: {_expiry}");
            Console.WriteLine("[Карта] Операция успешна.\n");
        }

        private static string Mask(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "****";
            string digits = raw.Replace(" ", "").Trim();
            if (digits.Length <= 4) return digits;
            return $"**** **** **** {digits[^4..]}";
        }
    }

    // 2) Наличный расчёт
    public sealed class CashPaymentStrategy : PaymentStrategy
    {
        public override void ProcessPayment(decimal amount)
        {
            Console.WriteLine($"[Наличные] К оплате: {amount} руб.");
            Console.WriteLine("[Наличные] Оплата наличными при получении.\n");
        }
    }

    // 3) Перевод при получении
    public sealed class TransferOnDeliveryPayment : PaymentStrategy
    {
        private readonly string _recipientRequisites;

        public TransferOnDeliveryPayment(string recipientRequisites)
        {
            _recipientRequisites = recipientRequisites;
        }

        public override void ProcessPayment(decimal amount)
        {
            Console.WriteLine($"[Перевод] К оплате: {amount} руб.");
            Console.WriteLine($"[Перевод] Реквизиты: {_recipientRequisites}");
            Console.WriteLine("[Перевод] Ожидаем перевод при получении заказа.\n");
        }
    }

    /*
            Как добавить новый метод оплаты (например, криптовалюта):
            1) Создать новый класс-стратегию, унаследованный от PaymentStrategy.
               Главное — переопределить ProcessPayment(decimal amount) и описать там логику оплаты.
            2) Если новому способу нужны данные (например, адрес кошелька, сеть, тип монеты),
               добавить их в поля и передавать через конструктор этой стратегии.
            3) В Order ничего менять не нужно: он как работал со стратегией через PaymentStrategy,
               так и продолжит работать
            4) В консольном интерфейсе (Main) добавить новый пункт меню и в обработчике:
               -считать параметры из консоли
               -присвоить order.PaymentStrategy = new CryptoPaymentStrategy(...);
               -вызвать order.ProcessOrder();
            */
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Интернет-магазин ===\n");

            var order = new Order();
            order.AddProduct("Смартфон", 45000m, 1);
            order.AddProduct("Чехол", 1500m, 1);
            order.AddProduct("Защитное стекло", 800m, 2);

            while (true)
            {
                Console.WriteLine("Способ оплаты:");
                Console.WriteLine("1 — Банковская карта");
                Console.WriteLine("2 — Наличными при получении");
                Console.WriteLine("3 — Перевод при получении");
                Console.WriteLine("4 — Выход");
                Console.Write("Выберите пункт: ");

                string menu = (Console.ReadLine() ?? "").Trim();


                switch (menu)
                {
                    case "1":
                        Console.Write("Номер карты: ");
                        string cardNumber = (Console.ReadLine() ?? "").Trim();

                        Console.Write("Срок (ММ/ГГ): ");
                        string expiry = (Console.ReadLine() ?? "").Trim();

                        Console.Write("CVV: ");
                        string cvv = (Console.ReadLine() ?? "").Trim();

                        order.PaymentStrategy = new CardPaymentStrategy(cardNumber, expiry, cvv);
                        order.ProcessOrder();
                        break;

                    case "2":
                        order.PaymentStrategy = new CashPaymentStrategy();
                        order.ProcessOrder();
                        break;

                    case "3":
                        Console.Write("Реквизиты получателя: ");
                        string requisites = (Console.ReadLine() ?? "").Trim();

                        order.PaymentStrategy = new TransferOnDeliveryPayment(requisites);
                        order.ProcessOrder();
                        break;
                    case "4":
                        break;
                    default:
                        Console.WriteLine("Неверный пункт меню. Введите 1–4.\n");
                        continue;
                }

                Console.Write("Хотите попробовать другой способ оплаты для этого же заказа? (Y/N): ");
                string again = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
                if (again != "y")
                    break;
            }
        }
    }
}
