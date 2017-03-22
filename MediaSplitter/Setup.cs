using MediaSplitter.Common;
using MediaSplitter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaSplitter
{
    public class Setup
    {
        private ILogService Log;
        private Arguments Arguments;

        public Setup(Arguments args, ILogService log)
        {
            this.Log = log;
            Arguments = args;

            Log.WriteHeader($"Media Splitter - {System.Reflection.Assembly.GetEntryAssembly().GetName().Version}");
            foreach(string val in args.Get())
            {
                Log.WriteLine(val);
            }

            if (Arguments.Debug)
            {
                Log.WriteDebug();
            }

        }

        public IEnumerable<MediaFile> GetMedia(string path)
        {
            var attr = File.GetAttributes(path);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(i => Arguments.Extensions.Contains(Path.GetExtension(i))))
                {
                    string fileName = Path.GetFileName(file);

                    if (Regex.IsMatch(fileName, MediaFile.RegexMultiEpisodeFile))
                    {
                        yield return new MediaFile(file);
                    }
                }
            }
            else
            {
                yield return new MediaFile(path);
            }
        }


        public ExitCode Execute()
        {
            foreach(MediaFile file in this.GetMedia(Arguments.Media).ToList())
            {
                Log.WriteHeader($"Checking File \"{file.FileInfo.Name}\"");
                FFmpeg ffmpeg = new FFmpeg();

                if(Arguments.CutTime != null && Arguments.CutTime != TimeSpan.Zero)
                {

                }
                else
                {
                    var output = ffmpeg.BlackScreenInfo(file.FileInfo.FullName, Arguments.BlackDuration, Arguments.BlackThreshold, Arguments.BlackPixelLuminance).ToList();

                    // If there is a range defined get only ones in within range.
                    if(Arguments.StartRange != null && Arguments.StartRange >= TimeSpan.Zero && Arguments.EndRange != null && Arguments.EndRange >= TimeSpan.Zero)
                    {
                        output = output.Where(i => i.StartTime >= Arguments.StartRange && i.EndTime <= Arguments.EndRange).ToList();
                    }
                    file.BlackScreenInfo.AddRange(output);

                    // Since 2 episodes should have 1 cut, and 3 episodes 2 cuts we subtract one.
                    if(file.BlackScreenInfo.Count > file.EpisodeCount - 1)
                    {
                        Log.WriteLine($"The amount of black screens ({file.BlackScreenInfo.Count}) to cut at is larger than the episode count ({file.EpisodeCount}).");
                        continue;
                    }

                    ffmpeg.Split(file.FileInfo.FullName, file.CutTimes);
                }


                


                //Log.WriteDebug();
            }


            return ExitCode.Success;
        }



    }
}
