using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter
{
    class Program
    {
        static void Main(string[] args)
        {
            Splitter media = new Splitter("C:\\Users\\v-lucael\\Documents\\Output", 2, ".mkv", ".mp4");
            foreach (FileInfo file in media.GetMedia())
            {
                FileSplit settings = media.GetSplitSettings(file);
                media.SplitVideo(settings);
            }

            Log.WriteLine("END");
        }
    }
}
