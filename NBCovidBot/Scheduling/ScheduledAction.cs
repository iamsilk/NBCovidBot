using System;

namespace NBCovidBot.Scheduling
{
    public partial class ActionScheduler
    {
        private class ScheduledAction : ScheduledEntity
        {
            public Action Action { get; }

            public ScheduledAction(string cronSchedule, Action action) : base(cronSchedule)
            {
                Action = action;
            }
        }
    }
}
