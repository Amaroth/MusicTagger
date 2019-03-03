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
        public Dictionary<int, SongTag> tags = new Dictionary<int, SongTag>();

        public Song(string filePath)
        {
            FileName = Path.GetFileName(filePath);
            SongName = FileName.Substring(0, FileName.Length - 4);
            FullPath = filePath;
            Save = true;
        }

        public void Move(string destination)
        {
            if (!File.Exists(destination))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Move(FullPath, destination);
                FullPath = destination;
                FileName = Path.GetFileName(destination);
                SongName = FileName.Substring(0, FileName.Length - 4);
            }
        }

        public string TagIds
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (tags.Count > 0)
                {
                    foreach (var tag in tags.Values)
                        sb.Append(string.Format("{0}, ", tag.ID));
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
                    foreach (var tag in tags.Values)
                        sb.Append(string.Format("{0}, ", tag.Name));
                    sb.Length -= 2;
                }
                return sb.ToString();
            }
        }

        public void AddTag(SongTag tag)
        {
            if (!tags.ContainsKey(tag.ID))
                tags.Add(tag.ID, tag);
        }

        public void RemoveFromTags()
        {
            foreach (var t in tags)
                t.Value.songs.Remove(FullPath);
            tags.Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("SongName: {0}\n", SongName));
            sb.Append(string.Format("FileName: {0}\n", FileName));
            sb.Append(string.Format("FullPath: {0}\n", FullPath));
            sb.Append("Tag IDs: ");
            foreach (var tag in tags)
            {
                sb.Append(tag.Value.ID);
                sb.Append(", ");
            }
            return sb.ToString();
        }
    }
}
