using System.Collections.Generic;

namespace MikuDownloader.image
{
    public class ImageData
    {
        public string OriginalImage { get; set; }
        public List<ImageDetails> MatchingImages { get; set; }

        public ImageData()
        {
            MatchingImages = new List<ImageDetails>();
        }

        public ImageData(string origImage)
        {
            OriginalImage = origImage;
            MatchingImages = new List<ImageDetails>();
        }

    }
}
