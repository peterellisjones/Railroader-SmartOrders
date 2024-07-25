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
using UI.Common;
using UI.EngineControls;
using UnityEngine;

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

        if (____car == null || !____car.IsLocomotive) {
            return;
        }

        UpdateWindowHeight(____car, ____window);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "BuildContextualOrders")]
    public static void BuildContextualOrders(UIPanelBuilder builder, AutoEngineerPersistence persistence, Car ____car, Window ____window) {
        if (!SmartOrdersPlugin.Shared.IsEnabled) {
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

    private static void UpdateWindowHeight(Car car, Window window) {
        var persistence = new AutoEngineerPersistence(car.KeyValueObject);
        var helper = new AutoEngineerOrdersHelper(car, persistence);
        var mode = helper.Mode();

        var size = window.GetContentSize();
        if (mode != AutoEngineerMode.Yard) {
            return;
        }

        window.SetContentSize(new Vector2(size.x - 2, 322 + 70));
    }

}