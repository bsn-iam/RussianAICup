using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class SquadCalculator
    {
        public List<Squad> SquadList { get; internal set; } =new List<Squad>();
        public List<DeferredAction> DeferredActionList { get; } = new List<DeferredAction>();
        public Queue<IMoveAction> ActionList { get; } = new Queue<IMoveAction>();
        public Universe Universe;
        private const int ActionListLength = 6;
        private const int MaxDispersionRelative = 2;
        private IdGenerator SquadIdGenerator;


        internal void RunTick(Universe universe)
        {
            Universe = universe;

            if (universe.World.TickIndex == 0)
                StartActionList(Universe);

            foreach (var squad in SquadList)
                squad.UpdateState(Universe);

            CheckDeferredActionList();

#if DEBUG
            if (ActionList.Count > 10)
                Console.WriteLine($"Action list already contains {ActionList.Count} actions.");
#endif

            GenerateActions();
        }

        private void CheckDeferredActionList()
        {
            var listToRemove = new List<DeferredAction>();
            foreach (var action in DeferredActionList)
            {
                if (action.RequestedExecutionTick >= Universe.World.TickIndex)
                {
                    ActionList.Enqueue(action.Action);
                    listToRemove.Add(action);
                }
            }
            listToRemove.ForEach(a => DeferredActionList.Remove(a));
        }

        private void GenerateActions()
        {
#if DEBUG
            if (Universe.World.TickIndex % 120 == 0)
            {
                SquadList.Where(g => g.IsEnabled).ToList()
                    .Where(f => !f.IsEmpty).ToList()
                    .ForEach(s => Console.WriteLine(s.ToString()));
                Console.WriteLine();
            }
#endif

            if (ActionList.Count == 0 )
                foreach (var squad in SquadList.Where(s=>s.IsEnabled).Where(s=>!s.IsEmpty))
                {

                    if (squad.DispersionRelative > MaxDispersionRelative)
                    {
                        Console.WriteLine("We must Hive off! Squad Id " + squad.Id);
                        continue;
                    }

                    foreach (var friendSquad in SquadList.Where(s =>!s.Id.Equals(squad.Id)))
                    {
                        var squadJoin=new Squad(squad, friendSquad);
                        if (squadJoin.Dispersion < squad.Dispersion)
                        {
                            Console.WriteLine($"We can join! Squads Id {squad.Id} and {friendSquad.Id}");
                        }
                    }


                    squad.Attack(ActionList, Universe.MyUnits.GetNearestPositionToTarget(Universe.OppUnits));
                }


                

            //if (ActionList.Count < 6 && Universe.World.TickCount > 2000)
            //{
            //    //ActionList.ActionSelectSquad((int)Squads.All);
            //    ActionList.ActionSelectAll();
            //    ActionList.ActionMoveSelectionToPosition(Universe.OppUnits.GetUnitsCenter());
            //    return;
            //}
        }

        public void StartActionList(Universe universe)  // Do not use Reference Types from Universe here! Links will be changed.
        {
            SquadIdGenerator = new IdGenerator(10, universe.Game.MaxUnitGroup);

            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Fighters, VehicleType.Fighter);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Arrvs, VehicleType.Arrv);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Ifvs, VehicleType.Ifv);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Helicopters, VehicleType.Helicopter);
            ActionList.ActionCreateNewSquad(SquadList, (int)Squads.Tanks, VehicleType.Tank);


            //ActionList.ActionMoveAndCombine(SquadList, (int)Squads.Helicopters, (int)Squads.Ifvs, SquadIdGenerator.New, DeferredActionList, Universe.World.TickIndex, 1500);
            //ActionList.ActionMoveAndCombine(SquadList, (int)Squads.Fighters, (int)Squads.Tanks, SquadIdGenerator.New, DeferredActionList, Universe.World.TickIndex, 1500);

        }


    }


}