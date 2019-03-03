using System.Text;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System;

namespace MusicTagger2.Core
{
    /// <summary>
    /// Config is singleton responsible for making sure that input/output settings file gets loaded/saved.
    /// </summary>
    class Config
    {
        private static Config instance;
        private XmlDocument currentSettingsXml;
        private List<Song> missingOnDrive = new List<Song>();

        private Config() { }

        public static Config Instance
        {
            get
            {
                if (instance == null)
                    instance = new Config();
                return instance;
            }
        }

        /// <summary>
        /// Sets a given root path and creates a new settings file in given file path.
        /// </summary>
        /// <param name="file">Path to new XML save file.</param>
        /// <param name="root">Root path of songs.</param>
        public void NewSettings(string file)
        {
            currentSettingsXml = new XmlDocument();
            missingOnDrive = new List<Song>();
            SaveUserSettings(Core.Instance.tags, Core.Instance.allSongs, file);
        }

        /// <summary>
        /// Loads all data from settings file and pushes them into Core.
        /// </summary>
        /// <param name="file">Path to XML save file.</param>
        public void LoadSettings(string file)
        {
            currentSettingsXml = new XmlDocument();
            missingOnDrive = new List<Song>();

            // Read file and load XML.
            try
            {
                using (var sr = new StreamReader(file))
                {
                    string xmlString = sr.ReadToEnd();
                    currentSettingsXml.LoadXml(xmlString);
                }
            }
            catch (Exception e) { throw new Exception("An error occured while attempting to read or load XML file. Provided file may be corrupted.", e); }

            // Get all tags from XML and pass them to Core.
            Dictionary<int, SongTag> songTags = new Dictionary<int, SongTag>();
            try
            {
                foreach (XmlNode node in currentSettingsXml.GetElementsByTagName("SongTags")[0].ChildNodes)
                {
                    var tag = new SongTag()
                    {
                        ID = int.Parse(node.Attributes["ID"].Value),
                        Name = node.Attributes["Name"].Value,
                        Category = node.Attributes["Category"].Value
                    };
                    songTags.Add(tag.ID, tag);
                    Core.Instance.tags.Add(tag);
                }
            }
            catch (Exception e) { throw new Exception("Tags were not successfully loaded. Provided file may be corrupted.", e); }

            // Get all songs from XML, assign them to tags and pass them to Core. If some songs are not existing on drive, notify user to determine what to do with them.
            try
            {
                foreach (XmlNode node in currentSettingsXml.GetElementsByTagName("Songs")[0].ChildNodes)
                {
                    var newSong = new Song(node.Attributes["FilePath"].Value);

                    foreach (XmlNode songTagNode in node.ChildNodes)
                    {
                        var tagId = int.Parse(songTagNode.Attributes["ID"].Value);
                        songTags[tagId].AddSong(newSong);
                    }

                    if (File.Exists(newSong.FullPath))
                        Core.Instance.allSongs.Add(newSong.FullPath, newSong);
                    else
                        missingOnDrive.Add(newSong);
                }
            }
            catch (Exception e) { throw new Exception("Songs were not successfully loaded. Provided file may be corrupted.", e); }

            HandleMissingSongs();
        }

        private void HandleMissingSongs()
        {
            if (missingOnDrive.Count > 0)
            {
                using (var sw = new StreamWriter("MissingSongs.txt"))
                {
                    sw.WriteLine("Following songs were in saved settings, but were not found on drive:");
                    foreach (var s in missingOnDrive)
                        sw.WriteLine(s.FullPath);
                }

                MessageBoxResult dialogResult = MessageBox.Show(string.Format("{0} songs were not found on drive (full list can be found in MissingSongs.txt. Do you wish to delete them from the system?", missingOnDrive.Count), "Missing songs found", MessageBoxButton.YesNo);
                if (dialogResult == MessageBoxResult.Yes)
                    missingOnDrive.Clear();
            }
        }

        /// <summary>
        /// Saves provided data into given file.
        /// </summary>
        /// <param name="songTags">All tags to be saved.</param>
        /// <param name="songs">All songs to be saved.</param>
        /// <param name="rootDir">Root directory under which songs are to be found.</param>
        /// <param name="filePath">Destination file into which settings are to be saved,</param>
        public void SaveUserSettings(ObservableCollection<SongTag> songTags, Dictionary<string, Song> songs, string filePath)
        {
            // Make sure path to file exists, otherwise create it.
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            catch (Exception e) { throw new Exception("Could not create path to provided output file.", e); }

            // Create header of XML and its root element.
            XmlDocument outputDocument;
            XmlElement rootElement;
            try
            {
                outputDocument = new XmlDocument();
                XmlDeclaration xmlDeclaration = outputDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = outputDocument.DocumentElement;
                outputDocument.InsertBefore(xmlDeclaration, root);
                rootElement = outputDocument.CreateElement(string.Empty, "Config", string.Empty);
                outputDocument.AppendChild(rootElement);
            }
            catch (Exception e) { throw new Exception("Could not prepare header and/or root of XML file.", e); }

            // Save tags.
            try
            {
                XmlElement tagsElement = outputDocument.CreateElement(string.Empty, "SongTags", string.Empty);
                rootElement.AppendChild(tagsElement);
                foreach (var t in songTags)
                {
                    XmlElement newTag = outputDocument.CreateElement(string.Empty, "SongTag", string.Empty);
                    newTag.SetAttribute("Name", t.Name);
                    newTag.SetAttribute("ID", t.ID.ToString());
                    newTag.SetAttribute("Category", t.Category);
                    tagsElement.AppendChild(newTag);
                }
            }
            catch (Exception e) { throw new Exception("Could not save tags into output file.", e); }

            // Save songs. Also include songs, which were excluded on first load as missing, in case they weren't cleaned up by user.
            try
            {
                foreach (var s in missingOnDrive)
                    songs.Add(s.FullPath, s);
                XmlElement songsElement = outputDocument.CreateElement(string.Empty, "Songs", string.Empty);
                rootElement.AppendChild(songsElement);
                foreach (var s in songs)
                {
                    if (s.Value.WasTagged)
                    {
                        XmlElement newSong = outputDocument.CreateElement(string.Empty, "Song", string.Empty);
                        newSong.SetAttribute("FilePath", s.Value.FullPath);
                        songsElement.AppendChild(newSong);

                        foreach (var t in s.Value.tags)
                        {
                            XmlElement tag = outputDocument.CreateElement(string.Empty, "SongTag", string.Empty);
                            tag.SetAttribute("ID", t.ToString());
                            newSong.AppendChild(tag);
                        }
                    }
                }
            }
            catch (Exception e) { throw new Exception("Could not save songs into output fule.", e); }

            // Save output XML into output file.
            try
            {
                using (TextWriter tw = new StreamWriter(filePath, false, Encoding.UTF8))
                    outputDocument.Save(tw);
            }
            catch (Exception e) { throw new Exception("Could not save output XML as file.", e); }
        }
    }
}
