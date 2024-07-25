using HarmonyLib;
using Model.AI;
using Model;
//using System.Reflection;
using UI.Builder;
using UI.CarInspector;
using Game.Messages;
using UI.EngineControls;
using UnityEngine;
using JetBrains.Annotations;

namespace SmartOrders.HarmonyPatches
{
    [UsedImplicitly]
    [HarmonyPatch(typeof(CarInspector), "BuildContextualOrders")]
    public static class CarInspectorBuildContextualOrdersPatch
    {
        static void Prefix(UIPanelBuilder builder, AutoEngineerPersistence persistence, CarInspector __instance, Car ____car)
        {
            if (!SmartOrdersPlugin.Shared.IsEnabled)
            {
                return;
            }

            Car _car = ____car;//(Car)typeof(CarInspector).GetField("_car", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
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
                }).Tooltip("Clear 4 switches", "Clear the next 4 switches from the back of the train in the direction of travel");
                bldr.AddButton("5", delegate
                {
                    SetOrdersValue(null, null, null, 1007f);
                }).Tooltip("Clear 5 switches", "Clear the next 5 switches from the back of the train in the direction of travel");
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