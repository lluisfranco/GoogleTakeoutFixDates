using ExifLibrary;
using System;

namespace GoogleTakeoutFixDates
{
    public static class ImageExtensions
    {
        public static DateTime? GetDateMetadata(string photoURL)
        {
            var file = ImageFile.FromFile(photoURL);
            var date = file.Properties.Get<ExifDateTime>(ExifTag.DateTimeOriginal);
            if (date?.Value != null) 
                return date;
            else
                return null;
        }

        public static void SaveDateMetadata(string photoURL, DateTime date)
        {
            var file = ImageFile.FromFile(photoURL);
            file.Properties.Set(ExifTag.DateTimeOriginal, date);
            file.Save(photoURL);
        }
    }

}
