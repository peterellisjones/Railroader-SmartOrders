namespace SmartOrders;

public class Settings
{

    public bool EnableDebug { get; set; }
    public bool AutoReleaseHandbrake { get; set; }
    public bool AutoCoupleAir { get; set; }
    public bool AutoApplyHandbrake { get; set; }

    public bool NoYardSpeedLimit {  get; set; }
    public bool ShowTargetSwitch {  get; set; }

    public MeasureType MeasureType { get; set; }

}

public enum MeasureType
{
    Feet,
    Meter,
    CarLengths
}