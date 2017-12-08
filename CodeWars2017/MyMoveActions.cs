using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{

    public static class MoveList
    {
        #region Selection

        public static void ActionSelectAll(this Queue<IMoveAction> moveActions) =>
            moveActions.Enqueue(new ActionSelectAll());

        public static void ActionSelectVenicleType(this Queue<IMoveAction> moveActions, VehicleType type) =>
            moveActions.Enqueue(new ActionSelectVenicleType(type));

        public static void ActionSelectSquad(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionSelectSquad(squadId));

        public static void ActionSelectInRange(this Queue<IMoveAction> moveActions, Range2 range2) =>
            moveActions.Enqueue(new ActionSelectInRange(range2));

        //Assignment
        public static void ActionAssignSelectionToSquad(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionAssignSelectionToSquadOld(squadId));

        #endregion

        #region Grouping
        public static void ActionAddSquadToCurrentSelection(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionAddSquadToCurrentSelection(squadId));

        public static void ActionCombineSquads(this Queue<IMoveAction> moveActions, List<Squad> squadList, Squad squadAlfa, Squad squadDelta, IdGenerator idGenerator, bool disableOld = true)
        {
            moveActions.ActionSelectSquad(squadAlfa.Id);
            moveActions.ActionAddSquadToCurrentSelection(squadDelta.Id);
            var newSquad = moveActions.ActionCreateNewSquadAlreadySelected(squadList, idGenerator);

            if (disableOld)
            {
                squadAlfa.Disable();
                squadDelta.Disable();
            }
            else
            {
                newSquad.Disable();
            }
        }

        public static void ActionCombineSquads(this Queue<IMoveAction> moveActions, List<Squad> squadList, int squadAlfaId, int squadDeltaId,
            IdGenerator squadIdGenerator, bool disableOld = true) => 
            moveActions.ActionCombineSquads(squadList, squadList.GetSquadById(squadAlfaId), squadList.GetSquadById(squadDeltaId), squadIdGenerator, disableOld);

        public static void ActionAppointNewScout(this Queue<IMoveAction> moveActions, List<Squad> squadList,
            IdGenerator squadIdGenerator)
        {
            if (squadIdGenerator.HasCapacity)
            {
                const VehicleType type = VehicleType.Fighter;
                var scoutOwner = squadList.FirstOrDefault(sq => sq.Units.Any(u => u.Type.Equals(type)) && !sq.IsScout);

                if (scoutOwner == null)
                    return;

                var candidateList = scoutOwner.Units.Where(u => u.Type == type).ToList();

                var scout = candidateList.GetMostDistantUnit();

                moveActions.Enqueue(new ActionSelectOneUnit(scout));
                foreach (var groupId in scout.Groups)
                    moveActions.Enqueue(new ActionDismissSelectionFromSquad(groupId));

                var newSquad = moveActions.ActionCreateNewSquadAlreadySelected(squadList, squadIdGenerator);
                newSquad.IsScout = true;

            }
        }


        #endregion

        #region Movement

        public static void ActionMoveSelectionToPosition(this Queue<IMoveAction> moveActions, AbsolutePosition position) =>
            moveActions.Enqueue(new ActionMoveSelectionToPosition(position));
        public static void ActionMoveSelectionToPosition(this Queue<IMoveAction> moveActions, AbsolutePosition position, double speed) =>
            moveActions.Enqueue(new ActionMoveSelectionToPosition(position, speed));
        public static void ActionRotateSelection(this Queue<IMoveAction> moveActions, AbsolutePosition center, double angleChange) =>
            moveActions.Enqueue(new ActionRotateSelection(center, angleChange));


        public static void ActionMoveToOnePoint(this Queue<IMoveAction> actions, List<Squad> squadList, int squadAlfaId,
            int squadDeltaId) =>
            actions.Enqueue(new ActionMoveToOnePoint(actions, squadList, squadAlfaId, squadDeltaId));

        public static void ActionRequestNuclearStrike(this Queue<IMoveAction> moveActions, List<Squad> squadList, Vehicle scout,
            Vehicle target)
        {
            var scoutSquad = squadList.GetSquadByUnit(scout);
            if (scoutSquad != null)
            {
                scoutSquad.DoStop(moveActions);
                moveActions.Enqueue(new ActionRequestNuclearStrike(scout, target));
            }
        }

        #endregion

        #region Production
        public static void ActionProductionStart(this Queue<IMoveAction> moveActions, Facility facility, VehicleType type) =>
            moveActions.Enqueue(new ActionProductionStart(facility, type));


        #endregion
    }

    internal class ActionProductionStart : IMoveAction
    {
        private readonly Facility facility;
        private readonly VehicleType type;

        public ActionProductionStart(Facility facility, VehicleType type)
        {
            this.facility = facility;
            this.type = type;
        }

        public bool Execute(Universe universe)
        {
            if (facility.OwnerPlayerId != universe.Player.Id)
            {
                universe.Print($"Facility [{facility.Id}] is reoccupied.");
                return false;
            }

            universe.Move.Action = ActionType.SetupVehicleProduction;
            universe.Move.VehicleType = type;
            universe.Move.FacilityId = facility.Id;
            return true;
        }
    }

    internal class ActionRotateSelection : IMoveAction
    {
        private readonly AbsolutePosition center;
        private readonly double angleChange;

        public ActionRotateSelection(AbsolutePosition center, double angleChange)
        {
            this.center = center;
            this.angleChange = angleChange;
        }

        public bool Execute(Universe universe)
        {
            universe.Move.Action = ActionType.Rotate;
            universe.Move.Angle = angleChange;
            universe.Move.X = center.X;
            universe.Move.Y = center.Y;
            return true;
        }
    }

    internal class ActionDismissSelectionFromSquad : IMoveAction
    {
        private readonly int groupId;

        public ActionDismissSelectionFromSquad(int groupId)
        {
            this.groupId = groupId;
        }

        public bool Execute(Universe universe)
        {
            universe.Move.Action = ActionType.Dismiss;
            universe.Move.Group = groupId;
            return true;
        }
    }


    internal class ActionRequestNuclearStrike : IMoveAction
    {
        private Vehicle scout;
        private Vehicle target;
        private AbsolutePosition targetPoint;

        public ActionRequestNuclearStrike(Vehicle scout, Vehicle target)
        {
            this.scout = scout;
            this.target = target;
            this.targetPoint = new AbsolutePosition(target.X, target.Y);
        }

        public bool Execute(Universe universe)
        {
            if (scout.Durability != 0 && scout.DoISeeThisPoint(targetPoint))
            {
                universe.Move.Action = ActionType.TacticalNuclearStrike;
                universe.Move.X = target.X;
                universe.Move.Y = target.Y;
                universe.Move.VehicleId = scout.Id;

                MyStrategy.SquadCalculator.SquadList.GetSquadByUnit(scout).SetNukeMarkerCount(30);
                MyStrategy.SquadCalculator.NukeStrikePosition = targetPoint;
                MyStrategy.SquadCalculator.NukeStrikeScoutId = scout.Id;

                universe.Print($"Action {this} is started to [{target.X:f2}, {target.Y:f2}].");
                return true;
            }
            else
            {
                universe.Print($"Warning! Action {this} is skipped.");
                if (scout.Durability == 0)
                    universe.Print($"Warning! Scout is dead..");
                if (!scout.DoISeeThisPoint(targetPoint))
                    universe.Print($"Warning! Scout outside the range.");
                return false;
            }
        }
    }

    internal class ActionScaleSelectedSquadToPosition : IMoveAction
    {
        private int duration;
        private double factor;
        private AbsolutePosition position;
        private Squad squad;

        public ActionScaleSelectedSquadToPosition(Squad squad, double factor, AbsolutePosition position, int duration)
        {
            this.squad = squad;
            this.factor = factor;
            this.position = position;
            this.duration = duration;
        }

        public bool Execute(Universe universe)
        {
//            if (squad.Id == 11)
//                throw new Exception();
            universe.Move.Action = ActionType.Scale;
            universe.Move.X = position.X;
            universe.Move.Y = position.Y;
            universe.Move.Factor = factor;
            squad.ScalingTimeDelay = duration;
            squad.IsWaitingForScaling = false;
            squad.LastScaleTick = universe.World.TickIndex;

            universe.Print($"Action {this} is started.");
            return true;
        }
    }

    internal class ActionMoveToOnePoint : IMoveAction
    {
        private int squadAlfaId;
        private int squadDeltaId;
        private List<Squad> squadList;
        private Queue<IMoveAction> actions;

        public ActionMoveToOnePoint(Queue<IMoveAction> actions, List<Squad> squadList, int squadAlfaId, int squadDeltaId)
        {
            this.squadList = squadList;
            this.squadAlfaId = squadAlfaId;
            this.squadDeltaId = squadDeltaId;
            this.actions = actions;
        }

        public bool Execute(Universe universe)
        {
            squadList.GetMeetingPoint(squadAlfaId, squadDeltaId);

            var meetingPoint = squadList.GetMeetingPoint(squadAlfaId, squadDeltaId);

            actions.ActionSelectSquad(squadAlfaId);
            actions.ActionMoveSelectionToPosition(meetingPoint);
            squadList.GetSquadById(squadAlfaId).UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);

            actions.ActionSelectSquad(squadDeltaId);
            actions.ActionMoveSelectionToPosition(meetingPoint);
            squadList.GetSquadById(squadDeltaId).UpdateLastCallTime(MyStrategy.Universe.World.TickIndex);
            return true;
        }
    }

    internal class ActionAddSquadToCurrentSelection : IMoveAction
    {
        private readonly int squadId;

        public ActionAddSquadToCurrentSelection(int squadId)
        {
            this.squadId = squadId;
        }

        public bool Execute(Universe universe)
        {
            universe.Move.Action = ActionType.AddToSelection;
            universe.Move.Group = squadId;
            //universe.Print($"Action {this} is started.");
            return true;
        }
    }

    internal class ActionSelectInRange : IMoveAction
    {
        private readonly Range2 range;

        public ActionSelectInRange(Range2 range)
        {
            this.range = range;
        }

        public bool Execute(Universe universe)
        {
            //if (universe.World.TickIndex !=0)
            //    universe.Crash("Executing without permission!");
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Right = range.XMax;
            universe.Move.Left = range.XMin;
            universe.Move.Top = range.YMin;
            universe.Move.Bottom = range.YMax;

            if (universe.Move.Left >= universe.Move.Right && universe.Move.Top >= universe.Move.Bottom)
                universe.Crash("Wrong Range in move!");
            universe.Print($"Action {this} is started. Selecting units in [{range}]");
            return true;
        }
    }

    public class ActionSelectAll : IMoveAction
    {
        public bool Execute(Universe universe)
        {
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Right = universe.World.Width;
            universe.Move.Bottom = universe.World.Height;
            //universe.Print($"Action {this} is started.");
            return true;
        }
    }
    internal class ActionSelectOneUnit : IMoveAction
    {
        private Vehicle unit;

        public ActionSelectOneUnit(Vehicle unit)
        {
            this.unit = unit;
        }

        public bool Execute(Universe universe)
        {
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Right = unit.X + 2;
            universe.Move.Left = unit.X - 2;
            universe.Move.Bottom = unit.Y + 2;
            universe.Move.Top = unit.Y - 2;
            //universe.Print($"Action {this} is started.");
            return true;
        }
    }


    public class ActionAssignSelectionToSquadOld : IMoveAction
    {
        private readonly int squadId;

        public ActionAssignSelectionToSquadOld(int squadId)
        {
            this.squadId = squadId;
        }
        public bool Execute(Universe universe)
        {
            var selectedUnits = universe.GetSelectedUnits();
            if (!selectedUnits.Any())
            {
                universe.Print("Can avoid the movement. Selected unit is absent or dead.");
                return false;
            }
            universe.Move.Action = ActionType.Assign;
            universe.Move.Group = squadId;
            universe.Print($"Action {this} is started. [{selectedUnits.Count}] units were selected.");
            return true;
        }
    }

    //TODO Request squad creation from Execute - to avoid dead unit in not created squad

//    public class ActionAssignSelectionToSquad : IMoveAction
//    {
//        private readonly int squadId;
//        private List<Squad> squadList;
//
//        public ActionAssignSelectionToSquad(int squadId, List<Squad> squadList)
//        {
//            this.squadId = squadId;
//            this.squadList = squadList;
//        }
//        public bool Execute(Universe universe)
//        {
//            var units = universe.GetSelectedUnits().Any();
//            if (universe.GetSelectedUnits().Any())
//            {
//                universe.Move.Action = ActionType.Assign;
//                universe.Move.Group = squadId;
//                //universe.Print($"Action {this} is started.");
//                squadList.Add(new Squad(squadId));
//            }
//            else
//            {
//                universe.Print("Warning! Attempt to assign to group empty selection!");
//            }
//
//
//        }
//    }

    public class ActionMoveSelectionToPosition : IMoveAction
    {
        private readonly AbsolutePosition position;
        private readonly double speed = 1;

        public ActionMoveSelectionToPosition(AbsolutePosition position, double speed = 1)
        {
            this.position = position;
            this.speed = speed;
        }

        public bool Execute(Universe universe)
        {
            var selectedUnits = universe.GetSelectedUnits();
            if (!selectedUnits.Any())
            {
                universe.Print("Can avoid the movement. Selected unit is absent or dead.");
                return false;
            }

            var centralUnit = selectedUnits.GetCentralUnit();
            var selectionCenter = new AbsolutePosition(centralUnit.X, centralUnit.Y);

            if (selectionCenter.GetDistanceToPoint(position.X, position.Y) < 5)
            {
                universe.Print("Can avoid the movement.");
                return false;
            }

            //var moveOrderList = MyStrategy.MoveOrder;

            SquadCalculator.MoveOrders.Update(selectedUnits, centralUnit, position);
            //if (centralUnit.GetDistanceTo(position) > 200)
            //    universe.Print("Wrong move order");

            universe.Move.Action = ActionType.Move;
            universe.Move.X = position.X - selectionCenter.X;
            universe.Move.Y = position.Y - selectionCenter.Y;
            universe.Move.MaxSpeed = speed;
            //universe.Print($"Action {this} is started.");
            return true;
        }


    }

    public class ActionSelectSquad : IMoveAction
    {
        private readonly int squadId;

        public ActionSelectSquad(int squadId)
        {
            this.squadId = squadId;
        }

        public bool Execute(Universe universe)
        {
            var squad = MyStrategy.SquadCalculator.SquadList.GetSquadById(squadId);
            if (squad.IsEmpty)
            {
                universe.Print($"Warning! Squad [{(Squads)squadId}] has no units.");
                return false;
            }
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Group = squadId;
            //universe.Print($"Action {this} is started.");
            return true;
        }
    }

    public class ActionSelectVenicleType : IMoveAction
    {
        private readonly VehicleType type;

        public ActionSelectVenicleType(VehicleType type)
        {
            this.type = type;
        }

        public bool Execute(Universe universe)
        {
            if (universe.GetTypeMyUnits(type).Count == 0)
            {
                universe.Print($"Warning! we have no units of type [{(VehicleType)type}].");
                return false;
            }
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.VehicleType = type;
            universe.Move.Right = universe.World.Width;
            universe.Move.Bottom = universe.World.Height;
            //universe.Print($"Action {this} is started.");
            return true;
        }
    }

    public interface IMoveAction
    {
        bool Execute(Universe universe);
    }
}
