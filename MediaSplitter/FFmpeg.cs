using MediaSplitter.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaSplitter
{
    public class FFmpeg
    {
        #region Public Constants
        /// <summary>
        /// Regular Express for getting the black screen information from the output of ffmpeg.
        /// </summary>
        public const string RegexBlackDetect = @"black_start:(\S*).black_end:(\S*).black_duration:\S*";

        /// <summary>
        /// The root folder for ffmpeg.exe.
        /// </summary>
        public static readonly string FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\ffmpeg.exe");
        #endregion

        #region Public Events
        /// <summary>
        /// When a file gets renamed in the split method.
        /// </summary>
        public event FileRenamed OnFileRenamed;
        public delegate void FileRenamed(string originalName, string newName);

        /// <summary>
        /// When I file is moved in the split method.
        /// </summary>
        public event FileMoved OnFileMoved;
        public delegate void FileMoved(string source, string destination);
        #endregion

        #region Private Fields
        Process _proc = new Process();
        #endregion

        #region Constructors
        public FFmpeg()
        {
            _proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = FFmpeg.FFmpegPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                }
            };
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets all the black screen moments in the video with in the settings specified.
        /// </summary>
        /// <param name="file">The file to check.</param>
        /// <param name="blackDuration">The minimum detected black duration (in seconds)</param>
        /// <param name="blackThreshold">The threshold for considering a picture as "black" (in percent)</param>
        /// <param name="pixelLuminance">The threshold for considering a pixel "black" (in luminance)</param>
        /// <returns></returns>
        public IEnumerable<BlackScreenInfo> BlackScreenInfo(string file, double blackDuration, double blackThreshold, double pixelLuminance)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            _proc.StartInfo.Arguments = $"-i \"{file}\" -vf blackdetect=d=\"{blackDuration}\":pic_th=\"{blackThreshold}\":pix_th=\"{pixelLuminance}\" -an -f null -";
            _proc.Start();

            string output = _proc.StandardError.ReadToEnd();
            _proc.WaitForExit();

            MatchCollection matches = Regex.Matches(output, FFmpeg.RegexBlackDetect);
            foreach(Match match in matches)
            {
                double start;
                double end;

                if(!double.TryParse(match.Groups[1].Value, out start))
                {
                    continue;
                }

                if(!double.TryParse(match.Groups[2].Value, out end))
                {
                    continue;
                }

                yield return new BlackScreenInfo(start, end);
            }
        }

        /// <summary>
        /// Splits a file at the cuts specified and renames the episodes if possible.
        /// </summary>
        /// <param name="file">The file to split.</param>
        /// <param name="episodeInfo">If exists will try to rename the split files to the episode info.</param>
        /// <param name="cuts">All the cuts the file should be split at.</param>
        public void Split(string file, EpisodeInfo[] episodeInfo, params TimeSpan[] cuts)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            string rootFolder = Path.GetDirectoryName(file);

            string cutOffs = string.Join(",", cuts.Where(i => i != TimeSpan.Zero).Select(i => Math.Floor(i.TotalSeconds)));
            string tempNameTemplate = Path.Combine(rootFolder, "split_" + Path.GetFileNameWithoutExtension(file) + "_{0}" + Path.GetExtension(file));
            _proc.StartInfo.Arguments = $"-i \"{file}\" -f segment -segment_atclocktime 1 -segment_times {cutOffs} -c:v copy -c:a copy \"{string.Format(tempNameTemplate, "%03d")}\"";
            _proc.Start();
            string output = _proc.StandardError.ReadToEnd();
            _proc.WaitForExit();
            Thread.Sleep(1000);

            // TODO: Rename videos and move original file.
            if(episodeInfo != null)
            {
                for (int i = 0; i < episodeInfo.Length; i++)
                {
                    string name = string.Format(tempNameTemplate, i.ToString("000"));
                    string newName = Path.Combine(rootFolder, episodeInfo[i].ToString().Trim() + Path.GetExtension(file));

                    // Rename to single episodes
                    File.Move(name, newName);
                    OnFileRenamed?.Invoke(Path.GetFileName(name), Path.GetFileName(newName));

                    // Move original file.
                    string destination = Path.Combine(rootFolder, "Originals", Path.GetFileName(file));

                    if (!Directory.Exists(Path.GetDirectoryName(destination)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    }

                    if (File.Exists(file))
                    {
                        File.Move(file, destination);
                        OnFileMoved?.Invoke(file, destination);
                    }

                }
            }


        }
        #endregion

    }
}
