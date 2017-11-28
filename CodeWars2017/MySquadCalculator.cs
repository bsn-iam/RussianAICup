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
        private const int ActionListLength = 6;
        private const double MaxDispersionRelative = 0.9;
        private IdGenerator SquadIdGenerator;

        private BonusMapCalculator BonusCalculator;
        //public int ExpectedTicksToNextUpdate => SquadList.Count(s => s.IsEnabled && s.IsCreated && !s.IsEmpty) * 2;

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
            CombineSquadsOnStart();

            if (ActionList.Count == 0)
            {
                //GenerateScoutsCommand();
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
                ReduceScaleForAll();
            }
        }

        private void GenerateSquadCommands()
        {
            var aggression = Aggression;
            var chosenSquad = SquadList.GetIteratorSquadListActive().OrderBy(s => s.LastCallTick).FirstOrDefault();

            if (chosenSquad == null) return;

            if (chosenSquad.IsScout || chosenSquad.Id == (int)Squads.Fighters)
                {
                    //var requiredPosition = GeneratePositionForScout(squad);
                    var requiredPosition = BonusCalculator.GetBonusMovePoint(chosenSquad);
                    chosenSquad.DoMove(ActionList, requiredPosition);
                }

                if (!chosenSquad.IsScout && chosenSquad.Id != (int)Squads.Fighters)
                {
                    if (aggression > 1.2)
                    {
                        //Atack follow mode

                        if (chosenSquad.Id == (int) Squads.Tanks || chosenSquad.Id == (int) Squads.Ifvs)
                            chosenSquad.DoMove(ActionList, chosenSquad.Units.GetPositionOfNearestTarget(Universe.OppUnits));


                        if (chosenSquad.Id == (int) Squads.Helicopters)
                            chosenSquad.DoFollow(ActionList, chosenSquad, SquadList.GetSquadById((int) Squads.Ifvs));
                        //if (chosenSquad.Id == (int) Squads.Fighters)
                        //    chosenSquad.DoFollow(ActionList, chosenSquad, SquadList.GetSquadById((int) Squads.Tanks));
                        if (chosenSquad.Id == (int) Squads.Arrvs)
                            chosenSquad.DoFollow(ActionList, chosenSquad, SquadList.GetSquadById((int) Squads.Fighters));

                        //Attack nearest mode
                        //var nearestTarget = Universe.MyUnits.GetPositionOfNearestTarget(Universe.OppUnits);
                        //squad.DoMove(ActionList, nearestTarget, Universe.Game.TankSpeed);
                    }
                    else
                    {
                        //going to deff
                        // if (Universe.World.TickIndex % 5 == 0)
                        //     squad.DoRotate(ActionList);
                        // else
                        chosenSquad.DoMove(ActionList, Universe.MapConerLeftUp);
                    }
                }

        }

        private double Aggression
        {
            get
            {
                var persentageToRemove = 2;

                var enemyUnits = Universe.OppUnits.GetClosestUnits(persentageToRemove).Where(u =>u.Type!= VehicleType.Fighter).ToList();
                var myUnits = Universe.MyUnits.GetClosestUnits(persentageToRemove).Where(u => u.Type != VehicleType.Fighter).ToList();

                var enemy = new Squad(enemyUnits);
                var me = new Squad(myUnits);
                var aggression = me.Energy / enemy.Energy;
                Universe.Print($"Agression {aggression:f2}, my energy {(me.Energy):f2}, enemy energy {(enemy.Energy):f2}");
                return aggression;
            }
        }


        private void GenerateScoutsCommand()
        {

        }

        private AbsolutePosition GeneratePositionForScout(Squad squad)
        {
            var scout = squad.Units.FirstOrDefault();

            var nextUpdateTick = Universe.World.TickIndex + squad.ExpectedTicksToNextUpdate;
            var predictedWorldState = MyStrategy.Predictor.GetStateOnTick(nextUpdateTick);
            var predictedEnemyUnits = predictedWorldState.OppUnits.Where(u => !u.Type.Equals(VehicleType.Arrv)).ToList();

            var enemyPosition = squad.Units.GetPositionOfNearestTarget(predictedEnemyUnits);

            //            var isCloseDistance = Universe.Player.RemainingNuclearStrikeCooldownTicks < 30 || Universe.Player.nuc ;
            // var scoutDistanceKoeff = isCloseDistance ?  3: 5;
            var scoutDistanceKoeff = 3;
            var distanceFromEnemy = scout.AerialAttackRange * scoutDistanceKoeff;
            var distanceToEnemy = scout.GetDistanceTo(enemyPosition.X, enemyPosition.Y);
            var koeff = (distanceToEnemy - distanceFromEnemy) / distanceToEnemy;
            var targetX = scout.X + (enemyPosition.X - scout.X) * koeff;
            var targetY = scout.Y + (enemyPosition.Y - scout.Y) * koeff;

            var requiredPosition = new AbsolutePosition(targetX, targetY);


            return requiredPosition;
        }

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
            //universe.Print($"Action count is {ActionList.Count}");
            //if (ActionList.Count == 0)
                foreach (var squad in SquadList.GetIteratorSquadListActive())
                {
                    var dispersionCondition = squad.DispersionRelative > MaxDispersionRelative || squad.Dispersion > 25;
                    if (dispersionCondition && !squad.IsWaitingForScaling)
                    {
                        ActionList.ActionScaleSquadToPosition(squad, 0.1, squad.Units.GetUnitsCenter(), 60);
                        continue;
                    }
                }

        if (ActionList.Count < 6 && Universe.World.TickCount > 2000)
            {
                //ActionList.ActionSelectSquad((int)Squads.All);
                ActionList.ActionSelectAll();
                ActionList.ActionMoveSelectionToPosition(Universe.OppUnits.GetUnitsCenter());
                return;
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