using System;
using System.Collections.Generic;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Linq;
using System.Runtime.InteropServices;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk {
    public sealed class MyStrategy : IStrategy
    {
        public List<Vehicle> UnitsMy = new List<Vehicle>();
        public List<Vehicle> UnitsOpp = new List<Vehicle>();

        public Universe Universe { get; set; }

        public MyActionHandler ActionHandler = new MyActionHandler();

        public void Move(Player me, World world, Game game, Move move)
        {
            UpdateUnitsStatus(world);
            Universe = new Universe(world, game, UnitsMy, UnitsOpp, move);
           
            ActionHandler.RunTick(Universe);
        }

        private void UpdateUnitsStatus(World world)
        {
            var playerMy = world.GetMyPlayer();
            var playerOpp = world.GetOpponentPlayer();

            foreach (var venicle in world.NewVehicles)
            {
                var currentVenicleUpdate = world.VehicleUpdates.FirstOrDefault(u => u.Id == venicle.Id);
                if (currentVenicleUpdate == null)
                    currentVenicleUpdate = new VehicleUpdate(
                        venicle.PlayerId, 
                        venicle.X, 
                        venicle.Y, 
                        venicle.Durability, 
                        venicle.RemainingAttackCooldownTicks, 
                        venicle.IsSelected, 
                        venicle.Groups);

                if (venicle.PlayerId == playerMy.Id)
                    UnitsMy.Add(new Vehicle(venicle, currentVenicleUpdate));
                if (venicle.PlayerId == playerOpp.Id)
                    UnitsOpp.Add(new Vehicle(venicle, currentVenicleUpdate));
            }

            ReplaceUnitWithUpdate(world, UnitsMy);
            //foreach (var unit in UnitsMy)
            //{
            //    if (unit.IsSelected)
            //        Console.WriteLine($"Unit {unit.Id}. IsSelected = {unit.IsSelected}");
            //}
            ReplaceUnitWithUpdate(world, UnitsOpp);
        }

        private void ReplaceUnitWithUpdate(World world, List<Vehicle> units)
        {
            foreach (var unit in units.ToList())
                foreach (var update in world.VehicleUpdates)
                    if (unit.Id == update.Id)
                    {
                        var newUnit = new Vehicle(unit, update);
                        units.Remove(unit);
                        units.Add(newUnit);
                       // if (newUnit.IsSelected)
                       //     Console.WriteLine($"Unit {newUnit.Id} is updated. IsSelected = {newUnit.IsSelected}");
                    }
       }
    }


}