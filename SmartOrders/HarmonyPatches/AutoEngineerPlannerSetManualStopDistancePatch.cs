namespace SmartOrders.HarmonyPatches;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using Track;
using UnityEngine;

[UsedImplicitly]
[HarmonyPatch(typeof(AutoEngineerPlanner), "SetManualStopDistance", new Type[] { typeof(float) })]
public static class AutoEngineerPlannerSetManualStopDistancePatch
{

    static void DebugLog(ref AutoEngineerPlanner __instance, string message)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }

        if (!SmartOrdersPlugin.Settings.EnableDebug)
        {
            return;
        }

        __instance.Say(message);
    }

    static void Prefix(ref float distanceInMeters, ref AutoEngineerPlanner __instance)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }

        // Distances above 1001m to 1012m are used to signal special behaviour
        if (distanceInMeters <= 1000f || distanceInMeters >= 1013f)
        {
            return;
        }

        const float FEET_PER_METER = 3.28084f;
        const float CAR_LENGTH_IN_METERS = 12.2f;
        const float MAX_DISTANCE_IN_METERS = 4000f / FEET_PER_METER;

        // 1001 => approach ahead
        // 1002 => clear ahead
        // 1003 => clear under

        bool stopBeforeSwitch = distanceInMeters == 1001f;
        bool clearSwitchesUnderTrain = distanceInMeters >= 1003f;

        int switchesToFind = 1;

        if (clearSwitchesUnderTrain)
        {
            switchesToFind = (int)Math.Round(distanceInMeters - 1003f) + 1;
        }

        if (stopBeforeSwitch)
        {
            DebugLog(ref __instance, $"Executing order to stop before next switch");
        }
        else if (clearSwitchesUnderTrain)
        {
            var str = switchesToFind == 1 ? "first switch" : $"{switchesToFind} switches";
            DebugLog(ref __instance, $"Executing order to stop after clearing {str}");
        }
        else
        {
            DebugLog(ref __instance, $"Executing order to stop after closest switch in front of train");
        }

        TrainController shared = TrainController.Shared;
        Graph graph = shared.graph;

        // Ensure car list is updated
        //typeof(AutoEngineerPlanner).GetMethod("UpdateCars", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { null });

        var locomotive = (BaseLocomotive)typeof(AutoEngineerPlanner).GetField("_locomotive", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
        if (locomotive == null)
        {
            DebugLog(ref __instance, $"Error: couldn't find locomotive");
        }

        var coupledCarsCached = (List<Car>)typeof(AutoEngineerPlanner).GetField("_coupledCarsCached", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
        if (coupledCarsCached == null)
        {
            DebugLog(ref __instance, $"Error: couldn't find coupledCarsCached");
        }

        var totalLength = coupledCarsCached.Sum((car) => car.carLength) + 1f * (coupledCarsCached.Count - 1);

        DebugLog(ref __instance, $"Found locomotive {locomotive.DisplayName} with {coupledCarsCached.Count} cars");

        var orders = (Orders)typeof(AutoEngineerPlanner).GetField("_orders", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

        TrackSegment segment;
        TrackSegment.End segmentEnd;
        Location start;

        // if we are stopping before the next switch the we can look forward from the logical front the train to find the next switch
        start = (Location)typeof(AutoEngineerPlanner).GetMethod("StartLocation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);
        if (start == null)
        {
            DebugLog(ref __instance, $"Error: couldn't find locomotive start location");
        }

        DebugLog(ref __instance, $"Start location: segment ID {start.segment.id}, distance {start.distance} ({start.end})");
        if (clearSwitchesUnderTrain)
        {
            // if we are clearing a switch, the train might currently be over it.
            // so we want to start our search from the end of the train
            start = graph.LocationByMoving(start.Flipped(), totalLength, false, false).Flipped();

            DebugLog(ref __instance, $"location for end of train: segment ID {start.segment.id}, distance {start.distance} ({start.end})");
        }

        segment = start.segment;
        segmentEnd = start.EndIsA ? TrackSegment.End.B : TrackSegment.End.A;

        distanceInMeters = 0;

        int switchesFound = 0;
        bool foundSwitch = false;
        var safetyMargin = 2; // distance to leave clear of switch
        var maxSegmentsToSearch = 50;

        for (var i = 0; i < maxSegmentsToSearch; i++)
        {
            if (i == 0)
            {
                DebugLog(ref __instance, $"Adding distance from start to next node {i + 2} {start.DistanceUntilEnd()}");
                distanceInMeters += start.DistanceUntilEnd();
            }
            else
            {
                DebugLog(ref __instance, $"Adding distance from node {i + 1} to next node {i + 2} {segment.GetLength()}");
                distanceInMeters += segment.GetLength();
            }

            if (distanceInMeters > MAX_DISTANCE_IN_METERS)
            {
                distanceInMeters = MAX_DISTANCE_IN_METERS;
                DebugLog(ref __instance, $"Reached max distance {MAX_DISTANCE_IN_METERS}m");
                break;
            }

            var node = segment.NodeForEnd(segmentEnd);
            if (node == null)
            {
                DebugLog(ref __instance, $"Next node is null");
                break;
            }


            if (graph.IsSwitch(node))
            {
                DebugLog(ref __instance, $"Found next switch at {distanceInMeters}m");

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

                // if we are coming from a switch exit, the next segment is the switch entrance
                if (switchExitSegmentA != null && segment.id == switchExitSegmentA.id || switchExitSegmentB != null && segment.id == switchExitSegmentB.id)
                {
                    DebugLog(ref __instance, $"Switch only has one exit");
                    segment = switchEnterSegment;
                }
                else
                {
                    // otherwise depends on if the switch is thrown
                    if (node.isThrown)
                    {
                        DebugLog(ref __instance, $"Following thrown exit");
                        segment = switchExitSegmentB;
                    }
                    else
                    {
                        DebugLog(ref __instance, $"Following normal exit");
                        segment = switchExitSegmentA;
                    }
                }
            }
            else
            {
                // next segment for non-switches
                var segmentsReachableFromArgs = new object[] {
                    segment,
                    segmentEnd,
                    null,
                    null,
                };

                typeof(Graph).GetMethod("SegmentsReachableFrom", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(graph, segmentsReachableFromArgs);
                var segmentExitNormal = (TrackSegment)segmentsReachableFromArgs[2];
                segment = segmentExitNormal;
            }

            if (segment == null)
            {
                DebugLog(ref __instance, $"Next segment is null");
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
                    DebugLog(ref __instance, $"Subtracting extra distance {nodeFoulingDistance} to not block other track entering switch");
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
                    DebugLog(ref __instance, $"Adding extra distance {nodeFoulingDistance}m to not block other track entering switch");
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

        switch (SmartOrdersPlugin.Settings.MeasureType) {
            case MeasureType.Feet:
                distanceString = $"{Math.Round(distanceInMeters * FEET_PER_METER)}ft";
                break;
            case MeasureType.Meter:
                distanceString = $"{Math.Round(distanceInMeters)}m";
                break;
            case MeasureType.Car:
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
                __instance.Say($"{action} {distanceString} up to switch");
            }
            else
            {
                var str = switchesToFind == 1 ? "switch" : $"{switchesToFind} switches";

                __instance.Say($"{action} {distanceString} to clear {str}");
            }
        }
        else
        {
            __instance.Say($"{action} {distanceString}");
        }

    }

}