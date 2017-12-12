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
        public Queue<IMoveAction> ImmediateActionList { get; } = new Queue<IMoveAction>();
        public static MoveOrder MoveOrders = new MoveOrder();
        public Boolean NukeRequested { get; internal set; } = false;
        public Universe Universe;
        //private const int ActionListLength = 6;
        private const double MaxDispersionRelative = 0.85;
        private IdGenerator SquadIdGenerator;
        private BonusMapCalculator BonusCalculator;
        private Dictionary<Facility, IEnumerable<Vehicle>> FreeUnitsOnFacilities = new Dictionary<Facility, IEnumerable<Vehicle>>();
        private const int MaxUnitsForNewSquad = 44;

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

            ShowSquadList();

            if (universe.World.TickIndex == 0)
                StartActionList(Universe);

            if (universe.World.TickIndex == 100)
                ReduceScaleForAll();



            //--------------Immediate Actions------------
            if (Universe.World.GetOpponentPlayer().NextNuclearStrikeTickIndex > 0)
                universe.Print("Nuclear launch detected!");

            //TODO Extend the sqaud if Nuclear Launch detected, then narrow it.
            if (CanMoveImmediateAction(universe))
                CheckForNukeComing();

            if (CanMoveImmediateAction(universe))
                CheckForNuclearStrike(universe);
            //------------Immediate Actions-------------

            //------------Common actions-----------------

            if (CanMoveCommonAction())
                CheckForScoutsAmount();

            if (CanMoveCommonAction() && SquadIdGenerator.HasCapacity)
                CalculateFreeUnitsOnFacilities();

            if (CanMoveCommonAction() && SquadIdGenerator.HasCapacity)
                CheckForNewUnits();

            if (CanMoveCommonAction())
                CheckForFacilityProduction();

            if (CanMoveCommonAction())
                CheckForDispersedSquads();

            if (CanMoveCommonAction())
            {
                universe.Print($"Action list is ready for moves.");
                GenerateSquadCommands();
            }

            //------------Common actions-----------------

        }

        private void CalculateFreeUnitsOnFacilities()
        {
            FreeUnitsOnFacilities = new Dictionary<Facility, IEnumerable<Vehicle>>();

            foreach (var facility in Universe.World.Facilities.Where(f => f.OwnerPlayerId == Universe.Player.Id))
            {
                var rangeX = new Range(facility.Left, facility.Left + Universe.Game.FacilityWidth);
                var rangeY = new Range(facility.Top, facility.Top + Universe.Game.FacilityHeight);

                var unitsOnFactory =
                    Universe.MyUnits.Where(u => u.X.IsInRange(rangeX) && u.Y.IsInRange(rangeY) && !u.Groups.Any());
                FreeUnitsOnFacilities.Add(facility, unitsOnFactory);
            }
        }

        private void CheckForDispersedSquads()
        {
            const int scalePeriod = 300; 
            var squad = SquadList.FirstOrDefault(s => !s.IsScout && 
                                                s.DispersionRelative > MaxDispersionRelative && 
                                                !s.IsWaitingForScaling &&
                                                Universe.World.TickIndex - s.LastScaleTick > scalePeriod);
            squad?.DoZoomIn(ActionList);

        }

        private void CheckForNewUnits()
        {
            foreach (var facilityUnit in FreeUnitsOnFacilities)
            {
                var unitsOnFactoryCount = facilityUnit.Value.Count();
                var facility = facilityUnit.Key;

                var timeToCreateSquad = SquadIdGenerator.HasCapacity && 
                        (unitsOnFactoryCount >= MaxUnitsForNewSquad ||
                         (facility.CapturePoints < 10 && unitsOnFactoryCount > 10) ||
                         (facility.VehicleType == null && unitsOnFactoryCount > 10));

                if (timeToCreateSquad)
                {
                    var factoryRange = GetFacilityRange(facility);
                    Universe.Print($"Creating new squad on factory [{facility.Id}]");
                    ActionList.ActionSelectInRange(factoryRange);
                    ActionList.ActionCreateNewSquadAlreadySelected(SquadList, SquadIdGenerator);
                    return; // Once created
                }
            }

        }

        private Range2 GetFacilityRange(Facility facility)
        {
            var rangeX = new Range(facility.Left, facility.Left + Universe.Game.FacilityWidth);
            var rangeY = new Range(facility.Top, facility.Top + Universe.Game.FacilityHeight);
            var factoryRange = new Range2(rangeX, rangeY);
            return factoryRange;
        }

        private void CheckForFacilityProduction()
        {
            var myProductionFacilities = Universe.World.Facilities.Where(f => 
                f.Type == FacilityType.VehicleFactory && 
                f.OwnerPlayerId == Universe.Player.Id).ToList();

            if (!myProductionFacilities.Any())
                return;

            if (!Universe.Game.IsFogOfWarEnabled)
            {
                var newUnitsRequired = CalculateNewUnitsNeсessity();
                if (newUnitsRequired)
                    UpdateProductionOnFacility(myProductionFacilities);
                else
                    StopPoductionOnFirstFacility(myProductionFacilities);
            }
            else
            {
                UpdateProductionOnFacility(myProductionFacilities);
            }
        }

        private bool CalculateNewUnitsNeсessity()
        {
            var newUnitsRequired = (double) Universe.MyUnits.Count(u => u.Type != VehicleType.Arrv) /
                                   Universe.OppUnits.Count(u => u.Type != VehicleType.Arrv) < 4;
            return newUnitsRequired;
        }

        private void UpdateProductionOnFacility(List<Facility> myProductionFacilities)
        {
            Facility chosenFacility = null;
            VehicleType chosenType = VehicleType.Ifv;

            foreach (var facility in myProductionFacilities)
            {
                var freeUnitsOnFacility = FreeUnitsOnFacilities[facility].Count();
                var isWaitingForOrder = freeUnitsOnFacility == 0 || freeUnitsOnFacility > MaxUnitsForNewSquad;
                if (isWaitingForOrder)
                {
                    var requiredType = GetRequiredUnitsType();
                    if (requiredType != facility.VehicleType)
                    {
                        chosenFacility = facility;
                        chosenType = requiredType;
                        break;
                    }
                }

            }

            if (chosenFacility == null)
                return;
            
            ActionList.ActionProductionStart(chosenFacility, chosenType);
        }

        private VehicleType GetRequiredUnitsType()
        {
            var superiority = new int [5];

            foreach (var oppUnit in Universe.OppUnits)
                superiority[(int)oppUnit.Type]--;

            foreach (var myUnit in Universe.MyUnits)
                superiority[(int)myUnit.Type]++;

            var minValue = superiority.Min();
            var requredType = Array.LastIndexOf(superiority, minValue);

            return (VehicleType)requredType;
        }

        private void StopPoductionOnFirstFacility(List<Facility> productionFacilities)
        {
            var facility = productionFacilities.FirstOrDefault(f => f.VehicleType != null);
            if (facility == null)
                return;
            ActionList.ActionProductionStop(facility);
        }

        private bool CanMoveImmediateAction(Universe universe)
        {
            return ImmediateActionList.Count == 0 && universe.Player.RemainingActionCooldownTicks == 0;
        }

        private bool CanMoveCommonAction()
        {
            return ActionHandler.HasActionsFree() && ActionList.Count == 0 && ImmediateActionList.Count == 0;
        }

        private void CheckForNukeComing()
        {
            var oppPlayer = Universe.World.GetOpponentPlayer();
            if (oppPlayer.NextNuclearStrikeTickIndex > 0)
            {
                var affectedSquadsList = new List<Squad>();
                var nukeCenter = new AbsolutePosition(oppPlayer.NextNuclearStrikeX, oppPlayer.NextNuclearStrikeY);
                var nukeRadius = Universe.Game.TacticalNuclearStrikeRadius;
                const int duration = 31;
                var squaredNukeRadius = nukeRadius * nukeRadius;

                foreach (var squad in SquadList.Where(s => !s.IsWaitingForScaling && 
                                                            s.IsEnabled && 
                                                            !s.IsEmpty && 
                                                            !s.IsScout))
                    foreach (var unit in squad.Units)
                        if (unit.GetSquaredDistanceTo(nukeCenter.X, nukeCenter.Y) < squaredNukeRadius)
                            affectedSquadsList.Add(squad);

                var chosenSquad = affectedSquadsList.Distinct().ToList().FirstOrDefault();
                chosenSquad?.DoScaleJerk(ImmediateActionList, DeferredActionList, 10, nukeCenter, duration, Universe.World.TickIndex + duration);
            }

        }

        private void CheckForScoutsAmount()
        {
                var scoutsAmount = SquadList.Count(
                    s => s.IsScout &&
                         (!s.IsCreated ||
                          s.Units.Any(u => u.Durability != 0)));

                if (scoutsAmount < 2)
                    ActionList.ActionAppointNewScout(SquadList, SquadIdGenerator);
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
                    ImmediateActionList.ActionRequestNuclearStrike(SquadList, scout, target);
                }
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
                 var dispersionCondition = squad.DispersionRelative > MaxDispersionRelative;
                 if (dispersionCondition && !squad.IsWaitingForScaling)
                     ActionList.ActionScaleSquadToPosition(squad, 0.1, squad.SquadCenter, 60);
             }
        }

        private void CheckForJoin(Squad squad)
        {
            foreach (var friendSquad in new List<Squad>(SquadList.Where(s => !s.Id.Equals(squad.Id))))
            {
                var squadJoin = new Squad(squad, friendSquad);
                if (squadJoin.DispersionSquared < squad.DispersionSquared && SquadIdGenerator.HasCapacity)
                {
                    Universe.Print($"We can join! Squads Id {squad.Id} and {friendSquad.Id}");
                    ActionList.ActionCombineSquads(SquadList, SquadList.GetSquadById(squad.Id),
                        SquadList.GetSquadById(friendSquad.Id), SquadIdGenerator);
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