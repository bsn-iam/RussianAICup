using System;
using System.Collections.Generic;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Linq;


namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk {
    public sealed class MyStrategy : IStrategy
    {
        public List<Vehicle> UnitsMy = new List<Vehicle>();
        public List<Vehicle> UnitsOpp = new List<Vehicle>();

        public Universe Universe { get; set; }
        public ActionHandler ActionHandler = new ActionHandler();
        public SquadCalculator SquadCalculator = new SquadCalculator();


        public void Move(Player me, World world, Game game, Move move)
        {
            UpdateUnitsStatus(world);
            Universe = new Universe(world, game, UnitsMy, UnitsOpp, move);

            SquadCalculator.RunTick(Universe);

            ActionHandler.RunTick(Universe, SquadCalculator.ActionList);
            
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
            foreach (var unit in UnitsMy)
            {
                if (unit.X < 0.1) throw new Exception("0 coordinate! Dead warrior!");
            }
            ReplaceUnitWithUpdate(world, UnitsOpp);
            foreach (var unit in UnitsOpp)
            {
                if (unit.X < 0.1) throw new Exception("0 coordinate! Dead enemy in the list.");
            }
        }

        private void ReplaceUnitWithUpdate(World world, List<Vehicle> units)
        {
            foreach (var unit in units.ToList())
                foreach (var update in world.VehicleUpdates)
                    if (unit.Id == update.Id)
                    {
                        var newUnit = new Vehicle(unit, update);
                        units.Remove(unit);
                        if (update.Durability!=0) //Note: Dead or not visible units are removed from the list! 
                            units.Add(newUnit);

                        // TODO if (UnitAliveButNotVisible) units.Add(newUnit);
                        // TODO For dead units position is 0, for for the hidden ones?
                    }
        }
    }


}