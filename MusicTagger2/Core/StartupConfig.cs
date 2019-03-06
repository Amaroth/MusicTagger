using System.IO;
using System.Text;
using System.Xml;

namespace MusicTagger2.Core
{
    class StartupConfig
    {
        private readonly string configPath = "startup.conf";

        public bool PlayRandom { get; private set; } = true;
        public bool PlayRepeat { get; private set; } = true;
        public Core.FilterType SelectedFilter { get; private set; } = Core.FilterType.Standard;
        public double SongVolume { get; private set; } = 50;
        public double SoundsVolume { get; private set; } = 50;
        public bool SongMute { get; private set; } = false;
        public bool SoundsMute { get; private set; } = false;

        /// <summary>
        /// Loads all settings from config file. If error occurs, it gets ignored and default value is used instead.
        /// </summary>
        public void LoadFile()
        {
            try
            {
                var doc = new XmlDocument();
                using (var sr = new StreamReader(configPath))
                    doc.LoadXml(sr.ReadToEnd());

                foreach (XmlNode node in doc.GetElementsByTagName("StartupConfig")[0].ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "PlayRandom": PlayRandom = bool.Parse(node.InnerText); break;
                        case "PlayRepeat": PlayRepeat = bool.Parse(node.InnerText); break;
                        case "SelectedFilter": SelectedFilter = ParseFilterType(node.InnerText); break;
                        case "SongVolume": SongVolume = double.Parse(node.InnerText); break;
                        case "SoundsVolume": SoundsVolume = double.Parse(node.InnerText); break;
                        case "SongMute": SongMute = bool.Parse(node.InnerText); break;
                        case "SoundsMute": SoundsMute = bool.Parse(node.InnerText); break;
                    }
                }
            }
            catch { }
        }

        public void SaveFile(bool playRandom, bool playRepeat, Core.FilterType selectedFilter, double songVolume, double soundsVolume, bool songMute, bool soundsMute)
        {
            try
            {
                var doc = new XmlDocument();
                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = doc.DocumentElement;
                doc.InsertBefore(xmlDeclaration, root);
                var rootElement = doc.CreateElement("StartupConfig");
                doc.AppendChild(rootElement);

                AppendValue(doc, rootElement, "PlayRandom", playRandom.ToString());
                AppendValue(doc, rootElement, "PlayRepeat", playRepeat.ToString());
                AppendValue(doc, rootElement, "SelectedFilter", selectedFilter.ToString());
                AppendValue(doc, rootElement, "SongVolume", songVolume.ToString());
                AppendValue(doc, rootElement, "SoundsVolume", soundsVolume.ToString());
                AppendValue(doc, rootElement, "SongMute", songMute.ToString());
                AppendValue(doc, rootElement, "SoundsMute", soundsMute.ToString());

                using (TextWriter tw = new StreamWriter(configPath, false, Encoding.UTF8))
                    doc.Save(tw);
            }
            catch { }
        }

        private void AppendValue(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var element = doc.CreateElement(name);
            element.InnerText = value;
            parent.AppendChild(element);
        }

        private Core.FilterType ParseFilterType(string name)
        {
            if (name == Core.FilterType.Standard.ToString())
                return Core.FilterType.Standard;
            else if (name == Core.FilterType.And.ToString())
                return Core.FilterType.And;
            else
                return Core.FilterType.Or;
        }
    }
}
