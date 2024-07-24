namespace SmartOrders.Scheduler;

using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using JetBrains.Annotations;
using Model;
using UnityEngine;

[PublicAPI]
public sealed class SchedulerBehaviour : MonoBehaviour {

    public Action? Refresh { get; set; }

    public static SchedulerBehaviour Shared { get; private set; }

    public SchedulerBehaviour() {
        Shared = this;
    }

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
        UI.Console.Console.shared.AddLine("AI Engineer: " + Locomotive.name + ":  Running schedule '" + schedule.Name + "' ...");

        // notification when schedule is finished?
    }

    public void RemoveSchedule(Schedule schedule) {
        var index = Data.FindIndex(o => o.Name == schedule.Name);
        Data.RemoveAt(index);

        SmartOrdersPlugin.SaveSettings();
    }

    public Schedule? NewSchedule { get; private set; }

    public void AddCommand(ScheduleCommand command) {
        if (NewSchedule == null) {
            return;
        }

        UI.Console.Console.shared!.AddLine("AI Engineer " + Locomotive.name + ": " + command + " got it ...");
        NewSchedule.Commands.Add(command);
        Refresh?.Invoke();
    }

    public bool IsRecording { get; private set; }
    public bool HasUnsavedSchedule { get; private set; }

    public void StartRecording() {
        UI.Console.Console.shared!.AddLine("AI Engineer " + Locomotive.name + ": Im watching ...");
        HasUnsavedSchedule = false;
        IsRecording = true;
        NewSchedule = new Schedule();
        Messenger.Default.Register<SwitchThrownDidChange>(this, o => {
            UI.Console.Console.shared.AddLine("Switch " + o.Node.id + " toggled");
        });
        Refresh?.Invoke();
    }

    public void StopRecording() {
        if (NewSchedule == null) {
            return;
        }

        IsRecording = false;
        HasUnsavedSchedule = true;
        Messenger.Default.Unregister<SwitchThrownDidChange>(this);
        Refresh?.Invoke();

        UI.Console.Console.shared!.AddLine("AI Engineer: I recorded " + NewSchedule.Commands.Count + " commands ...");
    }

    public void SaveSchedule(string scheduleName) {
        if (NewSchedule == null) {
            return;
        }

        NewSchedule.Name = scheduleName;
        Data.Add(NewSchedule);
        HasUnsavedSchedule = false;

        SmartOrdersPlugin.SaveSettings();
    }

    public void DiscardSchedule() {
        NewSchedule = null;
        HasUnsavedSchedule = false;
    }

}