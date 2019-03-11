using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;

namespace MusicTagger.Core
{
    class ProjectReader
    {
        private XmlDocument xml;

        public ProjectReader(string filePath) => ReadSettings(filePath);

        /// <summary>
        /// Reads data from Project file.
        /// </summary>
        /// <param name="filePath"></param>
        public void ReadSettings(string filePath)
        {
            xml = new XmlDocument();
            try
            {
                using (var sr = new StreamReader(filePath))
                    xml.LoadXml(sr.ReadToEnd());
            }
            catch (Exception e)
            {
                xml = null;
                throw new Exception("An error occured while attempting to read or load Project file. Provided file may be corrupted.", e);
            }
        }

        /// <summary>
        /// Gets a root directory from its Project file.
        /// </summary>
        /// <returns></returns>
        public string GetRootDir()
        {
            try
            {
                return "";// xml.GetElementsByTagName("RootDir")[0].InnerText;
            }
            catch (Exception e)
            {
                throw new Exception("Could not retrieve root dir. Provided Project file may be corrupted.", e);
            }
        }

        /// <summary>
        /// Provides song tags from its Project file.
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<SongTag> GetSongTags()
        {
            var result = new ObservableCollection<SongTag>();
            try
            {
                foreach (XmlNode node in xml.GetElementsByTagName("SongTags")[0].ChildNodes)
                {
                    var songTag = new SongTag()
                    {
                        ID = int.Parse(node.Attributes["ID"].Value),
                        Name = node.Attributes["Name"].Value,
                        Category = node.Attributes["Category"].Value
                    };
                    result.Add(songTag);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Song tags were not successfully loaded. Provided Project file may be corrupted.", e);
            }

            return result;
        }

        /// <summary>
        /// Provides songs from its Project file.
        /// </summary>
        /// <param name="songTags"></param>
        /// <returns></returns>
        public ObservableCollection<Song> GetSongs(ObservableCollection<SongTag> songTags)
        {
            var result = new ObservableCollection<Song>();
            try
            {
                foreach (XmlNode node in xml.GetElementsByTagName("Songs")[0].ChildNodes)
                {
                    var song = new Song(node.Attributes["FilePath"].Value);
                    // TO BE CHANGED - search for song on different drives in case project was moved to different PC with different drive letters
                    /*var drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    if (!File.Exists(song.FullPath))
                        foreach (var c in drives)
                        {
                            var possiblePath = string.Format("{0}{1}", c, song.FullPath.Substring(1));
                            if (File.Exists(possiblePath))
                                    song.FullPath = possiblePath;
                        }*/


                    foreach (XmlNode songTagNode in node.ChildNodes)
                    {
                        foreach (var t in songTags)
                            if (t.ID == int.Parse(songTagNode.Attributes["ID"].Value))
                            {
                                t.AddSong(song);
                                break;
                            }
                    }
                    result.Add(song);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Songs were not successfully loaded. Provided Project file may be corrupted.", e);
            }

            return result;
        }
    }
}
