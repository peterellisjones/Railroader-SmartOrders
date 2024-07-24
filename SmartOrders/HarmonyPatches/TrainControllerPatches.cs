namespace SmartOrders.HarmonyPatches;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Game.Messages;
using HarmonyLib;
using Model;

[HarmonyPatch]
public static class TrainControllerPatches {

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(TrainController), "ApplyHandbrakesAsNeeded")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static void ApplyHandbrakesAsNeeded(this TrainController instance, List<Car> cars, PlaceTrainHandbrakes handbrakes) {
        throw new NotImplementedException("It's a stub");
    }

}