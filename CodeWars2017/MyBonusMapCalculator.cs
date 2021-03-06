﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class BonusMapCalculator
    {
        public const int MapPointsAmount = 64;
        public const int WorldPointsAmount = 1024;
        public const double FacilitySize = 64; //Universe.Game.FacilityHeight
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

            var squadCenter = squad.SquadCenter;
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
                var commonWinMapAdditive = GetCommonWinMap(predictedWorldState.OppUnits, squad, MapType.Additive);
                foreach (var commonWinMap in commonWinMapAdditive)
                {
                    squadBonusMapList.Add(commonWinMap.Key > 0
                        ? commonWinMap.Value.SetWeight(1.6)
                        : commonWinMap.Value.SetWeight(-1));
                }

                var commonWinMapFlat = GetCommonWinMap(predictedWorldState.OppUnits, squad, MapType.Flat);
                foreach (var commonWinMap in commonWinMapFlat)
                {
                    squadBonusMapList.Add(commonWinMap.Key > 0
                        ? commonWinMap.Value.SetWeight(1.6)
                        : commonWinMap.Value.SetWeight(-1));
                }

                var collisionMap = squad.IsAerial ? 
                    GetAeroCollisionMap(predictedWorldState.MyUnits, squadIds, squadCenter, MapType.Flat) : 
                    GetGroundCollisionMap(predictedWorldState.MyUnits, squadIds, squadCenter, MapType.Flat);
                squadBonusMapList.Add(collisionMap.SetWeight(-1));

                if (squad.OfType(VehicleType.Arrv))
                {
                    var arrvEnemyCollisionMap = GetGroundCollisionMap(predictedWorldState.MyUnits, squadIds, squadCenter, MapType.Flat);
                    squadBonusMapList.Add(arrvEnemyCollisionMap.SetWeight(-1));
                }

                if (!squad.IsAerial)
                {
                    var facilityAttractionFlat = GetFacilityAttractionMap(MapType.Flat);
                    squadBonusMapList.Add(facilityAttractionFlat.SetWeight(1.8 * squad.Units.FirstOrDefault().MaxSpeed));
                    //var facilityAttractionAdditive = GetFacilityAttractionMap(MapType.Additive);
                    //squadBonusMapList.Add(facilityAttractionAdditive.SetWeight(1));
                }

            }

            squadBonusMapList.Add(StaticMap);

            SetRealTable(squadBonusMapList); //debug only

            var resultingMap = BonusMapSum(squadBonusMapList);
            squadBonusMapList.Add(resultingMap);

            BonusMapList.Add(squad.Id, squadBonusMapList);
            return resultingMap;
        }

        private static void SetRealTable(List<BonusMap> squadBonusMapList)
        {
#if DEBUG
            foreach (var map in squadBonusMapList)
                map.RealTable = (double[,])map.Table.Clone();
#endif
        }

        private BonusMap BonusMapSum(List<BonusMap> squadBonusMapList)
        {
            var sumMap = new BonusMap();
            var isInSurvivalMode = IsInSurvivalMode();
            foreach (var map in squadBonusMapList)
            {
                if (isInSurvivalMode && map.Weight > 0)
                    continue;

                map.Trim();

                if (map.IsEmpty)
                    continue;

                for (int i = 0; i < MapPointsAmount; i++)
                for (int j = 0; j < MapPointsAmount; j++)
                    sumMap.Table[i, j] += map.Table[i, j] * map.Weight;
            }

            CheckTileGeneration(squadBonusMapList);

            return sumMap.Trim();
        }

        private bool IsInSurvivalMode()
        {
            var amountOfMyWarriors = Universe.MyUnits.Count(u => u.Type != VehicleType.Arrv);
            var amountOfOppWarriors = Universe.OppUnits.Count(u => u.Type != VehicleType.Arrv);

            if (amountOfMyWarriors < amountOfOppWarriors && amountOfMyWarriors < 10)
                return true;

            return false;
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
            return chosenDestination.ToAbsolutePosition();
        }

        private static Dictionary<Point, double> GeneratePossibleRays(Squad squad, BonusMap map)
        {
            const int angleStep = 6;
            double radius = squad.CruisingSpeed * squad.ExpectedTicksToNextUpdate * 1.2;
            radius = Math.Max(radius, 3 * MapCellWidth);

            var squadCenterUnit = squad.CentralUnit;
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
                            }
                        }
                    }
                if (cellValuesOnRay.Any())
                    possibleRays.Add(possibleDestination, cellValuesOnRay.Average());
                else 
                    MyStrategy.Universe.Print($"Warning! Possible ray is null for squad [{squad.Id}]. It is near border.");

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

        private BonusMap GetFacilityAttractionMap(MapType maptype)
        {
            var facilities = Universe.World.Facilities;
            var map = new BonusMap(maptype);
            foreach (var facility in facilities.Where(f => f.CapturePoints < Universe.Game.MaxFacilityCapturePoints))
            {
                var weight = facility.Type == FacilityType.ControlCenter ? 1.00 : 1.00;
                map.AddFacilityCalculation(facility, FacilitySize / 2, weight, 1000);
            }

            return map;
        }

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

        private Dictionary<int, BonusMap> GetCommonWinMap(List<Vehicle> enemyUnits, Squad squad, MapType mapType)
        {
            const double affectedRange = 1200;
            var squadCenter = squad.SquadCenter;
            var enemyUnitsForMap = enemyUnits.Where(
                u => (u.X - squadCenter.X) < affectedRange &&
                     (u.Y - squadCenter.Y) < affectedRange
            ).ToList();


            var myIsAerialSquad = squad.IsAerial;

            var myAeroDamage = (squad.AirForce) / squad.Units.Count;
            var myGroundDamage = (squad.GroundForce) / squad.Units.Count;
            var myAeroDefence = (squad.AirDefence) / squad.Units.Count;
            var myGroundDefence = (squad.GroundDefence) / squad.Units.Count;


            var mapPositive = new BonusMap(mapType);
            var mapNegative = new BonusMap(mapType);
            foreach (var enemyUnit in enemyUnitsForMap)
            {
                var totalWin = CalculateTotalWin(enemyUnit, myIsAerialSquad, myAeroDamage, myGroundDamage, myAeroDefence, myGroundDefence);

                //totalWin = totalWin * 1.1;

                //if (totalWin < squad.FairValue)
                //    squad.FairValue = totalWin;

                var arrvRadius = enemyUnit.Type == VehicleType.Arrv ? 100 : 0;

                var squadRadius = squad.Radius;
                var firstRadius = myIsAerialSquad
                    ? (enemyUnit.AerialAttackRange + arrvRadius) * 1 + squadRadius
                    : (enemyUnit.GroundAttackRange + arrvRadius) * 1 + squadRadius;

                var secondRadiusAdditive = myIsAerialSquad
                    ? (enemyUnit.AerialAttackRange + arrvRadius) * 5 + squadRadius
                    : (enemyUnit.GroundAttackRange + arrvRadius) * 5 + squadRadius;
                var secondRadiusFlat = 1200;

                //firstRadius = 0;

                var secondRadius = mapType == MapType.Additive ? secondRadiusAdditive : secondRadiusFlat;


                if (totalWin > 0)
                    mapPositive.AddUnitCalculation(enemyUnit, firstRadius, totalWin, secondRadius);
                if (totalWin < 0)
                    mapNegative.AddUnitCalculation(enemyUnit, firstRadius, totalWin, secondRadius);
            }
            mapNegative.Reflect();

            //To fix value to fear of.
            //map.Table[MapPointsAmount - 1, MapPointsAmount - 1] = squad.FairValue;
            //map.Trim(1);

            var result = new Dictionary<int, BonusMap> {{-1, mapNegative}, {1, mapPositive}};

            return result;
        }

        private static double CalculateTotalWin(Vehicle enemyUnit, bool myIsAerialSquad, double myAeroDamage, double myGroundDamage,
            double myAeroDefence, double myGroundDefence)
        {
            var enemyHealthFactor = enemyUnit.GetUnitHealthIndex();
            var enemyDamage = myIsAerialSquad ? enemyUnit.AerialDamage : enemyUnit.GroundDamage;
            var enemyDefence = myIsAerialSquad ? 
                enemyUnit.AerialDefence * enemyHealthFactor : 
                enemyUnit.GroundDefence * enemyHealthFactor;

            var myDamage = enemyUnit.IsAerial ? myAeroDamage : myGroundDamage;
            var myDefence = enemyUnit.IsAerial ? myAeroDefence : myGroundDefence;

            var totalWin = myDamage / enemyDefence - enemyDamage / myDefence;

            //if (enemyDamage.Equals(0) && !myDamage.Equals(0))
            //{
            //    totalWin = totalWin * 10;
            //    MyStrategy.Universe.Print($"Increased win for {enemyUnit.Type}. My damage is {myDamage}");
            //}

            //if (enemyUnit.Type == VehicleType.Arrv)
            //    MyStrategy.Universe.Print("win for arrv");

            if (Double.IsInfinity(totalWin) || Double.IsNaN(totalWin))
                MyStrategy.Universe.Crash("Wrong calculated win number.");
            return totalWin;
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
                map.AddUnitCalculation(unit, scoutVisionRange, unit.AerialDamage + unit.GroundDamage, 800);
            return map;
        }

        private BonusMap GenerateStaticMap()
        {
            var map = new BonusMap().SetWeight(-1.5);
            const double borderWidth = 30;
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


        public static void AddFacilityCalculation(this BonusMap map, Facility facility, double maxValueDistance, double maxValue,
            double zeroValueDistance) =>
            AddUnitCalculation(map, new AbsolutePosition(facility.Left + BonusMapCalculator.FacilitySize/2, facility.Top + BonusMapCalculator.FacilitySize / 2), 
                maxValueDistance, maxValue, zeroValueDistance);

        public static void AddUnitCalculation(this BonusMap map, Vehicle unit, double maxValueDistance, double maxValue,
            double zeroValueDistance) =>
            AddUnitCalculation(map, new AbsolutePosition(unit.X, unit.Y), maxValueDistance, maxValue, zeroValueDistance);

        public static void AddUnitCalculation(BonusMap map, AbsolutePosition unitPosition, double maxValueDistance, double maxValue, double zeroValueDistance)
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
                var distanceSquared = unitPosition.GetSquaredDistanceToPoint(i * BonusMapCalculator.SizeWorldMapKoeff, j * BonusMapCalculator.SizeWorldMapKoeff);

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

        public static IEnumerable<Tile> GetTileList(this BonusMap map)
        {
            map.Trim();
            var tileList = new List<Tile>();
            double tileWidth = BonusMapCalculator.MapCellWidth; //8
            for (int i = 0; i < BonusMapCalculator.MapPointsAmount; i++)
            for (int j = 0; j < BonusMapCalculator.MapPointsAmount; j++)
            {
                var tileCenter = new Point(i * BonusMapCalculator.SizeWorldMapKoeff + tileWidth / 2,
                    j * BonusMapCalculator.SizeWorldMapKoeff + tileWidth / 2);
                tileList.Add(new Tile(tileCenter, tileWidth, map.Table[i, j], map.RealTable[i, j]));
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
