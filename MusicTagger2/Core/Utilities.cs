using System.Text;

namespace MusicTagger2.Core
{
    class Utilities
    {
        public static bool IsFileSupported(string filePath) => filePath.ToLower().EndsWith(".mp3");

        public static string GetTimeString(int timeInSeconds)
        {
            var hours = timeInSeconds / 3600;
            var minutes = (timeInSeconds % 3600) / 60;
            var seconds = timeInSeconds % 60;

            var result = new StringBuilder();
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
