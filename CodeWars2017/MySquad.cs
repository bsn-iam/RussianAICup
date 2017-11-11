﻿using System;
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


        public void UpdateState(Universe universe)
        {
            Units = universe.MyUnits.Where(u => u.Groups.Contains(Id)).ToList();
            IsCreated = true;
            IsEmpty = !Units.Any();
#if DEBUG
            //if (!this.IsEmpty && this.IsEnabled)
            //Console.WriteLine("Updating " + this);
#endif
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

        internal void Attack(Queue<IMoveAction> actions, AbsolutePosition position)
        {
            actions.ActionSelectSquad(Id);
            actions.ActionMoveSelectionToPosition(position);
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
        public double AirDefence
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.AerialDefence;
                return force;
            }
        }
        public double GroundDefence
        {
            get
            {
                double force = 0;
                foreach (var unit in Units)
                    force += unit.GroundDefence;
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

        public double AirEnergy => AirForce + AirDefence;
        public double GroundEnergy => GroundForce + GroundDefence;

        public override string ToString()
        {
            return $"Squad [{Id}], IsEnabled [{IsEnabled}], Amount [{Units.Count}], Dispersion [{Dispersion:f2}], AirEnergy [{AirEnergy}], GroundEnergy [{GroundEnergy}]";
        }

        public void Enable() => IsEnabled = true;


        public void Disable() => IsEnabled = false;

    }
}