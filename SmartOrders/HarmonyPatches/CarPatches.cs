namespace SmartOrders.HarmonyPatches;

using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using Model;

[UsedImplicitly]
[HarmonyPatch]
public static class CarPatches {

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Car), "KeyValueKeyFor")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static string KeyValueKeyFor(Car.EndGearStateKey key, Car.End end) {
        throw new NotImplementedException("It's a stub");
    }

}