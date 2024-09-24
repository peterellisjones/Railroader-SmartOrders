namespace SmartOrders.HarmonyPatches;

using System;
using System.Diagnostics.CodeAnalysis;
using Game.Messages;
using HarmonyLib;
using Model.AI;
using Track;
using UI.EngineControls;

[HarmonyPatch]
public static class AutoEngineerOrdersExtensionsPatches
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AutoEngineerOrdersExtensions), "MaxSpeedMph")]
    public static void MaxSpeedMph(this AutoEngineerMode mode, ref int __result)
    {
        __result = mode switch
        {
            AutoEngineerMode.Off => 0,
            AutoEngineerMode.Road => 45,
            AutoEngineerMode.Yard => SmartOrdersPlugin.Settings.NoYardSpeedLimit ? 45 : 15,
            _ => throw new ArgumentOutOfRangeException("mode", mode, null),
        };
    }
}