namespace SmartOrders.HarmonyPatches;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Core;
using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using Network;
using SmartOrders.Extensions;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UI.EngineControls;
using Game.State;
using UnityEngine;
using Track;
using System;

[PublicAPI]
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class CarInspectorPatches
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "PopulatePanel")]
    public static void PopulatePanel(UIPanelBuilder builder, Car? ____car, Window ____window)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }

        if (____car == null || !____car.IsLocomotive)
        {
            return;
        }

        UpdateWindowHeight(____car, ____window);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "BuildContextualOrders")]
    public static void BuildContextualOrders(UIPanelBuilder builder, AutoEngineerPersistence persistence, Car ____car)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }

        BaseLocomotive locomotive = (BaseLocomotive)____car;
        AutoEngineerOrdersHelper helper = new AutoEngineerOrdersHelper(locomotive, persistence);
        AutoEngineerMode mode2 = helper.Mode();

        if (mode2 != AutoEngineerMode.Yard)
        {
            return;
        }

        var coupledCars = locomotive.EnumerateCoupled()!.ToList()!;

        void setOrders(float? distanceInMeters)
        {
            if (distanceInMeters != null)
            {
                helper.SetOrdersValue(AutoEngineerMode.Yard, null, null, distanceInMeters!);
            }
        }

        builder.AddField("Switches", builder.ButtonStrip(delegate (UIPanelBuilder bldr)
        {
            bldr.AddButton("Approach Ahead", delegate
            {
                setOrders(GetDistanceForSwitchOrder(1, false, true, locomotive, coupledCars, persistence));

            }).Tooltip("Approach Ahead", "Approach, but do not pass, the first switch found in front of the train. Switches under the train are ignored.");
            bldr.AddButton("Clear Ahead", delegate
            {
                setOrders(GetDistanceForSwitchOrder(1, false, false, locomotive, coupledCars, persistence));
            }).Tooltip("Clear Ahead", "Clear the first switch found in front of the train. Switches under the train are ignored.");
        }, 4)).Tooltip("AI move to switch control", "Use Approach Ahead and Clear Ahead to approach or clear the first switch IN FRONT of the train. Switches under the train are ignored.\nUse the 1, 2, 3 etc buttons to move the train past switches in the direction travel starting from the BACK of the train. This includes switches that are currently UNDER the train as well as switches in front of the train");

        builder.AddField("", builder.ButtonStrip(delegate (UIPanelBuilder bldr)
                {
                    bldr.AddButton("1", delegate
                    {
                        setOrders(GetDistanceForSwitchOrder(1, true, false, locomotive, coupledCars, persistence));
                    }).Tooltip($"Clear 1 switch", $"Clear the next switch from the back of the train in the direction of travel");

                    for (int i = 2; i <= 10; i++)
                    {
                        bldr.AddButton($"{i}", delegate
                {
                    setOrders(GetDistanceForSwitchOrder(i, true, false, locomotive, coupledCars, persistence));

                }).Tooltip($"Clear {i} switches", $"Clear the next {i} switches from the back of the train in the direction of travel");

                    }
                }, 4));

        builder.AddField("", builder.ButtonStrip(strip =>
        {

            if (coupledCars.Any(c => c.air!.handbrakeApplied))
            {
                strip.AddButton($"Release {TextSprites.HandbrakeWheel}", () => ReleaseAllHandbrakes(coupledCars))!
                    .Tooltip("Release handbrakes", $"Iterates over cars in this consist and releases {TextSprites.HandbrakeWheel}.");
            }

            if (coupledCars.Any(c => c.EndAirSystemIssue()))
            {
                strip.AddButton("Connect Air", () => ConnectAir(coupledCars))!
                    .Tooltip("Connect Consist Air", "Iterates over each car in this consist and connects gladhands and opens anglecocks.");
            }
        })!);
    }

    private static void UpdateWindowHeight(Car car, Window window)
    {
        var persistence = new AutoEngineerPersistence(car.KeyValueObject);
        var helper = new AutoEngineerOrdersHelper(car, persistence);
        var mode = helper.Mode();

        var size = window.GetContentSize();
        if (mode != AutoEngineerMode.Yard)
        {
            return;
        }

        window.SetContentSize(new Vector2(size.x - 2, 322 + 70));
    }

    private static void ReleaseAllHandbrakes(List<Car> consist)
    {
        consist.Do(c => c.SetHandbrake(false));
    }

    private static void ConnectAir(List<Car> consist)
    {
        foreach (var car in consist)
        {
            ConnectAirCore(car, Car.LogicalEnd.A);
            ConnectAirCore(car, Car.LogicalEnd.B);
        }

        return;

        static void ConnectAirCore(Car car, Car.LogicalEnd end)
        {
            StateManager.ApplyLocal(new PropertyChange(car.id, CarPatches.KeyValueKeyFor(Car.EndGearStateKey.Anglecock, car.LogicalToEnd(end)), new FloatPropertyValue(car[end].IsCoupled ? 1f : 0f)));

            if (car.TryGetAdjacentCar(end, out var car2))
            {
                StateManager.ApplyLocal(new SetGladhandsConnected(car.id, car2.id, true));
            }
        }
    }

    private static float? GetDistanceForSwitchOrder(int switchesToFind, bool clearSwitchesUnderTrain, bool stopBeforeSwitch, BaseLocomotive locomotive, List<Car> coupledCars, AutoEngineerPersistence persistence)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return null;
        }

        const float FEET_PER_METER = 3.28084f;
        const float CAR_LENGTH_IN_METERS = 12.2f;
        const float MAX_DISTANCE_IN_METERS = 4000f / FEET_PER_METER;

        if (stopBeforeSwitch)
        {
            DebugLog($"Executing order to stop before next switch");
        }
        else if (clearSwitchesUnderTrain)
        {
            var str = switchesToFind == 1 ? "first switch" : $"{switchesToFind} switches";
            DebugLog($"Executing order to stop after clearing {str}");
        }
        else
        {
            DebugLog($"Executing order to stop after closest switch in front of train");
        }

        TrainController shared = TrainController.Shared;
        Graph graph = shared.graph;

        if (locomotive == null)
        {
            DebugLog($"Error: couldn't find locomotive");
        }

        var orders = persistence.Orders;

        var coupledCarsCached = locomotive.EnumerateCoupled(orders.Forward ? Car.End.F : Car.End.R).ToList();
        if (coupledCarsCached == null)
        {
            DebugLog($"Error: couldn't find coupledCarsCached");
        }

        var totalLength = coupledCarsCached.Sum((car) => car.carLength) + 1f * (coupledCarsCached.Count - 1);

        DebugLog($"Found locomotive {locomotive.DisplayName} with {coupledCarsCached.Count} cars");

        TrackSegment segment;
        TrackSegment.End segmentEnd;
        Location start;

        // if we are stopping before the next switch then we can look forward from the logical front the train to find the next switch
        start = StartLocation(locomotive, coupledCarsCached, orders.Forward);

        if (start == null)
        {
            DebugLog($"Error: couldn't find locomotive start location");
        }

        DebugLog($"Start location: segment ID {start.segment.id}, distance {start.distance} ({start.end})");
        if (clearSwitchesUnderTrain)
        {
            // if we are clearing a switch, the train might currently be over it.
            // so we want to start our search from the end of the train
            start = graph.LocationByMoving(start.Flipped(), totalLength, false, false).Flipped();

            DebugLog($"location for end of train: segment ID {start.segment.id}, distance {start.distance} ({start.end})");
        }

        segment = start.segment;
        segmentEnd = start.EndIsA ? TrackSegment.End.B : TrackSegment.End.A;

        float distanceInMeters = 0;

        int switchesFound = 0;
        bool foundSwitch = false;
        var safetyMargin = 2; // distance to leave clear of switch
        var maxSegmentsToSearch = 50;

        for (var i = 0; i < maxSegmentsToSearch; i++)
        {
            if (i == 0)
            {
                DebugLog($"Adding distance from start to next node {i + 2} {start.DistanceUntilEnd()}");
                distanceInMeters += start.DistanceUntilEnd();
            }
            else
            {
                DebugLog($"Adding distance from node {i + 1} to next node {i + 2} {segment.GetLength()}");
                distanceInMeters += segment.GetLength();
            }

            if (distanceInMeters > MAX_DISTANCE_IN_METERS)
            {
                distanceInMeters = MAX_DISTANCE_IN_METERS;
                DebugLog($"Reached max distance {MAX_DISTANCE_IN_METERS}m");
                break;
            }

            var node = segment.NodeForEnd(segmentEnd);
            if (node == null)
            {
                DebugLog($"Next node is null");
                break;
            }


            if (graph.IsSwitch(node))
            {
                DebugLog($"Found next switch at {distanceInMeters}m");

                switchesFound += 1;
                foundSwitch = switchesFound >= switchesToFind;

                if (foundSwitch)
                {
                    break;
                }

                // update segments if looking past switch

                // for switches we need to work out which way it is going
                TrackSegment switchEnterSegment;
                TrackSegment switchExitSegmentA;
                TrackSegment switchExitSegmentB;
                graph.DecodeSwitchAt(node, out switchEnterSegment, out switchExitSegmentA, out switchExitSegmentB);

                // switchEnterSegment, switchExitSegmentA, switchExitSegmentB cannot be null here, because graph.IsSwitch(node) call above ...

                // if we are coming from a switch exit, the next segment is the switch entrance
                if (switchExitSegmentA != null && segment.id == switchExitSegmentA.id || switchExitSegmentB != null && segment.id == switchExitSegmentB.id)
                {
                    DebugLog($"Switch only has one exit");
                    segment = switchEnterSegment;
                }
                else
                {
                    // otherwise depends on if the switch is thrown
                    if (node.isThrown)
                    {
                        DebugLog($"Following thrown exit");
                        segment = switchExitSegmentB;
                    }
                    else
                    {
                        DebugLog($"Following normal exit");
                        segment = switchExitSegmentA;
                    }
                }
            }
            else
            {
                // next segment for non-switches
                GraphPatches.SegmentsReachableFrom(graph, segment, segmentEnd, out var segmentExitNormal, out _);
                segment = segmentExitNormal;
            }

            if (segment == null)
            {
                DebugLog($"Next segment is null");
                break;
            }

            // next segment end is whatever end is NOT pointing at the current node
            segmentEnd = segment.NodeForEnd(TrackSegment.End.A).id == node.id ? TrackSegment.End.B : TrackSegment.End.A;
        }

        if (foundSwitch)
        {

            var node = segment.NodeForEnd(segmentEnd);

            TrackSegment switchEnterSegment;
            TrackSegment switchExitSegmentA;
            TrackSegment switchExitSegmentB;
            graph.DecodeSwitchAt(node, out switchEnterSegment, out switchExitSegmentA, out switchExitSegmentB);
            var nodeFoulingDistance = graph.CalculateFoulingDistance(node);

            var facingSwitchEntrance = switchEnterSegment == segment;

            if (stopBeforeSwitch)
            {
                if (!facingSwitchEntrance)
                {
                    DebugLog($"Subtracting extra distance {nodeFoulingDistance} to not block other track entering switch");
                    distanceInMeters = distanceInMeters - nodeFoulingDistance;
                }
                else
                {
                    distanceInMeters -= safetyMargin;
                }
            }
            else
            {
                if (facingSwitchEntrance)
                {
                    DebugLog($"Adding extra distance {nodeFoulingDistance}m to not block other track entering switch");
                    distanceInMeters = distanceInMeters + nodeFoulingDistance;
                }
                else
                {
                    distanceInMeters += safetyMargin;
                }

                // if we're not stopping before the switch, then we calculated the distance to the switch from
                // the front of the train and therefore need to add the train length to pass the next switch
                if (!clearSwitchesUnderTrain)
                {
                    distanceInMeters += totalLength;
                }
            }
        }

        distanceInMeters = Math.Max(0, distanceInMeters);

        var action = "Reversing";
        if (orders.Forward)
        {
            action = "Moving forwards";
        }

        String distanceString;

        switch (SmartOrdersPlugin.Settings.MeasureType)
        {
            case MeasureType.Feet:
                distanceString = $"{Math.Round(distanceInMeters * FEET_PER_METER)}ft";
                break;
            case MeasureType.Meter:
                distanceString = $"{Math.Round(distanceInMeters)}m";
                break;
            case MeasureType.CarLengths:
                var carLengths = Mathf.FloorToInt(distanceInMeters / CAR_LENGTH_IN_METERS);
                distanceString = $"{carLengths} car {"length".Pluralize(carLengths)}";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (foundSwitch)
        {
            if (stopBeforeSwitch)
            {
                Multiplayer.Broadcast($"{action} {distanceString} up to switch");
            }
            else
            {
                var str = switchesToFind == 1 ? "switch" : $"{switchesToFind} switches";

                Multiplayer.Broadcast($"{action} {distanceString} to clear {str}");
            }
        }
        else
        {
            Multiplayer.Broadcast($"{action} {distanceString}");
        }

        return distanceInMeters;
    }


    private static void DebugLog(string message)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }

        if (!SmartOrdersPlugin.Settings.EnableDebug)
        {
            return;
        }

        Multiplayer.Broadcast(message);
    }

    private static Location StartLocation(BaseLocomotive locomotive, List<Car> coupledCarsCached, bool forward)
    {
        int logical = (int)locomotive.EndToLogical(forward ? Model.Car.End.F : Model.Car.End.R);
        Model.Car car = coupledCarsCached[0];
        if (logical == 0)
        {
            Location locationA = car.LocationA;
            return !locationA.IsValid ? car.WheelBoundsA : locationA;
        }
        Location locationB = car.LocationB;
        return (locationB.IsValid ? locationB : car.WheelBoundsB).Flipped();
    }
}
