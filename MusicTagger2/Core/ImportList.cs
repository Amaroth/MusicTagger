using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace MusicTagger2.Core
{
    class ImportList
    {
        public ObservableCollection<Song> Songs = new ObservableCollection<Song>();

        /// <summary>
        /// Adds provided songs into import list.
        /// </summary>
        /// <param name="songs">Songs to be added.</param>
        public void AddIntoImport(List<Song> songs)
        {
            foreach (var s in songs)
                if (!Songs.Contains(s))
                    Songs.Add(s);
        }

        /// <summary>
        /// For valid file paths, creates new songs and adds them into import. For paths to songs already existing in project adds respective songs to import.
        /// </summary>
        /// <param name="filePaths">Paths to files being imported.</param>
        public void AddIntoImport(List<string> filePaths)
        {
            var existingSongs = new Dictionary<string, Song>();
            foreach (var s in Core.Instance.Songs)
                existingSongs.Add(s.FullPath, s);

            foreach (var filePath in filePaths)
                if (File.Exists(filePath) && Utilities.IsFileSupported(filePath))
                {
                    if (existingSongs.ContainsKey(filePath))
                    {
                        if (!Songs.Contains(existingSongs[filePath]))
                            Songs.Add(existingSongs[filePath]);
                    }
                    else
                        Songs.Add(new Song(filePath));
                }
        }

        /// <summary>
        /// Removes provided songs from import.
        /// </summary>
        /// <param name="forRemoval">List of songs to be removed.</param>
        public void RemoveFromImport(List<Song> forRemoval)
        {
            var removeList = new List<Song>();
            foreach (var s in forRemoval)
                removeList.Add(s);

            foreach (var s in removeList)
                Songs.Remove(s);
        }

        /// <summary>
        /// Removes all songs from import, which already have at least 1 tag.
        /// </summary>
        public void ClearImport()
        {
            var remove = new List<Song>();
            foreach (var s in Songs)
                if (s.tags.Count > 0)
                    remove.Add(s);
            foreach (var s in remove)
                Songs.Remove(s);
        }

        /// <summary>
        /// Assigns tags to provided songs.
        /// </summary>
        /// <param name="songs">Songs to be tagged.</param>
        /// <param name="tags">Tags to be added to songs.</param>
        /// <param name="remove">Remove tagged songs from import?</param>
        /// <param name="overwrite">Clean old tags from songs before adding new ones?</param>
        public void AssignTags(List<Song> songs, List<SongTag> tags, bool remove, bool overwrite)
        {
            if (overwrite)
                foreach (var s in songs)
                    s.RemoveFromAllTags();

            foreach (var t in tags)
                foreach (var s in songs)
                    s.AddTag(t);

            if (remove)
                foreach (var s in songs)
                    Songs.Remove(s);
        }
    }
}
