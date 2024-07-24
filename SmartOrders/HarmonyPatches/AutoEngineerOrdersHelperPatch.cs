namespace SmartOrders.HarmonyPatches;

using System.Diagnostics.CodeAnalysis;
using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using SmartOrders.Scheduler;
using UI.EngineControls;

[HarmonyPatch]
public static class AutoEngineerOrdersHelperPatch {

    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AutoEngineerOrdersHelper), "SetOrdersValue")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void AutoEngineerOrdersHelper(AutoEngineerOrdersHelper __instance, AutoEngineerMode? mode = null, bool? forward = null, int? maxSpeedMph = null, float? distance = null) {
        if (!SchedulerBehaviour.Shared.IsRecording || distance == null || distance == 0) {
            return;
        }

        UI.Console.Console.shared.AddLine("MOVE: " + distance);

        SchedulerBehaviour.Shared.AddCommand(ScheduleCommand.Move(forward ?? __instance.Orders.Forward, mode == AutoEngineerMode.Yard ? null : maxSpeedMph, distance.Value));
    }

}