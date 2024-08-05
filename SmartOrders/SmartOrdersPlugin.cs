using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using SmartOrders.Helpers;
using UnityEngine;

namespace SmartOrders;

using System;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Railloader;
using Serilog;
using UI.Builder;

[UsedImplicitly]
public sealed class SmartOrdersPlugin : SingletonPluginBase<SmartOrdersPlugin>, IModTabHandler
{

    public static IModdingContext Context { get; private set; } = null!;
    public static IUIHelper UiHelper { get; private set; } = null!;
    public static Settings Settings { get; private set; }

    public static TrackNodeHelper TrackNodeHelper { get; private set; }

    private readonly Serilog.ILogger _Logger = Serilog.Log.ForContext<SmartOrdersPlugin>()!;

    public SmartOrdersPlugin(IModdingContext context, IUIHelper uiHelper)
    {
        Context = context;
        UiHelper = uiHelper;

        Settings = Context.LoadSettingsData<Settings>("SmartOrders") ?? new Settings();
    }

    public override void OnEnable()
    {
        _Logger.Information("OnEnable");
        var harmony = new Harmony("SmartOrders");
        harmony.PatchAll();

        Messenger.Default!.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
        Messenger.Default.Register(this, new Action<MapDidUnloadEvent>(OnMapDidUnload));
    }

    public override void OnDisable()
    {
        _Logger.Information("OnDisable");
        var harmony = new Harmony("SmartOrders");
        harmony.UnpatchAll();

        Messenger.Default!.Unregister(this);
    }

    private void OnMapDidLoad(MapDidLoadEvent obj) {
        var go = new GameObject();
        TrackNodeHelper = go.AddComponent<TrackNodeHelper>()!;
    }

    private void OnMapDidUnload(MapDidUnloadEvent obj) {
        UnityEngine.Object.Destroy(TrackNodeHelper.gameObject!);
        TrackNodeHelper = null!;
    }

    public void ModTabDidOpen(UIPanelBuilder builder) {
        // 'Display names' from values from MeasureType enum
        List<string> lengths = ["Feet", "Meter", "Car Lengths"];
        builder.AddField("Measure distance in", builder.AddDropdown(lengths, (int)Settings.MeasureType, o => {
            Settings.MeasureType = (MeasureType)o;
            builder.Rebuild();
        })!);

        builder.AddField("Apply handbrakes", builder.AddToggle(() => Settings.AutoApplyHandbrake, o => Settings.AutoApplyHandbrake = o)!)!
                .Tooltip("Apply handbrakes", "When decoupling stationary cars, set the handbrake in the first car");

        builder.AddField("Release handbrakes", builder.AddToggle(() => Settings.AutoReleaseHandbrake, o => Settings.AutoReleaseHandbrake = o)!)!
                .Tooltip("Release handbrakes", "In Yard mode, automatically release the handbrakes for any cars in the train before moving");

        builder.AddField("Couple air", builder.AddToggle(() => Settings.AutoCoupleAir, o => Settings.AutoCoupleAir = o)!)!
                .Tooltip("Couple air", "In Yard mode, automatically couple air and open anglecocks for any cars in the train before moving");

        builder.AddField("No Yard Speed Limit", builder.AddToggle(() => Settings.NoYardSpeedLimit, o => Settings.NoYardSpeedLimit = o)!)!
                .Tooltip("Remove Yard Mode Speed Limit", "Remove 15mph speed limit in yard mode");

        builder.AddField("Show Target Switch", builder.AddToggle(() => Settings.ShowTargetSwitch, o => Settings.ShowTargetSwitch = o)!)!
               .Tooltip("Show Target Switch", "Moves camera to target switch and show yellow arrow pointing on switch node.");

        builder.AddField("Debug logs", builder.AddToggle(() => Settings.EnableDebug, o => Settings.EnableDebug = o)!)!
               .Tooltip("Send debug logs to console", "Send debug logs to console.");
    }

    public void ModTabDidClose()
    {
        Context.SaveSettingsData("SmartOrders", Settings);
    }

}