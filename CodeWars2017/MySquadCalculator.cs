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
        public Boolean NukeRequested { get; internal set; } = false;
        public Universe Universe;
        //private const int ActionListLength = 6;
        private const double MaxDispersionRelative = 0.9;
        private IdGenerator SquadIdGenerator;
        private BonusMapCalculator BonusCalculator;

        internal void RunTick(Universe universe)
        {

            #region UpdateStates

            Universe = universe;
            BonusCalculator = MyStrategy.BonusCalculator;

            foreach (var squad in SquadList)
                squad.UpdateState(Universe);
            foreach (var squad in SquadList)
                if (squad.NukeMarkerCounter > 0)
                    squad.NukeMarkerCounter--;

            #endregion

            if (universe.World.TickIndex == 0)
                StartActionList(Universe);

            if (universe.World.TickIndex == 100)
                ReduceScaleForAll();

            //TODO Extend the sqaud if Nuclear Launch detected, then narrow it.

            CheckForNuclearStrike(universe);

            CheckDeferredActionList();

            CheckForScoutsAmount();

            if (ActionList.Count == 0)
                universe.Print($"Action list is ready for moves.");

            ShowSquadList();

            GenerateActions();
        }

        private void CheckForScoutsAmount()
        {
            if (ActionList.Count == 0)
            {
                var scoutsAmount = SquadList.Count(
                    s => s.IsScout &&
                         (!s.IsCreated ||
                          s.Units.Any(u => u.Durability != 0)));

                if (scoutsAmount < 2)
                    ActionList.ActionAppointNewScout(SquadList, SquadIdGenerator);
            }
        }

        private void CheckForNuclearStrike(Universe universe)
        {
            if (universe.Player.RemainingNuclearStrikeCooldownTicks != 0)
                NukeRequested = false;

            if (universe.Player.RemainingNuclearStrikeCooldownTicks == 0 && !NukeRequested)
            {
                Dictionary<Vehicle, List<Vehicle>> scoutObservation = universe.MyUnits.GetScoutObservation(universe.OppUnits);
                Vehicle target = null;
                Vehicle scout = null;
                double maxNuclearResult = 0;
                var nulcearRange = universe.Game.TacticalNuclearStrikeRadius;

                foreach (var observation in scoutObservation)
                {
                    var potentialTarget = observation.Key.GetMostDistantAmong(observation.Value);


                    double currentNuclearResult = potentialTarget.GetPotentialNuclearWin(universe, nulcearRange);

                    if (currentNuclearResult > maxNuclearResult)
                    {
                        maxNuclearResult = currentNuclearResult;
                        scout = observation.Key;
                        target = potentialTarget;
                        universe.Print($"Max win in nuke {maxNuclearResult:f2}");
                    }
                }

                if (scout != null && target != null && maxNuclearResult > 0)
                {
                    NukeRequested = true;
                    ActionList.ActionRequestNuclearStrike(SquadList, scout, target);
                }
            }

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
            //CombineSquadsOnStart();
            if (ActionList.Count == 0)
            {
                GenerateSquadCommands();
            }
        }

        private void CombineSquadsOnStart()
        {
            if (Universe.World.TickIndex == 50)
            {
                //ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById((int) Squads.Tanks),
                //    SquadList.GetSquadById((int) Squads.Fighters), SquadIdGenerator.New, false);
                //ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById((int) Squads.Ifvs),
                //    SquadList.GetSquadById((int) Squads.Helicopters), SquadIdGenerator.New, false);
            }
        }

        private void GenerateSquadCommands()
        {
            var chosenSquad = SquadList.GetIteratorSquadListActive().OrderBy(s => s.LastCallTick).FirstOrDefault();

            if (chosenSquad == null)
                return;

            var requiredSpeed = chosenSquad.IsScout ? Universe.Game.FighterSpeed : chosenSquad.CruisingSpeed;

            var requiredPosition = BonusCalculator.GetBonusMovePoint(chosenSquad);
            chosenSquad.DoMove(ActionList, requiredPosition, requiredSpeed);
        }


        public AbsolutePosition NukeStrikePosition { get; internal set; }
        public long NukeStrikeScoutId { get; internal set; }

        private void ShowSquadList()
        {
            if (Universe.World.TickIndex % 60 == 0)
            {
                SquadList.Where(g => g.IsEnabled).ToList()
                    .Where(f => !f.IsEmpty).ToList()
                    .ForEach(s => Universe.Print(s.ToString()));
                Universe.Print("");
            }
        }


        private void ReduceScaleForAll()
        {
             foreach (var squad in SquadList.GetIteratorSquadListActive())
             {
                 var dispersionCondition = squad.DispersionRelative > MaxDispersionRelative || squad.Dispersion > 25;
                 if (dispersionCondition && !squad.IsWaitingForScaling)
                     ActionList.ActionScaleSquadToPosition(squad, 0.1, squad.SquadCenter, 60);
             }
        }

        private void CheckForJoin(Squad squad)
        {
            foreach (var friendSquad in new List<Squad>(SquadList.Where(s => !s.Id.Equals(squad.Id))))
            {
                var squadJoin = new Squad(squad, friendSquad);
                if (squadJoin.Dispersion < squad.Dispersion)
                {
                    Universe.Print($"We can join! Squads Id {squad.Id} and {friendSquad.Id}");
                    ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById(squad.Id),
                        SquadList.GetSquadById(friendSquad.Id), SquadIdGenerator.New);
                }
            }
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