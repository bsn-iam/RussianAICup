using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class MyActionHandler
    {


        public Queue<IMoveAction> MoveActions = new Queue<IMoveAction>();
        public Universe Universe { get; set; }

        internal void RunTick(Universe universe)
        {
            Universe = universe;
            if (Universe.World.TickIndex == 0) StartActionList(Universe);
            GenerateActions();

            if (MoveActions.Count > 0 && CanMove(universe.World.GetMyPlayer()))
            {
                var actionNow = MoveActions.Dequeue();
                actionNow.Execute(universe);
                return;
            }
            Universe.Move.Action = ActionType.None;
        }

        private void GenerateActions()
        {
            if (MoveActions.Count < 6 && Universe.World.TickCount > 200)
            {
                //MoveActions.ActionSelectSquad((int)Squads.All);
                MoveActions.ActionSelectAll();
                MoveActions.ActionMoveSelectionToPosition(Universe.MyUnits.GetNearestPositionToTarget(Universe.OppUnits));
                return;
            }


            if (MoveActions.Count < 6 && Universe.World.TickCount > 2000)
            {
                //MoveActions.ActionSelectSquad((int)Squads.All);
                MoveActions.ActionSelectAll();
                MoveActions.ActionMoveSelectionToPosition(Universe.OppUnits.GetUnitsCenter());
                return;
            }
        }

        public void StartActionList(Universe universe)
        {
            //do not use Universe here. Link will be changed.

            MoveActions.ActionSelectAll();
            MoveActions.ActionAssignSelectionToSquad((int)Squads.All);
            MoveActions.ActionMoveSelectionToPosition(Universe.MapCenter);

            MoveActions.ActionSelectVenicleType(VehicleType.Fighter);
            MoveActions.ActionAssignSelectionToSquad((int)Squads.Fighters);
            MoveActions.ActionMoveSelectionToPosition(Universe.MapConerRightUp);

        }

        public static bool CanMove(Player me) => me.RemainingActionCooldownTicks == 0;

    }
}
