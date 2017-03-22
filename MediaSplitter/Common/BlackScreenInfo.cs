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
        public TimeSpan Duration { get; set; }

        #region Public Constants
        public const string RegexBlackScreenStart = @"(?<=black_start:)\S*(?= black_end:)";
        public const string RegexBlackScreenEnd = @"(?<=black_end:)\S*(?= black_duration:)";
        #endregion

    }
}
