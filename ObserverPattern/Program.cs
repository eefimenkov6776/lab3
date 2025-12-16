namespace ObserverPattern
{
    public enum OrderStage
    {
        Placed,      // Оформлен
        Processing,  // В обработке
        Shipped,     // Отправлен
        Delivered    // Доставлен
    }

    public static class OrderStageTextExtensions
    {
        public static string AsRussianText(this OrderStage stage)
        {
            return stage switch
            {
                OrderStage.Placed => "Оформлен",
                OrderStage.Processing => "В обработке",
                OrderStage.Shipped => "Отправлен",
                OrderStage.Delivered => "Доставлен",
                _ => stage.ToString()
            };
        }
    }
    public interface IOrderSubscriber
    {
        void HandleOrderStageChange(Order trackedOrder, OrderStage previousStage);
    }

    public class Order
    {
        private readonly List<IOrderSubscriber> _subscribers = new List<IOrderSubscriber>();

        public int OrderNumber { get; }
        public OrderStage CurrentStage { get; private set; }

        public Order(int orderNumber, OrderStage initialStage)
        {
            OrderNumber = orderNumber;
            CurrentStage = initialStage;
        }

        public void AddObserver(IOrderSubscriber subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (!_subscribers.Contains(subscriber))
                _subscribers.Add(subscriber);
        }

        public void RemoveObserver(IOrderSubscriber subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            _subscribers.Remove(subscriber);
        }
        public void UpdateStage(OrderStage nextStage)
        {
            if (CurrentStage == nextStage)
                return;

            var oldStage = CurrentStage;
            CurrentStage = nextStage;

            NotifyObservers(oldStage);
        }

        public void NotifyObservers(OrderStage previousStage)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.HandleOrderStageChange(this, previousStage);
            }
        }
    }

    public class ClientNotification : IOrderSubscriber
    {
        private readonly string _customerName;
        private readonly string _contactChannel;

        public ClientNotification(string customerName, string contactChannel)
        {
            _customerName = customerName;
            _contactChannel = contactChannel;
        }

        public void HandleOrderStageChange(Order trackedOrder, OrderStage previousStage)
        {
            Console.WriteLine(
                $"[Клиент] {_customerName}: заказ №{trackedOrder.OrderNumber} — " +
                $"{previousStage.AsRussianText()} → {trackedOrder.CurrentStage.AsRussianText()} | " +
                $"контакт: {_contactChannel}");
        }
    }

    public class ManagerNotification : IOrderSubscriber
    {
        private readonly string _responsibleManager;

        public ManagerNotification(string responsibleManager)
        {
            _responsibleManager = responsibleManager;
        }

        public void HandleOrderStageChange(Order trackedOrder, OrderStage previousStage)
        {
            Console.WriteLine(
                $"[Менеджер] {_responsibleManager}: заказ №{trackedOrder.OrderNumber} — " +
                $"{previousStage.AsRussianText()} → {trackedOrder.CurrentStage.AsRussianText()}. Проверьте обработку.");
        }
    }

    public class AnalyticsSystem : IOrderSubscriber
    {
        public void HandleOrderStageChange(Order trackedOrder, OrderStage previousStage)
        {
            Console.WriteLine(
                $"[Analytics] log: order={trackedOrder.OrderNumber}, " +
                $"stage: {previousStage.AsRussianText()} -> {trackedOrder.CurrentStage.AsRussianText()}");
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            var trackedOrder = new Order(orderNumber: 101, initialStage: OrderStage.Placed);

            var customerNotifier = new ClientNotification("Егор Ефименков", "efimenkov@mail.ru");
            var managerNotifier = new ManagerNotification("Иван Иванов");
            var analyticsModule = new AnalyticsSystem();

            trackedOrder.AddObserver(customerNotifier);
            trackedOrder.AddObserver(managerNotifier);
            trackedOrder.AddObserver(analyticsModule);

            Console.WriteLine("Начало работы программы\n");

            trackedOrder.UpdateStage(OrderStage.Processing);
            trackedOrder.UpdateStage(OrderStage.Shipped);

            Console.WriteLine("\nМенеджер отписан.\n");
            trackedOrder.RemoveObserver(managerNotifier);

            trackedOrder.UpdateStage(OrderStage.Delivered);
        }
    }
}
