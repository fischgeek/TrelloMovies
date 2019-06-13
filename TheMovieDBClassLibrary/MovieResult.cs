using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMovieDBClassLibrary
{
    public class MovieResult
    {
        public int page { get; set; }
        public List<MatchResult> results { get; set; }
        public int total_results { get; set; }
        public int total_pages { get; set; }
    }
}
