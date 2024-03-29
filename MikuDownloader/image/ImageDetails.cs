﻿using MikuDownloader.enums;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MikuDownloader
{
    public class ImageDetails
    {
        public string PostURL { get; set; }
        public List<string> Tags { get; set; }
        public string Resolution { get; set; }
        public string Similarity { get; set; }
        public string ImageName { get; set; }
        public MatchSource MatchSource { get; set; }
        public MatchType MatchType { get; set; }
        public MatchRating MatchRating { get; set; }
        public int Priority { get; set; }

        public ImageDetails()
        {

        }

        public ImageDetails(string post, string tag, string res, string sim, string mType)
        {
            if (!post.Contains("https:") && !post.Contains("http:"))
            {
                PostURL = string.Format("https:{0}", post);
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

        public ImageDetails(string post, ImmutableList<string> tags, string res, string sim, string mType)
        {
            if (!post.Contains("https:") && !post.Contains("http:"))
            {
                PostURL = string.Format("https:{0}", post);
            }
            else
            {
                PostURL = post;
            }
            Tags = tags.ToList();
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
                Resolution = string.Format("{0}{1}{2}", strAparams[0], '×', strAparams[1]);
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
                Similarity = string.Format("{0}{1}", strBparams[0], '%');
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
            if (imageSource.Contains("danbooru"))
            {
                MatchSource = MatchSource.Danbooru;
                Priority = 1;
            }
            else if (imageSource.Contains("sankaku"))
            {
                MatchSource = MatchSource.SankakuChannel;
                Priority = 2;
            }
            else if (imageSource.Contains("gelbooru"))
            {
                MatchSource = MatchSource.Gelbooru;
                Priority = 3;
            }
            else if (imageSource.Contains("yande.re"))
            {
                MatchSource = MatchSource.Yandere;
                Priority = 4;
            }
            else if (imageSource.Contains("konachan"))
            {
                MatchSource = MatchSource.Konachan;
                Priority = 5;
            }
            else if (imageSource.Contains("zerochan"))
            {
                MatchSource = MatchSource.Zerochan;
                Priority = 6;
            }
            else if (imageSource.Contains("e-shuushuu"))
            {
                MatchSource = MatchSource.Eshuushuu;
                Priority = 7;
            }
            else if (imageSource.Contains("anime-pictures"))
            {
                MatchSource = MatchSource.AnimePictures;
                Priority = 8;
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
