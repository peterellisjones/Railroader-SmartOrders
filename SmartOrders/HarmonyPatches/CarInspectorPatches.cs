using TriangleNet.Voronoi.Legacy;

namespace SmartOrders.HarmonyPatches;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using Model.OpsNew;
using SmartOrders.Extensions;
using UI;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UI.EngineControls;
using UnityEngine;
using UnityEngine.Rendering;
using static Model.Car;

[PublicAPI]
[HarmonyPatch]
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

            ____window.SetContentSize(new Vector2(originalWindowSize.Value.x - 2, 424));

            BuildAlternateCarLengthsButtons(builder, locomotive, helper);
            BuildSwitchYardAIButtons(builder, locomotive, persistence, helper);
        }

        //BuildDisconnectCarsButtons(builder, locomotive, persistence, helper);

        if (mode2 == AutoEngineerMode.Road)
        {
            BuildRoadModeCouplingButton(builder, locomotive);
        }

        BuildHandbrakeAndAirHelperButtons(builder, locomotive);
    }

    private static void BuildRoadModeCouplingButton(UIPanelBuilder builder, BaseLocomotive locomotive)
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

    private static void BuildAlternateCarLengthsButtons(UIPanelBuilder builder, BaseLocomotive locomotive, AutoEngineerOrdersHelper helper)
    {
        // color is same-ish as vanilla, but it is workaround for bug in UIPanelBuilderPatches ...
        builder.AddField("Car Lengths".Color("aaaaaa"), builder.ButtonStrip(delegate (UIPanelBuilder builder)
        {
            builder.AddButton("Stop", delegate
            {
                MoveDistance(helper, locomotive, 0f);
            });
            builder.AddButton("½", delegate
            {
                MoveDistance(helper, locomotive, 6.1f);
            });
            builder.AddButton("1", delegate
            {
                MoveDistance(helper, locomotive, 12.2f);
            });
            builder.AddButton("2", delegate
            {
                MoveDistance(helper, locomotive, 24.4f);
            });
            builder.AddButton("5", delegate
            {
                MoveDistance(helper, locomotive, 61f);
            });
            builder.AddButton("10", delegate
            {
                MoveDistance(helper, locomotive, 122f);
            });
            builder.AddButton("20", delegate
            {
                MoveDistance(helper, locomotive, 244f);
            });
            builder.AddButton("inf", delegate
            {
                MoveDistance(helper, locomotive, 12.192f * 1_000_000.5f);
            }).Tooltip("INF", "Move infinity car lengths");
        }, 4));
    }

    private static void BuildHandbrakeAndAirHelperButtons(UIPanelBuilder builder, BaseLocomotive locomotive)
    {
        builder.AddField("",
          builder.ButtonStrip(strip =>
          {
              var cars = locomotive.EnumerateCoupled()!.ToList()!;

              if (cars.Any(c => c.air!.handbrakeApplied))
              {
                  strip.AddButton($"Release {TextSprites.HandbrakeWheel}", () => SmartOrdersUtility.ReleaseAllHandbrakes(locomotive))!
                      .Tooltip("Release handbrakes", $"Iterates over cars in this consist and releases {TextSprites.HandbrakeWheel}.");
              }

              if (cars.Any(c => c.EndAirSystemIssue()))
              {
                  strip.AddButton("Connect Air", () => SmartOrdersUtility.ConnectAir(locomotive))!
                      .Tooltip("Connect Consist Air", "Iterates over each car in this consist and connects gladhands and opens anglecocks.");
              }
          })!
       );
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
                
                strip.AddButton("1", () => MovePastSwitches(helper, 1, locomotive.KeyValueObject.Get("CLEAR_SWITCH_MODE"), locomotive, persistence, !GameInput.IsShiftDown))!
                    .Tooltip("1 switch", "Move 1 switch");

                for (var i = 2; i <= 10; i++)
                {
                    var numSwitches = i;
                    strip.AddButton($"{numSwitches}", () => MovePastSwitches(helper, numSwitches, locomotive.KeyValueObject.Get("CLEAR_SWITCH_MODE"), locomotive, persistence, !GameInput.IsShiftDown))!
                        .Tooltip($"{numSwitches} switches", $"Move {numSwitches} switches");
                }
            }, 4);
        }));
    }

    private static void MoveDistance(AutoEngineerOrdersHelper helper, BaseLocomotive locomotive, float distance)
    {
        helper.SetOrdersValue(AutoEngineerMode.Yard, null, null, distance);
    }

    private static void MovePastSwitches(AutoEngineerOrdersHelper helper, int switchesToFind, KeyValue.Runtime.Value mode, BaseLocomotive locomotive, AutoEngineerPersistence persistence, bool showTargetSwitch)
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

        var distanceInMeters = SmartOrdersUtility.GetDistanceForSwitchOrder(switchesToFind, clearSwitchesUnderTrain, stopBeforeSwitch, locomotive, persistence, out var targetSwitch);
        if (SmartOrdersPlugin.Settings.ShowTargetSwitch && showTargetSwitch && targetSwitch != null) {
            SmartOrdersUtility.MoveCameraToNode(targetSwitch);
        }

        if (distanceInMeters != null) {
            MoveDistance(helper, locomotive, distanceInMeters.Value);
        } else {
            SmartOrdersUtility.DebugLog("ERROR: distanceInMeters is null");
        }
    }

    // Not currently used but could be added if we want to merge FlyShuntUI into SmartOrders
    static void BuildDisconnectCarsButtons(UIPanelBuilder builder, BaseLocomotive locomotive, AutoEngineerPersistence persistence, AutoEngineerOrdersHelper helper)
    {
        AutoEngineerMode mode2 = helper.Mode();

        builder.AddField("Disconnect", builder.ButtonStrip(delegate (UIPanelBuilder bldr)
        {
            bldr.AddButton("All", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, -999, persistence);
            }).Tooltip("Disconnect all cars with waybills from the back", "Disconnect all cars with waybills from the back");

            bldr.AddButton("-3", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, -3, persistence);
            }).Tooltip("Disconnect 3 Car Groups From Back", "Disconnect 3 groups of cars from the back that are headed to 3 different locations");

            bldr.AddButton("-2", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, -2, persistence);
            }).Tooltip("Disconnect 2 Car Groups From Back", "Disconnect 2 groups of cars from the back that are headed to 2 different locations");

            bldr.AddButton("-1", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, -1, persistence);
            }).Tooltip("Disconnect 1 Car Group From Back", "Disconnect all cars from the back of the train headed to the same location");

            bldr.AddButton("1", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, 1, persistence);
            }).Tooltip("Disconnect 1 Car Group From Front", "Disconnect all cars from the front of the train headed to the same location");

            bldr.AddButton("2", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, 2, persistence);
            }).Tooltip("Disconnect 2 Car Groups From Front", "Disconnect 2 groups of cars from the front that are headed to 2 different locations");

            bldr.AddButton("3", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, 3, persistence);
            }).Tooltip("Disconnect 3 Car Groups From Front", "Disconnect 3 groups of cars from the front that are headed to 3 different locations");

            bldr.AddButton("All", delegate
            {
                SmartOrdersUtility.DisconnectCarGroups(locomotive, 999, persistence);
            }).Tooltip("Disconnect all cars with waybills from the front", "Disconnect all cars with waybills from the front");

        }, 4)).Tooltip("Disconnect Car Groups", "Disconnect groups of cars headed for the same location from the front (positive numbers) or the back (negative numbers) in the direction of travel");
    }
}