using System;

namespace GoogleTakeoutFixDates
{
    public class PhotoNode
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public DateTime? Date { get; set; }
        public PhotosAlbumNode AlbumNode { get; set; }
    }
}
