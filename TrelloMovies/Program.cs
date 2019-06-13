using System;
using System.Collections.Generic;
using System.Linq;
using TrelloClassLibrary;
using static SharedLibrary.ConsoleShortcuts;
using System.Configuration;
using TheMovieDBClassLibrary;
using System.IO;
using SharedLibrary;
using Newtonsoft.Json;
using System.Globalization;

namespace TrelloMovies
{
    // todo add ability to edit a title
    // todo add ability to add a format label to a movie
    class Program
    {
        private static string AppName = ConfigurationManager.AppSettings["app-name"];
        private static string appdata = Environment.GetEnvironmentVariable("AppData");
        private static MovieBase mb = MovieBase.Instance;
        private static TrelloBase tb = TrelloBase.Instance;
        private static List<Card> allMovies = new List<Card>();
        private static Config config = new Config();
        public static Mode mode = Mode.Normal;
        private static string projectDir
        {
            get {
                return Path.Combine(appdata, AppName);
            }
        }
        private static string configFile
        {
            get {
                return Path.Combine(appdata, $@"{AppName}\settings.json");
            }
        }
        static void Main(string[] args)
        {
            LoadConfig();
            mb.Init(config.theMovieDBApiKey);
            tb.Init(config.trelloApiKey, config.trelloApiTok);
            ListMovies(config.targetListId, true);
            while (true) {
                switch (mode) {
                    case Mode.Normal:
                        HandleNormalOperations();
                        break;
                    case Mode.Bulk:
                        HandleBulkOperations();
                        break;
                    case Mode.Config:
                        HandleConfigOperations();
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
                        break;
                    case NormalOperations.ViewMovies:
                        ListMovies(config.targetListId);
                        break;
                    case NormalOperations.UpdateDescription:
                        UpdateMovieDescription();
                        break;
                    case NormalOperations.UpdatePoster:
                        UpdateMovieWithPoster();
                        break;
                    default:
                        break;
                }
                rl();
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
                        break;
                    case BulkOperations.UpdateAllPosters:
                        UpdateAllPosters();
                        break;
                    default:
                        break;
                }
                rl();
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
                        cl(config.targetBoardId);
                        break;
                    case DebugOperations.ViewLists:
                        ListLists(config.targetBoardId);
                        break;
                    case DebugOperations.ViewCards:
                        break;
                    case DebugOperations.ForceRequery:
                        ForceRequery();
                        break;
                    case DebugOperations.ShowAllWithNoDescription:
                        foreach (var m in allMovies) {
                            if (m.desc == "No description found") {
                                cl($"{m.name}");
                            }
                        }
                        break;
                    default:
                        break;
                }
                rl();
            }
        }
        private static void HandleConfigOperations()
        {
            SwitchToConfigOperations();
            var cmd = rl();
            cmd = ProcessCharOptions(cmd);
            ConfigOperations configOps;
            if (Enum.TryParse(cmd, out configOps)) {
                switch (configOps) {
                    case ConfigOperations.SetTrelloAPIKey:
                        SetTrelloAPIKey();
                        break;
                    case ConfigOperations.SetTrelloAPIToken:
                        SetTrelloAPIToken();
                        break;
                    case ConfigOperations.SetTargetBoardID:
                        SetTrelloBoardId();
                        break;
                    case ConfigOperations.SetTargetListID:
                        SetTrelloListId();
                        break;
                    case ConfigOperations.SetTheMoveDBAPIKey:
                        SetTheMovieDBAPIKey();
                        break;
                    default:
                        break;
                }
                rl();
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
                var res = rl("Would you like to update the movie with this description?");
                if (res == "yes" || res == "y") {
                    tb.UpdateCardDescription(movie.id, desc);
                    cl("Done.");
                }
            }
        }
        private static void UpdateMovieWithPoster()
        {
            Card card = FindMovieCardByTitle();
            cl("Retrieving movie poster...");
            if (tb.GetAttachments(card.id).DataList.Count() == 0) {
                var url = mb.GetMoviePosterUrl(card.name);
                cl("Updating movie poster...");
                tb.AddAttachment(card.id, url);
            } else {
                cl("This card already has an attachment.");
            }
            cl("Done.");
        }
        private static Card FindMovieCardByTitle()
        {
            Card movie = null;
            var title = rl("Movie title");
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
            var newCardId = tb.AddCard(new Card() { name = name.ToTitleCase() }, config.targetListId);
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
            var confirm = rl("Are you sure?");
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
            var confirm = rl("Are you sure?");
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

        #region Config
        private static void SetTrelloAPIKey()
        {
            cl($"Current Trello API Key: {config.trelloApiKey}");
            var newApiKey = rl("Enter your Trello API Key");
            if (!newApiKey.JFIsNull() && newApiKey != config.trelloApiKey) {
                config.trelloApiKey = newApiKey;
                TrelloBase.Instance.Init(config.trelloApiKey, config.trelloApiTok);
                SaveConfig();
            }
        }
        private static void SetTrelloAPIToken()
        {
            cl($"Current Trello API Token: {config.trelloApiTok}");
            var newApiTok = rl("Enter your Trello API Token");
            if (!newApiTok.JFIsNull() && newApiTok != config.trelloApiTok) {
                config.trelloApiTok = newApiTok;
                TrelloBase.Instance.Init(config.trelloApiKey, config.trelloApiTok);
                SaveConfig();
            }
        }
        private static void SetTrelloBoardId()
        {
            cl($"Current board ID: {config.targetBoardId}");
            var boards = TrelloBase.Instance.GetBoards().DataList;
            var index = 1;
            foreach (var b in boards) {
                cl($"[{index}] {b.name}");
                index++;
            }
            var boardSelection = rl("Enter a number for a Board from above where you store your movie collection").Trim();
            int boardPos;
            if (Int32.TryParse(boardSelection, out boardPos)) {
                var newBoardId = boards[boardPos - 1].id;
                cl($"You've selected board {boards[boardPos - 1].name}");
                if (newBoardId != config.targetBoardId) {
                    config.targetBoardId = newBoardId;
                }
                SaveConfig();
                cl("Done.");
            }
        }
        private static void SetTrelloListId()
        {
            if (config.targetBoardId.JFIsNull()) {
                cl("Your target Board ID is not set.");
            } else {
                cl($"Current List ID: {config.targetListId}");
                var lists = TrelloBase.Instance.GetLists(config.targetBoardId).DataList;
                var index = 1;
                foreach (var l in lists) {
                    cl($"[{index}] {l.name}");
                    index++;
                }
                var listSelection = rl("Enter a number for a List from above where you store your movie collection on the board").Trim();
                int listPos;
                if (Int32.TryParse(listSelection, out listPos)) {
                    var newListId = lists[listPos - 1].id;
                    cl($"You've selected list {lists[listPos - 1].name}");
                    if (newListId != config.targetListId) {
                        config.targetListId = newListId;
                    }
                    SaveConfig();
                    cl("Done.");
                }
            }
        }
        private static void SetTheMovieDBAPIKey()
        {
            cl($"Current TheMovieDB API Key: {config.theMovieDBApiKey}");
            var newApiKey = rl("Enter your TheMovieDB API Key");
            if (!newApiKey.JFIsNull() && newApiKey != config.theMovieDBApiKey) {
                config.theMovieDBApiKey = newApiKey;
                MovieBase.Instance.Init(config.theMovieDBApiKey);
                SaveConfig();
            }
        }
        private static void LoadConfig()
        {
            var errors = false;
            if (!File.Exists(configFile)) {
                cl("Unable to load the config file.");
                errors = true;
            } else {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFile));
            }
            if (config.trelloApiKey.JFIsNull()) {
                cl("Trello API Key not configured.");
                errors = true;
            }
            if (config.trelloApiTok.JFIsNull()) {
                cl("Trello API Token not configured.");
                errors = true;
            }
            if (config.targetBoardId.JFIsNull()) {
                cl("Trello Board ID is not configured.");
                errors = true;
            }
            if (config.targetListId.JFIsNull()) {
                cl("Trello List ID is not configured.");
                errors = true;
            }
            if (config.theMovieDBApiKey.JFIsNull()) {
                cl("The TheMovieDB API Key is not configured.");
                errors = true;
            }
            if (errors) {
                rl();
            }
        }
        private static void SaveConfig()
        {
            if (!Directory.Exists(projectDir)) {
                Directory.CreateDirectory(projectDir);
            }
            var settings = JsonConvert.SerializeObject(config);
            try {
                File.WriteAllText(configFile, settings);
                cl("Settings file saved successfully.");
            } catch (Exception ex) {
                cl("There was an issue saving the settings file.");
                cl(ex.Message);
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
            if (movies != null) {
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
        }
        private static void ForceRequery()
        {
            allMovies.Clear();
            ListMovies(config.targetListId);
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
            cl("3 - Set the Trello Board ID");
            cl("4 - Set the Trello List ID");
            cl("5 - Set TheMovieDB API Key");
            bo();
        }
        private static void SwitchToDebugOperations()
        {
            mode = Mode.Debug;
            ct("TRELLO DEBUG OPERATIONS");
            int i = 1;
            foreach (var e in Enum.GetNames(typeof(DebugOperations))) {
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

    public enum ConfigOperations
    {
        SetTrelloAPIKey = 1,
        SetTrelloAPIToken = 2,
        SetTargetBoardID = 3,
        SetTargetListID = 4,
        SetTheMoveDBAPIKey = 5
    }
}