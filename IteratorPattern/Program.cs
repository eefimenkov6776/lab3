/*Если по критерию нет товаров, то итератор просто становится 'пустым'.
HasNext() → false, Next() → null, Next(count) → пустой список.
В Catalog после обхода проверяю, вывелось ли хоть что-то
 */

// Интерфейс итератора 
public interface CatalogIterator
{
    bool HasNext();
    Product Next();
    List<Product> Next(int count);
    void Reset();
}

// Товар
public class Product
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
    public int PopularityScore { get; set; }

    public Product(string id, string name, string category, decimal price, int popularityScore)
    {
        Id = id;
        Name = name;
        Category = category;
        Price = price;
        PopularityScore = popularityScore;
    }

    public override string ToString()
    {
        return $"{Name} (Категория: {Category}, Цена: {Price:C}, Популярность: {PopularityScore})";
    }
}

// Итератор по категориям (либо по конкретной категории, либо общий обход "по категориям")
public class CategoryIterator : CatalogIterator
{
    private readonly List<Product> _view;
    private int _cursor;

    public CategoryIterator(List<Product> products, string targetCategory = null)
    {
        var source = products ?? new List<Product>();

        if (!string.IsNullOrWhiteSpace(targetCategory))
        {
            _view = source
                .Where(p => string.Equals(p.Category, targetCategory, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name)
                .ToList();
        }
        else
        {
            _view = source
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToList();
        }

        Reset();
    }

    public bool HasNext()
    {
        return _cursor < _view.Count;
    }

    public Product Next()
    {
        if (!HasNext())
            return null;

        return _view[_cursor++];
    }

    public List<Product> Next(int count)
    {
        var batch = new List<Product>();

        for (int i = 0; i < count && HasNext(); i++)
            batch.Add(Next());

        return batch;
    }

    public void Reset()
    {
        _cursor = 0;
    }
}

// Итератор по цене (от меньшей к большей)
public class PriceIterator : CatalogIterator
{
    private readonly List<Product> _view;
    private int _cursor;

    public PriceIterator(List<Product> products)
    {
        var source = products ?? new List<Product>();

        _view = source
            .OrderBy(p => p.Price)
            .ThenBy(p => p.Name)
            .ToList();

        Reset();
    }

    public bool HasNext()
    {
        return _cursor < _view.Count;
    }

    public Product Next()
    {
        if (!HasNext())
            return null;

        return _view[_cursor++];
    }

    public List<Product> Next(int count)
    {
        var batch = new List<Product>();

        for (int i = 0; i < count && HasNext(); i++)
            batch.Add(Next());

        return batch;
    }

    public void Reset()
    {
        _cursor = 0;
    }
}

// Итератор по популярности (от более популярных к менее популярным)
public class PopularityIterator : CatalogIterator
{
    private readonly List<Product> _view;
    private int _cursor;

    public PopularityIterator(List<Product> products)
    {
        var source = products ?? new List<Product>();

        _view = source
            .OrderByDescending(p => p.PopularityScore)
            .ThenBy(p => p.Name)
            .ToList();

        Reset();
    }

    public bool HasNext()
    {
        return _cursor < _view.Count;
    }

    public Product Next()
    {
        if (!HasNext())
            return null;

        return _view[_cursor++];
    }

    public List<Product> Next(int count)
    {
        var batch = new List<Product>();

        for (int i = 0; i < count && HasNext(); i++)
            batch.Add(Next());

        return batch;
    }

    public void Reset()
    {
        _cursor = 0;
    }
}

// Каталог, который использует текущий итератор для обхода
public class Catalog
{
    private readonly List<Product> _items = new List<Product>();
    private CatalogIterator _iterator;

    public void AddProduct(Product product)
    {
        if (product == null) return;
        _items.Add(product);
    }

    public void SetIterator(CatalogIterator iterator)
    {
        _iterator = iterator;
        _iterator?.Reset();
    }

    // Нужен только чтобы создать итераторы снаружи (и не отдавать внутренний список напрямую)
    public List<Product> GetProductsSnapshot()
    {
        return new List<Product>(_items);
    }

    public void DisplayProducts()
    {
        if (_iterator == null)
        {
            Console.WriteLine("Итератор не установлен.");
            return;
        }

        _iterator.Reset();
        int lineNumber = 1;

        Console.WriteLine("Товары в каталоге:");
        Console.WriteLine(new string('-', 60));

        while (_iterator.HasNext())
        {
            var product = _iterator.Next();
            if (product == null) break;

            Console.WriteLine($"{lineNumber}. {product}");
            lineNumber++;
        }

        // Обработка ситуации, когда по критерию ничего не найдено
        if (lineNumber == 1)
            Console.WriteLine("Нет товаров, соответствующих выбранному критерию.");
    }

    public void DisplayNextProducts(int count)
    {
        if (_iterator == null)
        {
            Console.WriteLine("Итератор не установлен.");
            return;
        }

        var portion = _iterator.Next(count);

        if (portion.Count == 0)
        {
            Console.WriteLine("Больше нет товаров для отображения.");
            return;
        }

        Console.WriteLine($"Следующие {portion.Count} товаров:");
        Console.WriteLine(new string('-', 60));

        for (int i = 0; i < portion.Count; i++)
            Console.WriteLine($"{i + 1}. {portion[i]}");
    }
}

internal class Program
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var catalog = new Catalog();

        catalog.AddProduct(new Product("P001", "Игровая мышь", "Электроника", 39.99m, 87));
        catalog.AddProduct(new Product("P002", "Механическая клавиатура", "Электроника", 119.90m, 93));
        catalog.AddProduct(new Product("P003", "Худи", "Одежда", 54.99m, 84));
        catalog.AddProduct(new Product("P004", "Шапка", "Одежда", 14.99m, 72));
        catalog.AddProduct(new Product("P005", "Сковорода", "Кухня", 32.50m, 80));
        catalog.AddProduct(new Product("P006", "Тостер", "Кухня", 49.99m, 76));
        catalog.AddProduct(new Product("P007", "Настольная игра", "Хобби", 29.99m, 88));
        catalog.AddProduct(new Product("P008", "Роман", "Книги", 12.99m, 69));
        catalog.AddProduct(new Product("P009", "Термокружка", "Аксессуары", 18.99m, 79));
        catalog.AddProduct(new Product("P010", "Роликовые коньки", "Спорт", 89.00m, 91));


        var products = catalog.GetProductsSnapshot();
        Console.WriteLine($"В каталоге: {products.Count} товаров\n");

        Console.WriteLine("=== Обход по категориям (Электроника) ===");
        catalog.SetIterator(new CategoryIterator(products, "Электроника"));
        catalog.DisplayProducts();
        Console.WriteLine();

        Console.WriteLine("=== Обход по цене (от дешевых к дорогим) ===");
        catalog.SetIterator(new PriceIterator(products));
        catalog.DisplayProducts();
        Console.WriteLine();

        Console.WriteLine("=== Обход по популярности (от более популярных к менее популярным) ===");
        catalog.SetIterator(new PopularityIterator(products));
        catalog.DisplayProducts();
        Console.WriteLine();

        Console.WriteLine("=== Попытка обхода несуществующей категории ===");
        catalog.SetIterator(new CategoryIterator(products, "Мебель"));
        catalog.DisplayProducts();
        Console.WriteLine();

        Console.WriteLine("=== Постраничный вывод (по 3 товара) по популярности ===");
        catalog.SetIterator(new PopularityIterator(products));

        catalog.DisplayNextProducts(3);
        Console.WriteLine();
        catalog.DisplayNextProducts(3);
        Console.WriteLine();
        catalog.DisplayNextProducts(3);
        Console.WriteLine();
        catalog.DisplayNextProducts(3);

        Console.WriteLine("\n=== Обход всех товаров по категориям ===");
        catalog.SetIterator(new CategoryIterator(products));
        catalog.DisplayProducts();
    }
}
