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
        public const int MapPointsAmount = 128;
        public const int WorldPointsAmount = 1024;
        public const double SizeWorldMapKoeff = (double)WorldPointsAmount / MapPointsAmount; //8
        public const int MapCellWidth = WorldPointsAmount / MapPointsAmount;
        public double[,] Table = new double[MapPointsAmount, MapPointsAmount];
        public Universe Universe = MyStrategy.Universe;
        public SortedList<int, BonusMap> BonusMapList = new SortedList<int, BonusMap>();

        internal void RunTick(Universe universe) => Universe = universe;

        public BonusMap GenerateMap(Squad squad)
        {
            var squadCenter = squad.Units.GetUnitsCenter();
            List<long> squadIds = new List<long>();
            foreach (var unit in squad.Units)
                squadIds.Add(unit.Id);

            ClearBonusMapList(squad);

            var predictedWorldState = MyStrategy.Predictor.GetStateOnTick(Universe.World.TickIndex + squad.ExpectedTicksToNextUpdate);
            var allUnits = predictedWorldState.MyUnits.GetCombinedList(predictedWorldState.OppUnits);

//            var aeroCollisionMap = GetAeroCollisionMap(allUnits, squadIds, squadCenter);
            var aeroDangerMap = GetAeroDangerMap(allUnits, squadIds, squadCenter);

            //var groundCollision = GenerateMap(allUnits.Where(u => !u.IsAerial && !squadIds.Contains(u.Id)).ToList(), 10, MapPointsAmount);


            var resultingMap = aeroDangerMap;

            resultingMap.Trim();

            BonusMapList.Add(squad.Id, resultingMap);
            var tileListCheck = resultingMap.GetTileList();


            return resultingMap;
        }

        private void ClearBonusMapList(Squad squad)
        {
            foreach (var map in new SortedList<int, BonusMap>(BonusMapList))
                if (map.Key == squad.Id)
                    BonusMapList.Remove(map.Key);
        }

        private BonusMap GetAeroCollisionMap(List<Vehicle> allUnits, List<long> squadIds, AbsolutePosition squadCenter)
        {
            const double affectedRange = 300;
            var unitsForMap = allUnits.Where(
                u => u.IsAerial &&
                     !squadIds.Contains(u.Id) &&
                     (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange
            ).ToList();
            var map = new BonusMap();

            foreach (var unit in unitsForMap)
                map.AddUnitCalculation(unit, 5, 1, 100);

            map.Trim();
            return map;
        }

        private BonusMap GetAeroDangerMap(List<Vehicle> allUnits, List<long> squadIds, AbsolutePosition squadCenter)
        {
            const double affectedRange = 300;
            var unitsForMap = allUnits.Where(
                u => !squadIds.Contains(u.Id) &&
                u.PlayerId != Universe.Player.Id &&
                     (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange
            ).ToList();

            var map = new BonusMap();
            foreach (var unit in unitsForMap)
                map.AddUnitCalculation(unit, unit.AerialAttackRange, unit.AerialDamage, 1000);

            map.Trim();
            return map;
        }

    }

    #region BonusMapExtensions
    public static class BonusMapExtensions
    {
        public static void AddUnitCalculation(this BonusMap map, Vehicle unit, double maxValueDistance, double maxValue, double zeroValueDistance)
        {
            var maxValueDistanceSquared = maxValueDistance* maxValueDistance;

            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                var distanceSquared = unit.GetSquaredDistanceTo(i * BonusMapCalculator.SizeWorldMapKoeff, j * BonusMapCalculator.SizeWorldMapKoeff);
                if (distanceSquared <= maxValueDistanceSquared)
                    map.Table[i, j] = Math.Max(maxValue, map.Table[i, j]);
                if (distanceSquared > maxValueDistanceSquared && distanceSquared < zeroValueDistance)
                    map.Table[i, j] = Math.Max(maxValue - ((distanceSquared - maxValueDistanceSquared) / zeroValueDistance), map.Table[i, j]);
            }
        }

        public static void Trim(this BonusMap map)
        {
            double maxValue = Double.Epsilon;

            //find max value of the map
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
                if (map.Table[i, j] > maxValue)
                    maxValue = map.Table[i, j];

            //scale map to range [0, 1]
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                map.Table[i, j] = map.Table[i, j] / maxValue;
                if (map.Table[i, j] > 1 || map.Table[i, j] < 0 || Double.IsNaN(map.Table[i, j]))
                    throw new Exception("Wrong map trim.");
            }

        }

        public static IEnumerable<Tile> GetTileList(this BonusMap map)
        {
            var tileList = new List<Tile>();
            int tileWidth = BonusMapCalculator.MapCellWidth; //8
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                var tileCenter = new Point(i * BonusMapCalculator.SizeWorldMapKoeff + tileWidth / 2,
                    j * BonusMapCalculator.SizeWorldMapKoeff + tileWidth / 2);
                tileList.Add(new Tile(tileCenter, tileWidth, map.Table[i, j]));
                if (map.Table[i, j] > 1 || map.Table[i, j] < 0)
                    throw new Exception("Wrong tile trim.");
            }

            return tileList;
        }

    }

    #endregion



}
