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

                return new TimeSpan(BlackStart.Ticks);
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
        public double Duration { get; private set; }
        public TimeSpan StartRange { get; private set; }
        public TimeSpan EndRange { get; private set; }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        public string MediaFile { get; set; }
        public readonly string FFMPEGPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\ffmpeg.exe");
        private const string RegexMultiEpisodeFile = @"^([Ss]\d+)((?:[Ee]\d+[-])*[Ee]\d+) +((?:.)*)";
        private const string RegexBlackStart = @"(?<=black_start:)\S*(?= black_end:)";
        private const string RegexBlackEnd = @"(?<=black_end:)\S*(?= black_duration:)";
        

        public Splitter(string folder, double duration, params string[] extensions)
        {
            Folder = folder;
            Extensions = extensions;
            Duration = duration;
        }

        public Splitter(string folder, double duration, TimeSpan startRange, TimeSpan endRange, params string[] extensions) : this(folder,duration, extensions)
        {
            this.StartRange = startRange;
            this.EndRange = endRange;
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

            // Ensure the episode number count is the same as episode names.
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

            string ffmpegOutput = null;
            //ffmpegOutput = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Mock-3BlackSceen-4Files.txt"));

            if (string.IsNullOrEmpty(ffmpegOutput))
            {
                Log.WriteLine("Starting Black Detection");
                Process ffmpeg = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = FFMPEGPath,
                        UseShellExecute = false,
                        Arguments = $"-i \"{info.FullName}\" -vf blackdetect=d=\"{this.Duration}\":pic_th=\"0.5\":pix_th=\"0.09\" -an -f null -",
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                };

                ffmpeg.Start();
                ffmpegOutput = ffmpeg.StandardError.ReadToEnd();

                ffmpeg.WaitForExit();
            }

            List<string> lines = GetLine(ffmpegOutput, "black_start").ToList();

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

                if(StartRange != null && EndRange != null)
                {
                    if(startTime < StartRange.TotalSeconds || startTime > EndRange.TotalSeconds)
                    {
                        Log.WriteLine($"Skipping: \"{startTime}\"");
                        continue;
                    }
                }

                for(int x = 0; x < split.FileSplits.Count; x++)
                {
                    FileSplitInfo splitter = split.FileSplits[x];
                    TimeSpan start = TimeSpan.FromSeconds(startTime);
                    TimeSpan end = TimeSpan.FromSeconds(endTime);

                    // Is there any other splitInfo that falls with in 60 seconds of the current one found?
                    bool any = split.FileSplits.Where(y => y.BlackStart < start).Any(y => start.TotalSeconds - y.BlackStart.TotalSeconds <= 60);

                    if (splitter.BlackStart <= TimeSpan.Zero && splitter.BlackEnd <= TimeSpan.Zero && !any)
                    {
                        split.FileSplits[x].BlackStart = start;
                        split.FileSplits[x].BlackEnd = end;
                        break;
                    }
                }
            }

            // We add one because when there are 2 black_starts it means there will be 3 episodes and when 3 black_Starts 4 episodes and so on.
            if (episodeNumbers.Length != split.FileSplits.Count)
            {
                throw new InvalidOperationException("The number of splits from black screens do not match the amount of episodes mentioned in the files name.");
            }

            foreach(FileSplitInfo splitter in split.FileSplits)
            {
                if(splitter.BlackStart <= TimeSpan.Zero && splitter.BlackEnd <= TimeSpan.Zero)
                {
                    Log.WriteLine("Could not find any point to split the file.");
                }
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
            ffmpeg.StartInfo.Arguments = $"-i \"{file.File}\" -f segment -segment_times {string.Join(",", file.FileSplits.Where(i => i.BlackStart != TimeSpan.Zero).Select(i => i.Cut.TotalSeconds))} -c:v copy -c:a copy \"{newFile}\"";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            Thread.Sleep(2500);


            // TODO: If it can't find the file don't try to do anything.
            for(int i = 0; i < file.FileSplits.Count; i++)
            {
                FileSplitInfo split = file.FileSplits[i];

                string tempName = Path.Combine(directory, Path.GetFileNameWithoutExtension(file.File) + "_00" + i + extension);
                File.Move(tempName, Path.Combine(directory, split.ToString() + extension));
            }


            if(!Directory.Exists(Path.Combine(directory, "Originals")))
            {
                Directory.CreateDirectory(Path.Combine(directory, "Originals"));
            }

            File.Move(file.File, Path.Combine(directory, "Originals") + "\\" + Path.GetFileName(file.File));

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
