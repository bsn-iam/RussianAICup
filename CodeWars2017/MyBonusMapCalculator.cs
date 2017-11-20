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
        public const int MapSize = 128;
        public const int WorldSize = 1024;
        public const double SizeWorldMapKoeff = (double)WorldSize / MapSize;
        public const int MapCellWidth = WorldSize / MapSize;
        public double[,] Table = new double[MapSize, MapSize];
        public Universe Universe = MyStrategy.Universe;
        public SortedList<int, BonusMap> BonusMapList = new SortedList<int, BonusMap>();

        internal void RunTick(Universe universe) => Universe = universe;

        public BonusMap GenerateMap(Squad squad)
        {
            List<long> squadIds = new List<long>();
            foreach (var unit in squad.Units)
                squadIds.Add(unit.Id);

            var allUnits = Universe.MyUnits.GetCombinedList(Universe.OppUnits);
            var aeroCollision = GenerateMap(allUnits.Where(u => u.IsAerial && !squadIds.Contains(u.Id)).ToList(), 10, MapSize);
            //var groundCollision = GenerateMap(allUnits.Where(u => !u.IsAerial && !squadIds.Contains(u.Id)).ToList(), 10, MapSize);


            var resultingMap = aeroCollision;

            foreach (var map in new SortedList<int, BonusMap>(BonusMapList))
                if (map.Key == squad.Id)
                    BonusMapList.Remove(map.Key);

            resultingMap.Trim();

            BonusMapList.Add(squad.Id, resultingMap);
            var tileListCheck = resultingMap.GetTileList();


            return resultingMap;
        }

        private BonusMap GenerateMap(List<Vehicle> units, double maxValueDistance, double zeroValueDistance)
        {
            var map = new BonusMap();

            foreach (var unit in units)
            {
                for (int i = 0; i < MapSize; i++)
                    for (int j = 0; j < MapSize; j++)
                    {
                        var distance = unit.GetDistanceTo(i * SizeWorldMapKoeff, j * SizeWorldMapKoeff);
                        if (distance <= maxValueDistance)
                            map.Table[i, j] = 1;
                        if (distance > maxValueDistance && distance < zeroValueDistance)
                            map.Table[i, j] = distance / maxValueDistance;
                    }
            }
            return map;
        }

        
    }


    public static class BonusMapExtensions
    {
        public static void Trim(this BonusMap map)
        {
            double maxValue = Double.MinValue;
            
            for (int i = 0; i < BonusMapCalculator.MapSize; i++)
            for (int j = 0; j < BonusMapCalculator.MapSize; j++)
                if (map.Table[i, j] > maxValue)
                    maxValue = map.Table[i, j];

            for (int i = 0; i < BonusMapCalculator.MapSize; i++)
            for (int j = 0; j < BonusMapCalculator.MapSize; j++)
            {
                map.Table[i, j] = map.Table[i, j] / maxValue;
                if (map.Table[i, j] > 1 || map.Table[i, j] < 0)
                    throw new Exception("Wrong map trim.");
            }
                
        }

        public static IEnumerable<Tile> GetTileList(this BonusMap map)
        {
            var tileList = new List<Tile>();
            int tileWidth = BonusMapCalculator.MapCellWidth;
            for (int i = 0; i < BonusMapCalculator.MapSize; i = i + tileWidth)
            for (int j = 0; j < BonusMapCalculator.MapSize; j = j + tileWidth)
            {
                var tileCenter = new Point(i * BonusMapCalculator.SizeWorldMapKoeff + tileWidth / 2,
                    j * BonusMapCalculator.SizeWorldMapKoeff + tileWidth / 2);
                tileList.Add(new Tile(tileCenter, tileWidth, map.Table[i, j]));
                if (map.Table[i, j] > 1 || map.Table[i, j] < 0)
                    throw new Exception("Wrong tile trim.");
                }

            return tileList;
        }

        //public static IEnumerable<Tile> TransferToTileList(this BonusMap map)
        //{
        //    var tileList = new List<Tile>();
        //    for (int i = 0; i < BonusMapCalculator.MapSize; i = i + Tile.Size)
        //    for (int j = 0; j < BonusMapCalculator.MapSize; j = j + Tile.Size)
        //    {
        //        double tileValue = 0;
        //        for (int tilei = i; tilei < i + Tile.Size; tilei++)
        //            for (int tilej = j; tilej < j + Tile.Size; tilej++)
        //            {
        //                tileValue += map.Table[tilei, tilej];
        //            }
        //        var tileCenter = new Point(i + Tile.Size / 2, j + Tile.Size / 2);
        //        tileList.Add(new Tile(tileCenter, tileValue));
        //    }
        //
        //    return tileList;
        //}
    }
}
