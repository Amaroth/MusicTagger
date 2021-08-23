using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace MusicTagger.Core
{
    class Song : INotifyPropertyChanged
    {
        // Name of song (file name without extension).
        private string _songName;
        public string SongName
        {
            get => _songName;
            set
            {
                if (_songName != value)
                {
                    _songName = value;
                    NotifyPropertyChanged("SongName");
                }
            }
        }
        // Name of file song is saved in on drive.
        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    NotifyPropertyChanged("FileName");
                }
            }
        }
        // Full path to song's file on drive.
        private string _fullPath;
        public string FullPath
        {
            get => _fullPath;
            set
            {
                if (_fullPath != value)
                {
                    _fullPath = value;
                    NotifyPropertyChanged("FullPath");
                    FileName = Path.GetFileName(FullPath);
                    SongName = (FileName.Length >= 4) ? FileName.Substring(0, FileName.Length - 4) : "";
                }
            }
        }
        // If song was not tagged yet since it was imported to app, do not save it to saved settings.
        public bool WasTagged { get; private set; }
        // All tags assigned to songs.
        public HashSet<SongTag> tags = new HashSet<SongTag>();

        // Observable properties event handling.
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        private string _tagSignature;
        public string TagSignature
        {
            get => _tagSignature;
            set
            {
                if (_tagSignature != value)
                {
                    _tagSignature = value;
                    NotifyPropertyChanged("TagSignature");
                }
            }
        }
        public void UpdateTagSignature()
        {
            var sb = new StringBuilder();
            if (tags.Count > 0)
            {
                foreach (var t in tags)
                    sb.Append(string.Format("{0}({1}), ", t.Name, t.ID));
                sb.Length -= 2;
            }
            TagSignature = sb.ToString();
        }

        public Song(string filePath)
        {
            FullPath = filePath;
            WasTagged = false;
        }

        /// <summary>
        /// Move song file to provided path.
        /// </summary>
        /// <param name="destination"></param>
        public void Move(string destination)
        {
            if (!File.Exists(destination))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Move(FullPath, destination);
                FullPath = destination;
            }
        }

        /// <summary>
        /// Add provided tag to song's tags.
        /// </summary>
        /// <param name="tag"></param>
        public void AddTag(SongTag tag)
        {
            tags.Add(tag);
            WasTagged = true;
            UpdateTagSignature();
        }

        /// <summary>
        /// Clean song's tags completely. Cleans song from tags as well.
        /// </summary>
        public void RemoveFromAllTags()
        {
            foreach (var t in tags)
                t.songs.Remove(this);
            tags.Clear();
            UpdateTagSignature();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SongName: {0},\nFileName: {1},\nFullPath: {2},\nTag IDs: ", SongName, FileName, FullPath);
            if (tags.Count > 0)
            {
                foreach (var t in tags)
                {
                    sb.Append(t.ID);
                    sb.Append(", ");
                }
                sb.Length -= 2;
            }
            return sb.ToString();
        }
    }
}
