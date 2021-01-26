using NCrontab;
using System.Threading;

namespace NBCovidBot.Scheduling
{
    public partial class ActionScheduler
    {
        private abstract class ScheduledEntity
        {
            public string Key { get; internal set; }

            public CrontabSchedule Schedule { get; }

            public CancellationTokenSource CancellationToken { get; }

            protected ScheduledEntity(string cronSchedule)
            {
                Schedule = CrontabSchedule.Parse(cronSchedule);
                CancellationToken = new CancellationTokenSource();
            }
        }
    }
}
