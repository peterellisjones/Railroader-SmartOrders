namespace SmartOrders.HarmonyPatches;

using System;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using SmartOrders.Scheduler;

[HarmonyPatch]
public static class CarPropertyChangesPatch {

    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarPropertyChanges), "SetHandbrake")]
    public static void SetHandbrake(Car car, bool apply) {
        if (!SchedulerBehaviour.Shared.IsRecording || !apply) {
            return;
        }

        var consist = car.EnumerateCoupled()!.ToArray();
        var index = Array.IndexOf(consist, car);
        SchedulerBehaviour.Shared.AddCommand(ScheduleCommand.SetHandbrake(index));
    }

}