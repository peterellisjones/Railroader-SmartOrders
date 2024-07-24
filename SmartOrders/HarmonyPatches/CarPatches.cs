namespace SmartOrders.HarmonyPatches;

using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Track;

[UsedImplicitly]
[HarmonyPatch]
public static class CarPatches {

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Car), "KeyValueKeyFor")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static string KeyValueKeyFor(Car.EndGearStateKey key, Car.End end) {
        throw new NotImplementedException("It's a stub");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Car), "ApplyEndGearChange", typeof(Car.LogicalEnd), typeof(Car.EndGearStateKey), typeof(bool))]
    public static void ApplyEndGearChange(Car.LogicalEnd logicalEnd, Car.EndGearStateKey endGearStateKey, bool boolValue) {
        if (endGearStateKey != Car.EndGearStateKey.IsCoupled) {
            return;
        }


        UI.Console.Console.shared.AddLine("Car:ApplyEndGearChange" + logicalEnd + " | " + endGearStateKey + " | " + boolValue);

    }

}