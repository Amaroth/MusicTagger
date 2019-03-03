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

        private Config()
        {

        }

        public static Config Instance
        {
            get
            {
                if (instance == null)
                    instance = new Config();
                return instance;
            }
        }

        private XmlDocument xml;
        List<Song> missing = new List<Song>();

        private void Reset()
        {
            xml = new XmlDocument();
            missing = new List<Song>();
            Core.Instance.tags.Clear();
            Core.Instance.importList.Clear();
            Core.Instance.currentPlaylist.Clear();
            Core.Instance.allSongs.Clear();
            Core.Instance.randomIndexList.Clear();
        }

        /// <summary>
        /// Sets a given root path and creates a new settings file in given file path.
        /// </summary>
        /// <param name="file">Path to new XML save file.</param>
        /// <param name="root">Root path of songs.</param>
        public void NewSettings(string file)
        {
            Reset();
            SaveUserSettings(Core.Instance.tags, Core.Instance.allSongs, file);
        }

        /// <summary>
        /// Loads all data from settings file and pushes them into Core.
        /// </summary>
        /// <param name="file">Path to XML save file.</param>
        public void LoadSettings(string file)
        {
            Reset();

            // Read file and load XML.
            try
            {
                using (var sr = new StreamReader(file))
                {
                    string xmlString = sr.ReadToEnd();
                    sr.Close();
                    xml.LoadXml(xmlString);
                }
            }
            catch (Exception e) { throw new Exception("An error occured while attempting to read or load XML file. Provided file may be corrupted.", e); }

            // Get all tags from XML and pass them to Core.
            Dictionary<int, SongTag> tags = new Dictionary<int, SongTag>();
            try
            {
                foreach (XmlNode node in xml.GetElementsByTagName("Tags")[0].ChildNodes)
                {
                    var tag = new SongTag()
                    {
                        ID = int.Parse(node.Attributes["ID"].Value),
                        Name = node.Attributes["Name"].Value,
                        Category = node.Attributes["Category"].Value
                    };
                    tags.Add(tag.ID, tag);
                    Core.Instance.tags.Add(tag);
                }
            }
            catch (Exception e) { throw new Exception("Tags were not successfully loaded. Provided file may be corrupted.", e); }

            // Get all songs from XML, assign them to tags and pass them to Core. If some songs are not existing on drive, notify user to determine what to do with them.
            try
            {
                foreach (XmlNode node in xml.GetElementsByTagName("Songs")[0].ChildNodes)
                {
                    var newSong = new Song(node.Attributes["FilePath"].Value);

                    foreach (XmlNode tagNode in node.ChildNodes)
                    {
                        var tagId = int.Parse(tagNode.Attributes["ID"].Value);
                        tags[tagId].AddSong(newSong);
                    }

                    if (File.Exists(newSong.FullPath))
                        Core.Instance.allSongs.Add(newSong.FullPath, newSong);
                    else
                        missing.Add(newSong);
                }

                using (var sw = new StreamWriter("MissingSongs.txt"))
                {
                    sw.WriteLine("Following songs were in saved settings, but were not found on drive:");
                    foreach (var s in missing)
                        sw.WriteLine(s.FullPath);
                }

                if (missing.Count > 0)
                {
                    MessageBoxResult dialogResult = MessageBox.Show(string.Format("{0} songs were not found on drive (full list can be found in MissingSongs.txt. Do you wish to delete them from the system?", missing.Count), "Missing songs found", MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.Yes)
                        missing.Clear();
                }
            }
            catch (Exception e) { throw new Exception("Songs were not successfully loaded. Provided file may be corrupted.", e); }
        }

        /// <summary>
        /// Saves provided data into given file.
        /// </summary>
        /// <param name="tags">All tags to be saved.</param>
        /// <param name="songs">All songs to be saved.</param>
        /// <param name="rootDir">Root directory under which songs are to be found.</param>
        /// <param name="file">Destination file into which settings are to be saved,</param>
        public void SaveUserSettings(ObservableCollection<SongTag> tags, Dictionary<string, Song> songs, string file)
        {
            // Make sure path to file exists, otherwise create it.
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
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
                XmlElement tagsElement = outputDocument.CreateElement(string.Empty, "Tags", string.Empty);
                rootElement.AppendChild(tagsElement);
                foreach (var t in tags)
                {
                    XmlElement newTag = outputDocument.CreateElement(string.Empty, "Tag", string.Empty);
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
                foreach (var s in missing)
                    songs.Add(s.FullPath, s);
                XmlElement songsElement = outputDocument.CreateElement(string.Empty, "Songs", string.Empty);
                rootElement.AppendChild(songsElement);
                foreach (var s in songs)
                {
                    if (s.Value.Save)
                    {
                        XmlElement newSong = outputDocument.CreateElement(string.Empty, "Song", string.Empty);
                        newSong.SetAttribute("FilePath", s.Value.FullPath);
                        songsElement.AppendChild(newSong);

                        foreach (var t in s.Value.tags)
                        {
                            XmlElement tag = outputDocument.CreateElement(string.Empty, "Tag", string.Empty);
                            tag.SetAttribute("ID", t.Key.ToString());
                            newSong.AppendChild(tag);
                        }
                    }
                }
            }
            catch (Exception e) { throw new Exception("Could not save songs into output fule.", e); }

            // Save output XML into output file.
            try
            {
                using (TextWriter tw = new StreamWriter(file, false, Encoding.UTF8))
                {
                    outputDocument.Save(tw);
                    tw.Close();
                }
            }
            catch (Exception e) { throw new Exception("Could not save output XML as file.", e); }
        }
    }
}
