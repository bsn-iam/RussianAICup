using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
        private readonly BonusMap StaticMap;
        public Dictionary<Point, double> possibleRays = new Dictionary<Point, double>();

        public BonusMapCalculator()
        {
            StaticMap = GenerateStaticMap();
        }


        internal void RunTick(Universe universe) => Universe = universe;

        public AbsolutePosition GetBonusMovePoint(Squad squad)
        {
            var map = GenerateMap(squad);

            int angleStep = 4;
            double radius = squad.CruisingSpeed * squad.ExpectedTicksToNextUpdate * 1.2;
            
            var squadCenterUnit = squad.Units.GetCentralUnit();
            var squadCenter = new Point(squadCenterUnit.X, squadCenterUnit.Y);
            //var possibleRays = new Dictionary<Point, double>();
            possibleRays = new Dictionary<Point, double>();

            for (int angle = 0; angle < 360; angle += angleStep)
            {
                double angleSI = Math.PI / 180 * angle;
                var possibleDestination = new Point(squadCenter.X + radius * Math.Sin(angleSI), squadCenter.Y + radius * Math.Cos(angleSI));
                if (possibleDestination.X < 0 || possibleDestination.Y < 0 || possibleDestination.X >= 1024 || possibleDestination.Y >= 1024)
                    continue;
                var cellValuesOnRay = new List<double>();

                for (int i = (int)Math.Min(possibleDestination.X, squadCenter.X); i <= (int)Math.Max(possibleDestination.X, squadCenter.X); i++)
                    for (int j = (int)Math.Min(possibleDestination.Y, squadCenter.Y); j <= (int)Math.Max(possibleDestination.Y, squadCenter.Y); j++)
                    {
                        var worldPoint = new Point(i, j);
                        var isNearRay =
                            Geom.SegmentCircleIntersects(possibleDestination, squadCenter, worldPoint,
                                (double) MapCellWidth / 2);
                        if (isNearRay)
                        {
                            var mapX = (int)Math.Round(i / SizeWorldMapKoeff);
                            var mapY = (int)Math.Round(j / SizeWorldMapKoeff);
                            if (mapX >= 0 && mapY >= 0 && mapX < MapPointsAmount && mapY < MapPointsAmount)
                            {
                                cellValuesOnRay.Add(map.Table[mapX, mapY]);
                                //Universe.Print($"{worldPoint} is near line between {possibleDestination} and {squadCenter}.");
                            }

                        }
                    }

                possibleRays.Add(possibleDestination, cellValuesOnRay.Average());
            }

            var maxRayWin = Double.MinValue;
            var chosenDestination = new Point();
            foreach (var ray in possibleRays)
                if (ray.Value > maxRayWin)
                {
                    maxRayWin = ray.Value;
                    chosenDestination = ray.Key;
                }

            return chosenDestination.ToAbsolutePosition();
        }

        private BonusMap GenerateMap(Squad squad)
        {
            var timer = new Stopwatch();
            timer.Restart();
            if (!squad.Units.Any())
                return new BonusMap();

            var squadCenter = squad.Units.GetUnitsCenter();
            List<long> squadIds = new List<long>();
            foreach (var unit in squad.Units)
                squadIds.Add(unit.Id);

            ClearBonusMapList(squad);
            BonusMapList = new SortedList<int, BonusMap>();

            List<BonusMap> squadBonusMapList = new List<BonusMap>();

            var predictedWorldState = MyStrategy.Predictor.GetStateOnTick(Universe.World.TickIndex + squad.ExpectedTicksToNextUpdate);
            var allUnits = predictedWorldState.MyUnits.GetCombinedList(predictedWorldState.OppUnits);

            var aeroCollisionMap = GetAeroCollisionMap(allUnits, squadIds, squadCenter, MapType.Flat)
                .SetWeight(-1);
            squadBonusMapList.Add(aeroCollisionMap);
            BonusMapList.Add(1, aeroCollisionMap);

            var aeroDangerMap = GetAeroDangerMap(predictedWorldState.OppUnits, squadCenter, MapType.Additive)
                .SetWeight(-1);
            squadBonusMapList.Add(aeroDangerMap);
            BonusMapList.Add(2, aeroDangerMap);

            //if (squad.IsScout)
            //{
            //    
            //}
            var scoutWinMap = GetScoutBonusMap(predictedWorldState.OppUnits, squadCenter, squad.Units.First().VisionRange, MapType.Additive)
                .SetWeight(1);
            squadBonusMapList.Add(scoutWinMap);
            BonusMapList.Add(3, scoutWinMap);


            //var groundCollision = GenerateMap(enemyUnits.Where(u => !u.IsAerial && !squadIds.Contains(u.Id)).ToList(), 10, MapPointsAmount);

            squadBonusMapList.Add(StaticMap);
            BonusMapList.Add(4, StaticMap);

            var resultingMap = BonusMapSum(squadBonusMapList);
            BonusMapList.Add(5, resultingMap);
            //BonusMapList.Add(squad.Id, scoutWinMap);
            var tileListCheck = aeroCollisionMap.GetTileList();
            var tileListCheck2 = aeroDangerMap.GetTileList();
            var tileListCheck3 = scoutWinMap.GetTileList();
            var tileListCheck4 = resultingMap.GetTileList();

            timer.Stop();
            Universe.Print($"Spent on BonusMap {timer.ElapsedMilliseconds} ms");
            return resultingMap;
        }

        private BonusMap BonusMapSum(List<BonusMap> squadBonusMapList)
        {
            var sumMap= new BonusMap();
            foreach (var map in squadBonusMapList)
            {
                map.Trim();
                for (int i = 0; i < MapPointsAmount; i++)
                for (int j = 0; j < MapPointsAmount; j++)
                    sumMap.Table[i, j] += map.Table[i, j] * map.Weight;
            }
            return sumMap.Trim();
        }

        private void ClearBonusMapList(Squad squad)
        {
            foreach (var map in new SortedList<int, BonusMap>(BonusMapList))
                if (map.Key == squad.Id)
                    BonusMapList.Remove(map.Key);
        }


        #region MapConstructors

        private BonusMap GetAeroCollisionMap(List<Vehicle> allUnits, List<long> squadIds, AbsolutePosition squadCenter, MapType mapType)
        {
            const double affectedRange = 300;
            var unitsForMap = allUnits.Where(
                u => u.IsAerial &&
                     !squadIds.Contains(u.Id) &&
                     (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange
            ).ToList();
            var map = new BonusMap(mapType);

            foreach (var unit in unitsForMap)
                map.AddUnitCalculation(unit, 10, 1, 30);

            return map;
        }

        private BonusMap GetAeroDangerMap(List<Vehicle> enemyUnits, AbsolutePosition squadCenter, MapType mapType)
        {
            const double affectedRange = 500;
            var unitsForMap = enemyUnits.Where(
                u => (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange &&
                     u.Type != VehicleType.Tank
            ).ToList();

            var map = new BonusMap(mapType);
            foreach (var unit in unitsForMap)
                map.AddUnitCalculation(unit, unit.AerialAttackRange * 1, unit.AerialDamage, unit.AerialAttackRange *2.5);

            return map;
        }

        private BonusMap GetScoutBonusMap(List<Vehicle> enemyUnits, AbsolutePosition squadCenter, double scoutVisionRange, MapType mapType)
        {
            const double affectedRange = 1200;
            var unitsForMap = enemyUnits.Where(
                u => (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange &&
                     u.Type != VehicleType.Arrv
            ).ToList();

            var map = new BonusMap(mapType);
            foreach (var unit in unitsForMap)
            {
                map.AddUnitCalculation(unit, scoutVisionRange, unit.AerialDamage + unit.GroundDamage, 800);
            }

            return map;
        }

        private BonusMap GenerateStaticMap()
        {
            var map = new BonusMap().SetWeight(-1.5);
            const double borderWidth = 25;
            var worldWidth = WorldPointsAmount;
            var greenWorldZone = worldWidth - borderWidth;

            for (int i = 0; i < MapPointsAmount; i++)
            for (int j = 0; j < MapPointsAmount; j++)
            {
                if (i * SizeWorldMapKoeff > greenWorldZone)
                    map.Table[i, j] += i * SizeWorldMapKoeff - greenWorldZone;
                if (i * SizeWorldMapKoeff < borderWidth)
                    map.Table[i, j] += borderWidth - i * SizeWorldMapKoeff;

                if (j * SizeWorldMapKoeff > (worldWidth - borderWidth))
                    map.Table[i, j] += j * SizeWorldMapKoeff - greenWorldZone;
                if (j * SizeWorldMapKoeff < borderWidth)
                    map.Table[i, j] += borderWidth - j * SizeWorldMapKoeff;
                }
            
            return map;
        }

        #endregion


    }

    #region BonusMapExtensions
    public static class BonusMapExtensions
    {
        public static void AddUnitCalculation(this BonusMap map, Vehicle unit, double maxValueDistance, double maxValue, double zeroValueDistance)
        {
            if (maxValueDistance > zeroValueDistance)
                throw new Exception("Wrong distance limits.");

            var maxValueDistanceSquared = maxValueDistance* maxValueDistance;
            var zeroValueDistanceSquared = zeroValueDistance * zeroValueDistance;

            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                var distanceSquared = unit.GetSquaredDistanceTo(i * BonusMapCalculator.SizeWorldMapKoeff, j * BonusMapCalculator.SizeWorldMapKoeff);

                if (distanceSquared <= maxValueDistanceSquared)
                {
                    if (map.MapType == MapType.Flat)
                        map.Table[i, j] = Math.Max(maxValue, map.Table[i, j]);
                    else
                        map.Table[i, j] += maxValue;
                }

                if (distanceSquared > maxValueDistanceSquared && distanceSquared < zeroValueDistanceSquared)
                {
                    if (map.MapType == MapType.Flat)
                        map.Table[i, j] = Math.Max(maxValue - ((distanceSquared - maxValueDistanceSquared) / zeroValueDistanceSquared), map.Table[i, j]);
                    else
                    {
                        map.Table[i, j] += maxValue - ((distanceSquared - maxValueDistanceSquared) / zeroValueDistanceSquared);
                    }

                }
            }
        }

        public static BonusMap Trim(this BonusMap map)
        {
            double maxValue = Double.MinValue;
            double minValue = Double.MaxValue;

            //find max value of the map
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                if (map.Table[i, j] > maxValue)
                    maxValue = map.Table[i, j];
                if (map.Table[i, j] < minValue)
                    minValue = map.Table[i, j];
                }

            if (Math.Abs(minValue - maxValue) < Double.Epsilon)
            {
                MyStrategy.Universe.Print("Map is empty");
                return map;
            }

            //scale map to range [0, 1]
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                map.Table[i, j] = (map.Table[i, j] - minValue) / (maxValue - minValue);
                if (map.Table[i, j] > 1 || map.Table[i, j] < 0 || Double.IsNaN(map.Table[i, j]))
                    throw new Exception("Wrong map trim.");
            }
            return map;
        }

        public static IEnumerable<Tile> GetTileList(this BonusMap map)
        {
            map.Trim();
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

        public static BonusMap SetWeight(this BonusMap map, double weigth)
        {
            map.Weight = weigth;
            return map;
        }

    }

    #endregion

    public enum MapType
    {
        Additive,
        Flat,
    }


}
