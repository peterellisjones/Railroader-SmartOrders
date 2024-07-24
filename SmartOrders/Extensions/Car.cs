namespace SmartOrders.Extensions;

using Model;

public static class CarExtensions {

    public static bool EndAirSystemIssue(this Car car)
    {
        bool aEndAirSystemIssue = car[Car.LogicalEnd.A].IsCoupled && !car[Car.LogicalEnd.A].IsAirConnectedAndOpen;
        bool bEndAirSystemIssue = car[Car.LogicalEnd.B].IsCoupled && !car[Car.LogicalEnd.B].IsAirConnectedAndOpen;
        return aEndAirSystemIssue || bEndAirSystemIssue;
    }

}