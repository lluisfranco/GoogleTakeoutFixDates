using System.IO;

namespace GoogleTakeoutFixDates
{
    public static class StringExtensions
    {
        public static string ReplaceInvalidCharsInFileName(this string filename)
        {
            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {
                filename = filename.Replace(c.ToString(), "_");
            }
            return filename;
        }
    }

}
