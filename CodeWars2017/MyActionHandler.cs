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

            if (actionList.Count > 0 && CanMove(universe.World.GetMyPlayer()))
            {
                var actionNow = actionList.Dequeue();

                actionNow.Execute(universe);
#if DEBUG
                Console.WriteLine($"{Universe.World.TickIndex}. Action [{universe.Move.Action}] activated.");
                if (!universe.Move.Action.Equals(ActionType.ClearAndSelect))
                    Console.WriteLine($"Selection is {universe.GetSelectedUnits().Count} units.");
#endif
                return;
            }
            Universe.Move.Action = ActionType.None;
        }


        public static bool CanMove(Player me) => me.RemainingActionCooldownTicks == 0;

    }
}
