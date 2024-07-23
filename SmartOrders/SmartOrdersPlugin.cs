namespace SmartOrders;

using HarmonyLib;
using JetBrains.Annotations;
using Railloader;
using Serilog;
using SmartOrders.Scheduler;
using UI.Builder;

[UsedImplicitly]
public sealed class SmartOrdersPlugin : SingletonPluginBase<SmartOrdersPlugin>, IModTabHandler {

    public static IModdingContext Context { get; private set; } = null!;
    public static IUIHelper UiHelper { get; private set; } = null!;
    public static Settings Settings { get; private set; } = null!;

    private readonly ILogger _Logger = Log.ForContext<SmartOrdersPlugin>()!;

    public SmartOrdersPlugin(IModdingContext context, IUIHelper uiHelper) {
        Context = context;
        UiHelper = uiHelper;

        Settings = Context.LoadSettingsData<Settings>("SmartOrders") ?? new Settings();
    }

    public override void OnEnable() {
        _Logger.Information("OnEnable");
        var harmony = new Harmony("SmartOrders");
        harmony.PatchAll();
    }

    public override void OnDisable() {
        _Logger.Information("OnDisable");
        var harmony = new Harmony("SmartOrders");
        harmony.UnpatchAll();
    }

    public void ModTabDidOpen(UIPanelBuilder builder) {
        builder.AddField("Debug", builder.AddToggle(() => Settings.EnableDebug, o => Settings.EnableDebug = o)!);
        builder.AddField("Report in car lengths", builder.AddToggle(() => Settings.UseCarLengthInsteadOfFeet, o => Settings.UseCarLengthInsteadOfFeet = o)!);
    }

    public void ModTabDidClose() {
        SaveSettings();
    }

    public static void SaveSettings() {
        Context.SaveSettingsData("SmartOrders", Settings);
    }

    private static SchedulerDialog? _TrackSegmentDialog;
    public static SchedulerDialog TrackSegmentDialog => _TrackSegmentDialog ??= new SchedulerDialog();

}