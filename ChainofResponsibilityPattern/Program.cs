/*Если запрос не обработал ни один обработчик:

1. Меняю HandleRequest() так, чтобы он возвращал результат обработки (например, bool):
        true — кто-то в цепочке обработал запрос, false — дошли до конца цепочки и никто не обработал.

2. В базовом Handler: если текущий обработчик не подходит и следующего нет, значит запрос остаётся необработанным -> возвращаем false.

3. В клиентском коде проверяю false и выполняю другой вариант развития событий: уведомление пользователя, запись в лог, отправка на ручное согласование/отдельную процедуру.
 */
namespace ChainofResponsibilityPattern
{
    // запрос на возврат товара
    class ReturnRequest
    {
        public int RequestId { get; }
        public decimal RefundAmount { get; }
        public string Comment { get; }

        public ReturnRequest(int requestId, decimal refundAmount, string comment)
        {
            RequestId = requestId;
            RefundAmount = refundAmount;
            Comment = comment;
        }
    }

    // абстрактный обработчик
    abstract class Handler
    {
        private Handler _nextInChain;

        // связываем обработчики в цепочку
        public void SetNextHandler(Handler nextHandler)
        {
            _nextInChain = nextHandler;
        }

        public bool HandleRequest(ReturnRequest request)
        {
            if (CanHandle(request))
            {
                ProcessRequest(request);
                return true;
            }

            if (_nextInChain != null)
            {
                Console.WriteLine($"Заявка #{request.RequestId}: передаю дальше по цепочке...");
                return _nextInChain.HandleRequest(request);
            }

            // никто не обработал — дошли до конца
            Console.WriteLine($"Заявка #{request.RequestId}: не обработана ни одним из обработчиков.");
            return false;
        }

        protected abstract bool CanHandle(ReturnRequest request);
        protected abstract void ProcessRequest(ReturnRequest request);
    }

    // менеджер
    class ManagerHandler : Handler
    {
        protected override bool CanHandle(ReturnRequest request)
        {
            return request.RefundAmount <= 1000m;
        }

        protected override void ProcessRequest(ReturnRequest request)
        {
            Console.WriteLine(
                $"Менеджер одобрил возврат по заявке #{request.RequestId} на сумму {request.RefundAmount} руб. " +
                $"(Причина: {request.Comment})");
        }
    }

    // руководитель
    class SupervisorHandler : Handler
    {
        protected override bool CanHandle(ReturnRequest request)
        {
            return request.RefundAmount > 1000m && request.RefundAmount <= 10000m;
        }

        protected override void ProcessRequest(ReturnRequest request)
        {
            Console.WriteLine(
                $"Руководитель одобрил возврат по заявке #{request.RequestId} на сумму {request.RefundAmount} руб. " +
                $"(Причина: {request.Comment})");
        }
    }

    // служба поддержки
    class SupportHandler : Handler
    {
        protected override bool CanHandle(ReturnRequest request)
        {
            return request.RefundAmount > 10000m && request.RefundAmount <= 50000m;
        }

        protected override void ProcessRequest(ReturnRequest request)
        {
            Console.WriteLine(
                $"Служба поддержки одобрила возврат по заявке #{request.RequestId} на сумму {request.RefundAmount} руб. " +
                $"(Причина: {request.Comment})");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Handler manager = new ManagerHandler();
            Handler supervisor = new SupervisorHandler();
            Handler support = new SupportHandler();

            // строим цепочку: менеджер -> руководитель -> поддержка
            manager.SetNextHandler(supervisor);
            supervisor.SetNextHandler(support);

            var requestSmall = new ReturnRequest(
                requestId: 21,
                refundAmount: 850m,
                comment: "Товар не подошёл по цвету"
);

            var requestMedium = new ReturnRequest(
                requestId: 22,
                refundAmount: 7800m,
                comment: "Повреждение упаковки при доставке"
            );

            var requestLarge = new ReturnRequest(
                requestId: 23,
                refundAmount: 42000m,
                comment: "Серийный дефект, клиент вернул несколько позиций"
            );

            // пример случая, когда никто не обработает
            var requestOutOfPolicy = new ReturnRequest(
                requestId: 24,
                refundAmount: 125000m,
                comment: "Сумма возврата выше лимита, требуется решение финансового отдела"
            );



            Submit(manager, requestSmall);
            Submit(manager, requestMedium);
            Submit(manager, requestLarge);
            Submit(manager, requestOutOfPolicy);
        }

        // здесь как раз обработка ситуации "никто не взял"
        private static void Submit(Handler chainStart, ReturnRequest request)
        {
            Console.WriteLine();
            Console.WriteLine(new string('-', 70));
            Console.WriteLine($"Обработка заявки #{request.RequestId} на сумму {request.RefundAmount} руб. (Причина: {request.Comment})");

            bool wasHandled = chainStart.HandleRequest(request);

            if (!wasHandled)
            {
                Console.WriteLine(
                    $"Итог: заявка #{request.RequestId} осталась без обработки. " +
                    "Нужно отдельное рассмотрение или корректировка регламента.");
            }
        }
    }
}
