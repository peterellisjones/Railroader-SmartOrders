namespace SmartOrders.Scheduler;

using System.Collections.Generic;

public class Schedule {

    public string Name { get; set; } = null!;

    public List<ScheduleCommand> Commands { get; } = new();
}