using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public static class ActionHandler
    {
        public static Universe Universe { get; set; }
        private static List<int> lastMinuteTickActions = new List<int>();


        internal static void RunTick(Universe universe, Queue<IMoveAction> commonActionList, Queue<IMoveAction> immediateActionList)
        {
            Universe = universe;

            //run actions
            var somethingStarted = RunAction(universe, immediateActionList);

            if (!somethingStarted)
                somethingStarted = RunAction(universe, CheckDeferredActionList());

            if (!somethingStarted && HasActionsFree())
                somethingStarted = RunAction(universe, commonActionList);


            //update done actions array
            if (somethingStarted)
                lastMinuteTickActions.Add(universe.World.TickIndex);

            var cooldown = universe.Player.RemainingActionCooldownTicks;
            if (cooldown > 0)
                universe.Print("Unexpected action cooldown");

            foreach (var tickAction in new List<int>(lastMinuteTickActions))
                if (tickAction < universe.World.TickIndex - 60)
                    lastMinuteTickActions.Remove(tickAction);
        }

        private static Queue<IMoveAction> CheckDeferredActionList()
        {
            var listToRemove = new List<DeferredAction>();
            var listToExecute = new Queue<IMoveAction>();
            var defferedList = MyStrategy.SquadCalculator.DeferredActionList;

            var actionNow = defferedList.FirstOrDefault(a => a.RequestedExecutionTick <= Universe.World.TickIndex);
            if (actionNow != null && Universe.Player.RemainingActionCooldownTicks != 0 )
                Universe.Print("Warning! No free moves for deferred action.");

            if (actionNow != null && Universe.Player.RemainingActionCooldownTicks == 0)
            {
                listToExecute.Enqueue(actionNow.Action);
                listToRemove.Add(actionNow);
            }

            listToRemove.ForEach(a => defferedList.Remove(a));

            return listToExecute;
        }

        private static bool RunAction(Universe universe, Queue<IMoveAction> actionList)
        {
            var executed = false;
            if (CanMove(universe.Player, actionList))
            {
                executed = actionList.Dequeue().Execute(universe);

                while (!executed && CanMove(universe.Player, actionList))
                {
                    executed = actionList.Dequeue().Execute(universe);
                    universe.Print("Executing next.");
                }
            }
            return executed;
        }

        public static bool CanMove(Player me, Queue<IMoveAction> actions)
        {
            return me.RemainingActionCooldownTicks == 0 && actions.Any();
            //return HasActionsFree() && actions.Any();
        }

        public static bool HasActionsFree() => lastMinuteTickActions.Count < MyStrategy.MaxActionBalance;
    }
}
