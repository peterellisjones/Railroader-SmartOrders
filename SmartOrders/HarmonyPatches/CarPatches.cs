namespace SmartOrders.HarmonyPatches;

using System;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using UnityEngine;
using static Model.Car;

[UsedImplicitly]
[HarmonyPatch]
public static class CarPatches {

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Car), "KeyValueKeyFor")]
    public static string KeyValueKeyFor(Car.EndGearStateKey key, Car.End end) {
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
        if (__instance.EnumerateCoupled(otherEnd).Any((car) => car.IsLocomotive || car.air.handbrakeApplied))
        {
            return;
        }

        SmartOrdersUtility.DebugLog($"Automatically setting handbrake for {__instance.DisplayName}");

        __instance.SetHandbrake(true);
    }

}