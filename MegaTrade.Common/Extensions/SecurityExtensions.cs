﻿using TSLab.DataSource;
using TSLab.Script;

namespace MegaTrade.Common.Extensions
{
    public static class SecurityExtensions
    {
        public static ISecurity SimpleCompress(this ISecurity security, int interval) =>
            security.CompressTo(new Interval(interval, security.IntervalBase), 0, 1440, 0);

        public static string GetTimeframeName(this ISecurity security) => new TimeframeNamer(security).GetName();
    }
}
