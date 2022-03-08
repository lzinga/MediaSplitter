using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaSplitter.Common
{
    public class MediaFile
    {
        #region Private Fields
        private FileInfo _fileInfo;
        private Match _fileNameMatches;
        #endregion

        #region Public Properties
        public FileInfo FileInfo => _fileInfo;
        public string Extension => _fileInfo.Extension;
        public List<BlackScreenInfo> BlackScreenInfo { get; set; } = new List<Common.BlackScreenInfo>();
        public TimeSpan[] CutTimes => BlackScreenInfo.Select(i => i.MiddleTime).ToArray();
        public string Season => _fileNameMatches.Groups[1].Value.Trim();
        public List<EpisodeInfo> EpisodeInfo { get; private set; } = new List<EpisodeInfo>();
        public int EpisodeCount => EpisodeInfo.Count;
        #endregion

        #region public Constants
        public const string RegexMultiEpisodeFile = @"[^-]*- ([Ss]\d+)((?:[Ee]\d+[-])*[Ee]\d+) +((?:.)*)(?=[.])";
        #endregion

        #region Constructor
        public MediaFile(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            _fileInfo = new FileInfo(file);

            if (!Regex.IsMatch(Path.GetFileName(_fileInfo.Name), MediaFile.RegexMultiEpisodeFile))
            {
                throw new InvalidOperationException($"File \"{_fileInfo.FullName}\" does not match the Regular Expression \"{MediaFile.RegexMultiEpisodeFile}\"");
            }

            _fileNameMatches = Regex.Match(_fileInfo.Name, MediaFile.RegexMultiEpisodeFile);

            // Get all the episodes
            string[] episodeNumbers = _fileNameMatches.Groups[2].Value.Trim().Split('-');
            string[] episodeNames = _fileNameMatches.Groups[3].Value.Trim().Split('+');

            if(episodeNumbers.Length < 0 || episodeNames.Length < 0)
            {
                throw new InvalidOperationException($"Did not get any valid episode numbers from the file name \"{_fileInfo.Name}\".");
            }

            if(episodeNumbers.Length != episodeNames.Length)
            {
                throw new InvalidOperationException($"Episode numbers and the titles should be the same amount, found \"{episodeNumbers.Length}\" episode numbers and \"{episodeNames.Length}\" episode names from the file \"{_fileInfo.Name}\"");
            }

            for(int i = 0; i < episodeNumbers.Length; i++)
            {
                this.EpisodeInfo.Add(new EpisodeInfo(this.Season, episodeNumbers[i].Trim(), episodeNames[i].Trim()));
            }
        }
        #endregion
    }
}
