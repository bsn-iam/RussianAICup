using System;
using System.Collections.Generic;
using System.Linq;
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
            moveActions.Enqueue(new ActionAssignSelectionToSquad(squadId));

        #endregion

        #region Grouping
        public static void ActionAddSquadToCurrentSelection(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionAddSquadToCurrentSelection(squadId));

        public static void ActionCombineSquads(this Queue<IMoveAction> moveActions, List<Squad> squadList, Squad squadAlfa, Squad squadDelta, int newSquadId)
        {
            moveActions.ActionSelectSquad(squadAlfa.Id);
            moveActions.ActionAddSquadToCurrentSelection(squadDelta.Id);
            moveActions.ActionCreateNewSquadAlreadySelected(squadList, newSquadId);

            squadAlfa.Disable();
            squadDelta.Disable();
        }

        public static void ActionCombineSquads(this Queue<IMoveAction> moveActions, List<Squad> squadList, int squadAlfaId, int squadDeltaId,
            int newSquadId) => moveActions.ActionCombineSquads(squadList, squadList.GetSquadById(squadAlfaId), squadList.GetSquadById(squadDeltaId), newSquadId);

        #endregion

        #region Movement

        //public static void ActionScaleSelectionToPosition(this Queue<IMoveAction> moveActions, double factor, AbsolutePosition position) =>
        //    moveActions.Enqueue(new ActionScaleSelectedSquadToPosition(squad, factor, position));

        public static void ActionMoveSelectionToPosition(this Queue<IMoveAction> moveActions, AbsolutePosition position) =>
            moveActions.Enqueue(new ActionMoveSelectionToPosition(position));


        public static void ActionMoveToOnePoint(this Queue<IMoveAction> actions, List<Squad> squadList, int squadAlfaId,
            int squadDeltaId) =>
            actions.Enqueue(new ActionMoveToOnePoint(actions, squadList, squadAlfaId, squadDeltaId));


        #endregion

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

        public void Execute(Universe universe)
        {
//            if (squad.Id == 11)
//                throw new Exception();
            universe.Move.Action = ActionType.Scale;
            universe.Move.X = position.X;
            universe.Move.Y = position.Y;
            universe.Move.Factor = factor;
            squad.ScalingTimeDelay = duration;
            squad.IsWaitingForScaling = false;

#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
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

        public void Execute(Universe universe)
        {
            squadList.GetMeetingPoint(squadAlfaId, squadDeltaId);

            var meetingPoint = squadList.GetMeetingPoint(squadAlfaId, squadDeltaId);

            actions.ActionSelectSquad(squadAlfaId);
            actions.ActionMoveSelectionToPosition(meetingPoint);

            actions.ActionSelectSquad(squadDeltaId);
            actions.ActionMoveSelectionToPosition(meetingPoint);
        }
    }

    internal class ActionAddSquadToCurrentSelection : IMoveAction
    {
        private readonly int squadId;

        public ActionAddSquadToCurrentSelection(int squadId)
        {
            this.squadId = squadId;
        }

        public void Execute(Universe universe)
        {
            universe.Move.Action = ActionType.AddToSelection;
            universe.Move.Group = squadId;
#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
        }
    }

    internal class ActionSelectInRange : IMoveAction
    {
        private readonly Range range;

        public ActionSelectInRange(Range range)
        {
            this.range = range;
        }

        public void Execute(Universe universe)
        {
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Right = range.XRight;
            universe.Move.Left = range.XLeft;
            universe.Move.Top = range.YTop;
            universe.Move.Bottom = range.YBottom;
#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
        }
    }

    public class ActionSelectAll : IMoveAction
    {
        public void Execute(Universe universe)
        {
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Right = universe.World.Width;
            universe.Move.Bottom = universe.World.Height;
#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
        }
    }

    public class ActionAssignSelectionToSquad : IMoveAction
    {
        private readonly int squadId;

        public ActionAssignSelectionToSquad(int squadId)
        {
            this.squadId = squadId;
        }
        public void Execute(Universe universe)
        {
            universe.Move.Action = ActionType.Assign;
            universe.Move.Group = squadId;
#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
        }
    }

    public class ActionMoveSelectionToPosition : IMoveAction
    {
        private readonly AbsolutePosition position;

        public ActionMoveSelectionToPosition(AbsolutePosition position)
        {
            this.position = position;
        }
        public void Execute(Universe universe)
        {
            universe.Move.Action = ActionType.Move;
            universe.Move.MaxSpeed = universe.GetSpeedForSelection();
            var selectionCenter = universe.GetSelectionCenter();
            //Console.WriteLine($"Selection Center {selectionCenter.X}, {selectionCenter.Y}");
            universe.Move.X = position.X - selectionCenter.X;
            universe.Move.Y = position.Y - selectionCenter.Y;
#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
        }
    }

    public class ActionSelectSquad : IMoveAction
    {
        private readonly int squadId;

        public ActionSelectSquad(int squadId)
        {
            this.squadId = squadId;
        }

        public void Execute(Universe universe)
        {
            if (universe.GetSquadUnits(squadId).Count == 0)
            {
                Console.WriteLine($"Warning! Squad [{(Squads)squadId}] has no units.");
                universe.Move.Action = ActionType.None;
                return;
            }
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Group = squadId;
#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
        }
    }

    public class ActionSelectVenicleType : IMoveAction
    {
        private readonly VehicleType type;

        public ActionSelectVenicleType(VehicleType type)
        {
            this.type = type;
        }

        public void Execute(Universe universe)
        {
            if (universe.GetTypeMyUnits(type).Count == 0)
            {
                Console.WriteLine($"Warning! we have no units of type [{(VehicleType)type}].");
                universe.Move.Action = ActionType.None;
                return;
            }
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.VehicleType = type;
            universe.Move.Right = universe.World.Width;
            universe.Move.Bottom = universe.World.Height;
#if DEBUG
            Console.WriteLine($"{universe.World.TickIndex}.Action {this} is started.");
#endif
        }
    }

    public interface IMoveAction
    {
        void Execute(Universe universe);
    }
}
