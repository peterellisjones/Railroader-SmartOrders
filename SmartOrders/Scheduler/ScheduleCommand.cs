namespace SmartOrders.Scheduler;

using System;
using Core;
using UnityEngine;

public class ScheduleCommand {

    public static ScheduleCommand Move(bool forward, int? maxSpeed, float distance) {
        return new ScheduleCommand(ScheduleCommandType.MOVE, forward, maxSpeed, distance, true, 0);
    }

    public static ScheduleCommand ConnectAir() {
        return new ScheduleCommand(ScheduleCommandType.CONNECT_AIR, true, null, 0, true, 0);
    }

    public static ScheduleCommand ReleaseHandbrakes() {
        return new ScheduleCommand(ScheduleCommandType.RELEASE_HANDBRAKES, true, null, 0, true, 0);
    }

    public static ScheduleCommand SetSwitch(bool forward, bool switchToNormal) {
        return new ScheduleCommand(ScheduleCommandType.SET_SWITCH, forward, null, 0, switchToNormal, 0);
    }

    public static ScheduleCommand Uncouple(int index) {
        return new ScheduleCommand(ScheduleCommandType.UNCOUPLE, true, null, 0, true, index);
    }

    public static ScheduleCommand SetHandbrake(int index) {
        return new ScheduleCommand(ScheduleCommandType.SET_HANDBRAKE, true, null, 0, true, index);
    }

    public static ScheduleCommand RestoreSwitch(bool forward) {
        return new ScheduleCommand(ScheduleCommandType.RESTORE_SWITCH, forward, null, 0, true, 0);
    }

    private ScheduleCommand(ScheduleCommandType commandType, bool forward, int? maxSpeed, float distance, bool switchToNormal, int index) {
        CommandType = commandType;
        Forward = forward;
        MaxSpeed = maxSpeed;
        Distance = distance;
        SwitchToNormal = switchToNormal;
        Index = index;
    }

    public ScheduleCommandType CommandType { get; }

    // MOVE
    public bool Forward { get; }
    public int? MaxSpeed { get; } // null = yard mode
    public float Distance { get; }

    // SET_SWITCH
    public bool SwitchToNormal { get; } // set switch to normal

    // UNCOUPLE, SET_HANDBRAKE
    public int Index { get; } // car index: 0 = first, 1 = second, -1 = last, -2, second from end

    public override string ToString() {
        var carDistance = Mathf.FloorToInt(Distance / 12.2f);
        return CommandType switch {
                   ScheduleCommandType.CONNECT_AIR        => "Connect air",
                   ScheduleCommandType.MOVE               => $"Move ({(MaxSpeed == null ? "Yard" : "Road")}, {(Forward ? "forward" : "backward")}) {carDistance} {"car".Pluralize(carDistance)}",
                   ScheduleCommandType.RELEASE_HANDBRAKES => "Release handbrakes",
                   ScheduleCommandType.SET_SWITCH         => $"Set {(Forward ? "front" : "back")} switch to {(SwitchToNormal ? "normal" : "reverse")}",
                   ScheduleCommandType.UNCOUPLE           => $"Uncouple car #{Index}",
                   ScheduleCommandType.SET_HANDBRAKE      => $"Set handbrake on car #{Index}",
                   ScheduleCommandType.RESTORE_SWITCH     => $"Restore {(Forward ? "front" : "back")} switch position",
                   _                                      => throw new ArgumentOutOfRangeException()
               };
    }

    public override int GetHashCode() {
        unchecked {
            var hashCode = (int)CommandType;
            hashCode = (hashCode * 397) ^ Forward.GetHashCode();
            hashCode = (hashCode * 397) ^ MaxSpeed.GetHashCode();
            hashCode = (hashCode * 397) ^ Distance.GetHashCode();
            hashCode = (hashCode * 397) ^ SwitchToNormal.GetHashCode();
            hashCode = (hashCode * 397) ^ Index;
            return hashCode;
        }
    }

}