using System;
using System.Threading.Tasks;

namespace NBCovidBot.Scheduling
{
    public partial class ActionScheduler
    {
        private class ScheduledTask : ScheduledEntity
        {
            public Func<Task> Task { get; }

            public ScheduledTask(string cronSchedule, Func<Task> task) : base(cronSchedule)
            {
                Task = task;
            }
        }
    }
}
