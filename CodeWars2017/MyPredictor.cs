using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public class Predictor
    {
        public SortedList<int, WorldState> WorldStateList { get; internal set; } = new SortedList<int, WorldState>();
        public Universe Universe;
        internal void RunTick(Universe universe)
        {
            Universe = universe;

            //clear all predictions
            foreach (var state in new SortedList<int, WorldState>(WorldStateList))
                if (!state.Value.IsRealValue)
                    WorldStateList.Remove(state.Key);

            //clear all old reals state values
            foreach (var state in new SortedList<int, WorldState> (WorldStateList))
                if (state.Key < universe.World.TickIndex - 5)
                    WorldStateList.Remove(state.Key);

            WorldStateList.Add(universe.World.TickIndex, new WorldState(new List<Vehicle>(universe.MyUnits), new List<Vehicle>(universe.OppUnits), true));

        }

//        public WorldState Prediction() =>
//            GetStateOnTick(Universe.World.TickIndex + MyStrategy.SquadCalculator.ExpectedTicksToNextUpdate);

        public WorldState GetStateOnTick(int tick)
        {
            foreach (var state in WorldStateList)
                if (state.Key == tick)
                    return WorldStateList[tick];

            WorldStateList.Add(tick, IntegrateTillTick(tick));
            return WorldStateList[tick];
        }

        private WorldState IntegrateTillTick(int tick)
        {
            var predictedOppUnits = new List<Vehicle>();
            var predictedMyUnits = new List<Vehicle>();
            var myPlayerId = Universe.World.GetMyPlayer().Id;

            var allRealUnits = Universe.OppUnits.GetCombinedList(Universe.MyUnits);

            foreach (var unit in allRealUnits)
            {
                var unitSpeed = CalculateUnitSpeed(unit);

                var predictedX = unit.X + unitSpeed.SpeedX * (tick - Universe.World.TickIndex);
                var predictedY = unit.Y + unitSpeed.SpeedY * (tick - Universe.World.TickIndex);

                var predictedUnit = new Vehicle(unit,
                    new VehicleUpdate(unit.Id, predictedX, predictedY, unit.Durability,
                        unit.RemainingAttackCooldownTicks, unit.IsSelected, unit.Groups));

                if (unit.PlayerId == myPlayerId)
                    predictedMyUnits.Add(predictedUnit);
                else 
                    predictedOppUnits.Add(predictedUnit);
            }

            return new WorldState(predictedMyUnits, predictedOppUnits, false);
        }

        private Speed CalculateUnitSpeed(Vehicle unit)
        {
            var unitSpeed = new Speed(0, 0);
            if (WorldStateList.LastOrDefault().Value == null)
                return unitSpeed;
            
            var stateOld = WorldStateList.LastOrDefault(s => s.Value.IsRealValue && s.Key < Universe.World.TickIndex - 2);

            if (stateOld.Value == null)
                return unitSpeed;

            var allOldUnits = stateOld.Value.OppUnits.GetCombinedList(stateOld.Value.MyUnits);

            var unitOld = allOldUnits.FirstOrDefault(u => u.Id.Equals(unit.Id));

            if (unitOld == null)
                return unitSpeed;

            var ticksBetweenStates = Universe.World.TickIndex - stateOld.Key;

            if (ticksBetweenStates > 0)
                unitSpeed = new Speed((unit.X - unitOld.X) / ticksBetweenStates,
                    (unit.Y - unitOld.Y) / ticksBetweenStates);
            else
                unitSpeed = new Speed(0, 0);

            return unitSpeed;
        }
    }

    public class WorldState
    {
        public WorldState(List<Vehicle> myUnits, List<Vehicle> oppUnits, bool isRealValue)
        {
            OppUnits = oppUnits;
            MyUnits = myUnits;
            IsRealValue = isRealValue;
        }

        public List<Vehicle> MyUnits { get; set; }
        public List<Vehicle> OppUnits { get; set; }
        public bool IsRealValue { get; set; }
    }

    public class Speed
    {
        public Speed(double speedX, double speedY)
        {
            this.SpeedX = speedX;
            this.SpeedY = speedY;
        }

        public double SpeedX { get; set; }
        public double SpeedY { get; set; }

    }
}
