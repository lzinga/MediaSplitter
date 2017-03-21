using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaSplitter
{

    public class FileSplit
    {
        public string File { get; set; }
        public List<FileSplitInfo> FileSplits { get; set; } = new List<FileSplitInfo>();
    }

    public class FileSplitInfo
    {
        public string Title { get; set; }
        public string Season { get; set; }
        public string Episode { get; set; }
        public TimeSpan BlackStart { get; set; }
        public TimeSpan Cut
        {
            get
            {
                TimeSpan added = (BlackStart + BlackEnd);
                return new TimeSpan(added.Ticks / 2);
            }
        }
        public TimeSpan BlackEnd { get; set; }
        public override string ToString()
        {
            return $"{Season}{Episode} {Title}";
        }
    }



    public class Splitter
    {
        public string Folder { get; private set; }
        public string[] Extensions { get; private set; }
        public int Duration { get; private set; }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        public string MediaFile { get; set; }
        public readonly string FFMPEGPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\ffmpeg.exe");
        private const string RegexMultiEpisodeFile = @"^([Ss]\d+)((?:[Ee]\d+[-])*[Ee]\d+) +((?:[a-zA-Z0-9 +\d+])*)";
        private const string RegexBlackStart = @"(?<=black_start:)\S*(?= black_end:)";
        private const string RegexBlackEnd = @"(?<=black_end:)\S*(?= black_duration:)";
        

        public Splitter(string folder, int duration, params string[] extensions)
        {
            Folder = folder;
            Extensions = extensions;
            Duration = duration;
        }

        public IEnumerable<FileInfo> GetMedia()
        {
            foreach(string file in Directory.GetFiles(this.Folder, "*.*", SearchOption.TopDirectoryOnly).Where(i => this.Extensions.Contains(Path.GetExtension(i))))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (Regex.IsMatch(fileName, RegexMultiEpisodeFile))
                {
                    yield return new FileInfo(file);
                }
            }
        }

        public FileSplit GetSplitSettings(FileInfo info)
        {
            FileSplit split = new FileSplit();
            split.File = info.FullName;

            Match match = Regex.Match(info.Name, RegexMultiEpisodeFile);

            if(match.Groups.Count < 5 && match.Groups.Count > 5)
            {
                throw new InvalidDataException("The group count expects 5 results.");
            }


            string season = match.Groups[1].Value.Trim();
            
            string[] episodeNumbers = match.Groups[2].Value.Trim().Split('-');
            string[] episodeNames = match.Groups[3].Value.Trim().Split('+');

            // Ensure the episode numer count is the same as episode names.
            if (episodeNumbers.Length != episodeNames.Length)
            {
                throw new InvalidOperationException("The number of episodes did not match the number of names in the title");
            }

            // Loop over and get the episode/numbers
            // The episode index and name index should be the same.
            for(int i = 0; i < episodeNumbers.Length; i++)
            {
                string episode = episodeNumbers[i];
                string title = episodeNames[i];

                split.FileSplits.Add(new FileSplitInfo()
                {
                    Episode = episode.Trim(),
                    Season = season.Trim(),
                    Title = title.Trim()
                });

            }

            string ffmpegOutput = string.Empty;
            ffmpegOutput = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Mock-3BlackSceen-4Files.txt"));

            if (string.IsNullOrEmpty(ffmpegOutput))
            {
                Process ffmpeg = new Process();
                ffmpeg.StartInfo.FileName = FFMPEGPath;
                ffmpeg.StartInfo.Arguments = $"-i \"{info.FullName}\" -vf blackdetect=d=\"{this.Duration}\":pic_th=\"0.98\":pix_th=\"0.15\" -an -f null -";
                ffmpeg.StartInfo.UseShellExecute = false;
                //ffmpeg.StartInfo.RedirectStandardOutput = true;
                //ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.Start();
                ffmpeg.WaitForExit();
                ffmpegOutput = ffmpeg.StandardOutput.ReadToEnd();
            }


            List<string> lines = GetLine(ffmpegOutput, "black_start").ToList();

            // We add one because when there are 2 black_starts it means there will be 3 episodes.
            if(lines.Count + 1 != split.FileSplits.Count)
            {
                throw new InvalidOperationException("The number of splits from black screens do not match the amount of episodes mentioned in the files name.");
            }

            for(int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (!Regex.IsMatch(line, RegexBlackStart) || !Regex.IsMatch(line, RegexBlackEnd))
                {
                    throw new InvalidOperationException("Could not find the StartTime or the EndTime");
                }

                double startTime;
                double endTime;
                if (!double.TryParse(Regex.Match(line, RegexBlackStart).Value, out startTime) || !double.TryParse(Regex.Match(line, RegexBlackEnd).Value, out endTime))
                {
                    throw new InvalidOperationException("Could not parse start or end time into a double.");
                }

                // The current index should reference the episodes in order from 1+.
                split.FileSplits[i].BlackStart = TimeSpan.FromSeconds(startTime);
                split.FileSplits[i].BlackEnd = TimeSpan.FromSeconds(endTime);
            }

            return split;
        }

        public void SplitVideo(FileSplit file)
        {
            string directory = Path.GetDirectoryName(file.File);
            string extension = Path.GetExtension(file.File);

            // The name of the file to create (this is a temporary name)
            string newFile = $"{directory}\\{Path.GetFileNameWithoutExtension(file.File)}_%03d{extension}";

            // Splits the files into the respective temporary file names.
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = FFMPEGPath;
            ffmpeg.StartInfo.Arguments = $"-i \"{file.File}\" -f segment -segment_times {string.Join(",", file.FileSplits.Where(i => i.BlackStart != TimeSpan.Zero).Select(i => i.Cut.TotalSeconds))} -c copy -map 0 \"{newFile}\"";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            foreach(var splits in file.FileSplits)
            {
                
            }

            for(int i = 0; i < file.FileSplits.Count; i++)
            {
                FileSplitInfo split = file.FileSplits[i];

                string tempName = Path.Combine(directory, Path.GetFileNameWithoutExtension(file.File) + "_00" + i + extension);
                File.Move(tempName, Path.Combine(directory, split.ToString() + extension));
            }
        }

        private IEnumerable<string> GetLine(string input, string where)
        {
            foreach (string line in input.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains(where))
                {
                    yield return line;
                }
            }
        }


    }
}
