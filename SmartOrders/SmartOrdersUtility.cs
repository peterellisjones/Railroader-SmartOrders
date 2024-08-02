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
using Track;
using UI.Common;
using UnityEngine;
using Game;
using Network.Messages;
using Model.OpsNew;
using static Model.Car;

public static class SmartOrdersUtility
{
    public static void ConnectAir(BaseLocomotive locomotive)
    {
        DebugLog("Checking air");
        locomotive.EnumerateCoupled().Do(car =>
        {
            ConnectAirCore(car, Car.LogicalEnd.A);
            ConnectAirCore(car, Car.LogicalEnd.B);
        });

        static void ConnectAirCore(Car car, Car.LogicalEnd end)
        {
            if (car[end].IsCoupled)
            {
                StateManager.ApplyLocal(new PropertyChange(car.id, CarPatches.KeyValueKeyFor(Car.EndGearStateKey.Anglecock, car.LogicalToEnd(end)), new FloatPropertyValue(car[end].IsCoupled ? 1f : 0f)));

                if (car.TryGetAdjacentCar(end, out var car2))
                {
                    StateManager.ApplyLocal(new SetGladhandsConnected(car.id, car2.id, true));
                }
            }
        }
    }

    public static void ReleaseAllHandbrakes(BaseLocomotive locomotive)
    {
        DebugLog("Checking handbrakes");
        locomotive.EnumerateCoupled().Do(c => c.SetHandbrake(false));
    }

    public static float? GetDistanceForSwitchOrder(int switchesToFind, bool clearSwitchesUnderTrain, bool stopBeforeSwitch, BaseLocomotive locomotive, AutoEngineerPersistence persistence, out TrackNode? targetSwitch) {
        targetSwitch = null;
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return null;
        }

        const float FEET_PER_METER = 3.28084f;
        const float CAR_LENGTH_IN_METERS = 12.192f;
        const float MAX_DISTANCE_IN_METERS = 10000f / FEET_PER_METER;

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

        var switchesFound = 0;
        var foundAllSwitches = false;
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

            targetSwitch = node;

            if (graph.IsSwitch(node))
            {
                DebugLog($"Found next switch at {distanceInMeters}m");

                switchesFound += 1;
                foundAllSwitches = switchesFound >= switchesToFind;

                if (foundAllSwitches)
                {
                    break;
                }

                // update segments if looking past switch

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

        if (foundAllSwitches)
        {
            var node = segment.NodeForEnd(segmentEnd);
            targetSwitch = node;

            graph.DecodeSwitchAt(node, out var switchEnterSegment, out _, out _);
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

        // NOTE: This should not be here ... (method should only return distance  + targetSwitch
        //    and not also print message to console)
        // >>
        var action = "Reversing";
        if (orders.Forward)
        {
            action = "Moving forwards";
        }

        string distanceString;

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

        if (foundAllSwitches)
        {
            if (stopBeforeSwitch)
            {
                Say($"{action} {distanceString} up to switch");
            }
            else
            {
                var str = switchesToFind == 1 ? "switch" : $"{switchesToFind} switches";

                Say($"{action} {distanceString} to clear {str}");
            }
        }
        else
        {
            Say($"{action} {distanceString}");
        }
        // <<
        return distanceInMeters;
    }

    public static void DebugLog(string message)
    {
        if (!SmartOrdersPlugin.Settings.EnableDebug)
        {
            return;
        }

        Say(message);
    }


    private static void Say(string message)
    {
        Alert alert = new Alert(AlertStyle.Console, message, TimeWeather.Now.TotalSeconds);
        WindowManager.Shared.Present(alert);
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

    public static void DisconnectCarGroups(BaseLocomotive locomotive, int numGroups, AutoEngineerPersistence persistence)
    {
        var end = numGroups > 0 ? "front" : "back";
        numGroups = Math.Abs(numGroups);

        var orders = persistence.Orders;

        List<Car> cars;

        if (end == "front")
        {
            if (orders.Forward)
            {
                cars = locomotive.EnumerateCoupled(Car.End.R).Reverse().ToList();
            }
            else
            {
                cars = locomotive.EnumerateCoupled(Car.End.F).Reverse().ToList();
            }
        }
        else
        {
            if (orders.Forward)
            {
                cars = locomotive.EnumerateCoupled(Car.End.F).Reverse().ToList();
            }
            else
            {
                cars = locomotive.EnumerateCoupled(Car.End.R).Reverse().ToList();

            }
        }

        OpsController opsController = OpsController.Shared;

        if (cars.Count < 2)
        {
            DebugLog("ERROR: not enough cars");
            return;
        }

        Car firstCar = cars[0];

        var maybeFirstCarWaybill = firstCar.GetWaybill(opsController);
        if (maybeFirstCarWaybill == null)
        {
            return;
        }

        OpsCarPosition destination = maybeFirstCarWaybill.Value.Destination;

        Car? carToDisconnect = null;

        int carsToDisconnectCount = 0;
        int groupsFound = 1;

        foreach (Car car in cars)
        {
            var maybeWaybill = car.GetWaybill(opsController);
            if (maybeWaybill == null)
            {

                DebugLog($"Car {car.DisplayName}, has no waybill, stopping search");
                break;
            }

            OpsCarPosition thisCarDestination = maybeWaybill.Value.Destination;
            if (destination.Identifier == thisCarDestination.Identifier)
            {
                DebugLog($"Car {car.DisplayName} is part of group {groupsFound}");
                carToDisconnect = car;
                carsToDisconnectCount++;
            }
            else
            {
                if (groupsFound < numGroups)
                {
                    destination = thisCarDestination;
                    carToDisconnect = car;
                    carsToDisconnectCount++;
                    groupsFound++;
                    DebugLog($"Car {car.DisplayName} is part of new group {groupsFound}");
                }
                else
                {
                    DebugLog($"{groupsFound} groups found, stopping search");
                    break;
                }
            }
        }

        if (carsToDisconnectCount == 0)
        {
            DebugLog($"No cars found to disconnect");
            return;
        }

        Car newEndCar = cars[carsToDisconnectCount];

        var groupsMaybePlural = groupsFound > 1 ? "groups of cars" : "group of cars";

        var groupsString = numGroups == 999 ? "all cars with waybills" : $"{groupsFound} {groupsMaybePlural}";

        var carsMaybePlural = carsToDisconnectCount > 1 ? "cars" : "car";
        Say($"Disconnecting {groupsString} totalling {carsToDisconnectCount} {carsMaybePlural} from the {end} of the train");
        DebugLog($"Disconnecting coupler between {newEndCar.DisplayName} and {carToDisconnect.DisplayName}");

        var newEndCarEndToDisconnect = (newEndCar.CoupledTo(LogicalEnd.A) == carToDisconnect) ? LogicalEnd.A : LogicalEnd.B;
        var carToDisconnectEndToDisconnect = (carToDisconnect.CoupledTo(LogicalEnd.A) == newEndCar) ? LogicalEnd.A : LogicalEnd.B;

        newEndCar.ApplyEndGearChange(newEndCarEndToDisconnect, EndGearStateKey.CutLever, 1f);
        newEndCar.ApplyEndGearChange(newEndCarEndToDisconnect, EndGearStateKey.Anglecock, 0f);
        carToDisconnect.ApplyEndGearChange(carToDisconnectEndToDisconnect, EndGearStateKey.Anglecock, 0f);
    }
    
    public static void MoveCameraToNode(TrackNode node){
         CameraSelector.shared.ZoomToPoint(node.transform.localPosition);
         SmartOrdersPlugin.TrackNodeHelper.Show(node);
    }

}