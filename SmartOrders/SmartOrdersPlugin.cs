namespace SmartOrders;

using HarmonyLib;
using JetBrains.Annotations;
using Railloader;
using Serilog;
using UI.Builder;

[UsedImplicitly]
public sealed class SmartOrdersPlugin : SingletonPluginBase<SmartOrdersPlugin>, IModTabHandler {

    public static IModdingContext Context { get; private set; } = null!;
    public static IUIHelper UiHelper { get; private set; } = null!;
    public static Settings Settings { get; private set; }

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
    }

    public void ModTabDidClose() {
        Context.SaveSettingsData("SmartOrders", Settings);
    }

}