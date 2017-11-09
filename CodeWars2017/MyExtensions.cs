using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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
            var xs = new List<double>();
            var ys = new List<double>();
            foreach (var unit in units)
            {
                xs.Add(unit.X);
                ys.Add(unit.Y);
            }
            return new AbsolutePosition(xs.Average(), ys.Average());
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

    }
}
