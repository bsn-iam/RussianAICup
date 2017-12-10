using System;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class Squad
    {

        #region Properties

        public int Id { get; }

        public List<Vehicle> Units { get; internal set; } = new List<Vehicle>();
        public bool IsCreated { get; internal set; }
        public bool IsEnabled { get; internal set; }
        public bool IsEmpty { get; internal set; }
        public bool IsAbstract { get; }

        public double StartDispersionSquared { get; internal set; } = Double.MaxValue;
        //public double StartGroundEnergy { get; internal set; } = 0;
        //public double StartAirEnergy { get; internal set; } = 0;

        public int ScalingTimeDelay { get; internal set; }
        public bool IsWaitingForScaling { get; internal set; }
        public bool IsScout { get; internal set; } = false;
        public bool IsCwDirection { get; internal set; } = true;

        public int PreviousCallTick { get; internal set; } = 0;
        public int LastCallTick { get; internal set; } = 0;

        public double CruisingSpeed { get; internal set; }
        public int LastScaleTick { get; internal set; }
        public bool ScaleRotateFlag { get; internal set; }
        public double DispersionSquared { get; internal set; }


        public int ExpectedTicksToNextUpdate
        {
            get
            {
                var duration = LastCallTick - PreviousCallTick;
                if (duration > 1000 && LastCallTick !=0)
                    MyStrategy.Universe.Print("Huge time from previous step.");

                duration = Math.Min(duration, 500);
                duration = Math.Max(4, duration);

                return duration;
            }
        }

        #endregion

        #region Constructors

        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, int id, VehicleType type)
        {
            Id = id;
            actions.ActionSelectVenicleType(type);
            actions.ActionAssignSelectionToSquad(id);
            squadList.Add(this);
            IsCreated = false;
            IsEnabled = true;
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }
        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, IdGenerator idGenerator)
        {
            Id = idGenerator.New;
            actions.ActionAssignSelectionToSquad(Id);
            squadList.Add(this);
            IsCreated = false;
            IsEnabled = true;
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }


        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, int id, Range2 range2)
        {
            Id = id;
            actions.ActionSelectInRange(range2);
            actions.ActionAssignSelectionToSquad(id);
            squadList.Add(this);
            IsCreated = false;
            IsEnabled = true;
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        public Squad(List<Vehicle> units, bool isAbstract = true)
        {
            if (!isAbstract)
                throw new NotImplementedException();

            Id = new Random().Next(0, 100);
            Units = units;
            CalculateProperties();
            IsCreated = true;
            IsEnabled = false;
            IsAbstract = isAbstract;
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        public Squad(Squad squadAlfa, Squad squadDelta, bool isAbstract = true)
        {
            if (!isAbstract)
                throw new NotImplementedException();

            Id = new Random().Next(0, 100);

            squadAlfa.Units.ForEach(u => Units.Add(u));
            squadDelta.Units.ForEach(u => Units.Add(u));

            CalculateProperties();
            IsCreated = true;
            IsEnabled = false;
            IsAbstract = isAbstract;
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        //public Squad(int id)
        //{
        //    Id = id;
        //    IsCreated = false;
        //    IsEnabled = true;
        //}

        #endregion

        #region EnergyCalculations

        //public double DispersionSquared => Units.GetUnitsSquaredDispersionValue();

        public double AirDefence
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.AerialDefence * unit.GetUnitHealthIndex();
                return force;
            }
        }
        public double GroundDefence
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.GroundDefence * unit.GetUnitHealthIndex();
                return force;
            }
        }

        public double AirForce
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.AerialDamage;
                return force;
            }
        }
        public double GroundForce
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.GroundDamage;
                return force;
            }
        }

        public double Health
        {
            get
            {
                var healthIndexList = new List<double>();
                foreach (var unit in Units)
                    healthIndexList.Add(unit.GetUnitHealthIndex());
                return healthIndexList.Average();
            }
        }

        //public double AirEnergy => (AirForce + AirDefence) / DispersionSquared;
        //public double AirEnergyRelative => AirEnergy / StartAirEnergy;
        //public double GroundEnergyRelative => GroundEnergy / StartGroundEnergy;
        //public double GroundEnergy => (GroundForce + GroundDefence) / DispersionSquared;

        //public double Energy => GroundEnergy + AirEnergy;

        #endregion

        #region Actions

        internal void DoMove(Queue<IMoveAction> actions, AbsolutePosition position)
        {
            if (NukeMarkerCounter == 0)
            {
                actions.ActionSelectSquad(Id);
                actions.ActionMoveSelectionToPosition(position);
            }
            else
            {
                MyStrategy.Universe.Print($"Squad {(Squads)Id} is locked due to Nuke marker.");
            }
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        internal void DoMove(Queue<IMoveAction> actions, AbsolutePosition position, double speed)
        {
            if (NukeMarkerCounter == 0)
            {
                actions.ActionSelectSquad(Id);
                actions.ActionMoveSelectionToPosition(position, speed);
                MyStrategy.Universe.Print($"Squad {(Squads)Id}  - move requiested.");
            }
            else
            {
                MyStrategy.Universe.Print($"Squad {(Squads)Id} is locked due to Nuke marker.");
            }
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        internal void DoScaleJerk(Queue<IMoveAction> actions, List<DeferredAction> deferredActionsList, double factor, AbsolutePosition nukeCenter, int duration, int deferredTickIndex)
        {
            actions.ActionScaleSquadToPosition(this, factor, nukeCenter, duration);

            var deferredActions = new Queue<IMoveAction>();
            deferredActions.ActionScaleSquadToPosition(this, 0.1, nukeCenter, (int) 1.5 * duration);

            // TODO if queue is log, there is not time for movement, planned combining time is behind.
            foreach (var action in deferredActions)
            {
                //TODO impossible to set exact required time for deferred action
                deferredActionsList.Add(new DeferredAction(action, deferredTickIndex));
            }
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        internal void DoStop(Queue<IMoveAction> actions)
        {
            actions.ActionSelectSquad(Id);
            actions.ActionMoveSelectionToPosition(SquadCenter);

            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        internal void DoFollow(Queue<IMoveAction> actions, Squad thisSquad, Squad targetSquad)
        {
            actions.ActionSelectSquad(thisSquad.Id);
            actions.ActionMoveSelectionToPosition(targetSquad.SquadCenter);
            UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        internal void DoRotateJerk(Queue<IMoveAction> actions)
        {
            actions.ActionSelectSquad(Id);

            IsCwDirection = !IsCwDirection;
            var angleDelta = Math.PI / 4;
            var angleChange = IsCwDirection ? angleDelta : -angleDelta;

            actions.ActionRotateSelection(SquadCenter, angleChange);
        }

        public void DoZoomIn(Queue<IMoveAction> actions)
        {
            if (ScaleRotateFlag)
                actions.ActionScaleSquadToPosition(this, 0.1, SquadCenter, 60);
            else
                DoRotateJerk(actions);

            ScaleRotateFlag = !ScaleRotateFlag;
            LastScaleTick = MyStrategy.Universe.World.TickIndex;
            //UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
        }

        internal void SetNukeMarkerCount(int ticksLeft)
        {
            NukeMarkerCounter = ticksLeft;
        }

        public void UpdateLastCallTime(int thisCallTick)
        {
            PreviousCallTick = LastCallTick;
            LastCallTick = thisCallTick;
        }


        #endregion

        public void UpdateState(Universe universe)
        {
            Units = universe.MyUnits.Where(u => u.Groups.Contains(Id)).ToList();
            var firstUpdate = false;

            if (!IsCreated)
            {
                IsCreated = Units.Any();
                if (IsCreated)
                    firstUpdate = true;
            }

            if (ScalingTimeDelay > 0 && !IsWaitingForScaling)
                --ScalingTimeDelay;

            IsEmpty = !Units.Any();

            if (IsCreated)
                CalculateProperties();

            if (firstUpdate)
            {
                StartDispersionSquared = DispersionSquared;
                ScalingTimeDelay = 0;
                IsWaitingForScaling = false;
            }
        }

        private void CalculateProperties()
        {
            SquaredDispersionList = Units.GetUnitsSquaredDispersionList();
            CentralUnit = GetSquadCentralUnit(); // order metters. Depends on the SquaredDispersionList

            if (CentralUnit == null) SquadCenter = new AbsolutePosition();
            else SquadCenter = new AbsolutePosition(CentralUnit.X, CentralUnit.Y);

            CruisingSpeed = CalculateCruisingSpeed();
            DispersionSquared = GetUnitsSquaredDispersionValue(); // order metters. Depends on the SquaredDispersionList
            DispersionRelative = DispersionSquared / StartDispersionSquared;
        }

        public Dictionary<long, double> SquaredDispersionList { get; private set; }
        public Vehicle CentralUnit { get; private set; }
        public AbsolutePosition SquadCenter { get; private set; }

        public Vehicle GetSquadCentralUnit()
        {
            if (!Units.Any())
                return null;

            if (Units.Count == 1)
                return Units[0];

            var dispersionPerUnit = SquaredDispersionList;

            //get the ID of less distant.
            var minSquaredDistance = Double.MaxValue;
            long centerUnitId = 0;
            foreach (var pair in dispersionPerUnit)
            {
                if (pair.Value > 0.01 && pair.Value < minSquaredDistance)
                {
                    minSquaredDistance = pair.Value;
                    centerUnitId = pair.Key;
                }
            }

            //return position of less distant unit
            var centerUnit = Units.First(u => u.Id.Equals(centerUnitId));
            return centerUnit;
        }

        public double GetUnitsSquaredDispersionValue()
        {
            if (!Units.Any())
                return Double.MaxValue;
            var dispersionPerUnit = SquaredDispersionList;
            double dispersionSum = 0;

            foreach (var dispersion in dispersionPerUnit)
                dispersionSum += dispersion.Value;
            return dispersionSum / Units.Count;
        }

        public double CalculateCruisingSpeed()
        {
             var speed = Double.MaxValue;
             foreach (var unit in Units)
                 if (speed > unit.MaxSpeed)
                 speed = unit.MaxSpeed;
             return speed;
        }

        public int NukeMarkerCounter { get; set; }

        public double Radius
        {
            get
            {
                var centerDistanceList = new List<double>();
                foreach (var unit in Units)
                    centerDistanceList.Add(unit.GetDistanceTo(SquadCenter.X, SquadCenter.Y));
                return centerDistanceList.Average();
            }
        }

        public bool IsAerial
        {
            get
            {
                if (!Units.Any())
                    return true;

                return Units.FirstOrDefault().IsAerial;
                //var totalCount = Units.Count;
                //var aerialCount = Units.Count(u => u.IsAerial);
                //var groundCount = totalCount - aerialCount;
                //
                //return aerialCount > groundCount;
            }
        }

        public double DispersionRelative { get; private set; }

        public bool OfType(VehicleType type)
        {
            if (!Units.Any())
                return false;
            return Units.FirstOrDefault().Type == type;
            //var totalCount = Units.Count;
            //var requestedCount = Units.Count(u => u.Type == type);
            //var othersCount = totalCount - requestedCount;
            //
            //return requestedCount > othersCount;
        }

        //public double FairValue { get; internal set; } = 0;


        internal double GetNukeDamage(AbsolutePosition targetPoint, double range)
        {
            double damage = 0;
            //var visabilityKoeff = 0.8;

            foreach (var unit in Units)
            {
                var distanceFromNuceCenter = unit.GetDistanceTo(targetPoint.X, targetPoint.Y);
                var damageKoeff = (range - distanceFromNuceCenter) / range;
                var healthKoeff = 1 / unit.GetUnitHealthIndex();
                var unitForce = unit.AerialDamage + unit.AerialDefence + unit.GroundDamage + unit.GroundDefence;

                //damage += unitForce * damageKoeff * healthKoeff;
                damage += unitForce * damageKoeff;
            }
            return damage;
        }

        public void Enable() => IsEnabled = true;

        public void Disable() => IsEnabled = false;

        public override string ToString()
        {
            return $"Squad [{(Squads) Id}], IsEnabled [{IsEnabled}], Amount [{Units.Count}], " +
                   $"DispersionSquared [{DispersionSquared:f2}, {DispersionRelative:f2}], ";
        }

    }
}