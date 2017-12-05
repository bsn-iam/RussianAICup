using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public static class MyExtensions
    {

        #region UniverseExtensions
        public static AbsolutePosition GetSquadCenter(this Universe universe, int squadId)
        {
            var squad = MyStrategy.SquadCalculator.SquadList.GetSquadById(squadId);

            if (squad.IsEmpty)
            {
                universe.Print("Warning! Selection contains 0 units.");
                return new AbsolutePosition(0, 0);
            }

            return squad.SquadCenter;
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
            var selectedUnits = GetSelectedUnits(universe);
            //universe.Print($"Moving {squadUnits.Count} units");

            if (selectedUnits.Count == 0)
            {
                universe.Print("Warning! Selection contains 0 units.");
                return new AbsolutePosition(0, 0);
            }

            return selectedUnits.GetUnitsCenter();
        }

        public static List<Vehicle> GetSelectedUnits(this Universe universe)
        {
            var selectedUnits = new List<Vehicle>();

            foreach (var unit in universe.MyUnits)
                if (unit.IsSelected)
                    selectedUnits.Add(unit);
            return selectedUnits;
        }

        public static double GetSpeedForSelection(this Universe universe)
        {
            var speed = double.MaxValue;
            var units = universe.MyUnits;
            foreach (var unit in units)
                if (speed > unit.MaxSpeed)
                    speed = unit.MaxSpeed;

            return speed;
        }

        public static int SelectionCount(this Universe universe)
        {
            int counter = 0;
            foreach (var unit in universe.MyUnits)
                if (unit.IsSelected) counter += 1;

            if (counter == 0)
                universe.Print("Warning! Selection contains 0 units.");

            return counter;
        }

        public static void Print(this Universe universe, string message)
        {
#if DEBUG
            Console.WriteLine(universe.World.TickIndex + ". " + message.Replace("Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.", ""));
#endif
        }

        #endregion


        #region PositionExtensions

        public static Vehicle GetCentralUnit(this List<Vehicle> units)
        {
            if (!units.Any())
                return null;

            if (units.Count == 1)
                return units[0];

            var dispersionPerUnit = units.GetUnitsDispersionList();

            //get the ID of less distant.
            var minDistance = Double.MaxValue;
            long centerUnitId = 0;
            foreach (var pair in dispersionPerUnit)
            {
                if (pair.Value > 0.01 && pair.Value < minDistance)
                {
                    minDistance = pair.Value;
                    centerUnitId = pair.Key;
                }
            }

            //return position of less distant unit
            var centerUnit = units.First(u => u.Id.Equals(centerUnitId));
            return centerUnit;
        }

        public static AbsolutePosition GetUnitsCenter(this List<Vehicle> units)
        {
            var position = units.GetCentralUnit();
            return position == null ? new AbsolutePosition(0, 0) : new AbsolutePosition(position.X, position.Y);
        }

        public static AbsolutePosition GetPositionOfNearestTarget(this List<Vehicle> units, List<Vehicle> targetUnits)
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
                        position = new AbsolutePosition(target.X, target.Y);
                    }
                }
            return position;
        }


        #endregion


        #region VehicleExtensions

        public static double GetUnitHealthIndex(this Vehicle unit)
        {
            return (double)unit.Durability / unit.MaxDurability;
        }

        public static Vehicle GetMostDistantAmong(this Vehicle me, List<Vehicle> targets)
        {
            //carefully with 0 targets! We can get nuke for own position!

            var distance = 0;
            var mostDistant = me;
            foreach (var target in targets)
            {
                var currentDistance = me.GetSquaredDistanceTo(target);
                if ( currentDistance > distance)
                    mostDistant = target;
            }
            return mostDistant;
        }


        public static double GetPotentialNuclearWin(this Vehicle targetUnit, Universe universe, double range)
        {
            var myGuys = new List<Vehicle>();
            var enemyGuys = new List<Vehicle>();
            var squaredRange = range * range;

            var targetPoint = new AbsolutePosition(targetUnit.X, targetUnit.Y);

            //var predictedState = MyStrategy.Predictor.GetStateOnTick(universe.World.TickIndex + 30);
            //var enemyUnits = predictedState.OppUnits.Where(u => u.Type != VehicleType.Arrv);
            //var myUnits = predictedState.MyUnits.Where(u => u.Type != VehicleType.Arrv);

            var enemyUnits = universe.OppUnits.Where(u => u.Type != VehicleType.Arrv);
            var myUnits = universe.MyUnits.Where(u => u.Type != VehicleType.Arrv);

            foreach (var enemyGuy in enemyUnits)
            {
                var distanceSquaredFromNuceCenter = targetUnit.GetSquaredDistanceTo(enemyGuy);
                if (distanceSquaredFromNuceCenter < squaredRange)
                    enemyGuys.Add(enemyGuy);
            }

            foreach (var myGuy in myUnits)
            {
                var distanceSquaredFromNuceCenter = targetUnit.GetSquaredDistanceTo(myGuy);
                if (distanceSquaredFromNuceCenter < squaredRange && targetUnit != myGuy)
                    myGuys.Add(myGuy);
            }

            var totalEnergyWin = new Squad(enemyGuys).GetNukeDamage(targetPoint, range) - new Squad(myGuys).GetNukeDamage(targetPoint, range);

            return totalEnergyWin;
        }


        public static bool DoISeeThisUnit(this Vehicle myUnit, Vehicle targetUnit) => 
            DoISeeThisPoint(myUnit, new AbsolutePosition(targetUnit.X, targetUnit.Y));

        public static bool DoISeeThisPoint(this Vehicle myUnit, AbsolutePosition point)
        {
            var squaredRange = myUnit.VisionRange * myUnit.VisionRange * 0.6;
            return myUnit.GetSquaredDistanceTo(point.X, point.Y) < squaredRange;
        }
        #endregion


        #region VehicleListExtension

        public static List<Vehicle> GetClosestUnits(this List<Vehicle> initialList, double persentageToRemove)
        {
            List<Vehicle> shrinkedUnits = new List<Vehicle>(initialList);
            while (shrinkedUnits.Count > initialList.Count * (1 - persentageToRemove / 100))
                shrinkedUnits.Remove(shrinkedUnits.GetMostDistantUnit());

            return shrinkedUnits;
        }

        public static List<Vehicle> GetCombinedList(this List<Vehicle> units1, List<Vehicle> units2)
        {
            var combinedList = new List<Vehicle>();
            units1.ForEach(u => combinedList.Add(u));
            units2.ForEach(u => combinedList.Add(u));

            return combinedList;
        }

        public static double GetUnitsDispersionValue(this List<Vehicle> units)
        {
            if (!units.Any())
                return Double.MaxValue;
            var dispersionPerUnit = units.GetUnitsDispersionList();
            double dispersionSum = 0;

            foreach (var dispersion in dispersionPerUnit)
                dispersionSum += dispersion.Value;
            return dispersionSum / units.Count;
        }

        public static Vehicle GetMostDistantUnit(this List<Vehicle> candidateList)
        {
            var scuadCenterPosition = candidateList.GetUnitsCenter();
            var candidate = candidateList.FirstOrDefault();
            double measure = 0;
            foreach (var unit in candidateList)
            {
                var distance = unit.GetSquaredDistanceTo(scuadCenterPosition.X, scuadCenterPosition.Y);
                if (distance > measure)
                {
                    candidate = unit;
                    measure = distance;
                }
            }
            return candidate;
        }

        public static Dictionary<Vehicle, List<Vehicle>> GetScoutObservation(this List<Vehicle> myUnits, List<Vehicle> targetUnits)
        {
            var observationList = new Dictionary<Vehicle, List<Vehicle>>();
            foreach (var myUnit in myUnits)
            {
                var foundTargetUnits = new List<Vehicle>();

                foreach (var targetUnit in targetUnits)
                    if (myUnit.DoISeeThisUnit(targetUnit))
                        foundTargetUnits.Add(targetUnit);

                if (foundTargetUnits.Any())
                    observationList.Add(myUnit, foundTargetUnits);

            }
            return observationList;
        }


        public static Squad GetSquadById(this List<Squad> squadList, int squadId) =>
            squadList.FirstOrDefault(s => s.Id.Equals(squadId));


        public static Dictionary<long, double> GetUnitsDispersionList(this List<Vehicle> units)
        {
            Dictionary<long, double> dispersionPerUnit = new Dictionary<long, double>();
            if (units.Count == 1)
            {
                dispersionPerUnit.Add(units[0].Id, 1);
                return dispersionPerUnit;
            }

            //get sum of distance to friends
            foreach (var u1 in units)
            {
                double u1Distance = 0;
                foreach (var u2 in units)
                    u1Distance += GetDistanceBetweenUnits(u1, u2);
                if (!dispersionPerUnit.ContainsKey(u1.Id))
                    dispersionPerUnit.Add(u1.Id, u1Distance / units.Count);
            }
            //greater value - more distant unit

            return dispersionPerUnit;
        }

        #endregion


        #region SquadListExtensions

        public static Squad GetSquadByUnit(this List<Squad> squadList, Vehicle unit)
        {
            foreach (var groupId in unit.Groups)
                foreach (var squad in squadList)
                    if (squad.Id == groupId && squad.IsEnabled)
                        return squad;
            return null;
        }

        public static int GetPeriodPerMeeting(this List<Squad> squadList, int squadAlfaId, int squadDeltaId)
        {
            var meetingPoint = squadList.GetMeetingPoint(squadAlfaId, squadDeltaId);
            var squadAlfa = squadList.GetSquadById(squadAlfaId);
            var distance = meetingPoint.GetDistanceToPoint(squadAlfa.SquadCenter);
            return (int)Math.Round(distance / squadAlfa.CruisingSpeed);
        }


        public static AbsolutePosition GetMeetingPoint(this List<Squad> squadList, int squadAlfaId, int squadDeltaId)
        {
            var squadAlfa = squadList.GetSquadById(squadAlfaId);
            var squadAlfaPosition = squadAlfa.SquadCenter;

            var squadDelta = squadList.GetSquadById(squadDeltaId);
            var squadDeltaPosition = squadDelta.SquadCenter;

            var dX = squadDeltaPosition.X - squadAlfaPosition.X;
            var dY = squadDeltaPosition.Y - squadAlfaPosition.Y;
            var speedKoeff = squadAlfa.CruisingSpeed / squadDelta.CruisingSpeed;

            return new AbsolutePosition(squadAlfaPosition.X + dX * speedKoeff, squadAlfaPosition.Y + dY * speedKoeff);
        }


        #endregion


        #region ActionListExtensions


        public static Squad ActionCreateNewSquad(this Queue<IMoveAction> actions, List<Squad> squadList, int newSquadId, VehicleType type) =>
            new Squad(actions, squadList, newSquadId, type);

        public static Squad ActionCreateNewSquadAlreadySelected(this Queue<IMoveAction> actions, List<Squad> squadList, IdGenerator idGenerator) =>
            new Squad(actions, squadList, idGenerator);

        public static List<Squad> GetIteratorSquadListActive(this List<Squad> squadList) =>
            new List<Squad>(squadList
                .Where(s => s.IsEnabled)
                .Where(s => !s.IsEmpty)
                .Where(s => s.IsCreated)
                .Where(s => s.ScalingTimeDelay.Equals(0)));

        public static void ActionScaleSquadToPosition(this Queue<IMoveAction> actions, Squad squad, double factor, AbsolutePosition position, int duration)
        {
            actions.ActionSelectSquad(squad.Id);
            actions.Enqueue(new ActionScaleSelectedSquadToPosition(squad, factor, position, duration));
            squad.IsWaitingForScaling = true;
            //squad.UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        public static void ActionMoveAndCombine(this Queue<IMoveAction> actions, List<Squad> squadList, int squadAlfaId, int squadDeltaId,
            IdGenerator squadIdGenerator, List<DeferredAction> deferredActionsList, int tickIndex, int duration)
        {
            actions.ActionMoveToOnePoint(squadList, squadAlfaId, squadDeltaId);

            var deferredActions = new Queue<IMoveAction>();
            deferredActions.ActionCombineSquads(squadList, squadAlfaId, squadDeltaId, squadIdGenerator);

            // TODO if queue is log, there is not time for movement, planned combining time is behind.
            foreach (var action in deferredActions)
            {
                //TODO impossible to set exact required time for deferred action
                //deferredActionsList.Add(new DeferredAction(action, tickIndex + squadList.GetPeriodPerMeeting(squadId, squadDeltaId)));
                deferredActionsList.Add(new DeferredAction(action, tickIndex + duration));
            }
        }

        #endregion

        public static AbsolutePosition ToAbsolutePosition(this Point point) => new AbsolutePosition(point.X, point.Y);

        public static double GetDistanceBetweenUnits(Vehicle u1, Vehicle u2) =>
            Math.Sqrt((u1.X - u2.X) * (u1.X - u2.X) + (u1.Y - u2.Y) * (u1.Y - u2.Y));

        public static bool Equals(double x, double y)
        {
            return Math.Abs(x - y) < Double.Epsilon;
        }

        public static Range2 GetRange(this AbsolutePosition position, double width, double height)
        {
            var XMin = position.X - width / 2;
            var XMAx = position.X - width / 2;
            var YMin = position.Y - height / 2;
            var YMax = position.Y - height / 2;

            return new Range2(XMin, XMAx, YMin, YMax);
        }

        public static Range2 GetRange(this AbsolutePosition position, double radius)
        {
            var XMin = position.X - radius;
            var XMAx = position.X - radius;
            var YMin = position.Y - radius;
            var YMax = position.Y - radius;

            return new Range2(XMin, XMAx, YMin, YMax);
        }

        public static bool IsInRange(this double value, double minLimit, double maxLimit) =>
            Geom.Between(minLimit, maxLimit, value);
        public static bool IsInRange(this double value, Range range) =>
            Geom.Between(range.MinLimit, range.MaxLimit, value);
        public static bool IsInRange2(this AbsolutePosition position, Range2 range) =>
            Geom.Between(range.XMin, range.XMax, position.X) && Geom.Between(range.YMin, range.YMax, position.Y);

        public static IEnumerable<Vehicle> GetEnumeration(this List<Vehicle> units)
        {
            return new List<Vehicle>(units);
        }

    }
}
