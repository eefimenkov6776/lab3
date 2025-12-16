// Отмена нескольких команд делается тем же стеком истории: снимаем N команд и вызываем Undo() по очереди
// Ограничения: не все команды обратимы; история может расти; если менять состояние лифта мимо команд — Undo сломается

namespace CommandPattern
{
    internal class Program
    {
        private static void Main()
        {
            var building = Building.GetBuilding();
            var lift = building.Lift;

            var history = new CommandHistory();
            var controller = new LiftControl(history);

            PrintMenu();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine($"Состояние: этаж {lift.CurrentFloorNumber}, двери {(lift.DoorsOpen ? "открыты" : "закрыты")}");
                Console.Write("Введите команду: ");

                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("0", StringComparison.OrdinalIgnoreCase))
                    break;

                switch (input)
                {
                    case "1":
                        controller.ExecuteCommand(new MoveUpCommand(lift, building));
                        break;

                    case "2":
                        controller.ExecuteCommand(new MoveDownCommand(lift, building));
                        break;

                    case "3":
                        controller.ExecuteCommand(new OpenDoorCommand(lift));
                        break;

                    case "4":
                        controller.ExecuteCommand(new CloseDoorCommand(lift));
                        break;

                    case "5":
                        controller.UndoLastCommand();
                        break;

                    case "6":
                        Console.Write("На какой этаж поехать? ");
                        if (!int.TryParse(Console.ReadLine(), out var targetFloor))
                        {
                            Console.WriteLine("Некорректный номер этажа.");
                            break;
                        }

                        if (targetFloor < 1 || targetFloor > building.FloorsCount)
                        {
                            Console.WriteLine($"Этаж вне диапазона 1–{building.FloorsCount}.");
                            break;
                        }

                        controller.ExecuteCommand(new GoToFloorCommand(lift, building, targetFloor));
                        break;

                    default:
                        Console.WriteLine("Неизвестная команда.");
                        break;
                }
            }

            Console.WriteLine("Программа завершена.");
        }

        private static void PrintMenu()
        {
            Console.WriteLine("Система управления лифтом");
            Console.WriteLine("1 - подняться на этаж выше");
            Console.WriteLine("2 - опуститься на этаж ниже");
            Console.WriteLine("3 - открыть двери");
            Console.WriteLine("4 - закрыть двери");
            Console.WriteLine("5 - отменить последнюю команду");
            Console.WriteLine("6 - доехать до указанного этажа (как составная команда)");
            Console.WriteLine("0 - выход");
        }
    }

    public sealed class Building
    {
        private static Building _instance;

        private readonly List<FloorInfo> _floors = new List<FloorInfo>();

        public Elevator Lift { get; }

        public int FloorsCount => _floors.Count;

        public static Building GetBuilding()
        {
            if (_instance == null)
                _instance = new Building();

            return _instance;
        }

        private Building()
        {
            Console.WriteLine("Создано новое здание");

            SeedFloors(10);
            Lift = Elevator.GetElevator(bottomFloor: 1, topFloor: _floors.Count);
        }

        private void SeedFloors(int count)
        {
            for (var i = 1; i <= count; i++)
                _floors.Add(new FloorInfo(i));
        }

        public FloorInfo GetFloor(int floorNumber)
        {
            if (floorNumber < 1 || floorNumber > _floors.Count)
                throw new ArgumentOutOfRangeException(nameof(floorNumber), "Этаж отсутствует в здании.");

            return _floors[floorNumber - 1];
        }
    }

    public readonly struct FloorInfo
    {
        public int Number { get; }
        public string RoomLeft { get; }
        public string RoomRight { get; }

        public FloorInfo(int number)
        {
            Number = number;
            RoomLeft = $"Комната {number}A";
            RoomRight = $"Комната {number}B";
        }

        public void ShowRooms()
        {
            Console.WriteLine($"На этаже {Number}: {RoomLeft}, {RoomRight}");
        }
    }

    public sealed class Elevator
    {
        private static Elevator _instance;

        private readonly int _minFloor;
        private readonly int _maxFloor;

        public int CurrentFloorNumber { get; private set; }
        public bool DoorsOpen { get; private set; }

        private Elevator(int bottomFloor, int topFloor)
        {
            if (bottomFloor > topFloor)
                throw new ArgumentException("Нижний этаж не может быть выше верхнего.");

            _minFloor = bottomFloor;
            _maxFloor = topFloor;

            CurrentFloorNumber = _minFloor;
            DoorsOpen = false;

            Console.WriteLine($"Лифт установлен на {CurrentFloorNumber} этаже");
        }

        public static Elevator GetElevator(int bottomFloor, int topFloor)
        {
            if (_instance == null)
                _instance = new Elevator(bottomFloor, topFloor);

            return _instance;
        }

        public void MoveUp()
        {
            if (DoorsOpen)
            {
                Console.WriteLine("Нельзя ехать вверх с открытыми дверями.");
                return;
            }

            if (CurrentFloorNumber >= _maxFloor)
            {
                Console.WriteLine("Лифт уже на верхнем этаже.");
                return;
            }

            CurrentFloorNumber++;
            Console.WriteLine($"Лифт поднялся на {CurrentFloorNumber} этаж.");
        }

        public void MoveDown()
        {
            if (DoorsOpen)
            {
                Console.WriteLine("Нельзя ехать вниз с открытыми дверями.");
                return;
            }

            if (CurrentFloorNumber <= _minFloor)
            {
                Console.WriteLine("Лифт уже на нижнем этаже.");
                return;
            }

            CurrentFloorNumber--;
            Console.WriteLine($"Лифт опустился на {CurrentFloorNumber} этаж.");
        }

        public void OpenDoor()
        {
            if (DoorsOpen)
            {
                Console.WriteLine("Двери уже открыты.");
                return;
            }

            DoorsOpen = true;
            Console.WriteLine("Двери лифта открыты.");
        }

        public void CloseDoor()
        {
            if (!DoorsOpen)
            {
                Console.WriteLine("Двери уже закрыты.");
                return;
            }

            DoorsOpen = false;
            Console.WriteLine("Двери лифта закрыты.");
        }
    }


    public abstract class Command
    {
        public abstract string Name { get; }

        public bool IsExecutedSuccessfully { get; protected set; }

        public abstract void Execute();
        public abstract void Undo();
    }

    public sealed class MoveUpCommand : Command
    {
        private readonly Elevator _lift;
        private readonly Building _building;

        public MoveUpCommand(Elevator lift, Building building)
        {
            _lift = lift ?? throw new ArgumentNullException(nameof(lift));
            _building = building ?? throw new ArgumentNullException(nameof(building));
        }

        public override string Name => "Движение вверх";

        public override void Execute()
        {
            var before = _lift.CurrentFloorNumber;
            _lift.MoveUp();

            IsExecutedSuccessfully = _lift.CurrentFloorNumber != before;
            if (!IsExecutedSuccessfully) return;

            _building.GetFloor(_lift.CurrentFloorNumber).ShowRooms();
        }

        public override void Undo()
        {
            if (!IsExecutedSuccessfully) return;

            _lift.MoveDown();
            Console.WriteLine("Отмена: лифт вернулся на предыдущий этаж.");
            _building.GetFloor(_lift.CurrentFloorNumber).ShowRooms();
        }
    }

    public sealed class MoveDownCommand : Command
    {
        private readonly Elevator _lift;
        private readonly Building _building;

        public MoveDownCommand(Elevator lift, Building building)
        {
            _lift = lift ?? throw new ArgumentNullException(nameof(lift));
            _building = building ?? throw new ArgumentNullException(nameof(building));
        }

        public override string Name => "Движение вниз";

        public override void Execute()
        {
            var before = _lift.CurrentFloorNumber;
            _lift.MoveDown();

            IsExecutedSuccessfully = _lift.CurrentFloorNumber != before;
            if (!IsExecutedSuccessfully) return;

            _building.GetFloor(_lift.CurrentFloorNumber).ShowRooms();
        }

        public override void Undo()
        {
            if (!IsExecutedSuccessfully) return;

            _lift.MoveUp();
            Console.WriteLine("Отмена: лифт вернулся на предыдущий этаж.");
            _building.GetFloor(_lift.CurrentFloorNumber).ShowRooms();
        }
    }

    public sealed class OpenDoorCommand : Command
    {
        private readonly Elevator _lift;

        public OpenDoorCommand(Elevator lift)
        {
            _lift = lift ?? throw new ArgumentNullException(nameof(lift));
        }

        public override string Name => "Открыть двери";

        public override void Execute()
        {
            var wasOpen = _lift.DoorsOpen;
            _lift.OpenDoor();
            IsExecutedSuccessfully = !wasOpen && _lift.DoorsOpen;
        }

        public override void Undo()
        {
            if (!IsExecutedSuccessfully) return;

            _lift.CloseDoor();
            Console.WriteLine("Отмена: двери снова закрыты.");
        }
    }

    public sealed class CloseDoorCommand : Command
    {
        private readonly Elevator _lift;

        public CloseDoorCommand(Elevator lift)
        {
            _lift = lift ?? throw new ArgumentNullException(nameof(lift));
        }

        public override string Name => "Закрыть двери";

        public override void Execute()
        {
            var wasOpen = _lift.DoorsOpen;
            _lift.CloseDoor();
            IsExecutedSuccessfully = wasOpen && !_lift.DoorsOpen;
        }

        public override void Undo()
        {
            if (!IsExecutedSuccessfully) return;

            _lift.OpenDoor();
            Console.WriteLine("Отмена: двери снова открыты.");
        }
    }
    public sealed class GoToFloorCommand : Command
    {
        private readonly Elevator _lift;
        private readonly Building _building;
        private readonly int _destinationFloor;

        private readonly List<Command> _steps = new List<Command>();

        public GoToFloorCommand(Elevator lift, Building building, int destinationFloor)
        {
            _lift = lift ?? throw new ArgumentNullException(nameof(lift));
            _building = building ?? throw new ArgumentNullException(nameof(building));
            _destinationFloor = destinationFloor;
        }

        public override string Name => $"Поездка на этаж {_destinationFloor}";

        public override void Execute()
        {
            _steps.Clear();

            // если двери открыты — закрываем
            if (_lift.DoorsOpen)
            {
                var close = new CloseDoorCommand(_lift);
                close.Execute();
                if (close.IsExecutedSuccessfully) _steps.Add(close);
            }

            while (_lift.CurrentFloorNumber < _destinationFloor)
            {
                var up = new MoveUpCommand(_lift, _building);
                up.Execute();
                if (!up.IsExecutedSuccessfully) break;
                _steps.Add(up);
            }

            while (_lift.CurrentFloorNumber > _destinationFloor)
            {
                var down = new MoveDownCommand(_lift, _building);
                down.Execute();
                if (!down.IsExecutedSuccessfully) break;
                _steps.Add(down);
            }

            if (_lift.CurrentFloorNumber == _destinationFloor)
            {
                var open = new OpenDoorCommand(_lift);
                open.Execute();
                if (open.IsExecutedSuccessfully) _steps.Add(open);
            }

            IsExecutedSuccessfully = _steps.Count > 0;
        }

        public override void Undo()
        {
            if (!IsExecutedSuccessfully) return;

            Console.WriteLine($"Отмена поездки на этаж {_destinationFloor}");

            for (var i = _steps.Count - 1; i >= 0; i--)
                _steps[i].Undo();

            _steps.Clear();
        }
    }

    public sealed class CommandHistory
    {
        private readonly Stack<Command> _undoStack = new Stack<Command>();

        public bool HasCommands => _undoStack.Count > 0;

        public void Push(Command command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _undoStack.Push(command);
        }

        public Command Pop()
        {
            return _undoStack.Count == 0 ? null : _undoStack.Pop();
        }
    }

    public sealed class LiftControl
    {
        private readonly CommandHistory _history;

        public LiftControl(CommandHistory commandHistory)
        {
            _history = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
        }

        public void ExecuteCommand(Command command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            command.Execute();

            if (command.IsExecutedSuccessfully)
            {
                _history.Push(command);
            }
            else
            {
                Console.WriteLine($"Команда \"{command.Name}\" не выполнена — в историю не записываем.");
            }
        }

        public void UndoLastCommand()
        {
            var last = _history.Pop();
            if (last == null)
            {
                Console.WriteLine("Нет команд для отмены.");
                return;
            }

            Console.WriteLine($"Отмена команды: {last.Name}");
            last.Undo();
        }
    }
}
