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
        public Universe Universe { get; set; }

        internal void RunTick(Universe universe, Queue<IMoveAction> actionList)
        {
            Universe = universe;

            if (CanMove(universe.Player, actionList))
            {
                var executed = actionList.Dequeue().Execute(universe);
                
                while (!executed && CanMove(universe.Player, actionList))
                    executed = actionList.Dequeue().Execute(universe);
            }
            //Universe.Move.Action = ActionType.None;
        }

        public static bool CanMove(Player me, Queue<IMoveAction> actionList) => me.RemainingActionCooldownTicks == 0 && actionList.Any();

    }
}
