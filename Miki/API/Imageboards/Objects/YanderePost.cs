using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;

namespace Miki.API.Imageboards.Objects
{
    internal class YanderePost : BooruPost, ILinkable
    {
        public string Url => file_url;
		public string SourceUrl => source;
		public string Provider => "Yande.re";

		public int created_at { get; set; }
        public int updated_at { get; set; }
        public int creator_id { get; set; }
        public object approver_id { get; set; }
        public string author { get; set; }
        public int change { get; set; }
        public string source { get; set; }
        public string md5 { get; set; }
        public int file_size { get; set; }
        public string file_ext { get; set; }
        public string file_url { get; set; }
        public bool is_shown_in_index { get; set; }
        public string preview_url { get; set; }
        public int preview_width { get; set; }
        public int preview_height { get; set; }
        public int actual_preview_width { get; set; }
        public int actual_preview_height { get; set; }
        public string sample_url { get; set; }
        public int sample_width { get; set; }
        public int sample_height { get; set; }
        public int sample_file_size { get; set; }
        public string jpeg_url { get; set; }
        public int jpeg_width { get; set; }
        public int jpeg_height { get; set; }
        public int jpeg_file_size { get; set; }
        public string rating { get; set; }
        public bool is_rating_locked { get; set; }
        public bool has_children { get; set; }
        public object parent_id { get; set; }
        public string status { get; set; }
        public bool is_pending { get; set; }
        public bool is_held { get; set; }
        public string frames_pending_string { get; set; }
        public List<object> frames_pending { get; set; }
        public string frames_string { get; set; }
        public List<object> frames { get; set; }
        public bool is_note_locked { get; set; }
        public int last_noted_at { get; set; }
        public int last_commented_at { get; set; }
    }
}
