namespace SmartOrders.HarmonyPatches;

using System;
using System.Linq;
using System.Reflection;
using Core;
using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.Definition;
using UnityEngine;
using static Model.Car;

[UsedImplicitly]
[HarmonyPatch]
public static class CarPatches
{

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Car), "KeyValueKeyFor")]
    public static string KeyValueKeyFor(Car.EndGearStateKey key, Car.End end)
    {
        throw new NotImplementedException("It's a stub");
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(Car), "ApplyEndGearChange")]
    [HarmonyPatch(new Type[] { typeof(LogicalEnd), typeof(EndGearStateKey), typeof(bool) })]
    public static void ApplyEndGearChange(LogicalEnd logicalEnd, EndGearStateKey endGearStateKey, bool boolValue, Car __instance)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }

        if (!SmartOrdersPlugin.Settings.AutoApplyHandbrake)
        {
            return;
        }

        if (endGearStateKey != EndGearStateKey.IsCoupled)
        {
            return;
        }

        if (boolValue != false)
        {
            return;
        }

        // dont auto set handbrake if moving
        if (__instance.VelocityMphAbs > 0)
        {
            return;
        }

        LogicalEnd otherEnd = logicalEnd == LogicalEnd.A ? LogicalEnd.B : LogicalEnd.A;

        // dont auto set handbrake if there is a loco in the consist
        // dont auto set handbrake if there is another car with the handbrake applied
        if (__instance.EnumerateCoupled(otherEnd).Any((car) => car.IsLocomotive || car.Archetype == CarArchetype.Tender || car.air.handbrakeApplied))
        {
            return;
        }


        var cars = __instance.EnumerateCoupled(otherEnd).ToList();
        int numHanbrakesRequired = (int)typeof(TrainController).GetMethod("CalculateNumHandbrakes", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { cars, 1, 3 });

        SmartOrdersUtility.DebugLog($"Applying handbrakes on {numHanbrakesRequired} {"car".Pluralize(numHanbrakesRequired)} starting with {__instance.DisplayName}");

        cars.Reverse();

        cars.Take(numHanbrakesRequired).Do((car) => car.SetHandbrake(true));
    }

}