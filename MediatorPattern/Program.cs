/*Безопасность при обработке сообщений обеспечивается на уровне посредника и компонентов:

1. Проверка отправителя в OrderMediator.Notify() — каждое событие может отправлять только “свой” компонент (Client → NewOrder, Manager → OrderApproved, Warehouse → OrderPrepared), иначе действие отклоняется.

2. Проверка типа и целостности данных — перед обработкой проверяю, что data действительно является объектом заказа (OrderRequest) и что значения корректны (не пустое название, количество > 0).

3. Проверка бизнес-правил — менеджер дополнительно валидирует заказ (количество не отрицательное/не нулевое, не превышает лимит и т.п.), иначе заказ не подтверждается.

4. Защита от неизвестных событий — незнакомые события игнорируются, чтобы не выполнять непредусмотренные действия.
 */

namespace MediatorPattern
{
    // данные заказа, которые гоняются между компонентами через посредника
    class OrderDetails
    {
        public string ProductName { get; }
        public int Quantity { get; }

        public OrderDetails(string productName, int quantity)
        {
            ProductName = productName;
            Quantity = quantity;
        }
    }

    // абстрактный посредник
    abstract class Mediator
    {
        public abstract void Notify(object sender, string eventCode, object data);
    }

    // базовый участник взаимодействия через посредника
    abstract class Colleague
    {
        protected Mediator _mediator;

        protected Colleague(Mediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public void ChangeMediator(Mediator newMediator)
        {
            _mediator = newMediator ?? throw new ArgumentNullException(nameof(newMediator));
        }
    }

    // клиент
    class Client : Colleague
    {
        public string Name { get; }

        public Client(Mediator mediator, string name) : base(mediator)
        {
            Name = name;
        }

        // отправка сообщения через посредника (оформление заказа)
        public void CreateOrder(string productName, int quantity)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                Console.WriteLine("Клиент: название товара не может быть пустым.");
                return;
            }

            if (quantity <= 0)
            {
                Console.WriteLine("Клиент: количество должно быть больше нуля.");
                return;
            }

            var order = new OrderDetails(productName.Trim(), quantity);

            Console.WriteLine($"Клиент {Name}: оформляет заказ на '{order.ProductName}', {order.Quantity} шт.");
            _mediator.Notify(this, OrderMediator.EventNewOrder, order);
        }

        // обработка сообщения от других компонентов (через посредника)
        public void ReceiveOrderReady(OrderDetails order)
        {
            Console.WriteLine(
                $"Клиент {Name}: получил уведомление — заказ '{order.ProductName}' ({order.Quantity} шт.) готов к выдаче.");
        }
    }

    // менеджер
    class Manager : Colleague
    {
        public string Name { get; }

        public Manager(Mediator mediator, string name) : base(mediator)
        {
            Name = name;
        }

        // обработка сообщения от клиента (через посредника)
        public void ReviewOrder(OrderDetails order)
        {
            Console.WriteLine($"Менеджер {Name}: получил заказ на '{order.ProductName}' ({order.Quantity} шт.).");

            if (order.Quantity <= 0)
            {
                Console.WriteLine("Менеджер: некорректное количество, заказ отклонён.");
                return;
            }

            if (order.Quantity > 1000)
            {
                Console.WriteLine("Менеджер: слишком большой объём заказа, требуется дополнительное согласование.");
                return;
            }

            Console.WriteLine("Менеджер: заказ подтверждён, отправляем на склад.");
            _mediator.Notify(this, OrderMediator.EventOrderApproved, order);
        }
    }

    // склад
    class Warehouse : Colleague
    {
        public string Name { get; }

        public Warehouse(Mediator mediator, string name) : base(mediator)
        {
            Name = name;
        }

        // обработка сообщения от менеджера (через посредника)
        public void PrepareOrder(OrderDetails order)
        {
            Console.WriteLine($"Склад {Name}: готовит '{order.ProductName}' ({order.Quantity} шт.).");

            // заглушка
            Console.WriteLine("Склад: заказ подготовлен, сообщаем посреднику.");
            _mediator.Notify(this, OrderMediator.EventOrderPrepared, order);
        }
    }

    // конкретный посредник
    class OrderMediator : Mediator
    {
        // коды событий
        public const string EventNewOrder = "NewOrder";
        public const string EventOrderApproved = "OrderApproved";
        public const string EventOrderPrepared = "OrderPrepared";

        public Client Client { get; set; }
        public Manager Manager { get; set; }
        public Warehouse Warehouse { get; set; }

        public override void Notify(object sender, string eventCode, object data)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (string.IsNullOrWhiteSpace(eventCode)) throw new ArgumentNullException(nameof(eventCode));

            // (2) проверка типа/целостности данных
            if (data is not OrderDetails order)
            {
                Console.WriteLine("Посредник: пришли некорректные данные заказа, действие отменено.");
                return;
            }

            // базовая валидация содержимого
            if (string.IsNullOrWhiteSpace(order.ProductName) || order.Quantity <= 0)
            {
                Console.WriteLine("Посредник: данные заказа не прошли проверку, действие отменено.");
                return;
            }

            switch (eventCode)
            {
                case EventNewOrder:
                    // проверка отправителя
                    if (!ReferenceEquals(sender, Client))
                    {
                        Console.WriteLine("Посредник: только клиент может создавать заказ. Попытка отклонена.");
                        return;
                    }

                    if (Manager == null)
                    {
                        Console.WriteLine("Посредник: менеджер не назначен, обработка невозможна.");
                        return;
                    }

                    Console.WriteLine("Посредник: передаём заказ менеджеру.");
                    Manager.ReviewOrder(order);
                    break;

                case EventOrderApproved:
                    if (!ReferenceEquals(sender, Manager))
                    {
                        Console.WriteLine("Посредник: только менеджер может подтверждать заказ. Попытка отклонена.");
                        return;
                    }

                    if (Warehouse == null)
                    {
                        Console.WriteLine("Посредник: склад не назначен, обработка невозможна.");
                        return;
                    }

                    Console.WriteLine("Посредник: заказ подтверждён, передаём на склад.");
                    Warehouse.PrepareOrder(order);
                    break;

                case EventOrderPrepared:
                    if (!ReferenceEquals(sender, Warehouse))
                    {
                        Console.WriteLine("Посредник: только склад может подтверждать подготовку заказа. Попытка отклонена.");
                        return;
                    }

                    if (Client == null)
                    {
                        Console.WriteLine("Посредник: клиент не назначен, уведомление невозможно.");
                        return;
                    }

                    Console.WriteLine("Посредник: заказ готов, уведомляем клиента.");
                    Client.ReceiveOrderReady(order);
                    break;

                default:
                    // неизвестные события — игнор
                    Console.WriteLine($"Посредник: неизвестное событие '{eventCode}', действие проигнорировано.");
                    break;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var orderMediator = new OrderMediator();

            var customer = new Client(orderMediator, "Иван");
            var salesManager = new Manager(orderMediator, "Ольга");
            var stock = new Warehouse(orderMediator, "Центральный склад");

            orderMediator.Client = customer;
            orderMediator.Manager = salesManager;
            orderMediator.Warehouse = stock;

            Console.WriteLine("=== Система управления заказами ===\n");

            Console.Write("Введите название товара: ");
            string productNameInput = Console.ReadLine();

            Console.Write("Введите количество: ");
            string quantityInput = Console.ReadLine();

            if (!int.TryParse(quantityInput, out int quantity))
            {
                Console.WriteLine("Ошибка: количество должно быть целым числом.");
                return;
            }

            customer.CreateOrder(productNameInput, quantity);
        }
    }
}
