using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicTagger2.Core
{
    class ImportList
    {
        public ObservableCollection<Song> Songs = new ObservableCollection<Song>();



        public void AddIntoImport(List<string> filePaths, ObservableCollection<Song> alreadyExistingOnes)
        {
            try
            {
                // Clean up paths to standard appearance.
                try
                {
                    for (var i = 0; i < filePaths.Count; i++)
                        filePaths[i] = filePaths[i].Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                }
                catch (Exception e) { throw new Exception("Could not clean directory separators in import paths.", e); }

                // Check whether files exist and whether they are supported. In that case, create song objects and add them into import.
                try
                {
                    foreach (var s in filePaths)
                    {
                        if (File.Exists(s) && Utilities.IsFileSupported(s))
                        {
                            var newSong = new Song(s) { WasTagged = false };
                            bool found = false;
                            foreach (var existingSong in alreadyExistingOnes)
                                if (existingSong.FullPath == newSong.FullPath)
                                {
                                    found = true;
                                    break;
                                }
                            if (!found)
                            {
                                Songs.Add(newSong);
                                alreadyExistingOnes.Add(newSong);
                            }
                            bool inImport = false;
                            foreach (var imported in Songs)
                            if (imported.FullPath == newSong.FullPath)
                                {
                                    inImport = true;
                                    break;
                                }
                            else if (!inImport)
                                Songs.Add(newSong);
                        }
                    }
                }
                catch (Exception e) { throw new Exception("Error occured while attempting to create song objects from provided import paths.", e); }
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not add at least one of the provided file paths into the import list. Error message:\n\n{0}", e.ToString())); }
        }

        public void RemoveFromImport(List<Song> forRemoval)
        {
            try
            {
                var removeList = new List<Song>();
                foreach (var s in forRemoval)
                    removeList.Add(s);

                foreach (var s in removeList)
                    Songs.Remove(s);
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not remove provided songs from import list. Following error occured:\n\n{0}", e.ToString())); }
        }

        public void ClearImport()
        {
            try
            {
                var remove = new List<Song>();
                foreach (var s in Songs)
                    if (s.tags.Count > 0)
                        remove.Add(s);
                foreach (var s in remove)
                    Songs.Remove(s);
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not clear the import list. Following error occured:\n\n{0}", e.ToString())); }
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
