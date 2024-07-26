using SmartOrders.HarmonyPatches;

namespace SmartOrders;

using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Model;
using Model.AI;
using Network;
using Track;
using UI.Common;
using UI.EngineControls;
using UnityEngine;

public static class SmartOrdersUtility
{

    const float FEET_PER_METER = 3.28084f;
    const float CAR_LENGTH_IN_METERS = 12.2f;
    const float MAX_DISTANCE_IN_METERS = 4000f / FEET_PER_METER;

    public static void UpdateWindowHeight(Car? car, Window window)
    {
        if (car == null || !car.IsLocomotive)
        {
            return;
        }

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

    public static void ConnectAir(List<Car> consist)
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

    public static void ReleaseAllHandbrakes(List<Car> consist)
    {
        consist.Do(c => c.SetHandbrake(false));
    }

    public static void Move(AutoEngineerOrdersHelper helper, int switchesToFind, bool clearSwitchesUnderTrain, bool stopBeforeSwitch, BaseLocomotive locomotive, AutoEngineerPersistence persistence)
    {
        var distanceInMeters = GetDistanceForSwitchOrder(switchesToFind, clearSwitchesUnderTrain, stopBeforeSwitch, locomotive, persistence);
        if (distanceInMeters != null)
        {
            helper.SetOrdersValue(AutoEngineerMode.Yard, null, null, distanceInMeters!);
        }
    }

    private static float? GetDistanceForSwitchOrder(int switchesToFind, bool clearSwitchesUnderTrain, bool stopBeforeSwitch, BaseLocomotive locomotive, AutoEngineerPersistence persistence)
    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return null;
        }


        if (stopBeforeSwitch)
        {
            DebugLog("Executing order to stop before next switch");
        }
        else if (clearSwitchesUnderTrain)
        {
            var str = switchesToFind == 1 ? "first switch" : $"{switchesToFind} switches";
            DebugLog($"Executing order to stop after clearing {str}");
        }
        else
        {
            DebugLog("Executing order to stop after closest switch in front of train");
        }

        var shared = TrainController.Shared;
        var graph = shared.graph;

        if (locomotive == null)
        {
            DebugLog("Error: couldn't find locomotive");
            return null;
        }

        var orders = persistence.Orders;

        var coupledCars = locomotive.EnumerateCoupled(orders.Forward ? Car.End.F : Car.End.R).ToList();
        if (coupledCars == null)
        {
            DebugLog("Error: couldn't find coupledCarsCached");
        }

        var totalLength = coupledCars.Sum(car => car.carLength) + 1f * (coupledCars.Count - 1); // add 1m separation per car

        DebugLog($"Found locomotive {locomotive.DisplayName} with {coupledCars.Count} cars");

        TrackSegment segment;
        TrackSegment.End segmentEnd;
        Location start;

        // if we are stopping before the next switch then we can look forward from the logical front the train to find the next switch
        start = StartLocation(locomotive, coupledCars, orders.Forward);

        if (start == null)
        {
            DebugLog("Error: couldn't find locomotive start location");
        }

        DebugLog($"Start location: segment ID {start.segment.id}, distance {start.distance} ({start.end})");
        if (clearSwitchesUnderTrain)
        {
            // if we are clearing a switch, the train might currently be over it.
            // so we want to start our search from the end of the train
            start = graph.LocationByMoving(start.Flipped(), totalLength).Flipped();

            DebugLog($"location for end of train: segment ID {start.segment.id}, distance {start.distance} ({start.end})");
        }

        segment = start.segment;
        segmentEnd = start.EndIsA ? TrackSegment.End.B : TrackSegment.End.A;

        float distanceInMeters = 0;
        float distanceOfLastFoundSwitch = 0;

        var switchesFound = 0;
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
                DebugLog("Next node is null");
                break;
            }


            if (graph.IsSwitch(node))
            {
                DebugLog($"Found next switch at {distanceInMeters}m");

                switchesFound += 1;
                distanceOfLastFoundSwitch = distanceInMeters;

                // for switches we need to work out which way it is going
                graph.DecodeSwitchAt(node, out var switchEnterSegment, out var switchExitNormal, out var switchExitReverse);

                // switchEnterSegment, switchExitSegmentA, switchExitSegmentB cannot be null here, because graph.IsSwitch(node) call above ...

                // if we are coming from a switch exit, the next segment is the switch entrance
                if (switchExitNormal != null && segment.id == switchExitNormal.id || switchExitReverse != null && segment.id == switchExitReverse.id)
                {
                    DebugLog("Switch only has one exit");
                    segment = switchEnterSegment;
                }
                else
                {
                    // otherwise depends on if the switch is thrown
                    if (node.isThrown)
                    {
                        DebugLog("Following thrown exit");
                        segment = switchExitReverse;
                    }
                    else
                    {
                        DebugLog("Following normal exit");
                        segment = switchExitNormal;
                    }
                }

                if (switchesFound >= switchesToFind)
                {
                    break;
                }
            }
            else
            {
                // next segment for non-switches
                graph.SegmentsReachableFrom(segment, segmentEnd, out var segmentExitNormal, out _);
                segment = segmentExitNormal;
            }

            if (segment == null)
            {
                DebugLog("Next segment is null");
                break;
            }

            // next segment end is whatever end is NOT pointing at the current node
            segmentEnd = segment.NodeForEnd(TrackSegment.End.A).id == node.id ? TrackSegment.End.B : TrackSegment.End.A;
        }

        if (switchesFound >= switchesToFind)
        {
            var node = segment.NodeForEnd(segmentEnd);

            graph.DecodeSwitchAt(node, out var switchEnterSegment, out _, out _);
            var nodeFoulingDistance = graph.CalculateFoulingDistance(node);

            var facingSwitchEntrance = switchEnterSegment == segment;

            if (stopBeforeSwitch)
            {
                if (!facingSwitchEntrance)
                {
                    DebugLog($"Subtracting extra distance {nodeFoulingDistance} to not block other track entering switch");
                    distanceOfLastFoundSwitch = distanceOfLastFoundSwitch - nodeFoulingDistance;
                }
                else
                {
                    distanceOfLastFoundSwitch -= safetyMargin;
                }
            }
            else
            {
                if (facingSwitchEntrance)
                {
                    DebugLog($"Adding extra distance {nodeFoulingDistance}m to not block other track entering switch");
                    distanceOfLastFoundSwitch = distanceOfLastFoundSwitch + nodeFoulingDistance;
                }
                else
                {
                    distanceOfLastFoundSwitch += safetyMargin;
                }

                // if we're not stopping before the switch, then we calculated the distance to the switch from
                // the front of the train and therefore need to add the train length to pass the next switch
                if (!clearSwitchesUnderTrain)
                {
                    distanceOfLastFoundSwitch += totalLength;
                }
            }
        }

        // dont go the wrong way just to unblock another track
        distanceOfLastFoundSwitch = Math.Max(0, distanceOfLastFoundSwitch);

        var action = "Reversing";
        if (orders.Forward)
        {
            action = "Moving forwards";
        }

        string distanceString;

        if (switchesFound > 0)
        {
            string distanceStr = formatDistance(distanceOfLastFoundSwitch);
            if (stopBeforeSwitch)
            {
                Multiplayer.Broadcast($"{action} {distanceStr} up to switch");
            }
            else
            {
                var str = switchesToFind == 1 ? "switch" : $"{switchesFound} switches";

                Multiplayer.Broadcast($"{action} {distanceStr} to clear {str}");
            }
        }
        else if (switchesFound == 0)
        {
            var direction = "in front of";
            if (!orders.Forward)
            {
                direction = "behind";
            }
            Multiplayer.Broadcast($"Couldn't find any switches {direction} train");
        }

        return distanceOfLastFoundSwitch;
    }

    private static string formatDistance(float distance)
    {
        switch (SmartOrdersPlugin.Settings.MeasureType)
        {
            case MeasureType.Feet:
                return $"{Math.Round(distance * FEET_PER_METER)}ft";
            case MeasureType.Meter:
                return $"{Math.Round(distance)}m";
            case MeasureType.CarLengths:
                var carLengths = Mathf.FloorToInt(distance / CAR_LENGTH_IN_METERS);
                return $"{carLengths} car {"length".Pluralize(carLengths)}";

            default:
                throw new ArgumentOutOfRangeException();
        }

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
        var logical = (int)locomotive.EndToLogical(forward ? Car.End.F : Car.End.R);
        var car = coupledCarsCached[0];
        if (logical == 0)
        {
            var locationA = car.LocationA;
            return !locationA.IsValid ? car.WheelBoundsA : locationA;
        }

        var locationB = car.LocationB;
        return (locationB.IsValid ? locationB : car.WheelBoundsB).Flipped();
    }

}