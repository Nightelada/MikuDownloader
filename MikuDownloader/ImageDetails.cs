using MikuDownloader.enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MikuDownloader
{
    public class ImageDetails
    {
        public string PostURL { get; set; }
        public string PostID { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; set; }
        public string Resolution { get; set; }
        public string OriginalURL { get; set; }
        public string Similarity { get; set; }
        public string ImageName { get; set; }
        public MatchSource MatchSource { get; set; }
        public MatchType MatchType { get; set; }
        public MatchRating MatchRating { get; set; }

        public ImageDetails(string post, string src, string tag, string res, string sim, string mType)
        {
            if (!post.Contains("https:") && !post.Contains("http:"))
            {
                PostURL = String.Format("https:{0}", post);
            }
            else
            {
                PostURL = post;
            }
            Source = src;
            Tags = new List<string>();
            SetTags(tag);
            Resolution = res;
            Similarity = sim;
            SetMatchType(mType);

            PerformTransformations();

            SetImageName();
        }

        // transforms all fields to valid ones
        private void PerformTransformations()
        {
            // get only post ID number
            var temp = PostURL.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            PostID = temp.Last();
            
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
            // transform to final image url
            Source = TransformURL(Source);

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

        // transforms the image source to proper image URL
        private string TransformURL(string imageSource)
        {
            string transformedURL = string.Empty;

            // maybe different splits for different sites ?
            // md5 hash is the same for all (with same resolution/extension) ?
            string[] parts = imageSource.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string tempURL = parts.Last();

            // if-case for matching site (yandere, sankaku, etc..)
            // check extension (.jpg,.png,.gif), maybe split it and try different kinds of URLs if they return image
            // can also check tags for (png,vector,transparent,etc...)
            // manually add extensions!!!!
            if (imageSource.Contains("sankaku"))
            {
                MatchSource = MatchSource.SankakuChannel;
                transformedURL = String.Format("https://{0}/{1}{2}/{3}{4}/{5}", Constants.SankakuStart, tempURL[0], tempURL[1], tempURL[2], tempURL[3], tempURL);
            }
            else if (imageSource.Contains("danbooru"))
            {
                MatchSource = MatchSource.Danbooru;
                transformedURL = String.Format("https://{0}/{1}", Constants.DanbooruStart, tempURL);
            }
            else if (imageSource.Contains("gelbooru"))
            {
                MatchSource = MatchSource.Gelbooru;
                transformedURL = String.Format("https://{0}/{1}{2}/{3}{4}/{5}", Constants.GelbooruStart, tempURL[0], tempURL[1], tempURL[2], tempURL[3], tempURL);
            }
            else if (imageSource.Contains("yandere") || imageSource.Contains("moe.imouto")) // can probably be done without tags !!
            {
                MatchSource = MatchSource.Yandere;
                tempURL = tempURL.Remove(tempURL.IndexOf("."));
                transformedURL = String.Format("https://{0}/{1}/yande.re {2} {3}.jpg", Constants.YandereStart, tempURL, PostID, Tags);
            }
            else if (imageSource.Contains("konachan")) // can be done without tags !!
            {
                MatchSource = MatchSource.Konachan;
                tempURL = tempURL.Remove(tempURL.IndexOf("."));
                transformedURL = String.Format("https://{0}/{1}/Konachan.com - {2} {3}.jpg", Constants.KonachanStart, tempURL, PostID, Tags);
            }
            else if (imageSource.Contains("anime-pictures"))
            {
                MatchSource = MatchSource.AnimePictures;
                PostID = PostID.Remove(PostID.IndexOf("?"));
                transformedURL = String.Format("https://{0}/{1}.jpg", Constants.AnimePicturesStart, PostID);
            }
            else if (imageSource.Contains("zerochan"))
            {
                MatchSource = MatchSource.Zerochan;
                transformedURL = PostURL;
            }
            else if (imageSource.Contains("theanimegallery") || (imageSource.Contains("anigal")))
            {
                MatchSource = MatchSource.TheAnimeGallery;
                transformedURL = PostURL;
            }
            else if (imageSource.Contains("e-shuushuu"))
            {
                MatchSource = MatchSource.Eshuushuu;
                transformedURL = PostURL;
            }
            else
            {
                transformedURL = String.Format("Unavailable");
            }

            return transformedURL;
        }

        // sets image name to not contain extension
        private void SetImageName()
        {
            string[] tempStringArray = Source.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ImageName = tempStringArray.Last();
            // TODO: make with constants and foreach with linq
            if (ImageName.Contains(".jpg"))
            {
                ImageName = ImageName.Remove(ImageName.IndexOf(".jpg"));
            }
            else if (ImageName.Contains(".png"))
            {
                ImageName = ImageName.Remove(ImageName.IndexOf(".png"));
            }
            else if (ImageName.Contains(".gif"))
            {
                ImageName = ImageName.Remove(ImageName.IndexOf(".gif"));
            }
            else if (ImageName.Contains(".jpeg"))
            {
                ImageName = ImageName.Remove(ImageName.IndexOf(".jpeg"));
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
