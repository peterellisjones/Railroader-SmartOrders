namespace SmartOrders.HarmonyPatches;

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

        builder.AddField("", builder.ButtonStrip(strip => {
            var consist = ____car.EnumerateCoupled()!.ToArray();

            if (consist.Any(c => c.air!.handbrakeApplied)) {
                strip.AddButton($"Release {TextSprites.HandbrakeWheel}", () => Jobs.ReleaseAllHandbrakes(consist))!
                    .Tooltip("Release handbrakes", $"Iterates over cars in this consist and releases {TextSprites.HandbrakeWheel}.");
            }

            if (consist.Any(c => c.EndAirSystemIssue())) {
                strip.AddButton("Connect Air", () => Jobs.ConnectAir(consist))!
                    .Tooltip("Connect Consist Air", "Iterates over each car in this consist and connects gladhands and opens anglecocks.");
            }

        })!);
    }

}