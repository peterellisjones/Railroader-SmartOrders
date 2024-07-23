namespace SmartOrders.Scheduler;

using System.Collections.Generic;
using JetBrains.Annotations;
using Model;
using UnityEngine;

[PublicAPI]
public sealed class SchedulerBehaviour : MonoBehaviour {

    public BaseLocomotive Locomotive { get; set; } = null!;

    public List<Schedule> Data { get; private set; } = null!;

    public void Awake() {
        if (!SmartOrdersPlugin.Settings.Schedules.TryGetValue(Locomotive.id, out var data)) {
            data = new List<Schedule>();
            SmartOrdersPlugin.Settings.Schedules.Add(Locomotive.id, data);
        }

        Data = data!;
    }

    public void ExecuteSchedule(Schedule schedule) {
        UI.Console.Console.shared.AddLine("AI Engineer: Running schedule '" + schedule.Name + "' ...");

        // notification when schedule is finished?
    }

    public void RemoveSchedule(Schedule schedule) {
        var index = Data.FindIndex(o => o.Name == schedule.Name);
        Data.RemoveAt(index);

        SmartOrdersPlugin.SaveSettings();
    }

    private Schedule? _NewSchedule;

    public bool IsRecording { get; private set; }
    public bool HasUnsavedSchedule { get; private set; }

    public void StartRecording() {
        UI.Console.Console.shared!.AddLine("AI Engineer: Im watching ...");
        HasUnsavedSchedule = false;
        IsRecording = true;
        _NewSchedule = new Schedule();
    }

    public void StopRecording() {
        if (_NewSchedule == null) {
            return;
        }

        IsRecording = false;
        HasUnsavedSchedule = true;

        // dummy data
        _NewSchedule.Commands.Add(ScheduleCommand.ConnectAir());
        _NewSchedule.Commands.Add(ScheduleCommand.ReleaseHandbrakes());
        _NewSchedule.Commands.Add(ScheduleCommand.Move(true, 30, 122f));
        _NewSchedule.Commands.Add(ScheduleCommand.SetSwitch(true, true));
        _NewSchedule.Commands.Add(ScheduleCommand.Move(false, null, 20f));
        _NewSchedule.Commands.Add(ScheduleCommand.RestoreSwitch(false));
        _NewSchedule.Commands.Add(ScheduleCommand.Move(false, 30, 61f));
        _NewSchedule.Commands.Add(ScheduleCommand.SetHandbrake(5));
        _NewSchedule.Commands.Add(ScheduleCommand.Uncouple(5));
        _NewSchedule.Commands.Add(ScheduleCommand.Move(true, null, 12.2f));
        UI.Console.Console.shared!.AddLine("AI Engineer: I recorded " + _NewSchedule.Commands.Count + " commands ...");
    }

    public void SaveSchedule(string scheduleName) {
        if (_NewSchedule == null) {
            return;
        }

        _NewSchedule.Name = scheduleName;
        Data.Add(_NewSchedule);
        HasUnsavedSchedule = false;

        SmartOrdersPlugin.SaveSettings();
    }

    public void DiscardSchedule() {
        _NewSchedule = null;
        HasUnsavedSchedule = false;
    }

}