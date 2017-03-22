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
        public static readonly string FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\ffmpeg.exe");

        #region Public Constants
        public const string RegexBlackDetect = @"black_start:(\S*).black_end:(\S*).black_duration:\S*";
        #endregion

        Process _proc = new Process();

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

        public void Split(string file, params TimeSpan[] cuts)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            string cutOffs = string.Join(",", cuts.Where(i => i != TimeSpan.Zero).Select(i => (int)i.TotalSeconds));
            string tempName = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_%03d" + Path.GetExtension(file));
            _proc.StartInfo.Arguments = $"-i \"{file}\" -f segment -segment_atclocktime 1 -segment_times {cutOffs} -c:v copy -c:a copy \"{tempName}\"";
            _proc.Start();
            string output = _proc.StandardError.ReadToEnd();
            _proc.WaitForExit();
            Thread.Sleep(1000);

            // TODO: Rename videos and move original file.
        }
    }
}
