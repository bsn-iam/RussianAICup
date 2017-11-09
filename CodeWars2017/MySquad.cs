using System;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;
using System.Linq;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class Squad
    {
        public double Dispersion { get; }
        public int Id { get; internal set; } = 99999;
        public List<Vehicle> Units { get; internal set; }

        public void UpdateState(Universe universe)
        {
            Units = universe.MyUnits.Where(u => u.Groups.Contains(Id)).ToList();
        }

        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, int id, VehicleType type)
        {
            Id = id;
            actions.ActionSelectVenicleType(type);
            actions.ActionAssignSelectionToSquad(id);
            squadList.Add(this);
        }
        public Squad(Queue<IMoveAction> actions, List<Squad> squadList, int id, Range range)
        {
            //range=position.GetRange(radius)
            Id = id;
            actions.ActionSelectInRange(range);
            actions.ActionAssignSelectionToSquad(id);
            squadList.Add(this);
        }

        internal void Attack(Queue<IMoveAction> actions, AbsolutePosition position)
        {
            actions.ActionSelectSquad(Id);
            actions.ActionMoveSelectionToPosition(position);
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
    }
}