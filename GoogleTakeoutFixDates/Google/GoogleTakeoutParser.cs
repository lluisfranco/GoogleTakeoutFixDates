using System.Collections.Generic;

namespace GoogleTakeoutFixDates
{
    public class GoogleTakeoutParser
    {
        public string BaseFolderPath { get; set; }
        public string PhotosFolderPath { get; set; }
        public List<PhotosAlbumNode> PhotoAlbums { get; set; } = new();
    }
}
