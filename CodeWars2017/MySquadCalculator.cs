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
        private const int ActionListLength = 6;


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
                foreach (var squad in SquadList.Where(s=>s.IsEnabled).Where(s=>!s.IsEmpty))
                {
                    //check state and force, run actions
                    squad.Attack(ActionList, Universe.MyUnits.GetNearestPositionToTarget(Universe.OppUnits.Where(u => u.Type == VehicleType.Tank).ToList()));
                }

            //if (ActionList.Count < 6)
            //{
            //    //ActionList.ActionSelectSquad((int)Squads.All);
            //    ActionList.ActionSelectSquad((int)Squads.Helicopters);
            //    ActionList.ActionMoveSelectionToPosition(Universe.MyUnits.GetNearestPositionToTarget(Universe.OppUnits.Where(u => u.Type == VehicleType.Arrv).ToList()));
            //    return;
            //}

            if (Universe.World.TickIndex % 600  == 0)
                SquadList.ForEach(s => Console.WriteLine(s.ToString()));


            //if (ActionList.Count < 6 && Universe.World.TickCount > 2000)
            //{
            //    //ActionList.ActionSelectSquad((int)Squads.All);
            //    ActionList.ActionSelectAll();
            //    ActionList.ActionMoveSelectionToPosition(Universe.OppUnits.GetUnitsCenter());
            //    return;
            //}
        }


        private int generateUniqueSquadId()
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
            //new Squad(ActionList, SquadList, (int)Squads.All, new Range());

            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Fighters, VehicleType.Fighter);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Arrvs, VehicleType.Arrv);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Ifvs, VehicleType.Ifv);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Helicopters, VehicleType.Helicopter);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Tanks, VehicleType.Tank);

            ActionList.ActionCombineSquads(SquadList, (int) Squads.Tanks, (int) Squads.Fighters, generateUniqueSquadId());

            //new Squad(ActionList, SquadList, generateUniqueSquadId(), new Range());
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