namespace SmartOrders;

using System.Collections.Generic;
using SmartOrders.Dialogs;

public class Settings {

    public bool EnableDebug { get; set; } = false;

    public Dictionary<string, List<Schedule>> Schedules { get; set; } = new();

}