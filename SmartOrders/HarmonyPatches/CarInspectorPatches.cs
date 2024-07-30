namespace SmartOrders.HarmonyPatches;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
using UnityEngine;
using UnityEngine.Rendering;

[PublicAPI]
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class CarInspectorPatches
{

    const float ADDITIONAL_WINDOW_HEIGHT_NEEDED = 95;
    private static Vector2? originalWindowSize;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarInspector), "BuildContextualOrders")]
    public static void BuildContextualOrders(UIPanelBuilder builder, AutoEngineerPersistence persistence, Car ____car, Window ____window)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }
        var locomotive = (BaseLocomotive)____car;
        var helper = new AutoEngineerOrdersHelper(locomotive, persistence);
        var mode2 = helper.Mode();

        if (mode2 == AutoEngineerMode.Yard)
        {
            if (originalWindowSize == null)
            {
                originalWindowSize = ____window.GetContentSize();
            }

            ____window.SetContentSize(new Vector2(originalWindowSize.Value.x - 2, 400));

            BuildAlternateCarLengthsButtons(builder, locomotive, helper);
            BuildSwitchYardAIButtons(builder, locomotive, persistence, helper);

            builder.AddExpandingVerticalSpacer();
        }

        if (mode2 == AutoEngineerMode.Road)
        {
            // Ensure default value for allow coupling in road mode is false;
            if (locomotive.KeyValueObject.Get("ALLOW_COUPLING_IN_ROAD_MODE").IsNull)
            {
                locomotive.KeyValueObject.Set("ALLOW_COUPLING_IN_ROAD_MODE", false);
            }

            builder.AddField("Allow coupling", builder.AddToggle(() =>
            {
                return locomotive.KeyValueObject.Get("ALLOW_COUPLING_IN_ROAD_MODE").BoolValue;
            }, delegate (bool enabled)
            {
                locomotive.KeyValueObject.Set("ALLOW_COUPLING_IN_ROAD_MODE", enabled);
            })).Tooltip("Allow coupling with cars in front", "If enabled the AI will couple to cars in front. If disabled the AI will stop before cars in front");
        }
    }

    private static void BuildAlternateCarLengthsButtons(UIPanelBuilder builder, BaseLocomotive locomotive, AutoEngineerOrdersHelper helper)
    {
        builder.AddField("CarLengths", builder.ButtonStrip(delegate (UIPanelBuilder builder)
        {
            builder.AddButton("Stop", delegate
            {
                MoveDistance(helper, locomotive, 0f);
            });
            builder.AddButton("½", delegate
            {
                MoveDistance(helper,locomotive, 6.1f);
            });
            builder.AddButton("1", delegate
            {
                MoveDistance(helper,locomotive, 12.2f);
            });
            builder.AddButton("2", delegate
            {
                MoveDistance(helper,locomotive, 24.4f);
            });
            builder.AddButton("5", delegate
            {
                MoveDistance(helper,locomotive, 61f);
            });
            builder.AddButton("10", delegate
            {
                MoveDistance(helper,locomotive, 122f);
            });
            builder.AddButton("20", delegate
            {
                MoveDistance(helper,locomotive, 244f);
            });
            builder.AddButton("inf", delegate
            {
                MoveDistance(helper,locomotive, 12.192f * 1_000_000.5f);
            }).Tooltip("INF", "Move infinity car lengths");
        }, 4));
    }

    private static void BuildSwitchYardAIButtons(UIPanelBuilder builder, BaseLocomotive locomotive, AutoEngineerPersistence persistence, AutoEngineerOrdersHelper helper)
    {
        Func<BaseLocomotive, string> getClearSwitchMode = (BaseLocomotive loco) =>
        {
            string defaultMode = "CLEAR_AHEAD";
            var clearSwitchMode = loco.KeyValueObject.Get("CLEAR_SWITCH_MODE");
            if (clearSwitchMode.IsNull)
            {
                return defaultMode;
            }

            return clearSwitchMode;
        };

        builder.AddField("Switches", builder.ButtonStrip(delegate (UIPanelBuilder builder)
        {
            builder.AddObserver(locomotive.KeyValueObject.Observe("CLEAR_SWITCH_MODE", delegate
            {
                builder.Rebuild();
            }, callInitial: false));

            builder.AddButtonSelectable("Approach\nAhead", getClearSwitchMode(locomotive) == "APPROACH_AHEAD", delegate
            {
                SmartOrdersUtility.DebugLog("updating switch mode to 'approach ahead'");
                locomotive.KeyValueObject.Set("CLEAR_SWITCH_MODE", "APPROACH_AHEAD");
            }).Height(60).Tooltip("Approach Ahead", "Approach but do not pass switches in front of the train. Choose the number of switches below");

            builder.AddObserver(locomotive.KeyValueObject.Observe("CLEAR_SWITCH_MODE", delegate
            {
                builder.Rebuild();
            }, callInitial: false));

            builder.AddButtonSelectable("Clear\nAhead", getClearSwitchMode(locomotive) == "CLEAR_AHEAD", delegate
            {
                SmartOrdersUtility.DebugLog("updating switch mode to 'clear ahead'");
                locomotive.KeyValueObject.Set("CLEAR_SWITCH_MODE", "CLEAR_AHEAD");
            }).Height(60).Tooltip("Clear Ahead", "Clear switches in front of the train. Choose the number of switches below");

            builder.AddButtonSelectable("Clear\nUnder", getClearSwitchMode(locomotive) == "CLEAR_UNDER", delegate
            {
                SmartOrdersUtility.DebugLog("updating switch mode to 'clear under'");
                locomotive.KeyValueObject.Set("CLEAR_SWITCH_MODE", "CLEAR_UNDER");
            }).Height(60).Tooltip("Clear Under", "Clear switches under the train. Choose the number of switches below");
        })).Height(60);


        builder.AddField("", builder.ButtonStrip(delegate (UIPanelBuilder builder)
        {
            builder.ButtonStrip(
            strip =>
            {
                strip.AddButton("1", () => MovePastSwitches(helper, 1, locomotive.KeyValueObject.Get("CLEAR_SWITCH_MODE"), locomotive, persistence))!
                    .Tooltip("1 switch", "Move 1 switch");

                for (var i = 2; i <= 10; i++)
                {
                    var numSwitches = i;
                    strip.AddButton($"{numSwitches}", () => MovePastSwitches(helper, numSwitches, locomotive.KeyValueObject.Get("CLEAR_SWITCH_MODE"), locomotive, persistence))!
                        .Tooltip($"{numSwitches} switches", $"Move {numSwitches} switches");
                }
            }, 4);
        }));
    }

    private static void MoveDistance(AutoEngineerOrdersHelper helper, BaseLocomotive locomotive, float distance)
    {
        if (SmartOrdersPlugin.Settings.AutoSwitchOffHanbrake)
        {
            SmartOrdersUtility.ReleaseAllHandbrakes(locomotive);
        }

        if (SmartOrdersPlugin.Settings.AutoCoupleAir)
        {
            SmartOrdersUtility.ConnectAir(locomotive);
        }

        helper.SetOrdersValue(AutoEngineerMode.Yard, null, null, distance);
    }

    private static void MovePastSwitches(AutoEngineerOrdersHelper helper, int switchesToFind, KeyValue.Runtime.Value mode, BaseLocomotive locomotive, AutoEngineerPersistence persistence)
    {
        bool clearSwitchesUnderTrain = false;
        bool stopBeforeSwitch = false;

        if (mode.IsNull)
        {
            mode = "CLEAR_AHEAD";
        }

        if (mode == "CLEAR_UNDER")
        {
            clearSwitchesUnderTrain = true;
        }
        else if (mode == "APPROACH_AHEAD")
        {
            stopBeforeSwitch = true;
        }

        SmartOrdersUtility.DebugLog($"Handling move order mode: {mode}, switchesToFind: {switchesToFind}, clearSwitchesUnderTrain: {clearSwitchesUnderTrain}, stopBeforeSwitch: {stopBeforeSwitch}");

        var distanceInMeters = SmartOrdersUtility.GetDistanceForSwitchOrder(switchesToFind, clearSwitchesUnderTrain, stopBeforeSwitch, locomotive, persistence);
        if (distanceInMeters != null)
        {
            MoveDistance(helper, locomotive, distanceInMeters.Value);
        }
        else
        {
            SmartOrdersUtility.DebugLog("ERROR: distanceInMeters is null");
        }
    }
}