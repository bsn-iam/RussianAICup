﻿using System;
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

        public static void ActionSelectInRange(this Queue<IMoveAction> moveActions, Range range) =>
            moveActions.Enqueue(new ActionSelectInRange(range));

        //Assignment
        public static void ActionAssignSelectionToSquad(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionAssignSelectionToSquadOld(squadId));

        #endregion

        #region Grouping
        public static void ActionAddSquadToCurrentSelection(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionAddSquadToCurrentSelection(squadId));

        public static void ActionCombineSquads(this Queue<IMoveAction> moveActions, List<Squad> squadList, Squad squadAlfa, Squad squadDelta, int newSquadId, bool disableOld = true)
        {
            moveActions.ActionSelectSquad(squadAlfa.Id);
            moveActions.ActionAddSquadToCurrentSelection(squadDelta.Id);
            moveActions.ActionCreateNewSquadAlreadySelected(squadList, newSquadId);

            if (disableOld)
            {
                squadAlfa.Disable();
                squadDelta.Disable();
            }
            else
            {
                squadList.GetSquadById(newSquadId).Disable();
            }
        }

        public static void ActionCombineSquads(this Queue<IMoveAction> moveActions, List<Squad> squadList, int squadAlfaId, int squadDeltaId,
            int newSquadId, bool disableOld = true) => 
            moveActions.ActionCombineSquads(squadList, squadList.GetSquadById(squadAlfaId), squadList.GetSquadById(squadDeltaId), newSquadId, disableOld);

        public static void ActionAppointNewScout(this Queue<IMoveAction> moveActions, List<Squad> squadList,
            IdGenerator squadIdGenerator)
        {
            if (squadIdGenerator.HasCapacity)
            {
                const VehicleType type = VehicleType.Fighter;
                var scoutOwner = squadList.FirstOrDefault(s => s.Units.Any(u => u.Type.Equals(type)));

                if (scoutOwner == null)
                    return;

                var candidateList = scoutOwner.Units.Where(u => u.Type == type).ToList();
                var scout = candidateList.GetMostDistantUnit();

                moveActions.Enqueue(new ActionSelectOneUnit(scout));
                foreach (var groupId in scout.Groups)
                    moveActions.Enqueue(new ActionDismissSelectionFromSquad(groupId));
                var scoutSquadId = squadIdGenerator.New;

                moveActions.ActionCreateNewSquadAlreadySelected(squadList, scoutSquadId);
                squadList.GetSquadById(scoutSquadId).IsScout = true;

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
        private readonly Range range;

        public ActionSelectInRange(Range range)
        {
            this.range = range;
        }

        public bool Execute(Universe universe)
        {
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Right = range.XRight;
            universe.Move.Left = range.XLeft;
            universe.Move.Top = range.YTop;
            universe.Move.Bottom = range.YBottom;
            //universe.Print($"Action {this} is started.");
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
            universe.Move.Action = ActionType.Assign;
            universe.Move.Group = squadId;
            //universe.Print($"Action {this} is started.");
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
        private readonly double speed = 5;

        public ActionMoveSelectionToPosition(AbsolutePosition position)
        {
            this.position = position;
        }

        public ActionMoveSelectionToPosition(AbsolutePosition position, double speed) : this(position)
        {
            this.position = position;
            this.speed = speed;
        }

        public bool Execute(Universe universe)
        {
            var selectionCenter = universe.GetSelectionCenter();
            if (selectionCenter.GetDistanceToPoint(position.X, position.Y) < 5)
            {
                universe.Print("Can avoid the movement.");
                return false;
            }


            var selectedUnits = universe.GetSelectedUnits();
            if (!selectedUnits.Any())
            {
                universe.Print("Can avoid the movement. Selected unit is absent or dead.");
                return false;
            }

            var centralUnit = selectedUnits.GetCentralUnit();
            var moveOrderList = MyStrategy.MoveOrder;

            foreach (var unit in selectedUnits)
                foreach (var moveOrder in new SortedList<long, AbsolutePosition>(moveOrderList))
                {
                    if (moveOrder.Key == unit.Id)
                        moveOrderList.Remove(moveOrder.Key);
                }
            moveOrderList.Add(centralUnit.Id, position);

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
            if (universe.GetSquadUnits(squadId).Count == 0)
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
