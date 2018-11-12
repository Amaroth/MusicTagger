using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace MusicTagger2.Core
{
    class Song
    {
        public string SongName { get; set; }
        public string FileName { get; set; }
        public string SubDir { get; set; }
        public bool Save { get; set; }
        public Dictionary<int, Tag> tags = new Dictionary<int, Tag>();

        public string FullPath
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(Core.Instance.rootDir);
                sb.Append(SubDir);
                sb.Append(FileName);
                return sb.ToString();
            }
        }

        public string SubPath
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(SubDir);
                sb.Append(FileName);
                return sb.ToString();
            }
        }


        public Song(string filePath)
        {
            FileName = Path.GetFileName(filePath);
            SongName = FileName.Substring(0, FileName.Length - 4);
            SubDir = Utilities.MakeRelative(filePath, Core.Instance.rootDir).Replace(FileName, "");
            Save = true;
        }

        public Song(string filePath, string rootDir)
        {
            FileName = Path.GetFileName(filePath);
            SongName = FileName.Substring(0, FileName.Length - 4);
            SubDir = Utilities.MakeRelative(filePath, rootDir).Replace(FileName, "");
            Save = true;
        }

        public void Move(string destination)
        {
            if (!File.Exists(destination))
            {
                new FileInfo(destination).Directory.Create();
                File.Move(FullPath, destination);
                FileName = Path.GetFileName(destination);
                SongName = FileName.Substring(0, FileName.Length - 4);
                SubDir = Utilities.MakeRelative(destination, Core.Instance.rootDir).Replace(FileName, "");
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

        public void AddTag(Tag tag)
        {
            if (!tags.ContainsKey(tag.ID))
                tags.Add(tag.ID, tag);
        }

        public void RemoveFromTags()
        {
            foreach (var t in tags)
                t.Value.songs.Remove(SubDir);
            tags.Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("SongName: {0}\n", SongName));
            sb.Append(string.Format("FileName: {0}\n", FileName));
            sb.Append(string.Format("SubDir: {0}\n", SubDir));
            sb.Append(string.Format("FullPath: {0}\n", FullPath));
            sb.Append(string.Format("SubPath: {0}\n", SubPath));
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
