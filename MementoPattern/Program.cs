/*
Хранение нескольких точек сохранения:
Используются два стека: undo (история состояний) и redo (отмененные состояния).
После каждого изменения корзины сохраняется новый снимок в undo-стек. При Undo текущее состояние
переносится в redo-стек, а корзина восстанавливается из предыдущего снимка.

Ограничения паттерна Memento:
1) Память: каждый снимок хранит копию состояния.
2) Производительность: при частых изменениях постоянное копирование может быть дорогим.
3) Нужен лимит истории (иначе стек будет расти бесконечно).
*/

public class ShoppingCartMemento
{
    private readonly List<CartItem> _savedItems;
    private readonly DateTime _savedAt;

    public ShoppingCartMemento(List<CartItem> items)
    {
        // создаем глубокую копию списка товаров
        _savedItems = items
            .Select(i => new CartItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        _savedAt = DateTime.Now;
    }

    public List<CartItem> GetSavedState()
    {
        // возвращаем копию сохраненного состояния
        return _savedItems
            .Select(i => new CartItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();
    }

    public DateTime GetCreationTime()
    {
        return _savedAt;
    }
}

// класс для представления товара в корзине
public class CartItem
{
    public int ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; }

    public decimal TotalPrice => Quantity * UnitPrice;

    public CartItem(int productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public override string ToString()
    {
        return $"{ProductName} (ID: {ProductId}) - {Quantity} × {UnitPrice:C} = {TotalPrice:C}";
    }
}

public class ShoppingCart
{
    private List<CartItem> _cartItems;

    public ShoppingCart()
    {
        _cartItems = new List<CartItem>();
    }

    public void AddProduct(int productId, string productName, int quantity, decimal unitPrice)
    {
        var cartLine = _cartItems.FirstOrDefault(i => i.ProductId == productId);

        if (cartLine != null)
        {
            cartLine.Quantity += quantity;
            Console.WriteLine($"Обновлено количество товара '{productName}': {cartLine.Quantity} шт.");
        }
        else
        {
            _cartItems.Add(new CartItem(productId, productName, quantity, unitPrice));
            Console.WriteLine($"Добавлен товар '{productName}': {quantity} шт.");
        }
    }

    public void RemoveProduct(int productId, int quantityToRemove = 0)
    {
        var cartLine = _cartItems.FirstOrDefault(i => i.ProductId == productId);

        if (cartLine == null)
        {
            Console.WriteLine($"Товар с ID {productId} не найден в корзине.");
            return;
        }

        if (quantityToRemove <= 0 || quantityToRemove >= cartLine.Quantity)
        {
            _cartItems.Remove(cartLine);
            Console.WriteLine($"Товар '{cartLine.ProductName}' полностью удален из корзины.");
        }
        else
        {
            cartLine.Quantity -= quantityToRemove;
            Console.WriteLine($"Уменьшено количество товара '{cartLine.ProductName}': осталось {cartLine.Quantity} шт.");
        }
    }

    public void ClearCart()
    {
        _cartItems.Clear();
        Console.WriteLine("Корзина полностью очищена.");
    }

    public ShoppingCartMemento SaveState()
    {
        Console.WriteLine($"Сохранено состояние корзины ({_cartItems.Count} товаров)");
        return new ShoppingCartMemento(_cartItems);
    }

    public void RestoreState(ShoppingCartMemento memento)
    {
        if (memento == null)
        {
            Console.WriteLine("Не удалось восстановить состояние: снимок отсутствует.");
            return;
        }

        _cartItems = memento.GetSavedState();
        Console.WriteLine($"Восстановлено состояние корзины из снимка от {memento.GetCreationTime():HH:mm:ss}");
    }

    public void DisplayCart()
    {
        if (_cartItems.Count == 0)
        {
            Console.WriteLine("Корзина пуста.");
            return;
        }

        Console.WriteLine("\n=== СОДЕРЖИМОЕ КОРЗИНЫ ===");
        foreach (var line in _cartItems)
        {
            Console.WriteLine(line);
        }

        decimal cartTotal = _cartItems.Sum(i => i.TotalPrice);
        int totalCount = _cartItems.Sum(i => i.Quantity);

        Console.WriteLine($"ИТОГО: {cartTotal:C}");
        Console.WriteLine($"Количество товаров: {totalCount} шт.");
        Console.WriteLine("=========================\n");
    }

    public int GetItemsCount()
    {
        return _cartItems.Count;
    }
}

// Caretaker - управляет историей состояний
public class ShoppingCartHistory
{
    private readonly Stack<ShoppingCartMemento> _undoStates;
    private readonly Stack<ShoppingCartMemento> _redoStates;
    private readonly int _historyLimit;

    public ShoppingCartHistory(int maxHistorySize = 10)
    {
        _undoStates = new Stack<ShoppingCartMemento>();
        _redoStates = new Stack<ShoppingCartMemento>();
        _historyLimit = maxHistorySize;
    }

    public void SaveState(ShoppingCart cart)
    {
        if (_undoStates.Count >= _historyLimit)
        {
            // удаляем самый старый снимок (дно стека)
            var buffer = new Stack<ShoppingCartMemento>();

            while (_undoStates.Count > 1)
            {
                buffer.Push(_undoStates.Pop());
            }

            _undoStates.Clear();

            while (buffer.Count > 0)
            {
                _undoStates.Push(buffer.Pop());
            }
        }

        _undoStates.Push(cart.SaveState());
        _redoStates.Clear(); // при новом действии очищаем историю redo
    }

    public void Undo(ShoppingCart cart)
    {
        if (_undoStates.Count <= 1)
        {
            Console.WriteLine("Невозможно отменить: история изменений пуста.");
            return;
        }

        // текущее состояние уходит в redo
        _redoStates.Push(_undoStates.Peek());

        // убираем текущее состояние
        _undoStates.Pop();

        // восстанавливаем предыдущее
        var previousSnapshot = _undoStates.Peek();
        cart.RestoreState(previousSnapshot);
    }

    public void Redo(ShoppingCart cart)
    {
        if (_redoStates.Count == 0)
        {
            Console.WriteLine("Невозможно повторить: нет отмененных действий.");
            return;
        }

        var snapshotToRestore = _redoStates.Pop();
        _undoStates.Push(snapshotToRestore);
        cart.RestoreState(snapshotToRestore);
    }

    public int GetHistorySize()
    {
        return _undoStates.Count;
    }

    public int GetRedoSize()
    {
        return _redoStates.Count;
    }

    public void ClearHistory()
    {
        _undoStates.Clear();
        _redoStates.Clear();
        Console.WriteLine("История изменений очищена.");
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== СИСТЕМА УПРАВЛЕНИЯ КОРЗИНОЙ ПОКУПОК ===\n");

        var cart = new ShoppingCart();
        var history = new ShoppingCartHistory(maxHistorySize: 5);

        // сохраняем начальное состояние (пустая корзина)
        history.SaveState(cart);

        bool keepRunning = true;

        while (keepRunning)
        {
            Console.WriteLine("\nКоманды:");
            Console.WriteLine("1 - Показать корзину");
            Console.WriteLine("2 - Добавить товар");
            Console.WriteLine("3 - Удалить товар");
            Console.WriteLine("4 - Отменить последнее действие (Undo)");
            Console.WriteLine("5 - Повторить действие (Redo)");
            Console.WriteLine("6 - Очистить корзину");
            Console.WriteLine("7 - Показать размер истории");
            Console.WriteLine("0 - Выход");
            Console.Write("\nВыберите команду: ");

            if (!int.TryParse(Console.ReadLine(), out int menuChoice))
            {
                Console.WriteLine("Неверная команда.");
                continue;
            }

            switch (menuChoice)
            {
                case 1:
                    cart.DisplayCart();
                    break;

                case 2:
                    Console.Write("Введите ID товара: ");
                    int productId = int.Parse(Console.ReadLine());

                    Console.Write("Введите название товара: ");
                    string productName = Console.ReadLine();

                    Console.Write("Введите количество: ");
                    int amount = int.Parse(Console.ReadLine());

                    Console.Write("Введите цену за единицу: ");
                    decimal unitPrice = decimal.Parse(Console.ReadLine());

                    cart.AddProduct(productId, productName, amount, unitPrice);
                    history.SaveState(cart);
                    break;

                case 3:
                    Console.Write("Введите ID товара для удаления: ");
                    int productIdToRemove = int.Parse(Console.ReadLine());

                    Console.Write("Введите количество для удаления (0 для полного удаления): ");
                    int amountToRemove = int.Parse(Console.ReadLine());

                    cart.RemoveProduct(productIdToRemove, amountToRemove);
                    history.SaveState(cart);
                    break;

                case 4:
                    Console.WriteLine("\n--- Отмена действия ---");
                    history.Undo(cart);
                    break;

                case 5:
                    Console.WriteLine("\n--- Повтор действия ---");
                    history.Redo(cart);
                    break;

                case 6:
                    cart.ClearCart();
                    history.SaveState(cart);
                    break;

                case 7:
                    Console.WriteLine($"Размер истории: {history.GetHistorySize()} снимков");
                    Console.WriteLine($"Доступно повторов: {history.GetRedoSize()} действий");
                    break;

                case 0:
                    keepRunning = false;
                    Console.WriteLine("Выход из программы.");
                    break;

                default:
                    Console.WriteLine("Неизвестная команда.");
                    break;
            }
        }
    }
}

