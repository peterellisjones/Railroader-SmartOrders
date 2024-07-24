namespace SmartOrders.Scheduler;

public enum ScheduleCommandType {

    MOVE,               // move train (yard / roam mode)
    CONNECT_AIR,        // connect air on all cars
    RELEASE_HANDBRAKES, // release handbrake on all cars
    SET_SWITCH,         // set switch direction
    UNCOUPLE,           // uncouple nth car
    SET_HANDBRAKE,      // set handbrake on nth car
    RESTORE_SWITCH      // restore state of switch from before SET_SWITCH command call

}

/*
 
record commands:

MOVE                    * AutoEngineerOrdersHelper.SetOrdersValue(AutoEngineerMode? mode = null, bool? forward = null,int? maxSpeedMph = null, float? distance = null)
SET_SWITCH              Messenger.Default.Send<SwitchThrownDidChange>(new SwitchThrownDidChange(this));
UNCOUPLE                ? void ApplyEndGearChange(Car.LogicalEnd logicalEnd, Car.EndGearStateKey endGearStateKey, float f)
SET_HANDBRAKE           * CarPropertyChanges.SetHandbrake(this Car car, bool apply)

special commands: (game do not have buttons for those - need to add them manually in SchedulerDialog

CONNECT_AIR             * Jobs.ConnectAir
RELEASE_HANDBRAKES      * Jobs.ReleaseAllHandbrakes
RESTORE_SWITCH          
   
 */

