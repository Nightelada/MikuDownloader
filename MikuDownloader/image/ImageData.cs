using System.Collections.Generic;

namespace MikuDownloader.image
{
    public class ImageData
    {
        public string OriginalImage { get; set; }
        public List<ImageDetails> MatchingImages { get; set; }
        public bool Duplicate { get; set; }
        public bool HasBetterResolution { get; set; }

        public ImageData()
        {
            MatchingImages = new List<ImageDetails>();
        }

        public ImageData(string origImage)
        {
            OriginalImage = origImage;
            MatchingImages = new List<ImageDetails>();
        }

        public List<string> GetAllMatchingImages()
        {
            List<string> allMatches = new List<string>();

            foreach(ImageDetails matchingImage in MatchingImages)
            {
                allMatches.Add(matchingImage.PostURL);
            }

            return allMatches;
        }
    }
}
