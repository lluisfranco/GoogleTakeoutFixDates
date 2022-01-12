using System;
using System.Collections.Generic;

namespace GoogleTakeoutFixDates
{
    public class PhotosAlbumNode
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string URL { get; set; }
        public DateTime? Date { get; set; }
        public List<PhotoNode> Photos { get; set; } = new();
    }
}
