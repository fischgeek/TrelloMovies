using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrelloClassLibrary;
using System.Text.RegularExpressions;
using System.Diagnostics;
using static SharedLibrary.ConsoleShortcuts;
using System.Configuration;

namespace TrelloMovies
{
    class Program
    {
        private static string movieApiKey = ConfigurationManager.AppSettings["movie-api-key"];
        private static string fischflicks = ConfigurationManager.AppSettings["target-board-id"];
        private static string moviesList = ConfigurationManager.AppSettings["target-list-id"];
        private static string baseMovieUrl
        {
            get {
                return "https://api.themoviedb.org/3";
            }
        }
        private static string movieAuth
        {
            get {
                return $@"api_key={movieApiKey}&language=en-US&page=1&include_adult=false";
            }
        }
        public static List<Card> allMovies = new List<Card>();
        public static Mode mode = Mode.Normal;
        static void Main(string[] args)
        {
            ListCards(moviesList, true);
            while (true) {
                switch (mode) {
                    case Mode.Normal:
                        HandleNormalOperations();
                        break;
                    case Mode.Bulk:
                        HandleBulkOperations();
                        break;
                    case Mode.Config:
                        break;
                    case Mode.Debug:
                        HandleDebugOperations();
                        break;
                    default:
                        break;
                }
            }
        }
        private static void HandleNormalOperations()
        {
            SwitchToNormalOperations();
            var cmd = rl();
            cmd = ProcessCharOptions(cmd);
            NormalOperations normalOps;
            if (Enum.TryParse(cmd, out normalOps)) {
                switch (normalOps) {
                    case NormalOperations.AddMovie:
                        AddMovie();
                        rl();
                        break;
                    case NormalOperations.ViewMovies:
                        ListCards(moviesList);
                        rl();
                        break;
                    case NormalOperations.UpdateDescription:
                        UpdateMovieDescription();
                        rl();
                        break;
                    case NormalOperations.UpdatePoster:
                        UpdateMovieWithPoster();
                        rl();
                        break;
                    default:
                        break;
                }
            }
        }
        private static void HandleBulkOperations()
        {
            SwitchToBulkOperations();
            var cmd = rl();
            cmd = ProcessCharOptions(cmd);
            BulkOperations bulkOps;
            if (Enum.TryParse(cmd, out bulkOps)) {
                switch (bulkOps) {
                    case BulkOperations.UpdateAllDescriptions:
                        UpdateAllDescriptions();
                        rl();
                        break;
                    case BulkOperations.UpdateAllPosters:
                        UpdateAllPosters();
                        rl();
                        break;
                    default:
                        break;
                }
            }
        }
        private static void HandleDebugOperations()
        {
            SwitchToDebugOperations();
            var cmd = rl();
            cmd = ProcessCharOptions(cmd);
            DebugOperations debugOps;
            if (Enum.TryParse(cmd, out debugOps)) {
                switch (debugOps) {
                    case DebugOperations.ViewBoard:
                        rl();
                        break;
                    case DebugOperations.ViewLists:
                        ListLists(fischflicks);
                        rl();
                        break;
                    case DebugOperations.ViewCards:
                        break;
                    case DebugOperations.ForceRequery:
                        ForceRequery();
                        rl();
                        break;
                    case DebugOperations.ShowAllWithNoDescription:
                        foreach (var m in allMovies) {
                            if (m.desc == "No description found") {
                                cl($"{m.name}");
                            }
                        }
                        rl();
                        break;
                    default:
                        break;
                }
            }
        }
        private static void PromptOperationSwitch()
        {
            PrintPromptOperationSwitch();
            var cmd = rl();
            Mode selectedMode;
            if (Enum.TryParse(cmd, out selectedMode)) {
                switch (selectedMode) {
                    case Mode.Normal:
                        SwitchToNormalOperations();
                        break;
                    case Mode.Bulk:
                        SwitchToBulkOperations();
                        break;
                    case Mode.Config:
                        SwitchToConfigOperations();
                        break;
                    case Mode.Debug:
                        SwitchToDebugOperations();
                        break;
                    default:
                        break;
                }
            }
        }
        private static void UpdateMovieDescription()
        {
            var card = FindMovieByTitle();
            if (card != null) {
                var desc = GetMovieDescription(card.name);
                cl("Retrieving movie description...");
                br();
                cl(desc);
                br();
                cl("Would you like to update the movie with this description?");
                var res = rl();
                if (res == "yes" || res == "y") {
                    TrelloOps.UpdateCardDescription(card.id, desc);
                    cl("Done.");
                }
            }
        }
        private static void UpdateMovieWithPoster()
        {
            Card card = FindMovieByTitle();
            if (TrelloOps.GetAttachments(card.id).DataList.Count() == 0) {
                var url = GetMoviePosterUrl(card.name);
                TrelloOps.AddAttachment(card.id, url);
            } else {
                cl("This card already has an attachment.");
            }
        }
        private static Card FindMovieByTitle()
        {
            Card card = null;
            cw("Movie title: ");
            var title = rl();
            var cards = allMovies.Where(x => title.Contains(x.name.ToLower())).ToArray();
            card = null;
            if (cards.Count() == 0) {
                cl("No movies matched that title. Revise and retry.");
            } else if (cards.Count() > 1) {
                cl("Multiple movies matched that title. Revise and retry.");
                foreach (var c in cards) {
                    cl(c.name);
                }
            } else {
                card = cards[0];
            }
            if (card == null) {
                cl("Something else went wrong.");
            }
            return card;
        }
        private static void AddMovie()
        {
            cw("Name of the move: ");
            var name = rl();
            cl("Adding movie card...");
            var newCard = TrelloOps.AddCard(new Card() { name = name }, moviesList);
            cl("Retrieving movie description...");
            var desc = GetMovieDescription(name);
            cl("Retrieving movie poster...");
            var poster = GetMoviePosterUrl(name);
            cl("Updating movie description...");
            TrelloOps.UpdateCardDescription(newCard.id, desc);
            cl("Updating movie poster...");
            TrelloOps.AddAttachment(newCard.id, poster);
            cl("Done.");
        }
        private static void UpdateAllDescriptions()
        {
            cw("Are you sure?");
            var confirm = rl();
            if (confirm == "yes" || confirm == "y") {
                int index = 0;
                foreach (var m in allMovies) {
                    if (m.desc == null || m.desc == "" || m.desc == "No description found") {
                        var desc = GetMovieDescription(m.name);
                        br();
                        cl($@"Updating {m.name} with description: ");
                        cl(desc);
                        br();
                        TrelloOps.UpdateCardDescription(m.id, desc);
                        if (index == 3) {
                            index = 0;
                            System.Threading.Thread.Sleep(1200);
                        }
                        index++;
                    }
                }
            }
        }
        public static void ListLists(string boardId)
        {
            var res = TrelloBase.Instance.GetLists(boardId);
            foreach (var l in res.DataList) {
                cl($"{l.id} -- {l.name}");
            }
            rl();
        }
        public static void ListCards(string listId, bool silent = false, bool includeDesc = false, bool includeIds = false)
        {
            var localListEmpty = allMovies.Count() == 0;
            var cards = allMovies.Count() == 0 ? TrelloOps.GetCards(listId).DataList : allMovies;
            foreach (var c in cards) {
                if (!silent) {
                    if (includeIds) {
                        cl($"{c.id} -- {c.name}");
                    } else {
                        cl(c.name);
                    }
                    if (includeDesc) {
                        br();
                        cl(c.desc);
                        br();
                    }
                }
                if (localListEmpty) {
                    allMovies.Add(c);
                }
            }
        }
        public static string GetMovieDescription(string movieName)
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
            MovieResult x = JsonConvert.DeserializeObject<MovieResult>(GetRequest(url));
            if (x.total_results == 0) {
                description = "No description found";
            } else {
                description = x.results[0].overview;
            }
            return description;
        }
        public static string GetMoviePosterUrl(string movieName)
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
            MovieResult x = JsonConvert.DeserializeObject<MovieResult>(GetRequest(url));
            if (x.total_results == 0) {
                return "";
            }
            posterUrl = "https://image.tmdb.org/t/p/original" + x.results[0].poster_path;
            return posterUrl;
        }
        private static void UpdateAllPosters()
        {
            cw("Are you sure?");
            var confirm = rl();
            if (confirm == "yes" || confirm == "y") {
                int index = 0;
                foreach (var m in allMovies) {
                    if (m.idAttachmentCover == null || m.idAttachmentCover == "") {
                        var posterUrl = GetMoviePosterUrl(m.name);
                        if (posterUrl == "") {
                            continue;
                        }
                        cl($"Updating {m.name} with poster: {posterUrl}");
                        TrelloOps.AddAttachment(m.id, posterUrl);
                        if (index == 3) {
                            index = 0;
                            System.Threading.Thread.Sleep(1200);
                        }
                        index++;
                    }
                }
            }
        }
        public static string GetRequest(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            var response = client.Get(request);
            return response.Content;
        }
        private static void ForceRequery()
        {
            allMovies.Clear();
            ListCards(moviesList);
        }
        private static void SwitchToNormalOperations()
        {
            mode = Mode.Normal;
            ct("TRELLO MOVIES");
            cl("1  - View movies");
            cl("2  - Update a movie description");
            cl("3  - Update a movie with a poster");
            cl("4  - Add a movie");
            bo();
        }
        private static void SwitchToBulkOperations()
        {
            mode = Mode.Bulk;
            ct("TRELLO BULK OPERATIONS");
            cl("1 - Attempt to update all movies that do not have a description");
            cl("2 - Attempt to update all movies that do not have a poster");
            bo();
        }
        private static void SwitchToConfigOperations()
        {
            mode = Mode.Config;
            ct("TRELLO CONFIG OPERATIONS");
            cl("1 - Set Trello API Key");
            cl("2 - Set Trello API Token");
            cl("3 - Set The Movie DB API Key");
            bo();
        }
        private static void SwitchToDebugOperations()
        {
            mode = Mode.Debug;
            ct("TRELLO DEBUG OPERATIONS");
            int i = 1;
            foreach(var e in Enum.GetNames(typeof(DebugOperations))) {
                cl($"{i} - {e}");
                i++;
            }
            bo();
        }
        private static void PrintPromptOperationSwitch()
        {
            ct("TRELLO SWITCH OPERATIONS");
            cl("1 - Normal");
            cl("2 - Bulk");
            cl("3 - Config");
            cl("4 - Debug");
            bo();
        }
        private static string ProcessCharOptions(string cmd)
        {
            switch (cmd) {
                case "x":
                    ExitApp();
                    break;
                case "b":
                    SwitchToNormalOperations();
                    break;
                case "s":
                    PromptOperationSwitch();
                    break;
                default:
                    break;
            }
            return cmd;
        }
    }

    public enum Mode
    {
        Normal = 1,
        Bulk = 2,
        Config = 3,
        Debug = 4
    }

    public enum DebugOperations
    {
        ViewBoard = 1,
        ViewLists = 2,
        ViewCards = 3,
        ForceRequery = 4,
        ShowAllWithNoDescription = 5
    }

    public enum NormalOperations
    {
        ViewMovies = 1,
        UpdateDescription = 2,
        UpdatePoster = 3,
        AddMovie = 4
    }

    public enum BulkOperations
    {
        UpdateAllDescriptions = 1,
        UpdateAllPosters = 2
    }
}
