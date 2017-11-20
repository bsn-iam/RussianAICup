using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class BonusMapCalculator
    {
        private const double MapSize = 1024;
        public double[,] Table = new double[1024, 1024];
        public Universe Universe = MyStrategy.Universe;


        public BonusMap GenerateMap()
        {

            var allUnits = Universe.MyUnits.GetCombinedList(Universe.OppUnits);
            var aeroCollision = GenerateMap(allUnits.Where(u => u.IsAerial).ToList(), 10, MapSize);
            var groundCollision = GenerateMap(allUnits.Where(u => !u.IsAerial).ToList(), 10, MapSize);

            var resultingMap = new BonusMap();

            resultingMap = aeroCollision;

            return resultingMap;
        }

        private BonusMap GenerateMap(List<Vehicle> units, double maxValueDistance, double zeroValueDistance)
        {
            var map = new BonusMap();

            foreach (var unit in units.Where(u => u.IsAerial))
            {
                for (int i = 0; i < MapSize; i++)
                    for (int j = 0; j < MapSize; j++)
                    {
                        var distance = unit.GetDistanceTo(i, j);
                        if (distance <= maxValueDistance)
                            map.Table[i, j] = 1;
                        if (distance > maxValueDistance && distance < zeroValueDistance)
                            map.Table[i, j] = distance / maxValueDistance;
                    }
            }

            return map;
        }

        internal void RunTick(Universe universe)
        {
            Universe = universe;
        }
    }

    public class Tile
    {
        public const int Size = 16;

        public Tile(Point centerPosition, double value)
        {
            CenterPosition = centerPosition;
            Value = value;
        }

        public Point CenterPosition { get; set; }
        public double Value { get; set; }
    }

    public class BonusMap
    {
        public const double MapSize = 1024;
        public double[,] Table = new double[1024, 1024];
    }

    public static class BonusMapExtensions
    {
        public static IEnumerable<Tile> TransferToTileList(this BonusMap map)
        {
            var tileList = new List<Tile>();
            for (int i = 0; i < BonusMap.MapSize; i = i + Tile.Size)
            for (int j = 0; j < BonusMap.MapSize; j = j + Tile.Size)
            {
                double tileValue = 0;
                for (int tilei = 0; tilei < i + Tile.Size; tilei++)
                for (int tilej = 0; tilej < j + Tile.Size; tilej++)
                {
                    tileValue += map.Table[i, j];
                }
                var tileCenter = new Point(i + Tile.Size / 2, j + Tile.Size / 2);
                tileList.Add(new Tile(tileCenter, tileValue));
            }

            return tileList;
        }
    }
}
