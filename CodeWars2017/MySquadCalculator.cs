﻿using System;
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
        public Boolean nukeRequested { get; internal set; } = false;
        public Universe Universe;
        private const int ActionListLength = 6;
        private const double MaxDispersionRelative = 0.9;
        private IdGenerator SquadIdGenerator;

        internal void RunTick(Universe universe)
        {
            Universe = universe;

            if (universe.World.TickIndex == 0)
                StartActionList(Universe);

            foreach (var squad in SquadList)
                squad.UpdateState(Universe);

            CheckForNuclearStrike(universe);

            CheckDeferredActionList();

            if (ActionList.Count > 10)
                universe.Print($"Action list already contains {ActionList.Count} actions.");

            GenerateActions();
        }

        private void CheckForNuclearStrike(Universe universe)
        {
            if (universe.Player.RemainingNuclearStrikeCooldownTicks != 0)
                nukeRequested = false;

            if (universe.Player.RemainingNuclearStrikeCooldownTicks == 0 && !nukeRequested)
            {
                Dictionary<Vehicle, List<Vehicle>> scoutObservation = universe.MyUnits.GetScoutObservation(universe.OppUnits);
                Vehicle target = null;
                Vehicle scout = null;
                double maxNuclearResult = 0;

                foreach (var observation in scoutObservation)
                {
                    var potentialTarget = observation.Key.GetMostDistantAmong(observation.Value);
                    var nulcearRange = universe.Game.TacticalNuclearStrikeRadius;
                    double currentNuclearResult = -potentialTarget.GetTotalEnergyInRange(universe, nulcearRange);

                    if (currentNuclearResult > maxNuclearResult)
                    {
                        maxNuclearResult = currentNuclearResult;
                        scout = observation.Key;
                        target = potentialTarget;
                        universe.Print($"Expected win in nuke {maxNuclearResult:f2}");
                    }
                }

                if (scout != null && target != null && maxNuclearResult > 0)
                {
                    nukeRequested = true;
                    ActionList.ActionRequestNuclearStrike(scout, target);
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
    
            ShowSquadList();


            //if (Universe.World.TickIndex == 50)
            //{
            //    ActionList.ActionMoveToOnePoint(SquadList, (int)Squads.Tanks, (int)Squads.Fighters);
            //    ActionList.ActionMoveToOnePoint(SquadList, (int)Squads.Ifvs, (int)Squads.Helicopters);
            //}

            //if (Universe.World.TickIndex == 600)
            //{
            //    //ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById((int)Squads.Tanks), SquadList.GetSquadById((int)Squads.Fighters), SquadIdGenerator.New);
            //    //ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById((int)Squads.Ifvs), SquadList.GetSquadById((int)Squads.Helicopters), SquadIdGenerator.New);
            //}

            if (Universe.World.TickIndex == 50)
            {
                ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById((int)Squads.Tanks), SquadList.GetSquadById((int)Squads.Fighters), SquadIdGenerator.New, false);
                ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById((int)Squads.Ifvs), SquadList.GetSquadById((int)Squads.Helicopters), SquadIdGenerator.New, false);
                ReduceScaleForAll();
            }

            if (ActionList.Count == 0)
            {
                var enemy = new Squad(Universe.OppUnits);
                var me = new Squad(Universe.MyUnits);
                var aggression = me.Energy / enemy.Energy;
                Universe.Print($"Agression {aggression:f2}, my energy {(me.Energy):f2}, enemy energy {(enemy.Energy):f2}");

                foreach (var squad in SquadList.GetIteratorSquadListActive())
                {
                    //CheckForJoin(squad);

                    if (aggression > 0.9)
                    {
                        //Atack
                        if (squad.Id == (int)Squads.Tanks || squad.Id == (int)Squads.Ifvs)
                            squad.Attack(ActionList, squad.Units.GetPositionOfNearestTarget(Universe.OppUnits));


                        if (squad.Id == (int)Squads.Helicopters)
                            squad.Follow(ActionList, squad, SquadList.GetSquadById((int)Squads.Ifvs));
                        if (squad.Id == (int)Squads.Fighters)
                            squad.Follow(ActionList, squad, SquadList.GetSquadById((int)Squads.Tanks));
                        if (squad.Id == (int)Squads.Arrvs)
                            squad.Follow(ActionList, squad, SquadList.GetSquadById((int)Squads.Fighters));
                    }
                    else
                    {
                        //going to deff
                            squad.Attack(ActionList, Universe.MapConerLeftUp);
                    }


                }
            }

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