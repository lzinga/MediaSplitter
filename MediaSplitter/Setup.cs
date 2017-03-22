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
        #region Private Fields
        private ILogService Log;
        private Arguments Arguments;
        #endregion

        #region Constructors
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the media that it will try to work with, a single file or a folders media.
        /// </summary>
        /// <param name="path">The path of a single file or a directory.</param>
        /// <returns></returns>
        public IEnumerable<MediaFile> GetMedia(string path)
        {
            var attr = File.GetAttributes(path);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(i => Arguments.Extensions.Contains(Path.GetExtension(i))))
                {
                    string fileName = Path.GetFileName(file);

                    if (Regex.IsMatch(fileName, MediaFile.RegexMultiEpisodeFile) && !fileName.StartsWith("split_"))
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

        /// <summary>
        /// Try to split any files that are found.
        /// </summary>
        /// <returns></returns>
        public ExitCode Execute()
        {
            ExitCode exit = ExitCode.Success;


            var media = this.GetMedia(Arguments.Media).ToList();
            foreach (MediaFile file in media)
            {
                Log.WriteHeader($"Checking File \"{file.FileInfo.Name}\"");
                FFmpeg ffmpeg = new FFmpeg();
                ffmpeg.OnFileRenamed += (originalName, newName) =>
                {
                    Log.WriteLine($"[File] \"{originalName}\" renamed to \"{newName}\"");
                };
                ffmpeg.OnFileMoved += (source, destination) =>
                {
                    Log.WriteLine($"[File] \"{Path.GetFileName(source)}\" moved to \"..{destination.Replace(Path.GetDirectoryName(source), "")}\"");
                };

                if (Arguments.CutTime != null && Arguments.CutTime != TimeSpan.Zero)
                {
                    Log.WriteLine($"Using CutTime argument to split file. \"{Arguments.CutTime}\"");
                    ffmpeg.Split(file.FileInfo.FullName, file.EpisodeInfo.ToArray(), Arguments.CutTime);
                }
                else
                {
                    Log.WriteLine($"Using BlackScreenDetection to try to find the optimal place to cut.");
                    var output = ffmpeg.BlackScreenInfo(file.FileInfo.FullName, Arguments.BlackDuration, Arguments.BlackThreshold, Arguments.BlackPixelLuminance).ToList();
                    file.BlackScreenInfo.AddRange(this.CleanBlackScreenInfo(output));

                    foreach (var item in file.BlackScreenInfo)
                    {
                        Log.WriteLine($"[BlackScreen] Start: {item.StartTime} End: {item.EndTime} Duration: {item.Duration}");
                    }
                    
                    // Since 2 episodes should have 1 cut, and 3 episodes 2 cuts we subtract one.
                    if(file.BlackScreenInfo.Count > file.EpisodeCount - 1)
                    {
                        Log.WriteLine($"The amount of black screens ({file.BlackScreenInfo.Count}) would cut into {file.BlackScreenInfo.Count + 1} files. Expected files is {file.EpisodeCount}.");
                        continue;
                    }

                    ffmpeg.Split(file.FileInfo.FullName, file.EpisodeInfo.ToArray(), file.CutTimes);
                }

                if (Arguments.Debug)
                {
                    Log.WriteDebug();
                }

            }

            Log.WriteHeader("Validating Output");
            foreach(MediaFile file in media)
            {
                foreach(EpisodeInfo info in file.EpisodeInfo)
                {
                    // Check if all expected files exist.
                    if (!File.Exists(Path.Combine(file.FileInfo.DirectoryName, info.ToString() + file.FileInfo.Extension)))
                    {
                        Log.WriteLine($"[File] \"{file.FileInfo.Name}\" was not found, however it was expected to exist.");
                        exit = ExitCode.Failure;
                    }
                }
            }

            return exit;
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Cleans out unwanted/invalid black screen points that most likely aren't correct.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        private List<BlackScreenInfo> CleanBlackScreenInfo(List<BlackScreenInfo> output)
        {
            // If there is a range defined get only ones in within range.
            if (Arguments.StartRange != null && Arguments.StartRange >= TimeSpan.Zero && Arguments.EndRange != null && Arguments.EndRange >= TimeSpan.Zero)
            {
                output = output.Where(i => i.StartTime >= Arguments.StartRange && i.EndTime <= Arguments.EndRange).ToList();
            }

            var secondary = new List<BlackScreenInfo>(output);
            for (int i = 0; i < secondary.Count; i++)
            {
                BlackScreenInfo current = secondary[i];

                foreach (var item in secondary.Where(x => x.MiddleTime != current.MiddleTime))
                {
                    if ((item.MiddleTime - current.MiddleTime).TotalSeconds <= 10 && (item.MiddleTime - current.MiddleTime).TotalSeconds >= 0)
                    {
                        output.Remove(item);
                    }
                }
            }

            return output;
        }
        #endregion


    }
}
