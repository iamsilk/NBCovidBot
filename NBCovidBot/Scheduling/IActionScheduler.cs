using NCrontab;
using System;

namespace NBCovidBot.Scheduling
{
    public interface IActionScheduler
    {
        void ScheduleAction(string key, CrontabSchedule schedule, Action action);

        void UnscheduleAction(string key);
    }
}
