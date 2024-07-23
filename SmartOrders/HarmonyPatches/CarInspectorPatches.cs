// simple way to disable new code :)
//#define SCHEDULER_ENABLED
#if SCHEDULER_ENABLED

namespace SmartOrders.HarmonyPatches;

using System.Diagnostics.CodeAnalysis;
using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using UI.Builder;
using UI.CarInspector;
using UI.EngineControls;

[PublicAPI]
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class CarInspectorPatches {

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "BuildContextualOrders")]
    public static void BuildContextualOrders(UIPanelBuilder builder, AutoEngineerPersistence persistence, Car? ____car) {
        if (!SmartOrdersPlugin.Shared.IsEnabled) {
            return;
        }

        if (____car == null || !____car.IsLocomotive) {
            return; 
        }

        var helper = new AutoEngineerOrdersHelper(____car, persistence);
        var mode = helper.Mode();
        if (mode != AutoEngineerMode.Yard) {
            return;
        }

        builder.AddButton("Scheduler", () => SmartOrdersPlugin.TrackSegmentDialog.ShowWindow((BaseLocomotive)____car));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "Populate")]
    public static void Populate(Car? car) {
        if (!SmartOrdersPlugin.Shared.IsEnabled) {
            return;
        }

        if (car == null) {
            return;
        }

        if (!SmartOrdersPlugin.TrackSegmentDialog.IsShown) {
            return;
        }

        SmartOrdersPlugin.TrackSegmentDialog.CloseWindow();
        if (car.IsLocomotive) {
            SmartOrdersPlugin.TrackSegmentDialog.ShowWindow((BaseLocomotive)car);
        }
    }

}

#endif