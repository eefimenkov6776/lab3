/*
    заказ с предоплатой:
    1) Добавить новый тип в перечисление:

    2) В Order добавить состояние оплаты:
       -bool IsPaid { get; private set; }
       -метод MarkPaid(), который выставляет IsPaid = true.

    3) В Order.Process() расширить выбор обработчика:

    4) Создать класс PrepaidOrderProcessing : OrderProcessing:
       -переопределить Pay(order): принять предоплату -> order.MarkPaid();
       -запретить доставку без оплаты, а именно переопределить Deliver(order) и не отгружать, пока не будет произведена оплата)
    ProcessOrder() не меняем
    */
using System.Text;
namespace TemplateMethodPattern
{
    public enum OrderType
    {
        Standard = 1,
        Express = 2
    }

    public class Order
    {
        public string ItemName { get; }
        public int ItemCount { get; }
        public decimal PricePerItem { get; }
        public string ShippingAddress { get; }
        public OrderType Type { get; }

        public decimal TotalAmount => PricePerItem * ItemCount;

        public Order(string itemName, int itemCount, decimal pricePerItem, string shippingAddress, OrderType type)
        {
            ItemName = itemName;
            ItemCount = itemCount;
            PricePerItem = pricePerItem;
            ShippingAddress = shippingAddress;
            Type = type;
        }

        public void Process()
        {
            OrderProcessing processor;

            switch (Type)
            {
                case OrderType.Standard:
                    processor = new StandardOrderProcessing();
                    break;
                case OrderType.Express:
                    processor = new ExpressOrderProcessing();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Type), "Неизвестный тип заказа.");
            }

            processor.ProcessOrder(this);
        }
    }

    public abstract class OrderProcessing
    {
        public void ProcessOrder(Order order)
        {
            SelectProduct(order);
            Checkout(order);
            Pay(order);
            Deliver(order);
        }

        protected virtual void SelectProduct(Order order)
        {
            Console.WriteLine($"Товар: {order.ItemName}");
            Console.WriteLine($"Количество: {order.ItemCount}");
            Console.WriteLine($"Сумма: {order.TotalAmount}");
        }

        protected virtual void Checkout(Order order)
        {
            Console.WriteLine("Оформление заказа...");
            Console.WriteLine($"Адрес доставки: {order.ShippingAddress}");
        }

        protected virtual void Pay(Order order)
        {
            Console.WriteLine("Оплата заказа...");
            Console.WriteLine("Оплата принята.");
        }

        protected virtual void Deliver(Order order)
        {
            var deliveryMethod = GetDeliveryMethod(order);
            Console.WriteLine($"Доставка: {deliveryMethod}");
            Console.WriteLine("Заказ передан в службу доставки.");
        }

        protected abstract string GetDeliveryMethod(Order order);
    }

    public class StandardOrderProcessing : OrderProcessing
    {
        protected override string GetDeliveryMethod(Order order)
        {
            return "Стандартная доставка (3–5 дней)";
        }
    }

    public class ExpressOrderProcessing : OrderProcessing
    {
        protected override string GetDeliveryMethod(Order order)
        {
            return "Экспресс-доставка (1–2 дня)";
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Выберите тип заказа:");
            Console.WriteLine("1 — Стандартный");
            Console.WriteLine("2 — Экспресс");
            Console.Write("Сделайте выбор: ");

            if (!int.TryParse(Console.ReadLine(), out var selectedNumber) || (selectedNumber != 1 && selectedNumber != 2))
            {
                Console.WriteLine("Некорректный выбор типа заказа.");
                return;
            }

            var selectedType = (OrderType)selectedNumber;

            Console.Write("Название товара: ");
            var itemName = ReadTextOrDefault("Товар");

            Console.Write("Количество: ");
            if (!int.TryParse(Console.ReadLine(), out var itemCount) || itemCount <= 0)
            {
                Console.WriteLine("Некорректное количество.");
                return;
            }

            Console.Write("Цена за единицу: ");
            if (!decimal.TryParse(Console.ReadLine(), out var pricePerItem) || pricePerItem < 0)
            {
                Console.WriteLine("Некорректная цена.");
                return;
            }

            Console.Write("Адрес доставки: ");
            var shippingAddress = ReadTextOrDefault("Адрес не указан");

            var order = new Order(itemName, itemCount, pricePerItem, shippingAddress, selectedType);

            Console.WriteLine();
            Console.WriteLine("=== Обработка заказа ===");
            order.Process();
            Console.WriteLine("=== Готово ===");
        }

        private static string ReadTextOrDefault(string fallback)
        {
            var text = Console.ReadLine();
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }
    }
}
