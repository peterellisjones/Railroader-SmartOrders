namespace SmartOrders;

using System.Collections.Generic;
using SmartOrders.Scheduler;

public class Settings {

    public bool EnableDebug { get; set; }

    public bool UseCarLengthInsteadOfFeet { get; set; }

    public Dictionary<string, List<Schedule>> Schedules { get; set; } = new();

}