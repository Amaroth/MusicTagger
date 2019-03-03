using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicTagger2.Core
{
    class Song
    {
        // Name of song (file name without extension).
        public string SongName { get; set; }
        // Name of file song is saved in on drive.
        public string FileName { get; set; }
        // Full path to song's file on drive.
        public string FullPath { get; set; }
        // If song was not tagged yet since it was imported to app, do not save it to saved settings.
        public bool WasTagged { get; set; }
        // All tags assigned to songs.
        public HashSet<SongTag> tags = new HashSet<SongTag>();

        /// <summary>
        /// Returns string of comma separated IDs of all tags assigned to this song.
        /// </summary>
        public string TagIds
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (tags.Count > 0)
                {
                    foreach (var t in tags)
                        sb.Append(string.Format("{0}, ", t.ID));
                    sb.Length -= 2;
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns string of comma separated names of all tags assigned to this song.
        /// </summary>
        public string TagNames
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (tags.Count > 0)
                {
                    foreach (var t in tags)
                        sb.Append(string.Format("{0}, ", t.Name));
                    sb.Length -= 2;
                }
                return sb.ToString();
            }
        }

        public Song(string filePath)
        {
            FullPath = filePath;
            UpdateDerived();
            WasTagged = true;
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
                UpdateDerived();
            }
        }

        /// <summary>
        /// Update attributes derived from song's FullPath.
        /// </summary>
        private void UpdateDerived()
        {
            FileName = Path.GetFileName(FullPath);
            SongName = FileName.Substring(0, FileName.Length - 4);
        }

        /// <summary>
        /// Add provided tag to song's tags.
        /// </summary>
        /// <param name="tag"></param>
        public void AddTag(SongTag tag)
        {
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        /// <summary>
        /// Clean song's tags completely. Cleans song from tags as well.
        /// </summary>
        public void RemoveFromAllTags()
        {
            foreach (var t in tags)
                t.songs.Remove(this);
            tags.Clear();
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
