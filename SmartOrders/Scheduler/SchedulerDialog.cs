namespace SmartOrders.Scheduler;

using System.Linq;
using Model;
using UI.Builder;
using UI.Common;
using UnityEngine;

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

                    // add command CONNECT_AIR | RELEASE_HANDBRAKES | RESTORE_SWITCH
                });

                itemBuilder.AddListDetail(schedule.Commands.Select(o => GetScheduleCommandItem(schedule.Name, o)), _SelectedCommand, (commandBuilder, command) => { });
            }
        });

        return;

        UIPanelBuilder.ListItem<Schedule> GetScheduleDataItem(Schedule data) {
            return new UIPanelBuilder.ListItem<Schedule>(data.Name, data, "Saved schedules", data.Name);
        }

        UIPanelBuilder.ListItem<ScheduleCommand> GetScheduleCommandItem(string scheduleName, ScheduleCommand data) {
            return new UIPanelBuilder.ListItem<ScheduleCommand>(data.GetHashCode().ToString(), data, scheduleName, data.ToString());
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