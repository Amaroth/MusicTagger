using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;

namespace MusicTagger2.Core
{
    class SettingsReader
    {
        private XmlDocument xml;

        public SettingsReader() { }

        public SettingsReader(string filePath) => ReadSettings(filePath);

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

        public HashSet<SongTag> GetTags()
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
                MessageBox.Show("Song tags were not successfully loaded. Provided settings file may be corrupted. Error message:\n" + e.ToString());
            }

            return result;
        }

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
                MessageBox.Show("Songs were not successfully loaded. Provided settings file may be corrupted. Error message:\n" + e.ToString());
            }

            return result;
        }
    }
}
