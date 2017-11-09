using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class ActionHandler
    {
        //public Queue<IMoveAction> MoveActions = new Queue<IMoveAction>();
        public Universe Universe { get; set; }
        //public List<Squad> SquadState { get; set; }

        internal void RunTick(Universe universe, Queue<IMoveAction> actionList)
        {
            Universe = universe;
            //SquadState = squadState;

            if (actionList.Count > 0 && CanMove(universe.World.GetMyPlayer()))
            {
                var actionNow = actionList.Dequeue();
                actionNow.Execute(universe);
                return;
            }
            Universe.Move.Action = ActionType.None;
        }


        public static bool CanMove(Player me) => me.RemainingActionCooldownTicks == 0;

    }
}
