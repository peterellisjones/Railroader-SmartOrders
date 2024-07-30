namespace SmartOrders;

public class Settings
{

    public bool EnableDebug { get; set; }
    public bool AutoSwitchOffHanbrake { get; set; }
    public bool AutoCoupleAir { get; set; }

    public bool NoYardSpeedLimit {  get; set; }

    public MeasureType MeasureType { get; set; }

}

public enum MeasureType
{
    Feet,
    Meter,
    CarLengths
}