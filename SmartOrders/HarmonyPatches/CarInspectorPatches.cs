namespace SmartOrders.HarmonyPatches;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using SmartOrders.Extensions;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UI.EngineControls;

[PublicAPI]
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class CarInspectorPatches {

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "PopulatePanel")]
    public static void PopulatePanel(UIPanelBuilder builder, Car? ____car, Window ____window) {
        if (!SmartOrdersPlugin.Shared.IsEnabled) {
            return;
        }

        SmartOrdersUtility.UpdateWindowHeight(____car, ____window);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "BuildContextualOrders")]
    public static void BuildContextualOrders(UIPanelBuilder builder, AutoEngineerPersistence persistence, Car ____car) {
        if (!SmartOrdersPlugin.Shared.IsEnabled) {
            return;
        }

        var locomotive = (BaseLocomotive)____car;
        var helper = new AutoEngineerOrdersHelper(locomotive, persistence);
        var mode2 = helper.Mode();

        if (mode2 != AutoEngineerMode.Yard) {
            return;
        }

        builder.AddField("Switches",
            builder.ButtonStrip(
                strip => {
                    strip.AddButton("Approach Ahead", () => SmartOrdersUtility.Move(helper, 1, false, true, locomotive, persistence))!
                        .Tooltip("Approach Ahead", "Approach, but do not pass, the first switch found in front of the train. Switches under the train are ignored.");

                    strip.AddButton("Clear Ahead", () => SmartOrdersUtility.Move(helper, 1, false, false, locomotive, persistence))!
                        .Tooltip("Clear Ahead", "Clear the first switch found in front of the train. Switches under the train are ignored.");
                }, 4)!
        )!.Tooltip("AI move to switch control",
            "Use Approach Ahead and Clear Ahead to approach or clear the first switch IN FRONT of the train. Switches under the train are ignored.\n" +
            "Use the 1, 2, 3 etc buttons to move the train past switches in the direction travel starting from the BACK of the train. " +
            "This includes switches that are currently UNDER the train as well as switches in front of the train");

        builder.AddField("",
            builder.ButtonStrip(
                strip => {
                    strip.AddButton("1", () => SmartOrdersUtility.Move(helper, 1, true, false, locomotive, persistence))!
                        .Tooltip("Clear 1 switch", "Clear the next switch from the back of the train in the direction of travel");

                    for (var i = 2; i <= 10; i++) {
                        var numSwitches = i;
                        strip.AddButton($"{numSwitches}", () => SmartOrdersUtility.Move(helper, numSwitches, true, false, locomotive, persistence))!
                            .Tooltip($"Clear {numSwitches} switches", $"Clear the next {numSwitches} switches from the back of the train in the direction of travel");
                    }
                }, 4)!
        );

        builder.AddField("",
            builder.ButtonStrip(strip => {
                var cars = locomotive.EnumerateCoupled()!.ToList()!;

                if (cars.Any(c => c.air!.handbrakeApplied)) {
                    strip.AddButton($"Release {TextSprites.HandbrakeWheel}", () => SmartOrdersUtility.ReleaseAllHandbrakes(cars))!
                        .Tooltip("Release handbrakes", $"Iterates over cars in this consist and releases {TextSprites.HandbrakeWheel}.");
                }

                if (cars.Any(c => c.EndAirSystemIssue())) {
                    strip.AddButton("Connect Air", () => SmartOrdersUtility.ConnectAir(cars))!
                        .Tooltip("Connect Consist Air", "Iterates over each car in this consist and connects gladhands and opens anglecocks.");
                }
            })!
        );
    }

}