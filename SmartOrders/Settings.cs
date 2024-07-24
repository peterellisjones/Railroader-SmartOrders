namespace SmartOrders;

public class Settings
{

    public bool EnableDebug { get; set; }

    public MeasureType MeasureType { get; set; }

}

public enum MeasureType
{
    Feet,
    Meter,
    CarLengths
}