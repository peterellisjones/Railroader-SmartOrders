namespace SmartOrders;

public class Settings {

    public bool EnableDebug { get; set; }

    public MeasureType MeasureType { get; set; }
    public Dictionary<string, List<Schedule>> Schedules { get; set; } = new();
}

public enum MeasureType {
    Feet,
    Meter,
    Car
}