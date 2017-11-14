using System;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;
using System.Linq;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class Squad
    {
        public double Dispersion => Units.GetUnitsDispersionValue();

        public int Id { get; }

        public List<Vehicle> Units { get; internal set; } = new List<Vehicle>();
        public bool IsCreated { get; internal set; }
        public bool IsEnabled { get; internal set; }
        public bool IsEmpty { get; internal set; }
        public bool IsAbstract { get; }

        public double StartDispersion { get; internal set; } = Double.MaxValue;
        public double StartGroundEnergy { get; internal set; } = 0;
        public double StartAirEnergy { get; internal set; } = 0;

        public int ScalingTimeDelay { get; internal set; }
        public bool IsWaitingForScaling { get; internal set; }
        public bool IsScout { get; internal set; } = false;

        public void UpdateState(Universe universe)
        {
            Units = universe.MyUnits.Where(u => u.Groups.Contains(Id)).ToList();

            if (!IsCreated)
            {
                IsCreated = Units.Any();
                if (IsCreated)
                {
                    StartDispersion = Dispersion;
                    StartAirEnergy = AirEnergy;
                    StartGroundEnergy = GroundEnergy;
                    ScalingTimeDelay = 0;
                    IsWaitingForScaling = false;
                }
            }

            if (ScalingTimeDelay > 0 && !IsWaitingForScaling )
                 --ScalingTimeDelay;

            IsEmpty = !Units.Any();
            //if (!this.IsEmpty && this.IsEnabled)
            //universe.Print("Updating " + this);
        }


        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, int id, VehicleType type)
        {
            Id = id;
            actions.ActionSelectVenicleType(type);
            actions.ActionAssignSelectionToSquad(id);
            squadList.Add(this);
            IsCreated = false;
            IsEnabled = true;
        }
        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, int id)
        {
            Id = id;
            actions.ActionAssignSelectionToSquad(id);
            squadList.Add(this);
            IsCreated = false;
            IsEnabled = true;
        }


        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, int id, Range range)
        {
            //range=position.GetRange(radius)
            Id = id;
            actions.ActionSelectInRange(range);
            actions.ActionAssignSelectionToSquad(id);
            squadList.Add(this);
            IsCreated = false;
            IsEnabled = true;
        }

        public Squad(List<Vehicle> units, bool isAbstract = true)
        {
            if (!isAbstract)
                throw new NotImplementedException();

            Id = new Random().Next(0, 100);
            Units = units;
            IsCreated = true;
            IsEnabled = false;
            IsAbstract = isAbstract;
        }

        public Squad(Squad squadAlfa, Squad squadDelta, bool isAbstract = true)
        {
            if (!isAbstract)
                throw new NotImplementedException();

            Id = new Random().Next(0, 100);

            squadAlfa.Units.ForEach(u => Units.Add(u));
            squadDelta.Units.ForEach(u => Units.Add(u));

            IsCreated = true;
            IsEnabled = false;
            IsAbstract = isAbstract;
        }

        //public Squad(int id)
        //{
        //    Id = id;
        //    IsCreated = false;
        //    IsEnabled = true;
        //}

        internal void DoAttack(Queue<IMoveAction> actions, AbsolutePosition position)
        {
            actions.ActionSelectSquad(Id);
            actions.ActionMoveSelectionToPosition(position);
        }

        internal void DoFollow(Queue<IMoveAction> actions, Squad thisSquad, Squad targetSquad)
        {
            actions.ActionSelectSquad(thisSquad.Id);
            actions.ActionMoveSelectionToPosition(targetSquad.Units.GetUnitsCenter());
        }

        public double CruisingSpeed
        {
            get
            {
                double speed = double.MaxValue;
                foreach (var unit in Units)
                    if (speed > unit.MaxSpeed)
                    speed =unit.MaxSpeed;
                return speed;
            }
        }

        private double AirDefence
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.AerialDefence;
                return force;
            }
        }
        private double GroundDefence
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.GroundDefence;
                return force;
            }
        }

        private double AirForce
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.AerialDamage;
                return force;
            }
        }
        private double GroundForce
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.GroundDamage;
                return force;
            }
        }

        public double AirEnergy => (AirForce + AirDefence) / Dispersion;
        public double AirEnergyRelative => AirEnergy / StartAirEnergy;
        public double GroundEnergyRelative => GroundEnergy / StartGroundEnergy;
        public double DispersionRelative => Dispersion / StartDispersion;
        public double GroundEnergy => (GroundForce + GroundDefence) / Dispersion;

        public double Energy => GroundEnergy + AirEnergy;

        public override string ToString()
        {
            return $"Squad [{(Squads)Id}], IsEnabled [{IsEnabled}], Amount [{Units.Count}], " +
                   $"Dispersion [{Dispersion:f2}, {DispersionRelative:f2}], " +
                   $"AirEnergy [{AirEnergy:f2}, {AirEnergyRelative:f2}], " +
                   $"GroundEnergy [{GroundEnergy:f2}, {GroundEnergyRelative:f2}]";
        }

        public void Enable() => IsEnabled = true;


        public void Disable() => IsEnabled = false;

    }
}