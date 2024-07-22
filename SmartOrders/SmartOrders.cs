using HarmonyLib;
using Model.AI;
using Model;
using System.Reflection;
using UI.Builder;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using System.Collections.Generic;
using System;
using System.Linq;
using Track;
using UI.CarInspector;
using Game.Messages;
using UI.EngineControls;
using UnityEngine;

namespace SmartOrders
{
    static class Main
    {

        public static bool enabled;
        public static bool enableDebug;
        public static UnityModManager.ModEntry mod;

        public static Settings settings;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);
            enableDebug = settings.EnableDebug;

            mod = modEntry;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;

            return true;
        }

        static bool OnToggle(ModEntry modEntry, bool value)
        {
            enabled = value;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
            enableDebug = settings.EnableDebug;
        }
    }

    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Enable Debug Messages")] public bool EnableDebug = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }

    [HarmonyPatch(typeof(AutoEngineerPlanner))]
    [HarmonyPatch("SetManualStopDistance")]
    [HarmonyPatch(new System.Type[] { typeof(float) })]
    class AutoEngineerPlannerSetManualStopDistancePatch
    {

        static void DebugLog(ref AutoEngineerPlanner __instance, string message)
        {
            if (!Main.enabled)
            {
                return;
            }

            if (!Main.enableDebug)
            {
                return;
            }

            __instance.Say(message);
        }

        static void Prefix(ref float distanceInMeters, ref AutoEngineerPlanner __instance)
        {
            if (!Main.enabled)
            {
                return;
            }

            // Distances above 1001m to 1012m are used to signal special behaviour
            if (distanceInMeters <= 1000f || distanceInMeters >= 1013f)
            {
                return;
            }

            // 1001 => approach ahead
            // 1002 => clear ahead
            // 1003 => clear under

            bool stopBeforeSwitch = (distanceInMeters == 1001f);

            bool clearSwitchesUnderTrain = (distanceInMeters >= 1003f);

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

            var totalLength = coupledCarsCached.Sum((Car car) => car.carLength) + 1f * (float)(coupledCarsCached.Count - 1);

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

            var maxDistanceMeters = 1000;
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

                if (distanceInMeters > maxDistanceMeters)
                {
                    distanceInMeters = maxDistanceMeters;
                    DebugLog(ref __instance, $"Reached max distance {maxDistanceMeters}m");
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
                    foundSwitch = (switchesFound >= switchesToFind);

                    if (foundSwitch) {
                        break;
                    }

                    // update segments if looking past switch

                    // for switches we need to work out which way it is going
                    TrackSegment switchEnterSegment;
                    TrackSegment switchExitSegmentA;
                    TrackSegment switchExitSegmentB;
                    graph.DecodeSwitchAt(node, out switchEnterSegment, out switchExitSegmentA, out switchExitSegmentB);

                    // if we are coming from a switch exit, the next segment is the switch entrance
                    if ((switchExitSegmentA != null && segment.id == switchExitSegmentA.id) || (switchExitSegmentB != null && segment.id == switchExitSegmentB.id))
                    {
                        DebugLog(ref __instance, $"Switch only has one exit");
                        segment = switchEnterSegment;
                    } else
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
                } else
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

            if (foundSwitch)
            {
                if (stopBeforeSwitch)
                {
                    __instance.Say($"{action} {Math.Round(distanceInMeters)}m up to switch");
                }
                else
                {
                    var str = switchesToFind == 1 ? "switch" : $"{switchesToFind} switches";
                    __instance.Say($"{action} {Math.Round(distanceInMeters)}m to clear {str}");
                }
            }
            else
            {
                __instance.Say($"{action} {Math.Round(distanceInMeters)}m");
            }

        }

    }

    [HarmonyPatch(typeof(CarInspector))]
    [HarmonyPatch("BuildContextualOrders")]
    class CarInspectorBuildContextualOrdersPatch
    {
        static void Prefix(UIPanelBuilder builder, AutoEngineerPersistence persistence, CarInspector __instance)
        {
            if (!Main.enabled)
            {
                return;
            }

            Car _car = (Car)typeof(CarInspector).GetField("_car", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            AutoEngineerOrdersHelper helper = new AutoEngineerOrdersHelper(_car as BaseLocomotive, persistence);
            AutoEngineerMode mode2 = helper.Mode();

            if (mode2 != AutoEngineerMode.Yard)
            {
                return;
            }

            void SetOrdersValue(AutoEngineerMode? mode = null, bool? forward = null, int? maxSpeedMph = null, float? distance = null)
            {
                helper.SetOrdersValue(mode, forward, maxSpeedMph, distance);
            }

            builder.AddField("Switches", builder.ButtonStrip(delegate (UIPanelBuilder bldr)
            {
                bldr.AddButton("Approach Ahead", delegate
                {
                    SetOrdersValue(null, null, null, 1001f);
                }).Tooltip("Approach Ahead", "Approach, but do not pass, the first switch found in front of the train. Switches under the train are ignored.");
                bldr.AddButton("Clear Ahead", delegate
                {
                    SetOrdersValue(null, null, null, 1002f);
                }).Tooltip("Clear Ahead", "Clear the first switch found in front of the train. Switches under the train are ignored.");
            }, 4)).Tooltip("AI move to switch control", "Use Approach Ahead and Clear Ahead to approach or clear the first switch IN FRONT of the train. Switches under the train are ignored.\nUse the 1, 2, 3 etc buttons to move the train past switches in the direction travel starting from the BACK of the train. This includes switches that are currently UNDER the train as well as switches in front of the train");

            builder.AddField("", builder.ButtonStrip(delegate (UIPanelBuilder bldr)
            {
                bldr.AddButton("1", delegate
                {
                    SetOrdersValue(null, null, null, 1003f);
                }).Tooltip("Clear 1 switch", "Clear the next switch from the back of the train in the direction of travel");
                bldr.AddButton("2", delegate
                {
                    SetOrdersValue(null, null, null, 1004f);
                }).Tooltip("Clear 2 switches", "Clear the next 2 switches from the back of the train in the direction of travel");
                bldr.AddButton("3", delegate
                {
                    SetOrdersValue(null, null, null, 1005f);
                }).Tooltip("Clear 3 switches", "Clear the next 3 switches from the back of the train in the direction of travel");

                bldr.AddButton("4", delegate
                {
                    SetOrdersValue(null, null, null, 1006f);
                }).Tooltip("Clear 4 switches", "Clear the next 6 switches from the back of the train in the direction of travel");
                bldr.AddButton("5", delegate
                {
                    SetOrdersValue(null, null, null, 1007f);
                }).Tooltip("Clear 5 switches", "Clear the next 6 switches from the back of the train in the direction of travel");
                bldr.AddButton("6", delegate
                {
                    SetOrdersValue(null, null, null, 1008f);
                }).Tooltip("Clear 6 switches", "Clear the next 6 switches from the back of the train in the direction of travel");
                bldr.AddButton("7", delegate
                {
                    SetOrdersValue(null, null, null, 1009f);
                }).Tooltip("Clear 7 switches", "Clear the next 7 switches from the back of the train in the direction of travel");
                bldr.AddButton("8", delegate
                {
                    SetOrdersValue(null, null, null, 1010f);
                }).Tooltip("Clear 8 switches", "Clear the next 8 switches from the back of the train in the direction of travel");
                bldr.AddButton("9", delegate
                {
                    SetOrdersValue(null, null, null, 1011f);
                }).Tooltip("Clear 9 switches", "Clear the next 9 switches from the back of the train in the direction of travel");
                bldr.AddButton("10", delegate
                {
                    SetOrdersValue(null, null, null, 1012f);
                }).Tooltip("Clear 10 switches", "Clear the next 10 switches from the back of the train in the direction of travel");
            }, 4));
        }
    }
}