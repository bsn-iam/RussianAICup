using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class BonusMapCalculator
    {
        public const int MapPointsAmount = 64;
        public const int WorldPointsAmount = 1024;
        public const double SizeWorldMapKoeff = (double)WorldPointsAmount / MapPointsAmount; //8
        public const int MapCellWidth = WorldPointsAmount / MapPointsAmount;
        public double[,] Table = new double[MapPointsAmount, MapPointsAmount];
        public Universe Universe = MyStrategy.Universe;
        public SortedList<int, List<BonusMap>> BonusMapList = new SortedList<int, List<BonusMap>>();
        private readonly BonusMap StaticMap;
        public SortedList<int, Dictionary<Point, double>> PossibleRays = new SortedList<int, Dictionary<Point, double>>();

        public BonusMapCalculator()
        {
            StaticMap = GenerateStaticMap();
        }


        internal void RunTick(Universe universe) => Universe = universe;

        public AbsolutePosition GetBonusMovePoint(Squad squad)
        {
            var timer = new Stopwatch();
            timer.Restart();

            ClearBonusMapList(squad);
            ClearPossibleRaysList(squad);

            var map = GenerateMap(squad);

            var possibleRays = GeneratePossibleRays(squad, map);
            PossibleRays.Add(squad.Id, possibleRays);

            var chosenDestination = GetBestDestination(possibleRays);

            timer.Stop();
            Universe.Print($"Squad [{(Squads)squad.Id}], spent on BonusMap [{timer.ElapsedMilliseconds}] ms. Destination [{chosenDestination.X:f2}, {chosenDestination.Y:f2}]");

            return chosenDestination;
        }

        private BonusMap GenerateMap(Squad squad)
        {
            if (!squad.Units.Any())
                return new BonusMap();

            var squadCenter = squad.Units.GetUnitsCenter();
            List<long> squadIds = new List<long>();
            foreach (var unit in squad.Units)
                squadIds.Add(unit.Id);

            
            List<BonusMap> squadBonusMapList = new List<BonusMap>();

            var predictedWorldState = MyStrategy.Predictor.GetStateOnTick(Universe.World.TickIndex + squad.ExpectedTicksToNextUpdate);
            var allUnits = predictedWorldState.MyUnits.GetCombinedList(predictedWorldState.OppUnits);


            if (squad.IsScout)
            {
                var aeroDangerMap = GetAeroDangerMap(predictedWorldState.OppUnits, squadCenter, MapType.Additive);
                squadBonusMapList.Add(aeroDangerMap.SetWeight(-1));

                var aeroCollisionMap = GetAeroCollisionMap(allUnits, squadIds, squadCenter, MapType.Flat);
                squadBonusMapList.Add(aeroCollisionMap.SetWeight(-1));

                var scoutWinMap = GetScoutBonusMap(predictedWorldState.OppUnits, squadCenter,
                    squad.Units.First().VisionRange, MapType.Additive);
                squadBonusMapList.Add(scoutWinMap.SetWeight(1));
            }
            else
            {
                var commonWinMap = GetCommonWinMap(predictedWorldState.OppUnits, squad, MapType.Additive);
                squadBonusMapList.Add(commonWinMap.SetWeight(1));

                var collisionMap = squad.IsAerial ? 
                    GetAeroCollisionMap(predictedWorldState.MyUnits, squadIds, squadCenter, MapType.Flat) : 
                    GetGroundCollisionMap(predictedWorldState.MyUnits, squadIds, squadCenter, MapType.Flat);
                squadBonusMapList.Add(collisionMap.SetWeight(-1));

            }

            squadBonusMapList.Add(StaticMap);

            var resultingMap = BonusMapSum(squadBonusMapList);
            squadBonusMapList.Add(resultingMap);

            BonusMapList.Add(squad.Id, squadBonusMapList);
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

            CheckTileGeneration(squadBonusMapList);

            return sumMap.Trim();
        }


        private static AbsolutePosition GetBestDestination(Dictionary<Point, double> possibleRays)
        {
            var maxRayWin = Double.MinValue;
            var chosenDestination = new Point();
            foreach (var ray in possibleRays)
                if (ray.Value > maxRayWin)
                {
                    maxRayWin = ray.Value;
                    chosenDestination = ray.Key;
                }
            //MyStrategy.Universe.Print($"Chosen destination [{chosenDestination.X:f2}, {chosenDestination.Y:f2}], maxWin is {maxRayWin}");
            return chosenDestination.ToAbsolutePosition();
        }

        private static Dictionary<Point, double> GeneratePossibleRays(Squad squad, BonusMap map)
        {
            const int angleStep = 4;
            double radius = squad.CruisingSpeed * squad.ExpectedTicksToNextUpdate * 1.2;

            var squadCenterUnit = squad.Units.GetCentralUnit();
            var squadCenter = new Point(squadCenterUnit.X, squadCenterUnit.Y);
            var possibleRays = new Dictionary<Point, double>();

            for (int angle = 0; angle < 360; angle += angleStep)
            {
                double angleSI = Math.PI / 180 * angle;
                var possibleDestination = new Point(squadCenter.X + radius * Math.Sin(angleSI),
                    squadCenter.Y + radius * Math.Cos(angleSI));
                if (possibleDestination.X < 0 || possibleDestination.Y < 0 || possibleDestination.X >= 1024 ||
                    possibleDestination.Y >= 1024)
                    continue;
                var cellValuesOnRay = new List<double>();

                for (int i = (int)Math.Min(possibleDestination.X, squadCenter.X);
                    i <= (int)Math.Max(possibleDestination.X, squadCenter.X);
                    i++)
                    for (int j = (int)Math.Min(possibleDestination.Y, squadCenter.Y);
                        j <= (int)Math.Max(possibleDestination.Y, squadCenter.Y);
                        j++)
                    {
                        var worldPoint = new Point(i, j);
                        var isNearRay =
                            Geom.SegmentCircleIntersects(possibleDestination, squadCenter, worldPoint,
                                (double)MapCellWidth / 2);
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
            return possibleRays;
        }


        private static void CheckTileGeneration(List<BonusMap> squadBonusMapList)
        {
#if DEBUG
            foreach (var squadMap in squadBonusMapList)
            {
                var tileList = squadMap.GetTileList();
            }
#endif
        }

        private void ClearBonusMapList(Squad squad)
        {
            foreach (var map in new SortedList<int, List<BonusMap>>(BonusMapList))
                if (map.Key == squad.Id)
                    BonusMapList.Remove(map.Key);
        }

        private void ClearPossibleRaysList(Squad squad)
        {
            foreach (var rays in new SortedList<int, Dictionary<Point, double>>(PossibleRays))
                if (rays.Key == squad.Id)
                    PossibleRays.Remove(rays.Key);
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
        private BonusMap GetGroundCollisionMap(List<Vehicle> allUnits, List<long> squadIds, AbsolutePosition squadCenter, MapType mapType)
        {
            const double affectedRange = 300;
            var unitsForMap = allUnits.Where(
                u => !u.IsAerial &&
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
            var enemyUnitsForMap = enemyUnits.Where(
                u => (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange
            ).ToList();

            var map = new BonusMap(mapType);
            foreach (var unit in enemyUnitsForMap)
                map.AddUnitCalculation(unit, unit.AerialAttackRange * 1, unit.AerialDamage, unit.AerialAttackRange * 2.5);

            return map;
        }

        private BonusMap GetCommonWinMap(List<Vehicle> enemyUnits, Squad squad, MapType mapType)
        {
            const double affectedRange = 1200;
            var squadCenter = squad.Units.GetUnitsCenter();
            var enemyUnitsForMap = enemyUnits.Where(
                u => (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange
            ).ToList();


            var myIsAerialSquad = squad.IsAerial;

            var myAeroDamage = (squad.AirForce) / squad.Units.Count;
            var myGroundDamage = (squad.GroundForce) / squad.Units.Count;
            var myAeroDefence = (squad.AirDefence) / squad.Units.Count;
            var myGroundDefence = (squad.GroundDefence) / squad.Units.Count;


            var map = new BonusMap(mapType);
            foreach (var enemyUnit in enemyUnitsForMap)
            {
                var enemyHealthFactor = enemyUnit.GetUnitHealthIndex();

                var enemyAeroDamage = (enemyUnit.AerialDamage);
                var enemyGroundDamage = (enemyUnit.GroundDamage);
                var enemyAeroDefence = (enemyUnit.AerialDefence) * enemyHealthFactor;
                var enemyGroundDefence = (enemyUnit.GroundDefence) * enemyHealthFactor;

                var enemyDamage = myIsAerialSquad ? enemyAeroDamage : enemyGroundDamage;
                var enemyDefence = myIsAerialSquad ? enemyAeroDefence : enemyGroundDefence;

                var myDamage = enemyUnit.IsAerial ? myAeroDamage : myGroundDamage;
                var myDefence = enemyUnit.IsAerial ? myAeroDefence : myGroundDefence;

                var totalWin = myDamage / enemyDefence - enemyDamage / myDefence;

                if (Double.IsInfinity(totalWin) || Double.IsNaN(totalWin))
                    throw new Exception("Wrong calculated win number.");

                var firstRadius = myIsAerialSquad
                    ? enemyUnit.AerialAttackRange * 1 + squad.Radius
                    : enemyUnit.GroundAttackRange * 1 + squad.Radius;

                var secondRadiusMin = myIsAerialSquad
                    ? enemyUnit.AerialAttackRange * 2.5 + squad.Radius
                    : enemyUnit.GroundAttackRange * 2.5 + squad.Radius;
                var secondRadiusMax = 1200;

                var distanceToUnit = squad.Units.GetCentralUnit().GetDistanceTo(enemyUnit);

                var secondRadius = totalWin > 0 ? secondRadiusMax : secondRadiusMin;

                if (Math.Abs(totalWin) > Double.Epsilon )
                    map.AddUnitCalculation(enemyUnit, firstRadius, totalWin, secondRadiusMin);
            }
            //map.Trim(1);

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
            {
                MyStrategy.Universe.Print("Warning! Zero map distance greater then Max value distance!");
                //throw new Exception("Wrong distance limits.");
                zeroValueDistance = maxValueDistance;
            }
                

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

        public static BonusMap Trim(this BonusMap map, int power = 1)
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
                    map.Table[i, j] = Math.Pow((map.Table[i, j] - minValue) / (maxValue - minValue), power);

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
