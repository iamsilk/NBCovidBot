using NCrontab;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NBCovidBot.Scheduling
{
    public class ActionScheduler : IActionScheduler
    {
        private readonly ILogger<ActionScheduler> _logger;
        private readonly Dictionary<string, ScheduledAction> _scheduledActions;

        public ActionScheduler(ILogger<ActionScheduler> logger)
        {
            _logger = logger;
            _scheduledActions = new Dictionary<string, ScheduledAction>();
        }

        private static async Task RunScheduledAction(ScheduledAction action)
        {
            while (!action.CancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                await Task.Delay(action.Schedule.GetNextOccurrence(now) - now, action.CancellationToken.Token);

                action.Action();
            }
        }

        public void ScheduleAction(string key, CrontabSchedule schedule, Action action)
        {
            ScheduledAction scheduledAction;

            lock (_scheduledActions)
            {
                if (_scheduledActions.ContainsKey(key))
                    throw new ArgumentException("A scheduled action with the specified key already exists", nameof(key));

                scheduledAction = new ScheduledAction(schedule, action);

                _scheduledActions.Add(key, scheduledAction);
            }

            _logger.LogTrace("Scheduled action with key: " + key);

            Task.Run(() => RunScheduledAction(scheduledAction));
        }

        public void UnscheduleAction(string key)
        {
            ScheduledAction scheduledAction;

            lock (_scheduledActions)
            {
                if (!_scheduledActions.TryGetValue(key, out scheduledAction))
                    throw new ArgumentException("A scheduled action with the specified key does not exist", nameof(key));

                _scheduledActions.Remove(key);
            }

            _logger.LogTrace("Unscheduled action with key: " + key);

            scheduledAction.CancellationToken.Cancel();
        }
    }
}
