using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class SquadCalculator
    {
        public List<Squad> SquadList { get; internal set; } =new List<Squad>();
        public Queue<IMoveAction> ActionList { get; } = new Queue<IMoveAction>();
        public Universe Universe;
        private const int ActionListLength = 20;


        internal void RunTick(Universe universe)
        {
            Universe = universe;
            if (universe.World.TickIndex == 0) StartActionList(Universe);

            foreach (var squad in SquadList)
                squad.UpdateState(Universe);

                GenerateActions();
        }


        private void GenerateActions()
        {
            if (ActionList.Count < ActionListLength)
                foreach (var squad in SquadList)
                {
                    //check state and force, run actions
                    if (squad.Units.Count != 0)
                        squad.Attack(ActionList, Universe.MapCenter);
                }

            if (ActionList.Count < 6)
            {
                //ActionList.ActionSelectSquad((int)Squads.All);
                ActionList.ActionSelectSquad((int)Squads.Helicopters);
                ActionList.ActionMoveSelectionToPosition(Universe.MyUnits.GetNearestPositionToTarget(Universe.OppUnits.Where(u => u.Type == VehicleType.Arrv).ToList()));
                return;
            }


            //if (ActionList.Count < 6 && Universe.World.TickCount > 2000)
            //{
            //    //ActionList.ActionSelectSquad((int)Squads.All);
            //    ActionList.ActionSelectAll();
            //    ActionList.ActionMoveSelectionToPosition(Universe.OppUnits.GetUnitsCenter());
            //    return;
            //}
        }

        private int getUniqueSquadId()
        {
            var newId=0;
            foreach (var squad in SquadList)
                if (squad.Id >= newId)
                    newId = squad.Id + 1;

            return newId;
        }

        public void StartActionList(Universe universe)
        {
            //do not use Universe here. Link will be changed.
            new Squad(ActionList, SquadList, (int)Squads.All, new Range());
            new Squad(ActionList, SquadList, (int)Squads.Fighters, VehicleType.Fighter);
            new Squad(ActionList, SquadList, (int)Squads.Helicopters, VehicleType.Helicopter);
            new Squad(ActionList, SquadList, (int)Squads.Arrvs, VehicleType.Arrv);
            new Squad(ActionList, SquadList, (int)Squads.Ifvs, VehicleType.Ifv);
            new Squad(ActionList, SquadList, (int)Squads.Tanks, VehicleType.Tank);



            //new Squad(ActionList, SquadList, getUniqueSquadId(), new Range());
            //
            //ActionList.ActionSelectAll();
            //ActionList.ActionAssignSelectionToSquad((int)Squads.All);
            ////ActionList.ActionMoveSelectionToPosition(Universe.MapCenter);
            //
            //ActionList.ActionSelectVenicleType(VehicleType.Fighter);
            //ActionList.ActionAssignSelectionToSquad((int)Squads.Fighters);
            //
            //ActionList.ActionSelectVenicleType(VehicleType.Helicopter);
            //ActionList.ActionAssignSelectionToSquad((int)Squads.Helicopters);
            ////ActionList.ActionMoveSelectionToPosition(Universe.MapConerRightUp);
            //
            //ActionList.ActionSelectAll();

        }
    }
}