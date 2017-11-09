using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public static class MyExtensions
    {
        public static AbsolutePosition GetSquadCenter(this Universe universe, int squad)
        {
            var squadUnits = universe.GetSquadUnits(squad);
            //Console.WriteLine($"Moving {squadUnits.Count} units");

            if (squadUnits.Count == 0)
            {
                Console.WriteLine("Warning! Selection contains 0 units.");
                return new AbsolutePosition(0, 0);
            }

            return GetUnitsCenter(squadUnits);
        }

        public static List<Vehicle> GetSquadUnits(this Universe universe, int squad)
        {
            var squadUnits = new List<Vehicle>();

            foreach (var unit in universe.MyUnits)
                if (unit.Groups.ToList().All(g => g.Equals(squad)))
                    squadUnits.Add(unit);
            return squadUnits;
        }

        public static List<Vehicle> GetTypeMyUnits(this Universe universe, VehicleType type)
        {
            var typeMyUnits = new List<Vehicle>();

            foreach (var unit in universe.MyUnits)
                if (unit.Type.Equals(type))
                    typeMyUnits.Add(unit);
            return typeMyUnits;
        }

        public static AbsolutePosition GetSelectionCenter(this Universe universe)
        {
            var selectedUnits = new List<Vehicle>();

            foreach (var unit in universe.MyUnits)
                if (unit.IsSelected)
                    selectedUnits.Add(unit);
            //Console.WriteLine($"Moving {squadUnits.Count} units");

            if (selectedUnits.Count == 0)
            {
                   Console.WriteLine("Warning! Selection contains 0 units.");
                return new AbsolutePosition(0, 0);
            }

            return selectedUnits.GetUnitsCenter();
        }

        public static AbsolutePosition GetUnitsCenter(this List<Vehicle> units)
        {
            if (units.Count == 1) return 
                    new AbsolutePosition(units[0].X, units[0].Y);

            Dictionary<long, double> distancesPerUnit = new Dictionary<long, double>();

            //get sum of distance to friends
            foreach (var u1 in units)
            {
                double u1Distance = 0;
                foreach (var u2 in units)
                    u1Distance += GetDistanceBetweenUnits(u1, u2);
                distancesPerUnit.Add(u1.Id, u1Distance);
            }

            //get the ID of less distant.
            var minDistance = Double.MaxValue;
            long centerUnitId=0;
            foreach (var pair in distancesPerUnit)
            {
                if (pair.Value > 0.01 && pair.Value < minDistance)
                {
                    minDistance = pair.Value;
                    centerUnitId = pair.Key;
                }
            }

            //return position of less distant unit
            var centerUnit = units.First(u =>u.Id.Equals(centerUnitId));
            return new AbsolutePosition(centerUnit.X, centerUnit.Y);
        }

        public static AbsolutePosition GetNearestPositionToTarget(this List<Vehicle> units, List<Vehicle> targetUnits)
        {
            var minDistance = Double.MaxValue;
            var position = units.GetUnitsCenter();

            foreach (var unit in units)
                foreach (var target in targetUnits)
                {
                    var distance = GetDistanceBetweenUnits(unit, target);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        position= new AbsolutePosition(target.X, target.Y);
                    }
                }
            return position;
        }

        public static double GetDistanceBetweenUnits(Vehicle u1, Vehicle u2) =>
            Math.Sqrt((u1.X - u2.X) * (u1.X - u2.X) + (u1.Y - u2.Y) * (u1.Y - u2.Y));

        public static int SelectionCount(this Universe universe)
        {
            int counter = 0;
            foreach (var unit in universe.MyUnits)
                if (unit.IsSelected) counter += 1;

            if (counter == 0)
                Console.WriteLine("Warning! Selection contains 0 units.");

            return counter;
        }

        public static Range GetRange(this AbsolutePosition position, double radius)
        {
            var XLeft = position.X - radius;
            var XRight = position.X - radius;
            var YTop = position.Y - radius;
            var YBottom = position.Y - radius;

            return new Range(XLeft, XRight, YTop, YBottom);
        }

    }
}
