using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMovies
{
    public class Config
    {
        public string trelloApiKey { get; set; }
        public string trelloApiTok { get; set; }
        public string targetBoardId { get; set; }
        public string targetListId { get; set; }
        public string theMovieDBApiKey { get; set; }
    }
}
