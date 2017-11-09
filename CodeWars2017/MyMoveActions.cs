using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{

    public static class MoveList
    {
        //selection
        public static void ActionSelectAll(this Queue<IMoveAction> moveActions) =>
            moveActions.Enqueue(new ActionSelectAll());

        public static void ActionSelectVenicleType(this Queue<IMoveAction> moveActions, VehicleType type) =>
            moveActions.Enqueue(new ActionSelectVenicleType(type));

        public static void ActionSelectSquad(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionSelectSquad(squadId));



        //Assignment
        public static void ActionAssignSelectionToSquad(this Queue<IMoveAction> moveActions, int squadId) =>
            moveActions.Enqueue(new ActionAssignSelectionToSquad(squadId));

        //Movement
        public static void ActionMoveSelectionToPosition(this Queue<IMoveAction> moveActions, AbsolutePosition position) =>
            moveActions.Enqueue(new ActionMoveSelectionToPosition(position));
    }

    public class ActionSelectAll : IMoveAction
    {
        public void Execute(Universe universe)
        {
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Right = universe.World.Width;
            universe.Move.Bottom = universe.World.Height;
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
            var selectionCenter = universe.GetSelectionCenter();
            //Console.WriteLine($"Selection Center {selectionCenter.X}, {selectionCenter.Y}");
            universe.Move.X = position.X - selectionCenter.X;
            universe.Move.Y = position.Y - selectionCenter.Y;
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
            universe.GetSquadUnits(squadId);
            if (universe.GetSquadUnits(squadId).Count == 0)
            {
                Console.WriteLine($"Warning! Squad [{(Squads)squadId}] has no units.");
                universe.Move.Action = ActionType.None;
                return;
            }
            universe.Move.Action = ActionType.ClearAndSelect;
            universe.Move.Group = squadId;
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
        }
    }

    //if (world.TickIndex == 0)
    //    ActionSelectAll();
    //
    //if (world.TickIndex == 1)
    //    ActionAssignSelectionToSquad(Squads.All);
    //
    //if (world.TickIndex == 2)
    //    ActionMoveSquadToPosition((int) Squads.All, MapConerLeftLower);
    //
    //if (world.TickIndex == 3)
    //    ActionSelectVenicleType(VehicleType.Tank);
    //
    //if (world.TickIndex == 4)
    //    ActionAssignSelectionToSquad(Squads.Tanks);
    //
    //if (world.TickIndex == 5)
    //    ActionMoveSquadToPosition((int) Squads.Tanks, MapCenter);
    //return;


    //if (world.TickIndex == 0) {
    //    move.Action = ActionType.ClearAndSelect;
    //    move.Right = world.Width;
    //    move.Bottom = world.Height;
    //    return;
    //}
    //
    //if (world.TickIndex == 1) {
    //    move.Action = ActionType.Move;
    //    move.X = world.Width / 2.0D;
    //    move.Y = world.Height / 2.0D;
    //    return;
    //}

    public interface IMoveAction
    {
        void Execute(Universe universe);
    }
}
