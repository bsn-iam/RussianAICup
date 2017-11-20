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
        public Move Move { get; set; }
        public World World { get; }
        public Game Game { get; }
        public List<Vehicle> MyUnits { get; }
        public List<Vehicle> OppUnits { get; }
        public Player Player { get; }

        public Universe(World world, Game game, List<Vehicle> myUnits, List<Vehicle> oppUnits, Move move, Player player)
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

        public double GetDistanceToPoint(AbsolutePosition position) => GetDistanceToPoint(position.X, position.Y);
        public double GetDistanceToPoint(double x, double y)
        {
            var xRange = x - X;
            var yRange = y - Y;
            return Math.Sqrt(xRange * xRange + yRange * yRange);
        }
    }

    public class Range
    {
        public Range()
        {
            XLeft = 0;
            XRight = 1024;
            YTop = 0;
            YBottom = 1024;
        }
        public Range(double xLeft, double xRight, double yTop, double yBottom)
        {
            XLeft = xLeft;
            XRight = xRight;
            YTop = yTop;
            YBottom = yBottom;
        }

        public double XLeft { get; }
        public double XRight { get; }
        public double YTop { get; }
        public double YBottom { get; }
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
        public Tile(Point centerPosition, double size, double value)
        {
            CenterPosition = centerPosition;
            Value = value;
            Size = size;
        }

        public Point CenterPosition { get; set; }
        public double Value { get; set; }
        public double Size { get; set; }
    }

    public class BonusMap
    {
        //public const double MapSize = 1024;
        public double[,] Table = new double[BonusMapCalculator.MapSize, BonusMapCalculator.MapSize];
    }
}
