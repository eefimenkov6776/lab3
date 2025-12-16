/*
Новые типы расчётов (например, скидки) добавляются созданием нового посетителя,
который наследуется от Visitor и реализует VisitProduct() и VisitBox().
Код Product/Box менять не нужно, т.к. у них уже есть Accept(Visitor).

Изменения понадобятся только если появится новый тип элемента структуры (не Product/Box).
Тогда придётся:
1) добавить новый метод VisitNewType(NewType x) в Visitor,
2) реализовать его во всех существующих посетителях,
3) добавить Accept() в новом элементе.
 */

namespace VisitorPattern
{

    // Элемент заказа (компонент структуры), к которому можно применить Visitor
    public interface IOrderElement
    {
        decimal GetPrice();
        void Print(int level = 0);
        void Accept(Visitor visitor);
    }

    // Абстрактный посетитель
    public abstract class Visitor
    {
        public abstract void VisitProduct(Product product);
        public abstract void VisitBox(Box box);
    }

    // Товар
    public class Product : IOrderElement
    {
        public string Name { get; }
        public decimal Price { get; }

        public Product(string name, decimal price)
        {
            Name = name;
            Price = price;
        }

        public decimal GetPrice() => Price;

        public void Print(int level = 0)
        {
            string indent = new string(' ', level * 4);
            Console.WriteLine($"{indent}Товар: {Name} — {Price} руб.");
        }

        public void Accept(Visitor visitor)
        {
            visitor.VisitProduct(this);
        }
    }

    // Коробка (контейнер), внутри могут быть товары и другие коробки
    public class Box : IOrderElement
    {
        public string Name { get; }
        public decimal PackagingCost { get; }
        public List<IOrderElement> Contents { get; }

        public Box(string name, decimal packagingCost = 0)
        {
            Name = name;
            PackagingCost = packagingCost;
            Contents = new List<IOrderElement>();
        }

        public void Add(IOrderElement element)
        {
            Contents.Add(element);
        }

        public decimal GetPrice()
        {
            decimal sum = PackagingCost;

            foreach (var element in Contents)
            {
                sum += element.GetPrice();
            }

            return sum;
        }

        public void Print(int level = 0)
        {
            string indent = new string(' ', level * 4);
            Console.WriteLine($"{indent}Коробка: {Name} (упаковка: {PackagingCost} руб.)");

            foreach (var element in Contents)
            {
                element.Print(level + 1);
            }
        }

        public void Accept(Visitor visitor)
        {
            visitor.VisitBox(this);

            foreach (var element in Contents)
            {
                element.Accept(visitor);
            }
        }
    }

    public class CustomerOrder
    {
        private readonly List<IOrderElement> _elements = new List<IOrderElement>();

        public void Add(IOrderElement element)
        {
            _elements.Add(element);
        }

        public decimal GetTotalPrice()
        {
            decimal total = 0;
            foreach (var element in _elements)
                total += element.GetPrice();

            return total;
        }

        public void PrintContents()
        {
            Console.WriteLine("Состав заказа:");
            Console.WriteLine(new string('=', 25));

            foreach (var element in _elements)
            {
                element.Print();
            }

            Console.WriteLine(new string('=', 25));
            Console.WriteLine($"Итого по заказу: {GetTotalPrice()} руб.");
        }

        public void Accept(Visitor visitor)
        {
            foreach (var element in _elements)
            {
                element.Accept(visitor);
            }
        }
    }

    // Посетитель: расчёт доставки
    public class DeliveryCostCalculator : Visitor
    {
        public decimal TotalDeliveryCost { get; private set; }

        private const decimal ProductDeliveryRate = 0.05m; // 5% от цены товара
        private const decimal BoxHandlingCost = 100m;       // фикс. обработка каждой коробки

        public override void VisitProduct(Product product)
        {
            TotalDeliveryCost += product.Price * ProductDeliveryRate;
        }

        public override void VisitBox(Box box)
        {
            TotalDeliveryCost += box.PackagingCost + BoxHandlingCost;
        }
    }

    // Посетитель: расчёт налогов
    public class TaxCalculator : Visitor
    {
        public decimal TotalTax { get; private set; }

        private const decimal TaxRate = 0.20m; // НДС 20%

        public override void VisitProduct(Product product)
        {
            TotalTax += product.Price * TaxRate;
        }

        public override void VisitBox(Box box)
        {
            TotalTax += box.PackagingCost * TaxRate;
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            // Товары
            var productMonitor = new Product("Монитор 27\"", 28900m);
            var productWebcam = new Product("Веб-камера", 4990m);
            var productRouter = new Product("Wi-Fi роутер", 7990m);
            var productSsd = new Product("SSD 1 ТБ", 10990m);
            var productPowerBank = new Product("Повербанк 20000 мА·ч", 3990m);
            var productHdmiCable = new Product("HDMI-кабель", 690m);
            var productSetupService = new Product("Услуга настройки", 2500m);
            var productWarranty = new Product("Доп. гарантия", 5000m);

            // Коробки
            var boxSmall = new Box("Маленькая коробка", 200m);
            var boxMedium = new Box("Средняя коробка", 500m);
            var boxLarge = new Box("Большая коробка", 1000m);

            boxSmall.Add(productHdmiCable);
            boxSmall.Add(productPowerBank);

            boxMedium.Add(productWebcam);
            boxMedium.Add(productRouter);
            boxMedium.Add(boxSmall);

            boxLarge.Add(productMonitor);
            boxLarge.Add(boxMedium);
            boxLarge.Add(productSsd);

            // Заказ
            var order = new CustomerOrder();
            order.Add(boxLarge);
            order.Add(productWarranty);

            order.PrintContents();
            Console.WriteLine();

            // Посетители (доставка + налоги)
            var deliveryVisitor = new DeliveryCostCalculator();
            var taxVisitor = new TaxCalculator();

            order.Accept(deliveryVisitor);
            order.Accept(taxVisitor);

            Console.WriteLine($"Стоимость доставки: {deliveryVisitor.TotalDeliveryCost} руб.");
            Console.WriteLine($"Сумма налогов:      {taxVisitor.TotalTax} руб.");
        }
    }
}
