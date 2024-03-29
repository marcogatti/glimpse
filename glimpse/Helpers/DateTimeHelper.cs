﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Helpers
{
    public class DateTimeHelper
    {
        /*Changes a datetime into UTC Timezone wothout doing any translation*/
        public static DateTime ChangeToUtc(DateTime date)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(date, TimeZoneInfo.Utc);
        }
        public static Double GetAgeInSeconds(DateTime date)
        {
            TimeSpan elapsedSpan = new TimeSpan(DateTime.Now.Ticks - date.ToLocalTime().Ticks);
            Double age = Math.Truncate(elapsedSpan.TotalSeconds);

            return age;
        }
    }
}