namespace SmartOrders.Dialogs;

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Model;
using UI.Builder;
using UI.Common;
using UnityEngine;

public class Schedule {

    public string Name { get; set; }
    public List<ScheduleCommand> Commands { get; } = new();

}

public class ScheduleCommand {

    public string Name { get; set; }

}

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
        _NewSchedule.Commands.Add(new() { Name = "Command A" });
        _NewSchedule.Commands.Add(new() { Name = "Command B" });
        _NewSchedule.Commands.Add(new() { Name = "Command C" });
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

public sealed class SchedulerDialog {

    private readonly Window _Window;
    private bool _Populated;

    public SchedulerDialog() {
        _Window = SmartOrdersPlugin.UiHelper.CreateWindow(800, 500, Window.Position.Center);
        _Window.Title = "AI scheduler";
    }

    private SchedulerBehaviour _Scheduler = null!;
    private readonly UIState<string?> _SelectedSchedule = new(null);
    private readonly UIState<string?> _SelectedCommand = new(null);
    private string _NewScheduleName = string.Empty;
    private string? _NewScheduleNameError;

    private void BuildWindow(UIPanelBuilder builder) {
        
        builder.AddSection("Record new schedule", section => {
            section.ButtonStrip(strip => {
                if (!_Scheduler.IsRecording) {
                    strip.AddButton(_Scheduler.HasUnsavedSchedule ? "Continue" : "Start", () => {
                        _Scheduler.StartRecording();
                        section.Rebuild();
                    });
                } else {
                    strip.AddButton("Stop", () => {
                        _Scheduler.StopRecording();
                        section.Rebuild();
                    });
                }

                if (_Scheduler.HasUnsavedSchedule) {
                    strip.AddButton("Save", () => {
                        _NewScheduleNameError = ValidateNewScheduleName();
                        if (_NewScheduleNameError == null) {
                            _Scheduler.SaveSchedule(_NewScheduleName);
                            _NewScheduleName = string.Empty;
                            builder.Rebuild();
                        }
                    });
                    strip.AddButton("Discard", () => {
                        _Scheduler.DiscardSchedule();
                        section.Rebuild();
                    });
                }
            });
            if (_Scheduler.HasUnsavedSchedule) {
                section.AddField("Name", section.AddInputField(_NewScheduleName, o => _NewScheduleName = o, "New schedule name", 50)!);
                section.AddLabel(() => _NewScheduleNameError, UIPanelBuilder.Frequency.Periodic);
            }
        });

        builder.AddListDetail(_Scheduler.Data.Select(GetScheduleDataItem), _SelectedSchedule, (itemBuilder, schedule) => {
            if (schedule == null) {
                itemBuilder.AddLabel(_Scheduler.Data.Any() ? "Please select a schedule." : "No schedules configured.");
            } else {
                itemBuilder.ButtonStrip(strip => {
                    strip.AddButton("Execute", () => _Scheduler.ExecuteSchedule(schedule));
                    strip.AddButton("Remove", () => {
                        _Scheduler.RemoveSchedule(schedule);
                        _SelectedSchedule.Value = null;
                        builder.Rebuild();
                    });
                });

                itemBuilder.AddListDetail(schedule.Commands.Select(o => GetScheduleCommandItem(schedule.Name, o)), _SelectedCommand, (commandBuilder, command) => { });
            }
        });

        return;

        UIPanelBuilder.ListItem<Schedule> GetScheduleDataItem(Schedule data) {
            return new UIPanelBuilder.ListItem<Schedule>(data.Name, data, "Saved schedules", data.Name);
        }

        UIPanelBuilder.ListItem<ScheduleCommand> GetScheduleCommandItem(string scheduleName, ScheduleCommand data) {
            return new UIPanelBuilder.ListItem<ScheduleCommand>(data.Name, data, scheduleName, data.Name);
        }
    }

    private string? ValidateNewScheduleName() {
        if (string.IsNullOrWhiteSpace(_NewScheduleName)) {
            return "schedule name cannot be empty";
        }

        if (_Scheduler.Data.FirstOrDefault(o => o.Name == _NewScheduleName) != null) {
            return "schedule with given name already exists";
        }

        return null;
    }

    public bool IsShown => _Window.IsShown;

    private static SchedulerBehaviour GetOrAddScheduler(BaseLocomotive locomotive) {
        var scheduler = locomotive.GetComponentInChildren<SchedulerBehaviour>();
        if (scheduler == null) {
            var go = new GameObject(nameof(SchedulerBehaviour));
            go.transform!.SetParent(locomotive.transform!);
            go.SetActive(false);
            scheduler = go.AddComponent<SchedulerBehaviour>()!;
        }

        scheduler.Locomotive = locomotive;
        scheduler.gameObject!.SetActive(true);
        return scheduler;
    }

    public void ShowWindow(BaseLocomotive locomotive) {
        _Window.Title = "AI scheduler - " + locomotive.DisplayName;
        _Scheduler = GetOrAddScheduler(locomotive);


        if (!_Populated) {
            SmartOrdersPlugin.UiHelper.PopulateWindow(_Window, builder => BuildWindow(builder));
            _Populated = true;
        }

        if (!_Window.IsShown) {
            _Window.ShowWindow();
        }
    }

    public void CloseWindow() {
        _Window.CloseWindow();
    }

}