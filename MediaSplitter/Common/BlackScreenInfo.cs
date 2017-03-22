using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter.Common
{
    public class BlackScreenInfo
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan MiddleTime => new TimeSpan((StartTime + EndTime).Ticks / 2);
        public TimeSpan EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public BlackScreenInfo(double start, double end)
        {
            this.StartTime = TimeSpan.FromSeconds(start);
            this.EndTime = TimeSpan.FromSeconds(end);
        }
    }
}
