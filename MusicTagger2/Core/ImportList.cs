using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace MusicTagger2.Core
{
    class ImportList
    {
        public ObservableCollection<Song> Songs = new ObservableCollection<Song>();


        public void AddIntoImport(List<Song> songs)
        {
            foreach (var s in songs)
                if (!Songs.Contains(s))
                    Songs.Add(s);
        }

        public void AddIntoImport(List<string> filePaths, ObservableCollection<Song> alreadyExistingOnes)
        {
            var existingSongs = new Dictionary<string, Song>();
            foreach (var s in alreadyExistingOnes)
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


        public void RemoveFromImport(List<Song> forRemoval)
        {
            var removeList = new List<Song>();
            foreach (var s in forRemoval)
                removeList.Add(s);

            foreach (var s in removeList)
                Songs.Remove(s);
        }

        public void ClearImport()
        {
            var remove = new List<Song>();
            foreach (var s in Songs)
                if (s.tags.Count > 0)
                    remove.Add(s);
            foreach (var s in remove)
                Songs.Remove(s);
        }

        public void AssignTags(List<Song> songs, List<SongTag> tags, bool remove, bool overwrite)
        {
            try
            {
                // If tags are to be overwritten, start by removing any old ones.
                try
                {
                    if (overwrite)
                        foreach (var s in songs)
                            s.RemoveFromAllTags();
                }
                catch (Exception e) { throw new Exception("Error occured while attempting to remove original tags from songs.", e); }

                // Assign all tags to all songs, make sure they are saved into config file on next save.
                try
                {
                    foreach (var t in tags)
                        foreach (var s in songs)
                        {
                            t.AddSong(s);
                            s.WasTagged = true;
                        }
                }
                catch (Exception e) { throw new Exception("Could not assing tags to songs.", e); }

                // If songs are to be removed from import, remove all of them from there.
                if (remove)
                    RemoveFromImport(songs);
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not assign tags to songs, or something else failed during the process. Error message:\n\n{0}", e.ToString())); }
        }
    }
}
