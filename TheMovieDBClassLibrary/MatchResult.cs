using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMovieDBClassLibrary
{
    public class MatchResult
    {
        public string poster_path { get; set; }
        public bool adult { get; set; }
        public string overview { get; set; }
        public string release_date { get; set; }
        public int[] genre_ids { get; set; }
        public int id { get; set; }
        public string original_title { get; set; }
        public string original_language { get; set; }
        public string title { get; set; }
        public string backdrop_path { get; set; }
        public decimal popularity { get; set; }
        public int vote_count { get; set; }
        public decimal vote_average { get; set; }
    }
}
