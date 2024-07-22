namespace SmartOrders;

using HarmonyLib;
using JetBrains.Annotations;
using Railloader;
using Serilog;

[UsedImplicitly]
public sealed class SmartOrdersPlugin : SingletonPluginBase<SmartOrdersPlugin> {

    public static IModdingContext Context { get; private set; } = null!;
    public static IUIHelper UiHelper { get; private set; } = null!;

    private readonly ILogger _Logger = Log.ForContext<SmartOrdersPlugin>()!;

    public SmartOrdersPlugin(IModdingContext context, IUIHelper uiHelper) {
        Context = context;
        UiHelper = uiHelper;
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

}