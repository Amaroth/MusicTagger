using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicTagger2.Core
{
    class Song
    {
        public string SongName { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public bool Save { get; set; }
        public HashSet<SongTag> tags = new HashSet<SongTag>();

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
            Save = true;
        }

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

        private void UpdateDerived()
        {
            FileName = Path.GetFileName(FullPath);
            SongName = FileName.Substring(0, FileName.Length - 4);
        }

        public void AddTag(SongTag tag)
        {
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

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
