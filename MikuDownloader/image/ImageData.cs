﻿using MikuDownloader.enums;
using System.Collections.Generic;

namespace MikuDownloader.image
{
    public class ImageData
    {
        public string OriginalImage { get; set; }
        public FileType OriginalImageType { get; set; }
        public List<ImageDetails> MatchingImages { get; set; }
        public bool Duplicate { get; set; }
        public int DuplicateIndex { get; set; }
        public bool HasBetterResolution { get; set; }
        public bool? HasBeenDownloaded { get; set; }

        public ImageData()
        {
            MatchingImages = new List<ImageDetails>();
        }

        public ImageData(string origImage, FileType fileType)
        {
            OriginalImage = origImage;
            OriginalImageType = fileType;
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
