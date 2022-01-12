namespace GoogleTakeoutFixDates
{
    public class PhotoJsonInfo
    {
        public string title { get; set; }
        public string description { get; set; }
        public string imageViews { get; set; }
        public modificationTime modificationTime { get; set; } = new();
        public geoData geoData { get; set; } = new();
        public geoDataExif geoDataExif { get; set; } = new();
        public photoTakenTime photoTakenTime { get; set; } = new();
    }

    public class geoData
    {
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public decimal altitude { get; set; }
        public decimal latitudeSpan { get; set; }
        public decimal longitudeSpan { get; set; }

    }

    public class geoDataExif : geoData
    {

    }

    public class modificationTime : date
    {

    }

    public class photoTakenTime : date
    {

    }

    public class date
    {
        public string timestamp { get; set; }
        public string formatted { get; set; }
    }
}
