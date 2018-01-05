using MikuDownloader.enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MikuDownloader
{
    public class ImageDetails
    {
        public string PostURL { get; set; }
        public List<string> Tags { get; set; }
        public string Resolution { get; set; }
        public string OriginalURL { get; set; }
        public string Similarity { get; set; }
        public string ImageName { get; set; }
        public MatchSource MatchSource { get; set; }
        public MatchType MatchType { get; set; }
        public MatchRating MatchRating { get; set; }
        public string ImageSource { get; set; }

        public ImageDetails(string post, string tag, string res, string sim, string mType)
        {
            if (!post.Contains("https:") && !post.Contains("http:"))
            {
                PostURL = String.Format("https:{0}", post);
            }
            else
            {
                PostURL = post;
            }
            Tags = new List<string>();
            SetTags(tag);
            Resolution = res;
            Similarity = sim;
            SetMatchType(mType);

            PerformTransformations();
        }

        // transforms all fields to valid ones
        private void PerformTransformations()
        {
            // get only post ID number
            var temp = PostURL.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            
            // get only resolution
            string[] strAparams = Resolution.Split(new char[] { '×', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (strAparams != null && strAparams.Length > 2)
            {
                Resolution = String.Format("{0}{1}{2}", strAparams[0], '×', strAparams[1]);
                SetMatchRating(strAparams.Last());
            }
            else
            {
                Resolution = "Unavaiable";
                MatchRating = MatchRating.Unavailable;
            }

            // get only similarity %
            string[] strBparams = Similarity.Split(new char[] { '%', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (strAparams != null && strAparams.Length > 1)
            {
                Similarity = String.Format("{0}{1}", strBparams[0], '%');
            }
            else
            {
                Similarity = "Unavaiable";
            }
            SetMatchSource(PostURL);
        }

        private void SetMatchType(string matchType)
        {
            if (matchType.Contains("Best"))
            {
                MatchType = MatchType.BestMatch;
            }
            else if (matchType.Contains("Additional"))
            {
                MatchType = MatchType.AdditionalMatch;
            }
            else if (matchType.Contains("Possible"))
            {
                MatchType = MatchType.PossibleMatch;
            }
            else
            {
                MatchType = MatchType.OtherMatch;
            }
        }

        private void SetMatchRating(string matchRating)
        {
            switch (matchRating)
            {
                case "[Ero]":
                    MatchRating = MatchRating.Ero;
                    break;
                case "[Safe]":
                    MatchRating = MatchRating.Safe;
                    break;
                case "[Explicit]":
                    MatchRating = MatchRating.Explicit;
                    break;
                case "[Unrated]":
                    MatchRating = MatchRating.Unrated;
                    break;
                default:
                    MatchRating = MatchRating.Unavailable;
                    break;
            }
        }

        private void SetMatchSource(string imageSource)
        {
             if (imageSource.Contains("sankaku"))
            {
                MatchSource = MatchSource.SankakuChannel;
            }
            else if (imageSource.Contains("danbooru"))
            {
                MatchSource = MatchSource.Danbooru;
            }
            else if (imageSource.Contains("gelbooru"))
            {
                MatchSource = MatchSource.Gelbooru;
            }
            else if (imageSource.Contains("yande.re"))
            {
                MatchSource = MatchSource.Yandere;
            }
            else if (imageSource.Contains("konachan"))
            {
                MatchSource = MatchSource.Konachan;
            }
            else if (imageSource.Contains("anime-pictures"))
            {
                MatchSource = MatchSource.AnimePictures;
            }
            else if (imageSource.Contains("zerochan"))
            {
                MatchSource = MatchSource.Zerochan;
            }
            else if (imageSource.Contains("theanimegallery"))
            {
                MatchSource = MatchSource.TheAnimeGallery;
            }
            else if (imageSource.Contains("e-shuushuu"))
            {
                MatchSource = MatchSource.Eshuushuu;
            }
        }
        
        private void SetTags(string tag)
        {
            string tagLine;
            string[] parsedTagLine;
            // get only tags
            tagLine = tag.Substring(tag.IndexOf("Tags: ") + "Tags: ".Length);
            // check if tag separator is ','
            parsedTagLine = tagLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parsedTagLine != null && parsedTagLine.Count() > 1)
            {
                foreach (string singleTag in parsedTagLine)
                {
                    Tags.Add(singleTag);
                }
            }
            else
            {
                // check if separator is ' '
                parsedTagLine = tagLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parsedTagLine != null && parsedTagLine.Count() > 1)
                {
                    foreach (string singleTag in parsedTagLine)
                    {
                        Tags.Add(singleTag);
                    }
                }
            }
        }
    }
}
