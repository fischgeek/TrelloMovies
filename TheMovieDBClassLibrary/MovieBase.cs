using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharedLibrary;
using static SharedLibrary.ConsoleShortcuts;

namespace TheMovieDBClassLibrary
{
    public class MovieBase
    {
        public static MovieBase Instance = new MovieBase();
        private MovieBase() { }
        public static MovieBase GetInstance()
        {
            return Instance;
        }
        public void Init(string apiKey) => this.apiKey = apiKey;
        public string apiKey { get; set; }
        private string baseMovieUrl = "https://api.themoviedb.org/3";
        private string posterBaseUrl = "https://image.tmdb.org/t/p/original";
        private string movieAuth
        {
            get {
                return $@"api_key={apiKey}&language=en-US&page=1&include_adult=false";
            }
        }
        public string GetMovieDescription(string movieName)
        {
            var match = Regex.Match(movieName, @"\(\d{4}\)");
            var year = "";
            if (match.Success) {
                year = match.Value.Replace("(", "");
                year = year.Replace(")", "");
                cl("year is " + year);
                movieName = movieName.Replace(match.Value, "");
            }
            var url = $@"{baseMovieUrl}/search/movie/?query={movieName}&{movieAuth}&year={year}";
            var description = "";
            MovieResult x = JsonConvert.DeserializeObject<MovieResult>(WebCall.GetRequest(url));
            if (x.total_results == 0) {
                description = "No description found";
            } else {
                description = x.results[0].overview;
            }
            return description;
        }
        public string GetMoviePosterUrl(string movieName)
        {
            var match = Regex.Match(movieName, @"\(\d{4}\)");
            var year = "";
            if (match.Success) {
                year = match.Value.Replace("(", "");
                year = year.Replace(")", "");
                cl("year is " + year);
                movieName = movieName.Replace(match.Value, "");
            }
            var url = $@"{baseMovieUrl}/search/movie/?query={movieName}&{movieAuth}&year={year}";
            var posterUrl = "";
            MovieResult x = JsonConvert.DeserializeObject<MovieResult>(WebCall.GetRequest(url));
            if (x.total_results == 0) {
                return "";
            }
            posterUrl = posterBaseUrl + x.results[0].poster_path;
            return posterUrl;
        }
    }
}
