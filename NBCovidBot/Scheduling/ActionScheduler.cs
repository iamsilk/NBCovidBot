using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NBCovidBot.Scheduling
{
    public partial class ActionScheduler
    {
        private readonly ILogger<ActionScheduler> _logger;
        private readonly Dictionary<string, ScheduledEntity> _scheduledEntities;

        public ActionScheduler(ILogger<ActionScheduler> logger)
        {
            _logger = logger;
            _scheduledEntities = new Dictionary<string, ScheduledEntity>();
        }

        private async Task RunScheduledEntity(ScheduledEntity entity)
        {
            while (!entity.CancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                var next = entity.Schedule.GetNextOccurrence(now);

                var span = next - now;

                _logger.LogDebug($"Executing action {entity.Key} at {next} (in {span}).");

                await Task.Delay(span, entity.CancellationToken.Token);

                switch (entity)
                {
                    case ScheduledAction action:
                        action.Action();
                        break;

                    case ScheduledTask task:
                        await task.Task();
                        break;
                }
            }
        }

        private bool ScheduleEntity(string key, ScheduledEntity entity)
        {
            entity.Key = key;

            lock (_scheduledEntities)
            {
                if (_scheduledEntities.ContainsKey(key))
                {
                    _logger.LogWarning("A scheduled action with the specified key already exists: " + nameof(key));
                    return false;
                }

                _scheduledEntities.Add(key, entity);
            }

            // ReSharper disable once InconsistentlySynchronizedField
            _logger.LogTrace("Scheduled action with key: " + key);

            Task.Run(() => RunScheduledEntity(entity));
            
            return true;
        }

        public bool ScheduleAction(string key, string cronSchedule, Action action) =>
            ScheduleEntity(key, new ScheduledAction(cronSchedule, action));

        public bool ScheduleAction(string key, string cronSchedule, Func<Task> task) =>
            ScheduleEntity(key, new ScheduledTask(cronSchedule, task));

        public bool UnscheduleAction(string key)
        {
            ScheduledEntity scheduledEntity;

            lock (_scheduledEntities)
            {
                if (!_scheduledEntities.TryGetValue(key, out scheduledEntity))
                {
                    _logger.LogWarning("A scheduled action with the specified key does not exist: " + nameof(key));
                    return false;
                }

                _scheduledEntities.Remove(key);
            }

            // ReSharper disable once InconsistentlySynchronizedField
            _logger.LogTrace("Unscheduled action with key: " + key);

            scheduledEntity.CancellationToken.Cancel();

            return true;
        }
    }
}
