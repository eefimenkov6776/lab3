/*
Как обеспечена корректность переходов:
- Переходы выполняются только из конкретных состояний (каждое состояние знает “следующее”).
- У Order метод смены состояния TransitionTo() сделан internal: извне нельзя “перепрыгнуть” в любое состояние.
- Для отмены есть отдельный метод Cancel() с проверками недопустимых случаев.
*/


namespace StatePattern
{
    // абстрактный класс состояния заказа
    public abstract class OrderState
    {
        public abstract void ProcessOrder(Order order);
        public abstract string GetStatus();
    }

    // конкретные состояния

    public sealed class OrderNewState : OrderState
    {
        public override void ProcessOrder(Order order)
        {
            Console.WriteLine("Заказ принят и передан в обработку.");
            order.TransitionTo(new OrderProcessingState());
        }

        public override string GetStatus() => "Новый";
    }

    public sealed class OrderProcessingState : OrderState
    {
        public override void ProcessOrder(Order order)
        {
            Console.WriteLine("Заказ собран и отправлен.");
            order.TransitionTo(new OrderShippedState());
        }

        public override string GetStatus() => "В обработке";
    }

    public sealed class OrderShippedState : OrderState
    {
        public override void ProcessOrder(Order order)
        {
            Console.WriteLine("Заказ доставлен получателю.");
            order.TransitionTo(new OrderDeliveredState());
        }

        public override string GetStatus() => "Отправлен";
    }

    public sealed class OrderDeliveredState : OrderState
    {
        public override void ProcessOrder(Order order)
        {
            Console.WriteLine("Заказ уже доставлен — дальнейшая обработка невозможна.");
        }

        public override string GetStatus() => "Доставлен";
    }

    public sealed class OrderCancelledState : OrderState
    {
        public override void ProcessOrder(Order order)
        {
            Console.WriteLine("Заказ отменён — дальнейшая обработка невозможна.");
        }

        public override string GetStatus() => "Отменен";
    }

    // класс заказа
    public class Order
    {
        private OrderState _state;
        private readonly string _orderNumber;

        public Order(string orderNumber)
        {
            _orderNumber = orderNumber;
            _state = new OrderNewState();
        }

        // управление переходами
        internal void TransitionTo(OrderState nextState)
        {
            _state = nextState;
        }

        // основной метод обработки заказа
        public void ProcessOrder()
        {
            Console.WriteLine($"\nЗаказ {_orderNumber}");
            Console.WriteLine($"Текущий статус: {_state.GetStatus()}");
            _state.ProcessOrder(this);
        }

        // метод управления состоянием: отмена заказа
        public void Cancel()
        {
            if (_state is OrderDeliveredState)
            {
                Console.WriteLine("Отмена невозможна: заказ уже доставлен.");
                return;
            }

            if (_state is OrderCancelledState)
            {
                Console.WriteLine("Заказ уже отменён.");
                return;
            }

            Console.WriteLine("Заказ отменён.");
            TransitionTo(new OrderCancelledState());
        }

        // метод управления состоянием: текущий статус
        public string CurrentStatus()
        {
            return _state.GetStatus();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var orderA = new Order("ORD-001");

            orderA.ProcessOrder(); // Новый -> В обработке
            orderA.ProcessOrder(); // В обработке -> Отправлен
            orderA.ProcessOrder(); // Отправлен -> Доставлен
            orderA.ProcessOrder(); // попытка обработать доставленный

            Console.WriteLine("\n--- Второй заказ ---");
            var orderB = new Order("ORD-002");

            orderB.ProcessOrder(); // Новый -> В обработке
            orderB.Cancel();       // отмена
            orderB.ProcessOrder(); // попытка обработать отменённый

            Console.WriteLine("\n--- Проверка статусов ---");
            Console.WriteLine($"ORD-001: {orderA.CurrentStatus()}");
            Console.WriteLine($"ORD-002: {orderB.CurrentStatus()}");

            Console.WriteLine("\n--- Попытка отменить доставленный ---");
            orderA.Cancel();
        }
    }
}

