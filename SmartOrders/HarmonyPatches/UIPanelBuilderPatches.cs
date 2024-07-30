namespace SmartOrders.HarmonyPatches;

using HarmonyLib;
using JetBrains.Annotations;
using System;
using UI.Builder;
using UnityEngine;

[PublicAPI]
[HarmonyPatch]
public static class UIPanelBuilderPatches
{
    static bool cancelNextButtonStrip = false;
    static bool cancelNextExpandingVerticalSpacer = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIPanelBuilder), "AddField")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(RectTransform) })]
    public static bool AddField(string label, RectTransform control)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return true;
        }

        cancelNextButtonStrip = label == "Direction";
        cancelNextExpandingVerticalSpacer = label == "Car Lengths";

        // Don't let the base game add the car lengths buttons, because this mod has another implementation
        return label != "Car Lengths";
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIPanelBuilder), "ButtonStrip")]
    public static bool ButtonStrip(Action<UIPanelBuilder> closure, int spacing = 8)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return true;
        }

        if (cancelNextButtonStrip)
        {
            // Don't let the base game add the car lengths buttons, because this mod has another implementation
            cancelNextButtonStrip = false;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIPanelBuilder), "AddExpandingVerticalSpacer")]
    public static bool AddExpandingVerticalSpacer()
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return true;
        }

        if (cancelNextExpandingVerticalSpacer)
        {
            cancelNextExpandingVerticalSpacer = false;
            return false;
        }

        return true;
    }
}