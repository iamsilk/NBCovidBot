using NCrontab;
using System;
using System.Threading;

namespace NBCovidBot.Scheduling
{
    public class ScheduledAction
    {
        public CrontabSchedule Schedule { get; }

        public Action Action { get; }

        public CancellationTokenSource CancellationToken { get; }

        public ScheduledAction(CrontabSchedule schedule, Action action)
        {
            Schedule = schedule;
            Action = action;
            CancellationToken = new CancellationTokenSource();
        }
    }
}
