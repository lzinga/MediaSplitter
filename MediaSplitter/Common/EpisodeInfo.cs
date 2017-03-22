using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter.Common
{
    public class EpisodeInfo
    {
        public string Season { get; set; }
        public string Number { get; set; }
        public string Title { get; set; }

        public EpisodeInfo(string season, string number, string title)
        {
            this.Season = season;
            this.Number = number;
            this.Title = title;
        }

        public override string ToString()
        {
            return $"{this.Season}{this.Number} {this.Title}";
        }
    }
}
