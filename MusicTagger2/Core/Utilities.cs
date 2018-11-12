using System;
using System.Text;
using System.Xml;

namespace MusicTagger2.Core
{
    class Utilities
    {
        /// <summary>
        /// Adds a new element as child under provided node in give xml.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="innerText"></param>
        /// <param name="comment"></param>
        public static void XmlAddElement(XmlDocument xml, XmlNode parent, string name, string innerText, string comment)
        {
            XmlNode newNode = xml.CreateElement(name);
            if (comment != "" && comment != null)
            {
                XmlComment newComment = xml.CreateComment(name + ": " + comment);
                parent.AppendChild(newComment);
            }
            newNode.InnerText = innerText;
            parent.AppendChild(newNode);
        }

        public static string MakeRelative(string filePath, string referencePath)
        {
            return filePath.Substring(referencePath.Length);
        }

        public static bool IsFileSupported(string path)
        {
            return path.ToLower().EndsWith(".mp3");
        }

        public static string GetTimeString(int timeInSeconds)
        {
            int hours = timeInSeconds / 3600;
            int minutes = (timeInSeconds % 3600) / 60;
            int seconds = timeInSeconds % 60;

            StringBuilder result = new StringBuilder();
            result.Append(string.Format("{0}:", hours));

            if (minutes < 10)
                result.Append("0");
            result.Append(string.Format("{0}:", minutes));

            if (seconds < 10)
                result.Append("0");
            result.Append(seconds);

            return result.ToString();
        }
    }
}
