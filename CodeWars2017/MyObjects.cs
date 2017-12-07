using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public enum Squads
    {
        All,
        Arrvs,
        Fighters,
        Helicopters,
        Ifvs,
        Tanks,
        Mixture,
    }

    public class Universe
    {
        public Move Move { get; internal set; }
        public World World { get; internal set; }
        public Game Game { get; internal set; }
        public List<Vehicle> MyUnits { get; internal set; }
        public List<Vehicle> OppUnits { get; internal set; }
        public Player Player { get; internal set; }

        public void Update(World world, Game game, List<Vehicle> myUnits, List<Vehicle> oppUnits, Move move, Player player)
        {
            World = world;
            Game = game;
            MyUnits = myUnits;
            OppUnits = oppUnits;
            Move = move;
            Player = player;
        }
        public AbsolutePosition MapCenter => new AbsolutePosition(World.Width / 2.0D, World.Height / 2.0D);
        public AbsolutePosition MapConerLeftLower => new AbsolutePosition(0, World.Height);
        public AbsolutePosition MapConerRightUp => new AbsolutePosition(World.Width, 0);

        public AbsolutePosition MapConerLeftUp => new AbsolutePosition(0, 0);
    }

    public class AbsolutePosition
    {
        public double X { get; }
        public double Y { get; }
        public AbsolutePosition(double x, double y)
        {
            X = x;
            Y = y;
        }
        public AbsolutePosition()
        {
            X = 0;
            Y = 0;
        }

        public double GetDistanceToPoint(AbsolutePosition position) => GetDistanceToPoint(position.X, position.Y);
        public double GetSquaredDistanceToPoint(AbsolutePosition position) => GetSquaredDistanceToPoint(position.X, position.Y);
        public double GetDistanceToPoint(double x, double y) => Math.Sqrt(GetSquaredDistanceToPoint(x, y));

        public double GetSquaredDistanceToPoint(double x, double y)
        {
            var xRange = x - X;
            var yRange = y - Y;
            return xRange * xRange + yRange * yRange;
        }
    }


    public class DeferredAction
    {
        public DeferredAction(IMoveAction action, int requestedExecutionTick)
        {
            Action = action;
            RequestedExecutionTick = requestedExecutionTick;
        }

        public IMoveAction Action { get; }
        public int RequestedExecutionTick { get; }
    }

    public class IdGenerator
    {
        public IdGenerator(int firstNumber, int maxNumber)
        {
            this.firstNumber = firstNumber;
            this.maxNumber = maxNumber;
        }

        public List<int> squadNumbers { get; internal set; } = new List<int>();
        private int firstNumber { get; }
        private int maxNumber { get; }

        public int New
        {
            get
            {
                var newId = firstNumber;
                foreach (var number in squadNumbers)
                    if (number >= newId)
                        newId = number + 1;

                if (newId > maxNumber)
                    throw new Exception($"Group ID id outside the available range [{0}, {maxNumber}]");

                squadNumbers.Add(newId);
                return newId;
            }
        }
        public bool HasCapacity => !squadNumbers.Any() || squadNumbers.Max() < maxNumber;

        public void Remove(int id) => squadNumbers.Remove(id);
    }

    public class Tile
    {
        public Tile(Point centerPosition, double size, double value, double realValue = 0)
        {
            CenterPosition = centerPosition;
            Value = value;
            Size = size;
            RealValue = realValue;
        }

        public Point CenterPosition { get; set; }
        public double Value { get; set; }
        public double RealValue { get; set; }
        public double Size { get; set; }
    }

    public class BonusMap
    {
        public double[,] Table = new double[BonusMapCalculator.MapPointsAmount, BonusMapCalculator.MapPointsAmount];

        public double[,] RealTable { get; internal set; } =
            new double[BonusMapCalculator.MapPointsAmount, BonusMapCalculator.MapPointsAmount];

        public MapType MapType { get; }
        public bool IsPositive { get;  set; }
        public double Weight { get;  set; }

        public BonusMap(MapType mapType)
        {
            this.MapType = mapType;
        }

        public BonusMap()
        {
        }

        public void SetRealValues()
        {
            RealTable = Table;
        }

        public void Reflect()
        {
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                Table[i, j] = - Table[i, j];
            }
        }

        public BonusMap Trim(int power = 1)
        {
            double maxValue = Double.MinValue;
            double minValue = Double.MaxValue;

            //find max value of the map
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                if (Table[i, j] > maxValue)
                    maxValue = Table[i, j];
                if (Table[i, j] < minValue)
                    minValue = Table[i, j];
            }

            if (Math.Abs(minValue - maxValue) < Double.Epsilon)
            {
                MyStrategy.Universe.Print("Map is empty");
                return this;
            }

            //scale map to range [0, 1]
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                Table[i, j] = Math.Pow((Table[i, j] - minValue) / (maxValue - minValue), power);

                if (Table[i, j] > 1 || Table[i, j] < 0 || Double.IsNaN(Table[i, j]))
                    throw new Exception("Wrong map trim.");
            }
            return this;
        }

    }

    public class MoveOrder
    {
        public SortedList<long, AbsolutePosition> OrderList = new SortedList<long, AbsolutePosition>();

        public void Update(List<Vehicle> selectedUnits, Vehicle centralUnit, AbsolutePosition position)
        {
            foreach (var unit in selectedUnits)
            foreach (var moveOrder in new SortedList<long, AbsolutePosition>(OrderList))
            {
                if (moveOrder.Key == unit.Id)
                    OrderList.Remove(moveOrder.Key);
            }
            OrderList.Add(centralUnit.Id, position);
        }
    }
}
