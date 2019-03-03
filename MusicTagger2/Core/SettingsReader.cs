using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MusicTagger2.Core
{
    class SettingsReader
    {
        private XmlDocument xml;

        public SettingsReader() { }

        /// <summary>
        /// Calls ReadSettings on creation.
        /// </summary>
        /// <param name="filePath"></param>
        public SettingsReader(string filePath) => ReadSettings(filePath);

        /// <summary>
        /// Reads data from provided file.
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
                throw new Exception("An error occured while attempting to read or load XML file. Provided file may be corrupted.", e);
            }
        }

        /// <summary>
        /// Provides song tags from its settings file.
        /// </summary>
        /// <returns></returns>
        public HashSet<SongTag> GetSongTags()
        {
            var result = new HashSet<SongTag>();
            try
            {
                foreach (XmlNode node in xml.GetElementsByTagName("SongTags")[0].ChildNodes)
                {
                    result.Add(new SongTag()
                    {
                        ID = int.Parse(node.Attributes["ID"].Value),
                        Name = node.Attributes["Name"].Value,
                        Category = node.Attributes["Category"].Value
                    });
                }
            }
            catch (Exception e)
            {
                throw new Exception("Song tags were not successfully loaded. Provided settings file may be corrupted.", e);
            }

            return result;
        }

        /// <summary>
        /// Provides songs from its settings file.
        /// </summary>
        /// <param name="songTags"></param>
        /// <returns></returns>
        public HashSet<Song> GetSongs(Dictionary<int, SongTag> songTags)
        {
            var result = new HashSet<Song>();
            try
            {
                foreach (XmlNode node in xml.GetElementsByTagName("Songs")[0].ChildNodes)
                {
                    var newSong = new Song(node.Attributes["FilePath"].Value);
                    foreach (XmlNode songTagNode in node.ChildNodes)
                    {
                        var tagID = int.Parse(songTagNode.Attributes["ID"].Value);
                        if (songTags.ContainsKey(tagID))
                            songTags[tagID].AddSong(newSong);
                    }
                    result.Add(newSong);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Songs were not successfully loaded. Provided settings file may be corrupted.", e);
            }

            return result;
        }
    }
}
