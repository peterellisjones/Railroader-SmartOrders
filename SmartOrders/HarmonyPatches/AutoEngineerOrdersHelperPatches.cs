namespace SmartOrders.HarmonyPatches;

using Game.Messages;
using HarmonyLib;
using Model;
using UI.EngineControls;

[HarmonyPatch]
public static class AutoEngineerOrderHelperPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AutoEngineerOrdersHelper), "SetOrdersValue")]
    public static void SetOrdersValue(BaseLocomotive ____locomotive, AutoEngineerMode? mode = null, int? maxSpeedMph = null, float? distance = null)

    {
        if (!SmartOrdersPlugin.Shared.IsEnabled)
        {
            return;
        }

        if (SmartOrdersPlugin.Settings.AutoReleaseHandbrake)
        {
            SmartOrdersUtility.ReleaseAllHandbrakes(____locomotive);
        }

        if (SmartOrdersPlugin.Settings.AutoCoupleAir)
        {
            SmartOrdersUtility.ConnectAir(____locomotive);
        }

    }
}