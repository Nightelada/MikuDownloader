using System.Collections.Generic;

namespace MikuDownloader.misc.rest
{
    internal class SankakuChannelRest
    {// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
        public class Author
        {
            public int? id { get; set; }
            public string name { get; set; }
            public string avatar { get; set; }
            public string avatar_rating { get; set; }
        }

        public class CreatedAt
        {
            public string json_class { get; set; }
            public int? s { get; set; }
            public int? n { get; set; }
        }

        public class Root
        {
            public int? id { get; set; }
            public string rating { get; set; }
            public string status { get; set; }
            public Author author { get; set; }
            public string sample_url { get; set; }
            public int? sample_width { get; set; }
            public int? sample_height { get; set; }
            public string preview_url { get; set; }
            public int? preview_width { get; set; }
            public int? preview_height { get; set; }
            public string file_url { get; set; }
            public int? width { get; set; }
            public int? height { get; set; }
            public int? file_size { get; set; }
            public string file_type { get; set; }
            public CreatedAt created_at { get; set; }
            public bool has_children { get; set; }
            public bool has_comments { get; set; }
            public bool has_notes { get; set; }
            public bool is_favorited { get; set; }
            public object user_vote { get; set; }
            public string md5 { get; set; }
            public int? parent_id { get; set; }
            public int? change { get; set; }
            public int? fav_count { get; set; }
            public int? recommended_posts { get; set; }
            public int? recommended_score { get; set; }
            public int? vote_count { get; set; }
            public int? total_score { get; set; }
            public int? comment_count { get; set; }
            public string source { get; set; }
            public bool in_visible_pool { get; set; }
            public bool is_premium { get; set; }
            public bool is_rating_locked { get; set; }
            public bool is_note_locked { get; set; }
            public bool is_status_locked { get; set; }
            public bool redirect_to_signup { get; set; }
            public object sequence { get; set; }
            public object generation_directives { get; set; }
            public List<Tag> tags { get; set; }
            public object video_duration { get; set; }
            public List<object> reactions { get; set; }
        }

        public class Tag
        {
            public int? id { get; set; }
            public string name_en { get; set; }
            public string name_ja { get; set; }
            public int? type { get; set; }
            public int? count { get; set; }
            public int? post_count { get; set; }
            public int? pool_count { get; set; }
            public int? series_count { get; set; }
            public string locale { get; set; }
            public string rating { get; set; }
            public int? version { get; set; }
            public string tagName { get; set; }
            public int? total_post_count { get; set; }
            public int? total_pool_count { get; set; }
            public string name { get; set; }
        }


    }
}
