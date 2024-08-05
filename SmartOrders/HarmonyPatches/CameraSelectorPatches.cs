using System;
using HarmonyLib;

namespace SmartOrders.HarmonyPatches;

[HarmonyPatch]
public static class CameraSelectorPatches
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CameraSelector), "SelectCamera")]
    public static bool SelectCamera(this CameraSelector __instance, CameraSelector.CameraIdentifier cameraIdentifier) {
        throw new NotImplementedException("This is a stub");
    }
}
