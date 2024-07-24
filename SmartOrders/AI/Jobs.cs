﻿namespace SmartOrders.AI;

using Game.Messages;
using Game.State;
using HarmonyLib;
using Model;
using SmartOrders.HarmonyPatches;

public static class Jobs {

    public static void ReleaseAllHandbrakes(Car[] consist) {
        consist.Do(c => c.SetHandbrake(false));

    }
    
    public static void ConnectAir(Car[] consist) {
        foreach (var car in consist) {
            ConnectAirCore(car, Car.LogicalEnd.A);
            ConnectAirCore(car, Car.LogicalEnd.B);
        }

        return;

        static void ConnectAirCore(Car car, Car.LogicalEnd end) {
            StateManager.ApplyLocal(new PropertyChange(car.id, CarPatches.KeyValueKeyFor(Car.EndGearStateKey.Anglecock, car.LogicalToEnd(end)), new FloatPropertyValue(car[end].IsCoupled ? 1f : 0f)));

            if (car.TryGetAdjacentCar(end, out var car2)) {
                StateManager.ApplyLocal(new SetGladhandsConnected(car.id, car2.id, true));
            }
        }
    }

}