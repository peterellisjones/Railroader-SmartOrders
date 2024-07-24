namespace SmartOrders;

using System;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Railloader;
using Serilog;
using SmartOrders.Scheduler;
using UI.Builder;

[UsedImplicitly]
public sealed class SmartOrdersPlugin : SingletonPluginBase<SmartOrdersPlugin>, IModTabHandler
{

    public static IModdingContext Context { get; private set; } = null!;
    public static IUIHelper UiHelper { get; private set; } = null!;
    public static Settings Settings { get; private set; } = null!;

    private readonly ILogger _Logger = Log.ForContext<SmartOrdersPlugin>()!;

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
    }

    public override void OnDisable()
    {
        _Logger.Information("OnDisable");
        var harmony = new Harmony("SmartOrders");
        harmony.UnpatchAll();
    }

    public void ModTabDidOpen(UIPanelBuilder builder) {
        var lengths = Enum.GetNames(typeof(MeasureType)).ToList();
        builder.AddField("Measure distance in", builder.AddDropdown(lengths, (int)Settings.MeasureType, o => {
            Settings.MeasureType = (MeasureType)o;
            builder.Rebuild();
        })!);
        builder.AddField("Send debug logs to console", builder.AddToggle(() => Settings.EnableDebug, o => Settings.EnableDebug = o)!);
    }

    public void ModTabDidClose()
    {
        SaveSettings();
    }

    public static void SaveSettings() {
        Context.SaveSettingsData("SmartOrders", Settings);
    }

    private static SchedulerDialog? _TrackSegmentDialog;
    public static SchedulerDialog TrackSegmentDialog => _TrackSegmentDialog ??= new SchedulerDialog();

}