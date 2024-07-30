namespace SmartOrders.HarmonyPatches;

using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using System;
using System.Collections.Generic;
using Track;
using UI.Builder;
using UnityEngine;

[PublicAPI]
[HarmonyPatch]
public static class AutoEngineerPlannerPatches
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AutoEngineerPlanner), "Search")]
    private static void Search(BaseLocomotive locomotive, ref bool stopBeforeCar)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }


        bool allowCouplingInRoadMode = locomotive.KeyValueObject.Get("ALLOW_COUPLING_IN_ROAD_MODE").BoolValue;

        if (allowCouplingInRoadMode && stopBeforeCar)
        {
            stopBeforeCar = false;
        }
    }

}