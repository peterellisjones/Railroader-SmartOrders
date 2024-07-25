namespace SmartOrders.HarmonyPatches;

using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Track;

[HarmonyPatch]
public static class GraphPatches {
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Graph), "SegmentsReachableFrom")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void SegmentsReachableFrom(Graph __instance, TrackSegment segment, TrackSegment.End end, out TrackSegment normal, out TrackSegment reversed) {
        throw new NotImplementedException("This is a stub");
    }
}