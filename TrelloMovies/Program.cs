using System;
using System.Collections.Generic;
using System.Linq;
using TrelloClassLibrary;
using static SharedLibrary.ConsoleShortcuts;
using System.Configuration;
using TheMovieDBClassLibrary;

namespace TrelloMovies
{
    // todo extract movie methods into a class
    // todo fix case of card (title case)
    // todo add ability to edit a title
    // todo add ability to add a format label to a movie
    class Program
    {
        private static string trelloApiKey = ConfigurationManager.AppSettings["trello-key"];
        private static string trelloApiToken = ConfigurationManager.AppSettings["trello-token"];
        private static string movieApiKey = ConfigurationManager.AppSettings["movie-api-key"];
        private static string fischflicks = ConfigurationManager.AppSettings["target-board-id"];
        private static string moviesList = ConfigurationManager.AppSettings["target-list-id"];
        private static MovieBase mb = MovieBase.Instance;
        private static TrelloBase tb = TrelloBase.Instance;
        public static List<Card> allMovies = new List<Card>();

        // todo change verbage from "mode" to "menu"
        public static Mode mode = Mode.Normal;
        static void Main(string[] args)
        {
            mb.Init(movieApiKey);
            tb.Init(trelloApiKey, trelloApiToken);
            ListMovies(moviesList, true);
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
                        ListMovies(moviesList);
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
                        cl(fischflicks);
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

        #region Normal
        private static void UpdateMovieDescription()
        {
            var movie = FindMovieCardByTitle();
            if (movie != null) {
                var desc = mb.GetMovieDescription(movie.name);
                cl("Retrieving movie description...");
                br();
                cl(desc);
                br();
                cl("Would you like to update the movie with this description?");
                var res = rl();
                if (res == "yes" || res == "y") {
                    tb.UpdateCardDescription(movie.id, desc);
                    cl("Done.");
                }
            }
        }
        private static void UpdateMovieWithPoster()
        {
            Card card = FindMovieCardByTitle();
            if (tb.GetAttachments(card.id).DataList.Count() == 0) {
                var url = mb.GetMoviePosterUrl(card.name);
                tb.AddAttachment(card.id, url);
            } else {
                cl("This card already has an attachment.");
            }
        }
        private static Card FindMovieCardByTitle()
        {
            Card movie = null;
            cw("Movie title: ");
            var title = rl();
            var movies = allMovies.Where(x => title.Contains(x.name.ToLower())).ToArray();
            movie = null;
            if (movies.Count() == 0) {
                cl("No movies matched that title. Revise and retry.");
            } else if (movies.Count() > 1) {
                cl("Multiple movies matched that title. Revise and retry.");
                foreach (var c in movies) {
                    cl(c.name);
                }
            } else {
                movie = movies[0];
            }
            if (movie == null) {
                cl("Something else went wrong.");
            }
            return movie;
        }
        private static void AddMovie()
        {
            cw("Name of the move: ");
            var name = rl();
            cl("Adding movie card...");
            var newCardId = tb.AddCard(new Card() { name = name }, moviesList);
            cl("Retrieving movie description...");
            var desc = mb.GetMovieDescription(name);
            cl("Retrieving movie poster...");
            var poster = mb.GetMoviePosterUrl(name);
            cl("Updating movie description...");
            tb.UpdateCardDescription(newCardId, desc);
            cl("Updating movie poster...");
            tb.AddAttachment(newCardId, poster);
            cl("Done.");
        }
        #endregion

        #region Bulk
        private static void UpdateAllDescriptions()
        {
            cw("Are you sure?");
            var confirm = rl();
            if (confirm == "yes" || confirm == "y") {
                int index = 0;
                foreach (var m in allMovies) {
                    if (m.desc == null || m.desc == "" || m.desc == "No description found") {
                        var desc = mb.GetMovieDescription(m.name);
                        br();
                        cl($@"Updating {m.name} with description: ");
                        cl(desc);
                        br();
                        tb.UpdateCardDescription(m.id, desc);
                        if (index == 3) {
                            index = 0;
                            System.Threading.Thread.Sleep(1200);
                        }
                        index++;
                    }
                }
            }
        }
        private static void UpdateAllPosters()
        {
            cw("Are you sure?");
            var confirm = rl();
            if (confirm == "yes" || confirm == "y") {
                int index = 0;
                foreach (var m in allMovies) {
                    if (m.idAttachmentCover == null || m.idAttachmentCover == "") {
                        var posterUrl = mb.GetMoviePosterUrl(m.name);
                        if (posterUrl == "") {
                            continue;
                        }
                        cl($"Updating {m.name} with poster: {posterUrl}");
                        TrelloBase.Instance.AddAttachment(m.id, posterUrl);
                        if (index == 3) {
                            index = 0;
                            System.Threading.Thread.Sleep(1200);
                        }
                        index++;
                    }
                }
            }
        }
        #endregion

        #region Debug
        public static void ListLists(string boardId)
        {
            var res = tb.GetLists(boardId);
            foreach (var l in res.DataList) {
                cl($"{l.id} -- {l.name}");
            }
            rl();
        }
        public static void ListMovies(string listId, bool silent = false, bool includeDesc = false, bool includeIds = false)
        {
            var localListEmpty = allMovies.Count() == 0;
            var movies = allMovies.Count() == 0 ? tb.GetCards(listId).DataList : allMovies;
            foreach (var m in movies) {
                if (!silent) {
                    if (includeIds) {
                        cl($"{m.id} -- {m.name}");
                    } else {
                        cl(m.name);
                    }
                    if (includeDesc) {
                        br();
                        cl(m.desc);
                        br();
                    }
                }
                if (localListEmpty) {
                    allMovies.Add(m);
                }
            }
        }
        private static void ForceRequery()
        {
            allMovies.Clear();
            ListMovies(moviesList);
        }
        #endregion

        private static void SwitchToNormalOperations()
        {
            mode = Mode.Normal;
            ct("TRELLO MOVIES");
            cl("1 - View movies");
            cl("2 - Update a movie description");
            cl("3 - Update a movie with a poster");
            cl("4 - Add a movie");
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
