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
            Splitter media = new Splitter(@"C:\Users\Lucas\Downloads\Rocket Power - Complete Series\Split Test", 0.1, new TimeSpan(0, 11, 00), new TimeSpan(0, 12, 00), ".m4v");
            foreach (FileInfo file in media.GetMedia())
            {
                FileSplit settings = media.GetSplitSettings(file);
                media.SplitVideo(settings);
            }

            Log.WriteLine("END");
        }
    }
}
