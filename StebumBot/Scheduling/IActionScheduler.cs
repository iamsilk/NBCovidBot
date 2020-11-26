using NCrontab;
using System;

namespace StebumBot.Scheduling
{
    public interface IActionScheduler
    {
        void ScheduleAction(string key, CrontabSchedule schedule, Action action);

        void UnscheduleAction(string key);
    }
}
