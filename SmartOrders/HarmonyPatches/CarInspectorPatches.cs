// simple way to disable new code :)
#define SCHEDULER_ENABLED
#if SCHEDULER_ENABLED

namespace SmartOrders.HarmonyPatches;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using SmartOrders.AI;
using SmartOrders.Extensions;
using UI.Builder;
using UI.CarInspector;
using UI.EngineControls;
using static Model.Car;

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

        builder.ButtonStrip(strip => {
            var consist = ____car.EnumerateCoupled()!.ToArray();

            if (consist.Any(c => c.air!.handbrakeApplied)) {
                strip.AddButtonCompact($"Release", () => Jobs.ReleaseAllHandbrakes(consist))!
                    .Tooltip($"Release handbrakes", $"Iterates over cars in this consist and releases {TextSprites.HandbrakeWheel}.");
            }

            strip.AddPopupMenu($"Set handbrake on car:",
                consist.Select((o, i) => new PopupMenuItem($"#{i + 1} ({o.name})", () => Jobs.SetHandbrake(consist, i)))
            );

            strip.AddButtonCompact("Connect Air", () => Jobs.ConnectAir(consist))!
                .Tooltip("Connect Consist Air", "Iterates over each car in this consist and connects gladhands and opens anglecocks.");

            strip.AddButtonCompact("Scheduler", () => SmartOrdersPlugin.TrackSegmentDialog.ShowWindow((BaseLocomotive)____car));
        });
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