// -----------------------------------------------------------------------------
// DEBUG MODE — comment out for production builds.
//   Server: appends stats_debug_server.txt to the ./stats/ folder.
//   Client: appends stats_debug_client.txt to Application.persistentDataPath.
// -----------------------------------------------------------------------------
// #define DEBUG_MODE

// -----------------------------------------------------------------------------
// API DUMP — enable to write puck_api_dump.txt on first server start.
// Reflects every loaded assembly at runtime and dumps types/methods/fields so
// broken API references can be identified after a game update.
// Comment out after grabbing the dump file.
// -----------------------------------------------------------------------------
// #define PUCK_API_DUMP

    using Codebase;
using HarmonyLib;
using Newtonsoft.Json;
using StatsTooltip.Configs;
// Alias to disambiguate from the game's global ServerConfig struct
using ModServerConfig = StatsTooltip.Configs.ServerConfig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatsTooltip {
    public class Stats : IPuckPlugin {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private static readonly string MOD_VERSION = "1.31";

        /// <summary>
        /// The client version the server requires. Sent to each connecting client so it can
        /// compare against its own MOD_VERSION and notify the user if out of date.
        /// Bump this whenever a client update is mandatory/recommended.
        /// </summary>
        private static readonly string COMPATIBLE_CLIENT_VERSION = "1.31";

        /// <summary>
        /// ReadOnlyCollection of string, collection of datanames to not log.
        /// </summary>
        private static readonly ReadOnlyCollection<string> DATA_NAMES_TO_IGNORE = new ReadOnlyCollection<string>(new List<string> {
            "eventName",
        });

        /// <summary>
        /// Const string, data name for batching the SOG.
        /// </summary>
        private const string BATCH_SOG = Constants.MOD_NAME + "BATCHSOG";

        /*/// <summary>
        /// Const string, data name for resetting the SOG.
        /// </summary>
        private const string RESET_SOG = Constants.MOD_NAME + "RESETSOG";*/

        /// <summary>
        /// Const string, data name for batching the save percentage.
        /// </summary>
        private const string BATCH_SAVEPERC = Constants.MOD_NAME + "BATCHSAVEPERC";

        /*/// <summary>
        /// Const string, data name for resetting the save percentage.
        /// </summary>
        private const string RESET_SAVEPERC = Constants.MOD_NAME + "RESETSAVEPERC";*/

        /// <summary>
        /// Const string, data name for batching the blocked shots.
        /// </summary>
        private const string BATCH_BLOCK = Constants.MOD_NAME + "BATCHBLOCK";

        /*/// <summary>
        /// Const string, data name for resetting the blocked shots.
        /// </summary>
        private const string RESET_BLOCK = Constants.MOD_NAME + "RESETBLOCK";*/

        /// <summary>
        /// Const string, data name for batching the hits.
        /// </summary>
        private const string BATCH_HIT = Constants.MOD_NAME + "BATCHHIT";

        /*/// <summary>
        /// Const string, data name for resetting the hits.
        /// </summary>
        private const string RESET_HIT = Constants.MOD_NAME + "RESETHIT";*/

        /// <summary>
        /// Const string, data name for batching the takeaways.
        /// </summary>
        private const string BATCH_TAKEAWAY = Constants.MOD_NAME + "BATCHTAKEAWAY";

        /*/// <summary>
        /// Const string, data name for resetting the takeaways.
        /// </summary>
        private const string RESET_TAKEAWAY = Constants.MOD_NAME + "RESETTAKEAWAY";*/

        /// <summary>
        /// Const string, data name for batching the turnovers.
        /// </summary>
        private const string BATCH_TURNOVER = Constants.MOD_NAME + "BATCHTURNOVER";

        /*/// <summary>
        /// Const string, data name for resetting the turnovers.
        /// </summary>
        private const string RESET_TURNOVER = Constants.MOD_NAME + "RESETTURNOVER";*/

        /// <summary>
        /// Const string, data name for batching the passes.
        /// </summary>
        private const string BATCH_PASS = Constants.MOD_NAME + "BATCHPASS";

        private const string BATCH_PUCK_TOUCH = Constants.MOD_NAME + "BATCHPUCKTOUCH";

        private const string BATCH_EXIT = Constants.MOD_NAME + "BATCHEXIT";

        private const string BATCH_ENTRY = Constants.MOD_NAME + "BATCHENTRY";

        private const string BATCH_POSSESSION_TIME = Constants.MOD_NAME + "BATCHPOSSESSIONTIME";

        private const string BATCH_PUCK_BATTLE_WINS = Constants.MOD_NAME + "BATCHPUCKBATTLEWINS";

        private const string BATCH_PUCK_BATTLE_LOSSES = Constants.MOD_NAME + "BATCHPUCKBATTLELOSSES";

        private const string BATCH_SHOT_ATTEMPTS = Constants.MOD_NAME + "BATCHSHOTATTEMPTS";
        private const string BATCH_HOME_PLATE_SOGS = Constants.MOD_NAME + "BATCHHOMEPLATESOGS";
        private const string BATCH_TEAM_SHOTS = Constants.MOD_NAME + "BATCHTEAMSHOTS";
        private const string BATCH_TEAM_SHOT_ATTEMPTS = Constants.MOD_NAME + "BATCHTEAMSHOTATTEMPTS";
        private const string BATCH_TEAM_HOME_PLATE_SOGS = Constants.MOD_NAME + "BATCHTEAMHOMEPLATESOGS";
        private const string BATCH_TEAM_PASSES = Constants.MOD_NAME + "BATCHTEAMPASSES";
        private const string BATCH_TEAM_FACEOFF_WINS = Constants.MOD_NAME + "BATCHTEAMFACEOFFWINS";
        private const string BATCH_TEAM_FACEOFF_TOTAL = Constants.MOD_NAME + "BATCHTEAMFACEOFFTOTAL";
        private const string BATCH_TEAM_POSSESSION_TIME = Constants.MOD_NAME + "BATCHTEAMPOSSESSIONTIME";
        private const string BATCH_TEAM_PUCK_BATTLE_WINS = Constants.MOD_NAME + "BATCHTEAMPUCKBATTLEWINS";
        private const string BATCH_TEAM_PUCK_BATTLE_LOSSES = Constants.MOD_NAME + "BATCHTEAMPUCKBATTLELOSSES";
        private const string BATCH_TEAM_TAKEAWAYS = Constants.MOD_NAME + "BATCHTEAMTAKEAWAYS";
        private const string BATCH_TEAM_TURNOVERS = Constants.MOD_NAME + "BATCHTEAMTURNOVERS";
        private const string BATCH_TEAM_EXITS = Constants.MOD_NAME + "BATCHTEAMEXITS";
        private const string BATCH_TEAM_ENTRIES = Constants.MOD_NAME + "BATCHTEAMENTRIES";
        private const string BATCH_STICK_SAVES = Constants.MOD_NAME + "BATCHSTICKSAVES";
        private const string BATCH_BODY_SAVES = Constants.MOD_NAME + "BATCHBODYSAVES";
        private const string BATCH_HOME_PLATE_SAVES = Constants.MOD_NAME + "BATCHHOMEPLATESAVES";
        private const string BATCH_HOME_PLATE_SHOTS_FACED = Constants.MOD_NAME + "BATCHHOMEPLATESHOTSFACED";

        /*/// <summary>
        /// Const string, data name for resetting the passes.
        /// </summary>
        private const string RESET_PASS = Constants.MOD_NAME + "RESETPASS";*/

        /// <summary>
        /// Const string, data name for resetting all stats.
        /// </summary>
        private const string RESET_ALL = Constants.MOD_NAME + "RESETALL";

        /// <summary>
        /// Const string, data name for receiving a star player.
        /// </summary>
        private const string STAR = Constants.MOD_NAME + "STAR";

        private const string MEDAL_GOLD = "\U0001F947";
        private const string MEDAL_SILVER = "\U0001F948";
        private const string MEDAL_BRONZE = "\U0001F949";

        /// <summary>White star (U+2B50) for scoreboard rich-text; medal emoji often fails in UITK labels.</summary>
        private const string STAR_GLYPH = "\u2B50";

        private static string GetStarMedalGlyph(int starKey) {
            if (starKey == 1) return MEDAL_GOLD;
            if (starKey == 2) return MEDAL_SILVER;
            if (starKey == 3) return MEDAL_BRONZE;
            return "";
        }

        private const string SOG_HEADER_LABEL_NAME = "SOGHeaderLabel";

        private const string SOG_LABEL = "SOGLabel";

        private const string SOG_CELL_NAME = "SOGCell";

        private const string HIT_HEADER_LABEL_NAME = "HitHeaderLabel";

        private const string HIT_LABEL = "HitLabel";

        private const string TURNOVER_HEADER_LABEL_NAME = "TurnoverHeaderLabel";

        private const string TURNOVER_LABEL = "TurnoverLabel";

        private const string TAKEAWAY_HEADER_LABEL_NAME = "TakeawayHeaderLabel";

        private const string TAKEAWAY_LABEL = "TakeawayLabel";
        #endregion

        #region Fields and Properties
        // Server-side.
        /// <summary>
        /// ModServerConfig, config set and sent by the server.
        /// </summary>
        internal static ModServerConfig ModServerConfig { get; set; } = new ModServerConfig();

        private static bool? _rulesetModEnabled = null;

        private static bool _sendSavePercDuringGoalNextFrame = false;

        private static Player _sendSavePercDuringGoalNextFrame_Player = null;

        private static Vector3 _puckLastCoordinate = Vector3.zero;

        private static float _puckZCoordinateDifference = 0;

        /// <summary>
        /// LockDictionary of ulong and string, dictionary of all players clientId and steamId.
        /// </summary>
        private static readonly LockDictionary<ulong, string> _players_ClientId_SteamId = new LockDictionary<ulong, string>();

        /// <summary>
        /// Clients whose mod version didn't match COMPATIBLE_CLIENT_VERSION at connect time.
        /// Keyed by clientId, value is the client's reported version string.
        /// Broadcast is deferred until Event_OnPlayerRoleChanged so the player's name is populated.
        /// </summary>
        private static readonly LockDictionary<ulong, string> _pendingVersionMismatch = new LockDictionary<ulong, string>();

        /// <summary>
        /// Last MOD_VERSION reported by each client via ASK_SERVER_FOR_STARTUP_DATA.
        /// </summary>
        private static readonly LockDictionary<ulong, string> _clientReportedModVersions = new LockDictionary<ulong, string>();



        private static readonly LockDictionary<PlayerTeam, SaveCheck> _checkIfPuckWasSaved = new LockDictionary<PlayerTeam, SaveCheck> {
            { PlayerTeam.Blue, new SaveCheck() },
            { PlayerTeam.Red, new SaveCheck() },
        };

        private static readonly LockDictionary<PlayerTeam, BlockCheck> _checkIfPuckWasBlocked = new LockDictionary<PlayerTeam, BlockCheck> {
            { PlayerTeam.Blue, new BlockCheck() },
            { PlayerTeam.Red, new BlockCheck() },
        };

        private static readonly LockDictionary<PlayerTeam, bool> _lastShotWasCounted = new LockDictionary<PlayerTeam, bool> {
            { PlayerTeam.Blue, true },
            { PlayerTeam.Red, true },
        };

        /// <summary>
        /// Guards against SendSavePercDuringGoal firing twice for the same goal
        /// (once from the Harmony Prefix patch, once from Event_OnStatsTrigger SOG path).
        /// Reset to false whenever a new shot cycle begins (_lastShotWasCounted reset path).
        /// </summary>
        private static readonly LockDictionary<PlayerTeam, bool> _savePercDuringGoalProcessed = new LockDictionary<PlayerTeam, bool> {
            { PlayerTeam.Blue, false },
            { PlayerTeam.Red, false },
        };

        private static readonly LockDictionary<PlayerTeam, bool> _lastBlockWasCounted = new LockDictionary<PlayerTeam, bool> {
            { PlayerTeam.Blue, true },
            { PlayerTeam.Red, true },
        };

        private static readonly LockDictionary<PlayerTeam, (string SteamId, DateTime Time)> _lastPlayerOnPuckTipIncludedSteamId = new LockDictionary<PlayerTeam, (string, DateTime)> {
            { PlayerTeam.Blue, ("", DateTime.MinValue) },
            { PlayerTeam.Red, ("", DateTime.MinValue) },
        };

        private static readonly LockDictionary<PlayerTeam, (string SteamId, DateTime Time)> _lastPlayerOnPuckSteamId = new LockDictionary<PlayerTeam, (string, DateTime)> {
            { PlayerTeam.Blue, ("", DateTime.MinValue) },
            { PlayerTeam.Red, ("", DateTime.MinValue) },
        };

        /// <summary>
        /// LockDictionary of string and Stopwatch, dictionary of all players current puck touch time.
        /// </summary>
        private static readonly LockDictionary<string, Stopwatch> _playersCurrentPuckTouch = new LockDictionary<string, Stopwatch>();

        /// <summary>
        /// LockDictionary of string and Stopwatch, dictionary of all players last puck touch time.
        /// </summary>
        private static readonly LockDictionary<string, Stopwatch> _playersLastTimePuckPossession = new LockDictionary<string, Stopwatch>();

        /// <summary>
        /// LockDictionary of string and Stopwatch, dictionary of all players last puck OnCollisionStay or OnCollisionExit time.
        /// </summary>
        private static readonly LockDictionary<string, Stopwatch> _lastTimeOnCollisionStayOrExitWasCalled = new LockDictionary<string, Stopwatch>();

        private static readonly LockDictionary<string, bool> _playerIsDown = new LockDictionary<string, bool>();

        private static Possession _lastPossession = new Possession();

        private static PlayerTeam _lastTeamOnPuckTipIncluded = PlayerTeam.Blue;

        private static PlayerTeam _lastTeamOnPuck = PlayerTeam.Blue;

        private static PuckRaycast _puckRaycast;

        /// <summary>
        /// Bool, true if there's a pause in play.
        /// </summary>
        private static bool _paused = false;

        /// <summary>
        /// Bool, true if the mod's logic has to be runned.
        /// </summary>
        private static bool _logic = true;

        // Client-side and server-side.
        /// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);

        /// <summary>
        /// Bool, true if the mod has been patched in.
        /// </summary>
        private static bool _harmonyPatched = false;

        /// <summary>Original methods patched on enable — used for safe per-target unpatch on disable.</summary>
        private static readonly List<MethodBase> _harmonyPatchTargets = new List<MethodBase>();

        /// <summary>
        /// Bool, true if the mod has registered with the named message handler for server/client communication.
        /// </summary>
        private static bool _hasRegisteredWithNamedMessageHandler = false;

        private static readonly LockDictionary<string, int> _sog = new LockDictionary<string, int>();
        // Client-side: synced from server
        private static readonly LockDictionary<string, int> _shotAttempts = new LockDictionary<string, int>();
        private static readonly LockDictionary<string, int> _homePlateSogs = new LockDictionary<string, int>(); // SOGs only (for tooltip)

        private static readonly LockDictionary<string, (int Saves, int Shots)> _savePerc = new LockDictionary<string, (int Saves, int Shots)>();

        private static readonly LockDictionary<string, int> _stickSaves = new LockDictionary<string, int>();
        private static readonly LockDictionary<string, int> _bodySaves = new LockDictionary<string, int>();
        private static readonly LockDictionary<string, int> _homePlateSaves = new LockDictionary<string, int>();
        private static readonly LockDictionary<string, int> _homePlateShots = new LockDictionary<string, int>();

        private static readonly LockDictionary<string, int> _blocks = new LockDictionary<string, int>();

        private static readonly LockDictionary<string, int> _hits = new LockDictionary<string, int>();

        private static readonly LockDictionary<string, int> _takeaways = new LockDictionary<string, int>();

        private static readonly LockDictionary<string, int> _turnovers = new LockDictionary<string, int>();

        private static readonly LockDictionary<string, int> _passes = new LockDictionary<string, int>();

        private static readonly LockDictionary<string, int> _puckTouches = new LockDictionary<string, int>();
        private static readonly LockDictionary<string, DateTime> _lastPuckTouchTime = new LockDictionary<string, DateTime>();

        private static readonly LockDictionary<string, int> _exits = new LockDictionary<string, int>();
        private static readonly LockDictionary<string, int> _entries = new LockDictionary<string, int>();

        // Possession time tracking - only counts time between consecutive touches
        private static readonly LockDictionary<string, DateTime> _lastPossessionTouchTime = new LockDictionary<string, DateTime>();
        private static readonly LockDictionary<string, DateTime> _lastPossessionUpdateTime = new LockDictionary<string, DateTime>();
        private static readonly LockDictionary<string, double> _possessionTimeSeconds = new LockDictionary<string, double>();

        // Time on ice (TOI) tracking - tracks total time player is on ice during Playing phase
        private static readonly LockDictionary<string, double> _timeOnIceSeconds = new LockDictionary<string, double>();

        // Plus/Minus tracking - tracks +/- for skaters (not goalies)
        private static readonly LockDictionary<string, int> _plusMinus = new LockDictionary<string, int>();

        // Turnovers/Takeaways cooldown and possession validation
        private static readonly LockDictionary<string, DateTime> _lastTakeawayTime = new LockDictionary<string, DateTime>();
        private static readonly LockDictionary<string, DateTime> _lastTurnoverTime = new LockDictionary<string, DateTime>();
        
        // Track recent turnovers to cancel them if shots occur shortly after
        // Key: player steam ID, Value: list of (gameTime, turnoverEventId, takeawayEventId) tuples
        private static readonly LockDictionary<string, List<(float GameTime, int TurnoverEventId, int TakeawayEventId)>> _recentTurnovers = new LockDictionary<string, List<(float, int, int)>>();
        
        // Track last zone for each player (for zone exit/entry detection)
        private static readonly LockDictionary<string, EventZone> _lastPlayerZone = new LockDictionary<string, EventZone>();
        
        // Track possession start times for validation
        private static readonly LockDictionary<string, DateTime> _possessionStartTime = new LockDictionary<string, DateTime>();
        private static readonly LockDictionary<string, (string SteamId, DateTime StartTime, bool Validated, int PreviousPlayCount)> _pendingTakeaways = new LockDictionary<string, (string, DateTime, bool, int)>();
        private static readonly LockDictionary<string, (string SteamId, DateTime StartTime, bool Validated, int PreviousPlayCount)> _pendingTurnovers = new LockDictionary<string, (string, DateTime, bool, int)>();
        private static bool _isValidatingTurnoversTakeaways = false; // Prevent re-entrancy in validation

        // Puck battle tracking - no longer tracking wins/losses, just recording battles
        private static readonly LockDictionary<string, int> _puckBattleWins = new LockDictionary<string, int>();
        private static readonly LockDictionary<string, int> _puckBattleLosses = new LockDictionary<string, int>();
        private static bool _wasTippedLastFrame = false;
        private static DateTime _lastPuckBattleTime = DateTime.MinValue;
        private static readonly int PUCK_BATTLE_COOLDOWN_MS = 500; // Prevent duplicate battles within 500ms
        
        // Pending shot tracking - tracks release info for shots that may be confirmed by raycast
        private static readonly LockDictionary<PlayerTeam, (string ShooterSteamId, Vector3 ShooterPosition, Vector3 PuckPosition, Vector3 PuckVelocity, DateTime ReleaseTime)> _pendingShotReleases = new LockDictionary<PlayerTeam, (string, Vector3, Vector3, Vector3, DateTime)>();
        
        // Track previous raycast state to detect when it transitions to true (shot confirmed)
        private static readonly LockDictionary<PlayerTeam, bool> _previousRaycastState = new LockDictionary<PlayerTeam, bool> {
            { PlayerTeam.Blue, false },
            { PlayerTeam.Red, false }
        };
        
        // Track if shot has been recorded for current raycast confirmation (to prevent duplicates)
        private static readonly LockDictionary<PlayerTeam, bool> _shotRecordedForRaycast = new LockDictionary<PlayerTeam, bool> {
            { PlayerTeam.Blue, false },
            { PlayerTeam.Red, false }
        };
        
        // Track consecutive frames that raycast has been true (to filter false positives)
        private static readonly LockDictionary<PlayerTeam, int> _raycastTrueFrames = new LockDictionary<PlayerTeam, int> {
            { PlayerTeam.Blue, 0 },
            { PlayerTeam.Red, 0 }
        };
        
        // Track last shot attempt game time per player to prevent duplicates (cooldown)
        private static readonly LockDictionary<string, float> _lastShotAttemptGameTime = new LockDictionary<string, float>();

        private static readonly LockList<GoalInfo> _goals = new LockList<GoalInfo>();

        private static readonly LockDictionary<int, string> _stars = new LockDictionary<int, string> {
            { 1, "" },
            { 2, "" },
            { 3, "" },
        };

        // Play-by-play tracking
        private static readonly List<PlayByPlayEvent> _playByPlayEvents = new List<PlayByPlayEvent>();
        private static int _nextPlayByPlayEventId = 0;
        private static int _currentPeriod = 1;
        private static float _gameStartTime = 0f;
        // Track last whole-second game time and Unity time for fractional precision
        private static float _lastWholeSecondGameTime = 0f;
        private static float _lastUnityTimeForGameTime = 0f;
        private static int _lastCountdownValue = -1;
        // Fractional second timer (0.0 to 1.0) that resets when countdown changes and pauses when game clock pauses
        private static float _fractionalSecondTimer = 0f;
        private static string _currentGameReferenceId = "";
        private static PlayerTeam _currentTeamInPossession = PlayerTeam.None;
        private static int _currentPlayInPossession = 0;
        // Track last event for outcome determination
        private static PlayByPlayEvent _lastEvent = null;
        
        // Track if team has already recorded zone exits/entries in current possession (to prevent duplicate credits)
        private static readonly LockDictionary<PlayerTeam, bool> _teamHasExitedDZ = new LockDictionary<PlayerTeam, bool> {
            { PlayerTeam.Blue, false },
            { PlayerTeam.Red, false }
        };
        private static readonly LockDictionary<PlayerTeam, bool> _teamHasEnteredOZ = new LockDictionary<PlayerTeam, bool> {
            { PlayerTeam.Blue, false },
            { PlayerTeam.Red, false }
        };
        // Removed _lastPassReceiverSteamId - Reception events are no longer used
        // Track faceoff outcomes
        private static bool _trackingFaceoffOutcome = false;
        private static int _faceoffPossessionChainCount = 0;
        private static PlayerTeam _faceoffPossessionTeam = PlayerTeam.None;
        private static int _lastFaceoffEventId = -1;
        private static GamePhase _lastRecordedPhase = GamePhase.None;
        // Deduplication guard for goal recording ? Server_NotifyGoalScoredRpc can fire
        // multiple times (once per connected client) for a single goal event.
        private static float _lastGoalDedupeTime = -1f;
        private static string _lastGoalDedupeSteamId = null;
        private static int _lastGoalDedupePeriod = -1;
        /// <summary>True when we already exported at GameOver (so Warmup block can skip to avoid double-export).</summary>
        // Two separate flags govern the GameOver → Warmup export handoff.
        //
        // _gameOverBlockEntered: set to true the FIRST time the GameOver branch runs for a given game.
        //   The game-state event fires on every engine tick while the podium/GameOver screen is showing,
        //   so without this guard the export and chat message would repeat on every tick.
        //   Crucially, it is set at the TOP of the else-block (before any work), so even if an exception
        //   occurs mid-block, subsequent ticks are still blocked and we don't spam partial exports.
        //   Reset to false in the Warmup handler so it's ready for the next game.
        //
        // _exportedAtGameOver: set to true only AFTER ExportGameStats() actually succeeds for a
        //   qualifying game. The Warmup fallback checks this flag: if true, Warmup skips its own
        //   export (the game was already exported at GameOver). If false (criteria weren't met, or an
        //   exception prevented the export), Warmup still tries its fallback export path.
        //   Reset to false in the Warmup handler so it's ready for the next game.
        //
        // NOTE: Previously, the guard used `_lastRecordedPhase == GamePhase.GameOver` instead of
        //   _gameOverBlockEntered. That was broken: _lastRecordedPhase is set to GameOver in the
        //   outer else-branch (line ~2560) BEFORE the guard is evaluated, so the guard was always
        //   true on the very first tick and the entire GameOver export block was permanently dead.
        //   All exports were silently falling through to the Warmup fallback, which announced
        //   correctly most of the time but was gated on `GameManager.Instance != null` — causing
        //   silent (no chat) exports whenever GameManager was null at the Warmup transition.
        private static bool _gameOverBlockEntered = false;
        private static bool _exportedAtGameOver    = false;
        /// <summary>True when we exported in ResetGameState Prefix (before stats were cleared). Prevents Warmup block from re-exporting.</summary>
        private static bool _exportedAtResetGameState = false;
        /// <summary>Suppress Puck's "Unknown command" for mod slash commands (may arrive async after the RPC).</summary>
        private static float _suppressUnknownChatCommandUntil = 0f;
        private static bool _faceoffRecordedForCurrentPeriod = false;
        private static bool _faceoffTotalIncremented = false; // Track if total has been incremented for current faceoff
        private static int _lastTrackedPeriod = 0; // Track previous period to detect period transitions
        private static PlayByPlayEvent _pendingBlueFaceoffOutcome = null;
        private static PlayByPlayEvent _pendingRedFaceoffOutcome = null;
        // Track if current goal is an own goal (set in Prefix when goalPlayer is null)
        private static bool _isCurrentGoalOwnGoal = false;

        // Client-side.
        /// <summary>
        /// ClientConfig, config set by the client.
        /// </summary>
        internal static ClientConfig _clientConfig = new ClientConfig();

        /// <summary>
        /// DateTime, last time client asked the server for startup data.
        /// </summary>
        private static DateTime _lastDateTimeAskStartupData = DateTime.MinValue;

        /// <summary>
        /// Bool, true if the server has responded and sent the startup data.
        /// </summary>
        private static bool _serverHasResponded = false;


        /// <summary>
        /// Int, number of time client asked the server for startup data.
        /// </summary>
        private static int _askServerForStartupDataCount = 0;

        private static readonly List<string> _hasUpdatedUIScoreboard = new List<string>();
        private static bool _teamTooltipsSetup = false;
        /// <summary>True while a deferred SetupTeamTooltips callback is queued (avoids stacking retries).</summary>
        private static bool _teamTooltipSetupScheduled = false;

        private static readonly LockDictionary<string, Label> _sogLabels = new LockDictionary<string, Label>();

        private static readonly LockDictionary<string, VisualElement> _playerTooltips = new LockDictionary<string, VisualElement>();
        private static readonly LockDictionary<PlayerTeam, VisualElement> _teamTooltips = new LockDictionary<PlayerTeam, VisualElement>();
        private static readonly LockDictionary<PlayerTeam, VisualElement> _teamHitAreas = new LockDictionary<PlayerTeam, VisualElement>();
        private static readonly LockDictionary<string, Label> _playerTooltipNameLabels = new LockDictionary<string, Label>();
        private static readonly LockDictionary<string, VisualElement> _playerTooltipContainers = new LockDictionary<string, VisualElement>();
        private static readonly LockDictionary<string, bool> _playerTooltipIsGoalie = new LockDictionary<string, bool>();

        private static bool _scoreboardColumnSyncRegistered;
        private static VisualElement _sogColHeaderRow;
        private static readonly HashSet<string> _sogHoverCallbacksRegistered = new HashSet<string>();
        private sealed class PlayerTooltipPointerHandlers {
            internal EventCallback<PointerEnterEvent> Enter;
            internal EventCallback<PointerLeaveEvent> Leave;
        }

        private static readonly Dictionary<string, PlayerTooltipPointerHandlers> _playerTooltipPointerHandlers =
            new Dictionary<string, PlayerTooltipPointerHandlers>();

        /// <summary>Minimum width so goalie save % (e.g. 87.5%) fits in the stat column.</summary>
        private const float SOG_COLUMN_MIN_WIDTH = 44f;

        /// <summary>
        /// Dictionary mapping tooltip VisualElements to their last mouse move time for throttling.
        /// </summary>
        private static Dictionary<VisualElement, float> _tooltipMoveTimes = new Dictionary<VisualElement, float>();
        
        // Team-level stat tracking (stats stay with team when players switch)
        private static readonly LockDictionary<PlayerTeam, int> _teamShots = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamShotAttempts = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamHomePlateSogs = new LockDictionary<PlayerTeam, int>(); // SOGs only (for tooltip)
        private static readonly LockDictionary<PlayerTeam, int> _teamPasses = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, double> _teamPossessionTime = new LockDictionary<PlayerTeam, double>();
        private static readonly LockDictionary<PlayerTeam, int> _teamPuckBattleWins = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamPuckBattleLosses = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamFaceoffWins = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamFaceoffTotal = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamTakeaways = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamTurnovers = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamExits = new LockDictionary<PlayerTeam, int>();
        private static readonly LockDictionary<PlayerTeam, int> _teamEntries = new LockDictionary<PlayerTeam, int>();
        
        // Continuous team possession time tracking (separate from individual player possession)
        private static PlayerTeam _currentTeamPossession = PlayerTeam.None;
        private static DateTime _teamPossessionStartTime = DateTime.UtcNow;
        private static readonly LockDictionary<PlayerTeam, DateTime> _teamLastEventTime = new LockDictionary<PlayerTeam, DateTime>();

        // Stat update batching system
        private static readonly LockDictionary<string, string> _pendingStatUpdates = new LockDictionary<string, string>();
        private static DateTime _lastStatBatchSendTime = DateTime.UtcNow;
        /// <summary>Throttles expensive play-by-play scans in Server_Tick (matches PuckRaycast CHECK_EVERY_X_FRAMES).</summary>
        private static int _serverPlayLogicTick = 0;
        private const int SERVER_PLAY_LOGIC_EVERY_N_TICKS = 6;
        
        // Circuit breaker for batching failures - prevents server crashes
        private static int _batchingFailureCount = 0;
        private static DateTime _lastBatchingFailureTime = DateTime.MinValue;
        private static bool _batchingDisabled = false;
        private const int MAX_BATCHING_FAILURES = 3; // Disable after 3 failures
        private const double BATCHING_DISABLE_DURATION_SECONDS = 60.0; // Re-enable after 60 seconds
        private static DateTime _lastZoneFlagScanTime = DateTime.UtcNow;
        private static int _lastProcessedZoneFlagEventId = -1; // Track last event ID processed for zone flags
        private const double STAT_BATCH_INTERVAL_SECONDS = 5.0;
        #endregion

        // =====================================================================
        // DEBUG TRACE SYSTEM
        // Enabled by #define DEBUG_MODE at the top of the file.
        // Server trace ? ./stats/stats_debug_server.txt
        // Client trace ? {Application.persistentDataPath}/stats_debug_client.txt
        // =====================================================================
#if DEBUG_MODE
        private static class DebugTrace {
            private static readonly System.Text.StringBuilder _buf = new System.Text.StringBuilder(8192);
            private static readonly object _lock = new object();
            private static DateTime _startTime = DateTime.UtcNow;
            private static string _filePath = null;
            private static bool _isServer;
            private static int _lineCount = 0;

            public static void Init(bool isServer) {
                _isServer = isServer;
                _startTime = DateTime.UtcNow;
                _lineCount = 0;
                try {
                    if (isServer) {
                        string dir = Path.Combine(Path.GetFullPath("."), "stats");
                        Directory.CreateDirectory(dir);
                        _filePath = Path.Combine(dir, "stats_debug_server.txt");
                    } else {
                        // Write to AppData\Local ? always writable without admin rights.
                        string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        dir = Path.Combine(dir, "Puck");
                        Directory.CreateDirectory(dir);
                        _filePath = Path.Combine(dir, "stats_debug_client.txt");
                    }
                    // Truncate the file fresh each run
                    File.WriteAllText(_filePath,
                        $"========================================\r\n" +
                        $"  STATS MOD DEBUG TRACE ? {(isServer ? "SERVER" : "CLIENT")}\r\n" +
                        $"  Started : {_startTime:yyyy-MM-dd HH:mm:ss} UTC\r\n" +
                        $"  Version : {MOD_VERSION}\r\n" +
                        $"========================================\r\n\r\n");
                } catch { /* if path fails we still buffer in memory */ }
            }

            /// <summary>Write one trace line.  section = "LIFECYCLE", "NETWORK", "SCOREBOARD", etc.</summary>
            public static void Write(string section, string message) {
                string elapsed = (DateTime.UtcNow - _startTime).ToString(@"hh\:mm\:ss\.fff");
                string line = $"[{elapsed}] [{section,-14}] {message}";
                lock (_lock) {
                    _buf.AppendLine(line);
                    _lineCount++;
                    if (_lineCount % 20 == 0) Flush();    // flush every 20 lines
                }
            }

            /// <summary>Flush the in-memory buffer to the trace file.</summary>
            public static void Flush() {
                lock (_lock) {
                    if (_buf.Length == 0 || _filePath == null) return;
                    try {
                        File.AppendAllText(_filePath, _buf.ToString());
                        _buf.Clear();
                    } catch { }
                }
            }

            /// <summary>Write a section separator to make the file easier to scan.</summary>
            public static void Section(string title) {
                string line = $"\r\n--- {title} {new string('-', Math.Max(0, 50 - title.Length))} [{DateTime.UtcNow:HH:mm:ss}]\r\n";
                lock (_lock) { _buf.AppendLine(line); }
            }
        }
#endif

        #region Harmony Patches
        /// <summary>
        /// Class that patches the Server_SpawnPuck event from PuckManager.
        /// </summary>
        [HarmonyPatch(typeof(PuckManager), nameof(PuckManager.Server_SpawnPuck))]
        public class PuckManager_Server_SpawnPuck_Patch {
            [HarmonyPostfix]
            public static void Postfix(ref Puck __result, Vector3 position, Quaternion rotation, bool isReplay) {
                try {
                    // If this is not the server or this is a replay or game is not started, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || isReplay || (GameManager.Instance.Phase != GamePhase.Play && GameManager.Instance.Phase != GamePhase.FaceOff))
                        return;

                    __result.gameObject.AddComponent<PuckRaycast>();
                    _puckRaycast = __result.gameObject.GetComponent<PuckRaycast>();
                    
                    // Record Faceoff event when puck spawns (only once per period/game start)
                    if (!isReplay && GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Play && !_faceoffRecordedForCurrentPeriod) {
                        // Clear any pending outcomes from previous faceoff
                        _pendingBlueFaceoffOutcome = null;
                        _pendingRedFaceoffOutcome = null;
                        
                        RecordFaceoffEvent();
                        _faceoffRecordedForCurrentPeriod = true;
                        
                        // Start tracking faceoff outcome
                        _trackingFaceoffOutcome = true;
                        _faceoffPossessionChainCount = 0;
                        _faceoffPossessionTeam = PlayerTeam.None;
                        _faceoffTotalIncremented = false; // Reset flag for new faceoff
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in PuckManager_Server_SpawnPuck_Patch Postfix().\n{ex}", ModServerConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the Server_NotifyGoalScoredRpc event from GameManager.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Server_NotifyGoalScoredRpc))]
        public class GameManager_Server_GoalScored_Patch {
            [HarmonyPrefix]
            public static bool Prefix(PlayerTeam byTeam,
                NetworkObjectReference goalPlayerNetworkObjectReference,
                NetworkObjectReference assistPlayerNetworkObjectReference,
                NetworkObjectReference secondAssistPlayerNetworkObjectReference,
                NetworkObjectReference puckNetworkObjectReference) {
                try {
                    // If this is not the server or game is not started, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || RulesetModEnabled() || !_logic)
                        return true;

                    // Reset own goal flag
                    _isCurrentGoalOwnGoal = false;

                    // Resolve goal player from NetworkObjectReference
                    Player goalPlayer = NetworkingUtils.GetPlayerFromNetworkObjectReference(goalPlayerNetworkObjectReference);

                    if (goalPlayer != null) {
                        // Normal goal - offensive player got credit
                        SendSavePercDuringGoal(byTeam, SendSOGDuringGoal(goalPlayer));
                        return true;
                    }

                    // Own goal - no offensive player got credit (goalPlayer reference was empty)
                    _isCurrentGoalOwnGoal = true;
                    // For own goals, the player who last touched the puck is from the defending team (the team that got scored on)
                    PlayerTeam defendingTeam = TeamFunc.GetOtherTeam(byTeam);
                    Player lastTouchPlayer = PlayerManager.Instance.GetPlayerBySteamId(_lastPlayerOnPuckTipIncludedSteamId[defendingTeam].SteamId);

                    if (lastTouchPlayer != null) {
                        // Server_NotifyGoalScoredRpc fires once per connected client — only announce once.
                        float currentGameTime = GetCurrentGameTime();
                        int currentPeriod = GetCurrentPeriod();
                        string ownGoalKey = lastTouchPlayer.SteamId.Value.Value;
                        bool isDuplicateOwnGoal = ownGoalKey == _lastGoalDedupeSteamId
                            && currentPeriod == _lastGoalDedupePeriod
                            && Math.Abs(currentGameTime - _lastGoalDedupeTime) < 1.0f;

                        if (!isDuplicateOwnGoal) {
                            NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage($"OWN GOAL BY {lastTouchPlayer.Username.Value}");
                        }
                        // Don't call SendSOGDuringGoal for own goals - they shouldn't count as shots on goal
                    }

                    // Still need to update save percentage for the goalie (they didn't save it)
                    SendSavePercDuringGoal(byTeam, false);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in GameManager_Server_GoalScored_Patch Prefix().\n{ex}", ModServerConfig);
                }

                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(PlayerTeam byTeam,
                NetworkObjectReference goalPlayerNetworkObjectReference,
                NetworkObjectReference assistPlayerNetworkObjectReference,
                NetworkObjectReference secondAssistPlayerNetworkObjectReference,
                NetworkObjectReference puckNetworkObjectReference) {
                try {
                    // If this is not the server, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer())
                        return;

                    // Resolve players and puck from NetworkObjectReferences
                    Player goalPlayer = NetworkingUtils.GetPlayerFromNetworkObjectReference(goalPlayerNetworkObjectReference);
                    Player assistPlayer = NetworkingUtils.GetPlayerFromNetworkObjectReference(assistPlayerNetworkObjectReference);
                    Player secondAssistPlayer = NetworkingUtils.GetPlayerFromNetworkObjectReference(secondAssistPlayerNetworkObjectReference);
                    Puck puck = NetworkingUtils.GetPuckFromNetworkObjectReference(puckNetworkObjectReference);

                    // For own goals, identify the last defender who touched the puck via our internal tracking
                    Player lastPlayer = null;
                    bool isOwnGoal = _isCurrentGoalOwnGoal;
                    if (isOwnGoal) {
                        PlayerTeam defendingTeam = TeamFunc.GetOtherTeam(byTeam);
                        lastPlayer = PlayerManager.Instance.GetPlayerBySteamId(_lastPlayerOnPuckTipIncludedSteamId[defendingTeam].SteamId);
                    }

                    // Record play-by-play event first to get precise timestamp
                    PlayByPlayEvent goalEvent = null;
                    if (puck != null) {
                        Vector3 goalPos = puck.transform.position;
                        // Don't track velocity for goal events
                        if (isOwnGoal) {
                            // For own goals, use lastPlayer (last touch) for player info, but set goalscorer (PlayerSteamId) to empty
                            if (lastPlayer != null && lastPlayer) {
                                float currentGameTime = GetCurrentGameTime();
                                int currentPeriod = GetCurrentPeriod();
                                string ownGoalKey = lastPlayer.SteamId.Value.Value;
                                bool isDuplicateOwnGoal = ownGoalKey == _lastGoalDedupeSteamId
                                    && currentPeriod == _lastGoalDedupePeriod
                                    && Math.Abs(currentGameTime - _lastGoalDedupeTime) < 1.0f;

                                if (!isDuplicateOwnGoal) {
                                    RecordPlayByPlayEventInternal(PlayByPlayEventType.OwnGoal, lastPlayer, goalPos, Vector3.zero, "successful");
                                    _lastGoalDedupeSteamId = ownGoalKey;
                                    _lastGoalDedupePeriod = currentPeriod;
                                    _lastGoalDedupeTime = currentGameTime;
                                    // Set goalscorer to empty for own goals (playerReferenceSteamID should be null)
                                    if (_playByPlayEvents.Count > 0) {
                                        var lastEvent = _playByPlayEvents[_playByPlayEvents.Count - 1];
                                        if (lastEvent.EventType == PlayByPlayEventType.OwnGoal) {
                                            lastEvent.PlayerSteamId = ""; // Goalscorer is null for own goals
                                        }
                                    }
                                }
                            }
                        }
                        else {
                            // Normal goal - use goalPlayer
                            if (goalPlayer != null && goalPlayer) {
                                // Only record the PBP Goal event once ? same dedup check used for _goals below.
                                float currentGameTime = GetCurrentGameTime();
                                int currentPeriod = GetCurrentPeriod();
                                string scorerId = goalPlayer.SteamId.Value.Value;
                                bool alreadyRecordedInPBP = scorerId == _lastGoalDedupeSteamId
                                    && currentPeriod == _lastGoalDedupePeriod
                                    && Math.Abs(currentGameTime - _lastGoalDedupeTime) < 1.0f;

                                if (!alreadyRecordedInPBP) {
                                    RecordPlayByPlayEventInternal(PlayByPlayEventType.Goal, goalPlayer, goalPos, Vector3.zero, "successful");
                                }
                                // Get the just-recorded goal event to use its precise timestamp
                                if (_playByPlayEvents.Count > 0) {
                                    goalEvent = _playByPlayEvents[_playByPlayEvents.Count - 1];
                                    if (goalEvent.EventType != PlayByPlayEventType.Goal) {
                                        goalEvent = _playByPlayEvents
                                            .Where(e => e.EventType == PlayByPlayEventType.Goal &&
                                                       e.PlayerSteamId == goalPlayer.SteamId.Value.Value &&
                                                       e.GameTime > 0f)
                                            .OrderByDescending(e => e.GameTime)
                                            .FirstOrDefault();
                                    }
                                }
                            }
                        }
                    }

                    // Only process goal stats if we have a goalPlayer (NOT for own goals - goalPlayer should be null)
                    if (!isOwnGoal && goalPlayer != null && goalPlayer) {
                        // Use precise gameTime and period from play-by-play event if available, otherwise fall back to GetCurrentGameTime
                        float gameTime = goalEvent != null ? goalEvent.GameTime : GetCurrentGameTime();
                        int period = goalEvent != null ? goalEvent.Period : GetCurrentPeriod();
                        string scorerSteamId = goalPlayer.SteamId.Value.Value;

                        // Dedup guard: Server_NotifyGoalScoredRpc fires once per connected client.
                        // Only record the goal the first time we see it (same scorer, period, and game time).
                        bool isDuplicateGoal = scorerSteamId == _lastGoalDedupeSteamId
                                              && period == _lastGoalDedupePeriod
                                              && Math.Abs(gameTime - _lastGoalDedupeTime) < 1.0f;

                        if (!isDuplicateGoal) {
                            _lastGoalDedupeSteamId = scorerSteamId;
                            _lastGoalDedupePeriod = period;
                            _lastGoalDedupeTime = gameTime;

                            // Create goal info object
                            var (defendingGoalieSteamId, isEmptyNet) = ResolveDefendingGoalieForGoal(goalEvent, byTeam);
                            GoalInfo goalInfo = new GoalInfo {
                                GameTime = gameTime,
                                Period = period,
                                Team = byTeam == PlayerTeam.Blue ? "Blue" : "Red",
                                Scorer = scorerSteamId,
                                PrimaryAssist = assistPlayer != null ? assistPlayer.SteamId.Value.Value : null,
                                SecondaryAssist = secondAssistPlayer != null ? secondAssistPlayer.SteamId.Value.Value : null,
                                GWG = false, // Will be calculated later during export
                                DefendingGoalieSteamId = defendingGoalieSteamId,
                                IsEmptyNet = isEmptyNet
                            };

                            _goals.Add(goalInfo);

                            if (!isEmptyNet && !string.IsNullOrEmpty(defendingGoalieSteamId))
                                ReconcileGoalieShotsFaced(defendingGoalieSteamId);
#if DEBUG_MODE
                            DebugTrace.Write("PBP", $"GOAL recorded. scorer={goalInfo.Scorer} team={goalInfo.Team} assist1={goalInfo.PrimaryAssist} assist2={goalInfo.SecondaryAssist} totalGoals={_goals.Count}");
                            DebugTrace.Flush();
#endif
                        }
#if DEBUG_MODE
                        else {
                            DebugTrace.Write("PBP", $"GOAL deduped (RPC fired again). scorer={scorerSteamId} period={period} gameTime={gameTime:F2} ? skipped.");
                        }
#endif
                    }

                    // Reset possession/event logic on goals
                    _currentTeamInPossession = PlayerTeam.None;
                    _currentPlayInPossession = 0;
                    _lastEvent = null;
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in GameManager_Server_GoalScored_Patch Postfix().\n{ex}", ModServerConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the RemovePlayer event from UIScoreboard.
        /// </summary>
        [HarmonyPatch(typeof(UIScoreboard), nameof(UIScoreboard.RemovePlayer))]
        public class UIScoreboard_RemovePlayer_Patch {
            [HarmonyPostfix]
            public static void Postfix(Player player) {
                try {
                    // If this is the server, do not use the patch.
                    if (ServerFunc.IsDedicatedServer())
                        return;

                    string steamId = player.SteamId.Value.Value;
                    _sogLabels.Remove(steamId);
                    UnregisterPlayerTooltipPointerHandlers(steamId);
                    _sogHoverCallbacksRegistered.Remove(steamId);
                    if (_playerTooltips.TryGetValue(steamId, out VisualElement tooltip)) {
                        tooltip.parent?.Remove(tooltip);
                        _playerTooltips.Remove(steamId);
                    }
                    _playerTooltipNameLabels.Remove(steamId);
                    _playerTooltipContainers.Remove(steamId);
                    _playerTooltipIsGoalie.Remove(steamId);
                    _hasUpdatedUIScoreboard.Remove(steamId);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in UIScoreboard_RemovePlayer_Patch Postfix().\n{ex}", _clientConfig);
                }
            }
        }

        /// <summary>
        /// Exports play-by-play when an in-progress game is aborted (e.g. vote reset). Returns true if exported.
        /// </summary>
        private static bool TryExportAbortedGameStats(string endedMessage) {
            if (_exportedAtResetGameState || _playByPlayEvents.Count == 0)
                return false;

            int uniquePlayerCount = 0;
            int eventCount = _playByPlayEvents.Count;
            try {
                uniquePlayerCount = _playByPlayEvents.Where(e => !string.IsNullOrEmpty(e.PlayerSteamId)).Select(e => e.PlayerSteamId).Distinct().Count();
            }
            catch { }

            bool meetsCriteria = !ModServerConfig.EnableExportLimit || (uniquePlayerCount >= 8 && eventCount >= 300);
            if (!meetsCriteria) {
                if (ModServerConfig.EnableExportLimit) {
                    Logging.Log($"Aborted game export skipped — {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)", ModServerConfig);
                }
                return false;
            }

            Logging.Log($"Aborted game export ({endedMessage}) — exporting before reset", ModServerConfig);
            bool hasGameEndEvent = _playByPlayEvents.Any(e => e.EventType == PlayByPlayEventType.GameEnd);
            if (!hasGameEndEvent) {
                int finalPeriod = GetCurrentPeriod();
                float maxGameTime = _playByPlayEvents.Count > 0 ? _playByPlayEvents.Max(e => e.GameTime) : 0f;
                float gameEndGameTime = maxGameTime > 0f ? maxGameTime : GetCurrentGameTime();
                var gameEndEvent = new PlayByPlayEvent { EventId = _nextPlayByPlayEventId++, EventType = PlayByPlayEventType.GameEnd, GameTime = gameEndGameTime, Period = finalPeriod, PlayerSteamId = "", PlayerName = "", PlayerTeam = 0, PlayerPosition = "", PlayerJersey = 0, PlayerSpeed = 0f, Zone = EventZone.Neutral, Position = Vector3.zero, Velocity = Vector3.zero, ForceMagnitude = 0f, Outcome = "end", Flags = "", Team = "", TeamInPossession = _currentTeamInPossession != PlayerTeam.None ? (_currentTeamInPossession == PlayerTeam.Blue ? "Blue" : "Red") : "", CurrentPlayInPossession = _currentPlayInPossession.ToString(), ScoreState = GetScoreState(PlayerTeam.None), Timestamp = DateTime.UtcNow };
                CaptureTeamRosterData(gameEndEvent);
                int insertIndex = _playByPlayEvents.Count;
                for (int i = 0; i < _playByPlayEvents.Count; i++) {
                    if (_playByPlayEvents[i].GameTime >= gameEndGameTime) {
                        insertIndex = i;
                        break;
                    }
                }
                _playByPlayEvents.Insert(insertIndex, gameEndEvent);
            }

            ExportGameStats(forceExport: false);
            _exportedAtResetGameState = true;
            string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string sanitizedFileHeader = StripHtmlTags(ModServerConfig.FileHeaderName);
            string fullFileName = $"{sanitizedFileHeader}_{gameReferenceId}_stats";
            NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage($"{endedMessage} Stats exported - {fullFileName}");
            return true;
        }

        /// <summary>
        /// Performs full server-side stats and play-by-play reset. Called on PreGame entry (new game) or Warmup -> FaceOff fallback.
        /// </summary>
        private static void ResetAllServerStatsAndPlayByPlay() {
            List<Player> players = PlayerManager.Instance != null ? PlayerManager.Instance.GetPlayers() : new List<Player>();
            foreach (string key in new List<string>(_savePerc.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _savePerc[key] = (0, 0);
                else
                    _savePerc.Remove(key);
            }
            foreach (string key in new List<string>(_stickSaves.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _stickSaves[key] = 0;
                else
                    _stickSaves.Remove(key);
            }
            foreach (string key in new List<string>(_bodySaves.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _bodySaves[key] = 0;
                else
                    _bodySaves.Remove(key);
            }
            foreach (string key in new List<string>(_blocks.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _blocks[key] = 0;
                else
                    _blocks.Remove(key);
            }
            foreach (string key in new List<string>(_hits.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _hits[key] = 0;
                else
                    _hits.Remove(key);
            }
            foreach (string key in new List<string>(_takeaways.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _takeaways[key] = 0;
                else
                    _takeaways.Remove(key);
            }
            foreach (string key in new List<string>(_turnovers.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _turnovers[key] = 0;
                else
                    _turnovers.Remove(key);
            }
            foreach (string key in new List<string>(_puckTouches.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _puckTouches[key] = 0;
                else
                    _puckTouches.Remove(key);
            }
            foreach (string key in new List<string>(_shotAttempts.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _shotAttempts[key] = 0;
                else
                    _shotAttempts.Remove(key);
            }
            foreach (string key in new List<string>(_exits.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _exits[key] = 0;
                else
                    _exits.Remove(key);
            }
            foreach (string key in new List<string>(_entries.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _entries[key] = 0;
                else
                    _entries.Remove(key);
            }
            foreach (string key in new List<string>(_passes.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _passes[key] = 0;
                else
                    _passes.Remove(key);
            }
            foreach (string key in new List<string>(_sog.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _sog[key] = 0;
                else
                    _sog.Remove(key);
            }
            foreach (string key in new List<string>(_possessionTimeSeconds.Keys)) {
                if (players.Count > 0 && players.FirstOrDefault(x => x.SteamId.Value.Value == key) != null)
                    _possessionTimeSeconds[key] = 0.0;
                else
                    _possessionTimeSeconds.Remove(key);
            }
            _lastPossessionTouchTime.Clear();
            _lastPossessionUpdateTime.Clear();
            _lastPuckTouchTime.Clear();
            _lastPlayerZone.Clear();
            _teamShots.Clear();
            _teamShotAttempts.Clear();
            _teamHomePlateSogs.Clear();
            _teamPasses.Clear();
            _teamPossessionTime.Clear();
            _teamPuckBattleWins.Clear();
            _teamPuckBattleLosses.Clear();
            _teamFaceoffWins.Clear();
            _teamFaceoffTotal.Clear();
            _teamTakeaways.Clear();
            _teamTurnovers.Clear();
            _teamExits.Clear();
            _teamEntries.Clear();
            _recentTurnovers.Clear();
            _timeOnIceSeconds.Clear();
            _plusMinus.Clear();
            _currentTeamPossession = PlayerTeam.None;
            _teamPossessionStartTime = DateTime.UtcNow;
            _teamLastEventTime.Clear();
            _goals.Clear();
            _lastGoalDedupeTime = -1f;
            _lastGoalDedupeSteamId = null;
            _lastGoalDedupePeriod = -1;
            _lastPossession = new Possession();
            _wasTippedLastFrame = false;
            _playByPlayEvents.Clear();
            _nextPlayByPlayEventId = 0;
            _lastProcessedZoneFlagEventId = -1;
            _currentGameReferenceId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            _currentTeamInPossession = PlayerTeam.None;
            _currentPlayInPossession = 0;
            _gameStartTime = Time.time;
            _currentPeriod = 1;
            _lastTrackedPeriod = 0;
            _faceoffRecordedForCurrentPeriod = false;
            _lastEvent = null;
            _lastWholeSecondGameTime = 0f;
            _lastUnityTimeForGameTime = 0f;
            _lastCountdownValue = -1;
            foreach (PlayerTeam team in new List<PlayerTeam>(_pendingShotReleases.Keys))
                _pendingShotReleases[team] = ("", Vector3.zero, Vector3.zero, Vector3.zero, DateTime.MinValue);
            foreach (PlayerTeam team in new List<PlayerTeam>(_previousRaycastState.Keys))
                _previousRaycastState[team] = false;
            foreach (PlayerTeam team in new List<PlayerTeam>(_raycastTrueFrames.Keys))
                _raycastTrueFrames[team] = 0;
            foreach (PlayerTeam team in new List<PlayerTeam>(_shotRecordedForRaycast.Keys))
                _shotRecordedForRaycast[team] = false;
            _lastShotAttemptGameTime.Clear();
            foreach (PlayerTeam team in new List<PlayerTeam>(_teamHasExitedDZ.Keys))
                _teamHasExitedDZ[team] = false;
            foreach (PlayerTeam team in new List<PlayerTeam>(_teamHasEnteredOZ.Keys))
                _teamHasEnteredOZ[team] = false;
            _pendingStatUpdates.Clear();
            _serverPlayLogicTick = 0;
        }

        // Server_ResetGameState was removed in B310.
        // Early-export is handled by Event_Everyone_OnGameStateChanged.
        // Per-game stat reset is handled by ResetAllServerStatsAndPlayByPlay().

        /// <summary>
        /// Class that patches the Update event from ServerManager.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), "Server_Tick")]
        public class GameManager_Server_Tick_Patch {
            [HarmonyPostfix]
            public static void Postfix() {
                try {
                    // If this is not the server, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || !_logic)
                        return;

                    // Check if it's time to send batched stat updates
                    DateTime now = DateTime.UtcNow;
                    if ((now - _lastStatBatchSendTime).TotalSeconds >= STAT_BATCH_INTERVAL_SECONDS) {
                        // SendBatchedStatUpdates has its own error handling, but wrap here as extra safety
                        try {
                            SendBatchedStatUpdates();
                        }
                        catch (Exception ex) {
                            // Extra safety net - should never reach here due to internal try-catch
                            // but this ensures the server never crashes from batching
                            Logging.LogError($"Unexpected error calling SendBatchedStatUpdates (this should not happen): {ex}", ModServerConfig);
                        }
                    }
                    
                    // Check if it's time to scan play-by-play events and add zone flags (every 6 seconds)
                    if ((now - _lastZoneFlagScanTime).TotalSeconds >= 6.0) {
                        AddZoneFlagsToPlayByPlayEvents();
                        _lastZoneFlagScanTime = now;
                    }

                    // Legacy SOG stat-trigger path removed: it called SendSavePercDuringGoal on non-goal
                    // SOG updates and could decrement saves or add phantom shots faced. Goals are
                    // handled exclusively by GameManager_Server_GoalScored_Patch (Harmony Prefix).

                    // If game is not started, do not use the rest of the patch.
                    if (PlayerManager.Instance == null || PuckManager.Instance == null || GameManager.Instance.Phase != GamePhase.Play || _paused)
                        return;

                    _serverPlayLogicTick++;
                    bool runPlayByPlayScan = (_serverPlayLogicTick % SERVER_PLAY_LOGIC_EVERY_N_TICKS == 0);

                    // Check for raycast state changes and record shots when raycast confirms
                    foreach (PlayerTeam defendingTeam in new List<PlayerTeam> { PlayerTeam.Blue, PlayerTeam.Red }) {
                        float currentGameTime = GetCurrentGameTime();
                        bool currentRaycastState = _puckRaycast.PuckIsGoingToNet[defendingTeam];
                        bool previousRaycastState = _previousRaycastState.TryGetValue(defendingTeam, out bool prev) ? prev : false;
                        
                        // Track consecutive frames raycast has been true (to filter false positives)
                        if (currentRaycastState) {
                            _raycastTrueFrames[defendingTeam] = _raycastTrueFrames.TryGetValue(defendingTeam, out int frames) ? frames + 1 : 1;
                        } else {
                            _raycastTrueFrames[defendingTeam] = 0;
                        }
                        
                        // Raycast confirmed it's going to net - require it to be true for at least 2 checks (12 frames) to filter false positives
                        // Note: raycast uses defending team (goal team), but shots are stored by attacking team
                        const int MIN_RAYCAST_CONFIRMATION_FRAMES = 2; // 2 checks = 12 frames (CHECK_EVERY_X_FRAMES = 6)
                        if (currentRaycastState && _raycastTrueFrames[defendingTeam] >= MIN_RAYCAST_CONFIRMATION_FRAMES && !_shotRecordedForRaycast[defendingTeam]) {
                            // Get the attacking team (opposite of defending team)
                            // IMPORTANT: If raycast confirms, record shot regardless of velocity (retroactive correction)
                            PlayerTeam attackingTeamForUpdate = TeamFunc.GetOtherTeam(defendingTeam);
                            
                            // Raycast confirms shot is on net, but don't mark as "on net" yet
                            // Shots will only be marked as "on net" when saves or goals are actually recorded
                            // This keeps play-by-play in sync with tooltip stats (SOG only increments on save/goal)
                            
                            // Mark that raycast confirmed a shot (use defending team for tracking)
                            _shotRecordedForRaycast[defendingTeam] = true;
                            
                            // Don't clear pending release here - it will be cleared when save/goal is recorded
                        }
                        
                        // Expensive play-by-play scans — throttled to every N ticks (PuckRaycast updates every 6 frames).
                        if (runPlayByPlayScan) {
                            // Check for missed shots (unconfirmed attempts that never got raycast confirmation)
                            // Mark as "missed" based on zone-based timeout: OZ=2s, NZ=4s, DZ=6s
                            PlayerTeam attackingTeamForMissed = TeamFunc.GetOtherTeam(defendingTeam);
                            
                            // Check ALL "attempt" shots for this team and mark as "missed" if old enough
                            var unconfirmedAttemptShots = _playByPlayEvents
                                .Where(e => e.EventType == PlayByPlayEventType.Shot &&
                                           e.PlayerTeam == (int)attackingTeamForMissed &&
                                           e.Outcome == "attempt")
                                .ToList();
                            
                            foreach (var shotEvent in unconfirmedAttemptShots) {
                                // Get zone-based timeout: OZ=2s, NZ=4s, DZ=6s
                                float timeoutSeconds = GetShotTimeoutByZone(shotEvent.Zone);
                                float ageInSeconds = currentGameTime - shotEvent.GameTime;
                                
                                // Only check if shot is old enough based on its zone
                                if (ageInSeconds > timeoutSeconds) {
                                    // Only mark as missed if no save/goal/block occurred for this shot
                                    // Use longer timeout (3 seconds) to account for delayed saves
                                    bool saveGoalOrBlockRecorded = _playByPlayEvents.Any(e =>
                                        (e.EventType == PlayByPlayEventType.Save ||
                                         e.EventType == PlayByPlayEventType.Goal ||
                                         e.EventType == PlayByPlayEventType.Block) &&
                                        e.GameTime >= shotEvent.GameTime &&
                                        e.GameTime <= shotEvent.GameTime + 3f);
                                    
                                    if (!saveGoalOrBlockRecorded) {
                                        shotEvent.Outcome = "missed";
                                    }
                                }
                            }
                        }
                        
                        // Reset shot recorded flag when raycast goes back to false (new shot cycle)
                        // If shot was marked "on net" but no save/goal was recorded, mark it as "missed"
                        if (!currentRaycastState && previousRaycastState) {
                            if (runPlayByPlayScan) {
                                PlayerTeam attackingTeamForReset = TeamFunc.GetOtherTeam(defendingTeam);
                                
                                // Find recent "on net" shots for this team (within last 2 seconds) that might need correction
                                float resetGameTime = GetCurrentGameTime();
                                var recentOnNetShots = _playByPlayEvents
                                    .Where(e => e.EventType == PlayByPlayEventType.Shot && 
                                               e.PlayerTeam == (int)attackingTeamForReset &&
                                               e.Outcome == "on net" &&
                                               e.GameTime >= resetGameTime - 2f)
                                    .ToList();
                                
                                foreach (var shotEvent in recentOnNetShots) {
                                    // Check if save, goal, or block was recorded for this shot (within 4 seconds after shot)
                                    bool saveGoalOrBlockRecorded = _playByPlayEvents.Any(e => 
                                        (e.EventType == PlayByPlayEventType.Save || 
                                         e.EventType == PlayByPlayEventType.Goal ||
                                         e.EventType == PlayByPlayEventType.Block) &&
                                        e.GameTime >= shotEvent.GameTime &&
                                        e.GameTime <= shotEvent.GameTime + 4f);
                                    
                                    // Only mark as "missed" if it wasn't already marked as "blocked" and no save/goal/block occurred
                                    if (!saveGoalOrBlockRecorded && shotEvent.Outcome != "blocked") {
                                        // Shot was marked "on net" but didn't result in save/goal/block - mark as "missed"
                                        shotEvent.Outcome = "missed";
                                        
                                        // Clear home plate flag for missed shots - they shouldn't count as home plate SOGs
                                        // (Home plate SOGs should only be tracked when save/goal occurs)
                                        if (shotEvent.Flags == "HomePlate") {
                                            shotEvent.Flags = ""; // Clear flag for missed shots
                                        }
                                    }
                                }
                            }
                            
                            _shotRecordedForRaycast[defendingTeam] = false;
                            _raycastTrueFrames[defendingTeam] = 0;
                        }
                        
                        // Update previous state
                        _previousRaycastState[defendingTeam] = currentRaycastState;
                    }

                    // Save logic.
                    foreach (PlayerTeam key in new List<PlayerTeam>(_checkIfPuckWasSaved.Keys)) {
                            SaveCheck saveCheck = _checkIfPuckWasSaved[key];
                            if (!saveCheck.HasToCheck) {
                                _checkIfPuckWasSaved[key] = new SaveCheck();
                                continue;
                            }

                            //Logging.Log($"kvp.Check {saveCheck.FramesChecked} for team net {key} by {saveCheck.ShooterSteamId}.", ModServerConfig, true);

                            if (!_puckRaycast.PuckIsGoingToNet[key] && !_lastShotWasCounted[saveCheck.ShooterTeam]) {
                                // Record SOG first
                                if (!_sog.TryGetValue(saveCheck.ShooterSteamId, out int _))
                                    _sog.Add(saveCheck.ShooterSteamId, 0);

                                _sog[saveCheck.ShooterSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.SOG + saveCheck.ShooterSteamId, _sog[saveCheck.ShooterSteamId].ToString());
                                LogSOG(saveCheck.ShooterSteamId, _sog[saveCheck.ShooterSteamId]);
                                
                                // Track team stat
                                if (!_teamShots.TryGetValue(saveCheck.ShooterTeam, out int _))
                                    _teamShots.Add(saveCheck.ShooterTeam, 0);
                                _teamShots[saveCheck.ShooterTeam] += 1;
                                QueueStatUpdate(Codebase.Constants.TEAM_SHOTS + saveCheck.ShooterTeam.ToString(), _teamShots[saveCheck.ShooterTeam].ToString());

                                _lastShotWasCounted[saveCheck.ShooterTeam] = true;
                                
                                // Record Save immediately after SOG (Saves + Goals = SOG)
                                // Get other team goalie.
                                Player goalie = PlayerFunc.GetOtherTeamGoalie(saveCheck.ShooterTeam);
                                if (goalie != null) {
                                    string _goaliePlayerSteamId = goalie.SteamId.Value.Value;
                                    if (!_savePerc.TryGetValue(_goaliePlayerSteamId, out var savePercValue)) {
                                        _savePerc.Add(_goaliePlayerSteamId, (0, 0));
                                        savePercValue = (0, 0);
                                    }

                                    _savePerc[_goaliePlayerSteamId] = (++savePercValue.Saves, ++savePercValue.Shots);
                                    ReconcileGoalieShotsFaced(_goaliePlayerSteamId);
                                    QueueStatUpdate(Codebase.Constants.SAVEPERC + _goaliePlayerSteamId, _savePerc[_goaliePlayerSteamId].ToString());
                                    LogSavePerc(_goaliePlayerSteamId, _savePerc[_goaliePlayerSteamId].Saves, _savePerc[_goaliePlayerSteamId].Shots);
                                    
                                    // Reset shot attempt cooldown for the shooter when a save is recorded
                                    // This allows immediate shot attempts after a save
                                    if (!string.IsNullOrEmpty(saveCheck.ShooterSteamId)) {
                                        _lastShotAttemptGameTime[saveCheck.ShooterSteamId] = -1f;
                                    }
                                    
                                    // Record Save event in play-by-play
                                    Puck savePuck = PuckManager.Instance?.GetPuck();
                                    bool isHomePlateSave = false;
                                    if (goalie != null && goalie && savePuck != null) {
                                        Vector3 puckPos = savePuck.transform.position;
                                        Vector3 puckVel = savePuck.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                                        
                                        // Determine if this is a home plate save (home plate shot)
                                        // Check if the shot that resulted in this save was a home plate shot
                                        // First check the shot event's position (Flags might not be set yet for "attempt" shots)
                                        float saveCheckGameTime = GetCurrentGameTime();
                                        var lastShot = _playByPlayEvents.LastOrDefault(e => 
                                            e.PlayerSteamId == saveCheck.ShooterSteamId && 
                                            e.EventType == PlayByPlayEventType.Shot &&
                                            e.GameTime >= saveCheckGameTime - 3f);
                                        if (lastShot != null) {
                                            // Use shot event's position to determine flag (more reliable than Flags field)
                                            string shotFlag = DetermineShotFlag(lastShot.Position, (PlayerTeam)lastShot.PlayerTeam);
                                            if (shotFlag == "HomePlate") {
                                                isHomePlateSave = true;
                                            }
                                        }
                                        
                                        // Fallback 1: check pending shot release for home plate (if shot event not found)
                                        if (!isHomePlateSave) {
                                            if (_pendingShotReleases.TryGetValue(saveCheck.ShooterTeam, out var releaseInfo) && 
                                                !string.IsNullOrEmpty(releaseInfo.ShooterSteamId) && 
                                                releaseInfo.ShooterSteamId == saveCheck.ShooterSteamId) {
                                                string shotFlag = DetermineShotFlag(releaseInfo.PuckPosition, saveCheck.ShooterTeam);
                                                if (shotFlag == "HomePlate") {
                                                    isHomePlateSave = true;
                                                }
                                            }
                                        }
                                        
                                        // Note: Touch event fallback for isHomePlateSave is handled AFTER validation
                                        // when we create retroactive shot events (to avoid matching unrelated touches)
                                        
                                        // Build flags string: "Stick" or "Body", optionally combined with "HomePlate"
                                        string saveFlags = saveCheck.HitStick ? "Stick" : "Body";
                                        if (isHomePlateSave) {
                                            saveFlags += ",HomePlate";
                                        }
                                        
                                        RecordPlayByPlayEventInternal(PlayByPlayEventType.Save, goalie, puckPos, puckVel, "successful", saveFlags);
                                        
                                        // If shot was marked as "missed" but we just recorded a save, update shot to "on net"
                                        // This handles cases where the save occurs after the shot timeout window
                                        if (lastShot != null && lastShot.Outcome == "missed") {
                                            lastShot.Outcome = "on net";
                                            // Always set the flag based on shot position (HomePlate or Outside)
                                            // This ensures all "on net" shots have consistent flagging
                                            if (string.IsNullOrEmpty(lastShot.Flags)) {
                                                string shotFlag = DetermineShotFlag(lastShot.Position, (PlayerTeam)lastShot.PlayerTeam);
                                                lastShot.Flags = shotFlag;
                                            }
                                        }
                                        
                                        // If shot was marked as "blocked" but we just recorded a save, update shot to "on net"
                                        // This handles cases where a shot is blocked but still goes on net
                                        if (lastShot != null && lastShot.Outcome == "blocked") {
                                            lastShot.Outcome = "on net";
                                            // Always set the flag based on shot position (HomePlate or Outside)
                                            // The flag was cleared when marked as blocked, so restore it now
                                            if (string.IsNullOrEmpty(lastShot.Flags)) {
                                                string shotFlag = DetermineShotFlag(lastShot.Position, (PlayerTeam)lastShot.PlayerTeam);
                                                lastShot.Flags = shotFlag;
                                            }
                                            
                                            // Find and update any block event for this shot to mark it as "failed"
                                            // A block that doesn't prevent the shot from reaching the goalie is a failed block
                                            float saveCheckGameTimeForBlock = GetCurrentGameTime();
                                            var blockEvent = _playByPlayEvents
                                                .Where(e => e.EventType == PlayByPlayEventType.Block &&
                                                           e.GameTime >= lastShot.GameTime &&
                                                           e.GameTime <= saveCheckGameTimeForBlock &&
                                                           e.GameTime <= lastShot.GameTime + 3f)
                                                .OrderByDescending(e => e.GameTime)
                                                .FirstOrDefault();
                                            
                                            if (blockEvent != null && blockEvent.Outcome == "successful") {
                                                // Mark block as failed since shot still went on net
                                                blockEvent.Outcome = "failed";
                                            } else if (blockEvent == null) {
                                                // No block event was recorded, but shot was marked as blocked
                                                // This shouldn't happen, but if it does, we should record a failed block event
                                                // Find the blocker from the block check if available
                                                var blockCheck = _checkIfPuckWasBlocked.TryGetValue(saveCheck.ShooterTeam, out var bc) ? bc : null;
                                                if (blockCheck != null && !string.IsNullOrEmpty(blockCheck.BlockerSteamId)) {
                                                    Player blocker = PlayerManager.Instance.GetPlayerBySteamId(blockCheck.BlockerSteamId);
                                                    if (blocker != null && blocker && savePuck != null) {
                                                        RecordPlayByPlayEventInternal(PlayByPlayEventType.Block, blocker, puckPos, puckVel, "failed");
                                                    }
                                                }
                                            }
                                        }
                                        
                                        // Update body/stick saves dictionaries from pbp scanning (server-side only)
                                        UpdateBodyStickSavesFromPbp();
                                        
                                        // Track home plate saves and shots
                                        if (isHomePlateSave) {
                                            if (!_homePlateSaves.TryGetValue(_goaliePlayerSteamId, out int hpSaveValue)) {
                                                _homePlateSaves.Add(_goaliePlayerSteamId, 0);
                                                hpSaveValue = 0;
                                            }
                                            int hpSaves = _homePlateSaves[_goaliePlayerSteamId] = ++hpSaveValue;
                                            QueueStatUpdate(Codebase.Constants.HOME_PLATE_SAVES + _goaliePlayerSteamId, hpSaves.ToString());
                                            
                                            // Track home plate shot on goal (for HP SV% calculation)
                                            if (!_homePlateShots.TryGetValue(_goaliePlayerSteamId, out int hpShotValue)) {
                                                _homePlateShots.Add(_goaliePlayerSteamId, 0);
                                                hpShotValue = 0;
                                            }
                                            int hpShots = _homePlateShots[_goaliePlayerSteamId] = ++hpShotValue;
                                            QueueStatUpdate(Codebase.Constants.HOME_PLATE_SHOTS_FACED + _goaliePlayerSteamId, hpShots.ToString());
                                        }
                                    }
                                }

                                // Now do validation - update play-by-play events but don't roll back SOG/Save
                                float currentGameTime = GetCurrentGameTime();
                                var shotEvent = _playByPlayEvents.LastOrDefault(e => 
                                    e.PlayerSteamId == saveCheck.ShooterSteamId && 
                                    e.EventType == PlayByPlayEventType.Shot && 
                                    e.GameTime >= currentGameTime - 3f); // Shot should be within last 3 seconds
                                
                                if (shotEvent != null) {
                                    // Shot exists - update to "on net" if it's still "attempt"
                                    if (shotEvent.Outcome == "attempt") {
                                        shotEvent.Outcome = "on net";
                                        
                                        // Set home plate flag only for shots on goal (not attempts)
                                        string shotFlag = DetermineShotFlag(shotEvent.Position, (PlayerTeam)shotEvent.PlayerTeam);
                                        shotEvent.Flags = shotFlag;
                                        
                                        // Track home plate SOGs (only for shots on goal)
                                        if (shotFlag == "HomePlate") {
                                            if (!_homePlateSogs.TryGetValue(saveCheck.ShooterSteamId, out int _))
                                                _homePlateSogs.Add(saveCheck.ShooterSteamId, 0);
                                            _homePlateSogs[saveCheck.ShooterSteamId] += 1;
                                            QueueStatUpdate(Codebase.Constants.HOME_PLATE_SOGS + saveCheck.ShooterSteamId, _homePlateSogs[saveCheck.ShooterSteamId].ToString());
                                            
                                            // Track team home plate SOGs
                                            if (!_teamHomePlateSogs.TryGetValue(saveCheck.ShooterTeam, out int _))
                                                _teamHomePlateSogs.Add(saveCheck.ShooterTeam, 0);
                                            _teamHomePlateSogs[saveCheck.ShooterTeam] += 1;
                                            QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + saveCheck.ShooterTeam.ToString(), _teamHomePlateSogs[saveCheck.ShooterTeam].ToString());
                                        }
                                    }
                                    // If shot outcome is "missed", SOG and Save remain recorded (validation doesn't roll back)
                                } else {
                                    // No shot event found - try to record retroactively if release info available
                                    bool retroactiveShotCreated = false;
                                    if (_pendingShotReleases.TryGetValue(saveCheck.ShooterTeam, out var releaseInfo) && !string.IsNullOrEmpty(releaseInfo.ShooterSteamId) && releaseInfo.ShooterSteamId == saveCheck.ShooterSteamId) {
                                        // Check if release info is recent (within last 5 seconds) to prevent using stale data
                                        float timeSinceRelease = (float)(DateTime.UtcNow - releaseInfo.ReleaseTime).TotalSeconds;
                                        if (timeSinceRelease <= 5f) {
                                            Player shooter = PlayerManager.Instance.GetPlayerBySteamId(releaseInfo.ShooterSteamId);
                                            if (shooter != null && shooter) {
                                                // Double-check that no shot event already exists for this shooter (prevent duplicates)
                                                var existingShot = _playByPlayEvents.LastOrDefault(e => 
                                                    e.PlayerSteamId == saveCheck.ShooterSteamId && 
                                                    e.EventType == PlayByPlayEventType.Shot && 
                                                    e.GameTime >= currentGameTime - 5f);
                                                
                                                if (existingShot == null) {
                                                    string shotFlag = DetermineShotFlag(releaseInfo.PuckPosition, shooter.Team);
                                                    RecordPlayByPlayEventInternal(PlayByPlayEventType.Shot, shooter, releaseInfo.PuckPosition, releaseInfo.PuckVelocity, "on net", shotFlag);
                                                    
                                                    // Track shot attempt stats for retroactive shot (player and team)
                                                    if (!_shotAttempts.TryGetValue(saveCheck.ShooterSteamId, out int _))
                                                        _shotAttempts.Add(saveCheck.ShooterSteamId, 0);
                                                    _shotAttempts[saveCheck.ShooterSteamId] += 1;
                                                    QueueStatUpdate(Codebase.Constants.SHOT_ATTEMPTS + saveCheck.ShooterSteamId, _shotAttempts[saveCheck.ShooterSteamId].ToString());
                                                    
                                                    // Track team shot attempts
                                                    if (!_teamShotAttempts.TryGetValue(saveCheck.ShooterTeam, out int _))
                                                        _teamShotAttempts.Add(saveCheck.ShooterTeam, 0);
                                                    _teamShotAttempts[saveCheck.ShooterTeam] += 1;
                                                    QueueStatUpdate(Codebase.Constants.TEAM_SHOT_ATTEMPTS + saveCheck.ShooterTeam.ToString(), _teamShotAttempts[saveCheck.ShooterTeam].ToString());
                                                    
                                                    // Track home plate SOGs if applicable
                                                    if (shotFlag == "HomePlate") {
                                                        if (!_homePlateSogs.TryGetValue(saveCheck.ShooterSteamId, out int _))
                                                            _homePlateSogs.Add(saveCheck.ShooterSteamId, 0);
                                                        _homePlateSogs[saveCheck.ShooterSteamId] += 1;
                                                        QueueStatUpdate(Codebase.Constants.HOME_PLATE_SOGS + saveCheck.ShooterSteamId, _homePlateSogs[saveCheck.ShooterSteamId].ToString());
                                                        
                                                        // Track team home plate SOGs
                                                        if (!_teamHomePlateSogs.TryGetValue(saveCheck.ShooterTeam, out int _))
                                                            _teamHomePlateSogs.Add(saveCheck.ShooterTeam, 0);
                                                        _teamHomePlateSogs[saveCheck.ShooterTeam] += 1;
                                                        QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + saveCheck.ShooterTeam.ToString(), _teamHomePlateSogs[saveCheck.ShooterTeam].ToString());
                                                        
                                                        // Note: Home plate saves/shots for goalie are already handled above when isHomePlateSave was set
                                                        // from pending release check, so no need to increment again here
                                                    }
                                                    retroactiveShotCreated = true;
                                                }
                                            }
                                        }
                                    }
                                    
                                    // Fallback: If no release info available, use touch events (mimic goal fallback process)
                                    if (!retroactiveShotCreated) {
                                        Player shooter = PlayerManager.Instance.GetPlayerBySteamId(saveCheck.ShooterSteamId);
                                        if (shooter != null && shooter) {
                                            // Find the last touch event for this shooter (try multiple time windows)
                                            var lastTouchEvent = _playByPlayEvents.LastOrDefault(e => 
                                                e.PlayerSteamId == saveCheck.ShooterSteamId && 
                                                e.EventType == PlayByPlayEventType.Touch &&
                                                e.GameTime >= currentGameTime - 3f);
                                            
                                            // If no touch found for this specific player within 3 seconds, try longer window (12 seconds)
                                            if (lastTouchEvent == null) {
                                                lastTouchEvent = _playByPlayEvents.LastOrDefault(e => 
                                                    e.PlayerSteamId == saveCheck.ShooterSteamId && 
                                                    e.EventType == PlayByPlayEventType.Touch &&
                                                    e.GameTime >= currentGameTime - 12f);
                                            }
                                            
                                            // If still no touch found for this specific player, find last touch by any player on the shooting team
                                            if (lastTouchEvent == null) {
                                                lastTouchEvent = _playByPlayEvents.LastOrDefault(e => 
                                                    e.PlayerTeam == (int)saveCheck.ShooterTeam && 
                                                    e.EventType == PlayByPlayEventType.Touch &&
                                                    e.GameTime >= currentGameTime - 12f);
                                            }
                                            
                                            // Use position from touch event if found, otherwise use blank/zero coordinates
                                            Vector3 shotPosition = lastTouchEvent != null ? lastTouchEvent.Position : Vector3.zero;
                                            Vector3 shotVelocity = lastTouchEvent != null ? lastTouchEvent.Velocity : Vector3.zero;
                                            float? shotGameTime = lastTouchEvent != null ? (float?)lastTouchEvent.GameTime : null;
                                            float? shotPlayerSpeed = lastTouchEvent != null ? (float?)lastTouchEvent.PlayerSpeed : null;
                                            
                                            // If position is still zero, try to find ANY recent touch by this shooter (even older)
                                            if (shotPosition == Vector3.zero) {
                                                var fallbackTouchEvent = _playByPlayEvents.LastOrDefault(e => 
                                                    e.PlayerSteamId == saveCheck.ShooterSteamId && 
                                                    e.EventType == PlayByPlayEventType.Touch);
                                                
                                                if (fallbackTouchEvent != null && fallbackTouchEvent.Position != Vector3.zero) {
                                                    shotPosition = fallbackTouchEvent.Position;
                                                    shotVelocity = fallbackTouchEvent.Velocity;
                                                    shotGameTime = fallbackTouchEvent.GameTime;
                                                    shotPlayerSpeed = fallbackTouchEvent.PlayerSpeed;
                                                }
                                            }
                                            
                                            // Determine flag based on position (only if position is valid)
                                            string shotFlag = (shotPosition != Vector3.zero) ? DetermineShotFlag(shotPosition, shooter.Team) : "";
                                            
                                            // Record shot with touch event data (GameTime, PlayerSpeed, Velocity, Position)
                                            // ForceMagnitude will be automatically calculated from velocity.magnitude
                                            RecordPlayByPlayEventInternal(PlayByPlayEventType.Shot, shooter, shotPosition, shotVelocity, "on net", shotFlag, shotPlayerSpeed, false, shotGameTime);
                                            
                                            // Track shot attempt stats for retroactive shot (player and team)
                                            if (!_shotAttempts.TryGetValue(saveCheck.ShooterSteamId, out int _))
                                                _shotAttempts.Add(saveCheck.ShooterSteamId, 0);
                                            _shotAttempts[saveCheck.ShooterSteamId] += 1;
                                            QueueStatUpdate(Codebase.Constants.SHOT_ATTEMPTS + saveCheck.ShooterSteamId, _shotAttempts[saveCheck.ShooterSteamId].ToString());
                                            
                                            // Track team shot attempts
                                            if (!_teamShotAttempts.TryGetValue(saveCheck.ShooterTeam, out int _))
                                                _teamShotAttempts.Add(saveCheck.ShooterTeam, 0);
                                            _teamShotAttempts[saveCheck.ShooterTeam] += 1;
                                            QueueStatUpdate(Codebase.Constants.TEAM_SHOT_ATTEMPTS + saveCheck.ShooterTeam.ToString(), _teamShotAttempts[saveCheck.ShooterTeam].ToString());
                                            
                                            // Track home plate SOGs if applicable
                                            if (shotFlag == "HomePlate") {
                                                if (!_homePlateSogs.TryGetValue(saveCheck.ShooterSteamId, out int _))
                                                    _homePlateSogs.Add(saveCheck.ShooterSteamId, 0);
                                                _homePlateSogs[saveCheck.ShooterSteamId] += 1;
                                                QueueStatUpdate(Codebase.Constants.HOME_PLATE_SOGS + saveCheck.ShooterSteamId, _homePlateSogs[saveCheck.ShooterSteamId].ToString());
                                                
                                                // Track team home plate SOGs
                                                if (!_teamHomePlateSogs.TryGetValue(saveCheck.ShooterTeam, out int _))
                                                    _teamHomePlateSogs.Add(saveCheck.ShooterTeam, 0);
                                                _teamHomePlateSogs[saveCheck.ShooterTeam] += 1;
                                                QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + saveCheck.ShooterTeam.ToString(), _teamHomePlateSogs[saveCheck.ShooterTeam].ToString());
                                                
                                                // Also track home plate save for goalie (since we're creating retroactive shot from touch)
                                                if (goalie != null && goalie) {
                                                    string _goaliePlayerSteamId = goalie.SteamId.Value.Value;
                                                    if (!_homePlateSaves.TryGetValue(_goaliePlayerSteamId, out int hpSaveValue)) {
                                                        _homePlateSaves.Add(_goaliePlayerSteamId, 0);
                                                        hpSaveValue = 0;
                                                    }
                                                    int hpSaves = _homePlateSaves[_goaliePlayerSteamId] = ++hpSaveValue;
                                                    QueueStatUpdate(Codebase.Constants.HOME_PLATE_SAVES + _goaliePlayerSteamId, hpSaves.ToString());
                                                    
                                                    // Track home plate shot on goal (for HP SV% calculation)
                                                    if (!_homePlateShots.TryGetValue(_goaliePlayerSteamId, out int hpShotValue)) {
                                                        _homePlateShots.Add(_goaliePlayerSteamId, 0);
                                                        hpShotValue = 0;
                                                    }
                                                    int hpShots = _homePlateShots[_goaliePlayerSteamId] = ++hpShotValue;
                                                    QueueStatUpdate(Codebase.Constants.HOME_PLATE_SHOTS_FACED + _goaliePlayerSteamId, hpShots.ToString());
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // Clear the save check to prevent reprocessing
                                _checkIfPuckWasSaved[key] = new SaveCheck();
                                _checkIfPuckWasBlocked[key] = new BlockCheck();
                            }
                            else {
                                if (++saveCheck.FramesChecked > ServerManager.Instance.Server.Value.TickRate)
                                    _checkIfPuckWasSaved[key] = new SaveCheck();
                            }
                        }

                        // Block logic.
                        // Blocks should only happen if a shot was determined to be on net, but was touched by a defending player
                        // and did not reach the goalie or goal.
                        foreach (PlayerTeam key in new List<PlayerTeam>(_checkIfPuckWasBlocked.Keys)) {
                            BlockCheck blockCheck = _checkIfPuckWasBlocked[key];
                            if (!blockCheck.HasToCheck) {
                                _checkIfPuckWasBlocked[key] = new BlockCheck();
                                continue;
                            }

                            //Logging.Log($"kvp.Check {blockCheck.FramesChecked} for team {key} blocked by {blockCheck.BlockerSteamId}.", ModServerConfig, true);

                            // Only process block if:
                            // 1. Puck stopped going to net (PuckIsGoingToNet became false)
                            // 2. Shot was on net (PuckIsGoingToNet was true when block check was set up)
                            // 3. No save occurred (goalie didn't touch it - verified by _lastShotWasCounted)
                            // 4. Shot didn't result in a goal
                            // 5. A shot attempt occurred within the last 3 seconds
                            if (!_puckRaycast.PuckIsGoingToNet[key] && !_lastBlockWasCounted[blockCheck.ShooterTeam]) {
                                // Check if a save occurred - _lastShotWasCounted is set to true when a save is processed
                                // If it's false, no save occurred, so this can be counted as a block
                                bool saveOccurred = _lastShotWasCounted[blockCheck.ShooterTeam];
                                
                                // Also check if a save event was recorded recently (within last 3 seconds)
                                // This handles cases where save occurred but _lastShotWasCounted wasn't set yet
                                float currentGameTime = GetCurrentGameTime();
                                bool recentSaveExists = _playByPlayEvents.Any(e =>
                                    e.EventType == PlayByPlayEventType.Save &&
                                    e.GameTime >= currentGameTime - 3f);
                                
                                // Get shooter SteamId from last player on puck
                                string shooterSteamId = _lastPlayerOnPuckTipIncludedSteamId[blockCheck.ShooterTeam].SteamId;
                                
                                // Verify that a shot attempt occurred within the last 3 seconds
                                bool shotAttemptExists = !string.IsNullOrEmpty(shooterSteamId) && _playByPlayEvents.Any(e =>
                                    e.PlayerSteamId == shooterSteamId &&
                                    e.EventType == PlayByPlayEventType.Shot &&
                                    e.PlayerTeam == (int)blockCheck.ShooterTeam &&
                                    e.GameTime >= currentGameTime - 3f); // Shot attempt within last 3 seconds
                                
                                // Only count as block if: shot was on net, no save occurred, AND shot attempt exists
                                // Shot is on net because PuckIsGoingToNet was true when block check was set up
                                // (block checks are only set up when puck is going to net - line 1411)
                                if (!saveOccurred && !recentSaveExists && shotAttemptExists) {
                                    ProcessBlock(blockCheck.BlockerSteamId, shooterSteamId, blockCheck.ShooterTeam, blockCheck.BlockGameTime);
                                    _lastBlockWasCounted[blockCheck.ShooterTeam] = true;
                                } else if ((saveOccurred || recentSaveExists) && shotAttemptExists) {
                                    // Save occurred after block check was set up - record as failed block
                                    // This means the shot was blocked but still went on net
                                    Player blocker = PlayerManager.Instance.GetPlayerBySteamId(blockCheck.BlockerSteamId);
                                    Puck blockPuck = PuckManager.Instance?.GetPuck();
                                    if (blocker != null && blocker && blockPuck != null) {
                                        Vector3 puckPos = blockPuck.transform.position;
                                        Vector3 puckVel = blockPuck.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                                        // Use the block game time when the block check was set up, not current time
                                        RecordPlayByPlayEventInternal(PlayByPlayEventType.Block, blocker, puckPos, puckVel, "failed", null, blockCheck.BlockGameTime);
                                    }
                                    _lastBlockWasCounted[blockCheck.ShooterTeam] = true;
                                }

                                // Get other team goalie.
                                Player goalie = PlayerFunc.GetOtherTeamGoalie(blockCheck.ShooterTeam);

                                _checkIfPuckWasSaved[key] = new SaveCheck();
                                _checkIfPuckWasBlocked[key] = new BlockCheck();
                            }
                            else {
                                if (++blockCheck.FramesChecked > ServerManager.Instance.Server.Value.TickRate)
                                    _checkIfPuckWasBlocked[key] = new BlockCheck();
                            }
                        }

                    Puck puck = PuckManager.Instance.GetPuck();
                    if (puck) {
                        _puckZCoordinateDifference = (puck.Rigidbody.transform.position.z - _puckLastCoordinate.z) / 240 * ServerManager.Instance.Server.Value.TickRate;
                        _puckLastCoordinate = new Vector3(puck.Rigidbody.transform.position.x, puck.Rigidbody.transform.position.y, puck.Rigidbody.transform.position.z);
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in ServerManager_Update_Patch Postfix().\n{ex}", ModServerConfig);
                }

                return;
            }
        }

        /// <summary>
        /// Class that patches the UpdatePlayer event from UIScoreboard.
        /// </summary>
        // UpdatePlayer was renamed in B323; using string literal so compile succeeds ? verify method name at runtime
        [HarmonyPatch(typeof(UIScoreboard), nameof(UIScoreboard.StylePlayer))]
        public class UIScoreboard_StylePlayer_Patch {
            [HarmonyPostfix]
            public static void Postfix(UIScoreboard __instance, Player player) {
                try {
                    // If this is the server, do not use the patch.
                    if (ServerFunc.IsDedicatedServer())
                        return;

                    #if DEBUG_MODE
                    DebugTrace.Write("StylePlayer", $"Patch firing. serverHasResponded={_serverHasResponded} registered={_hasRegisteredWithNamedMessageHandler}");
                    #endif

                    if (!_hasRegisteredWithNamedMessageHandler) {
                        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(Constants.FROM_SERVER_TO_CLIENT, ReceiveData);
                        _hasRegisteredWithNamedMessageHandler = true;
                    }

                    if (!_serverHasResponded) {
                        DateTime now = DateTime.UtcNow;
                        if (_lastDateTimeAskStartupData + TimeSpan.FromSeconds(1) < now && _askServerForStartupDataCount++ < 10) {
                            _lastDateTimeAskStartupData = now;
                            NetworkCommunication.SendData(Constants.ASK_SERVER_FOR_STARTUP_DATA, MOD_VERSION, NetworkManager.ServerClientId, Constants.FROM_CLIENT_TO_SERVER, _clientConfig);
                        }
                    }


                    OnClientStylePlayer(__instance, player);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in UIScoreboard_UpdateServer_Patch Postfix().\n{ex}", _clientConfig);
                }
            }
        }

        /// <summary>
        /// Re-applies position sort after the game's SortPlayers so vanilla reordering does not win.
        /// </summary>
        [HarmonyPatch(typeof(UIScoreboard), nameof(UIScoreboard.SortPlayers))]
        public class UIScoreboard_SortPlayers_Patch {
            [HarmonyPostfix]
            public static void Postfix(UIScoreboard __instance) {
                if (ServerFunc.IsDedicatedServer())
                    return;

                try {
                    VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), __instance, "scoreboard");
                    ReorderScoreboardPlayers(scoreboardContainer);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in UIScoreboard_SortPlayers_Patch Postfix().\n{ex}", _clientConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the OnCollisionEnter event from Puck.
        /// </summary>
        [HarmonyPatch(typeof(Puck), "OnCollisionEnter")]
        public class Puck_OnCollisionEnter_Patch {
            [HarmonyPostfix]
            public static void Postfix(Puck __instance, Collision collision) {
                // If this is not the server or game is not started, do not use the patch.
                if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Play || !_logic)
                    return;

                try {
                    Player player = null;
                    Stick stick = SystemFunc.GetStick(collision.gameObject);
                    if (!stick) {
                        PlayerBody playerBody = SystemFunc.GetPlayerBody(collision.gameObject);
                        if (!playerBody || !playerBody.Player)
                            return;

                        player = playerBody.Player;
                    }
                    else {
                        if (!stick.Player)
                            return;

                        player = stick.Player;
                    }

                    string currentPlayerSteamId = player.SteamId.Value.Value;

                    // Track puck touches - only count when first touching (OnCollisionEnter)
                    // Handle stick touches for both skaters and goalies
                    // Goalie body touches do NOT count as possessions - only stick touches
                    Stick stickForTouch = SystemFunc.GetStick(collision.gameObject);
                    if (stickForTouch != null) {
                        ProcessPuckTouch(currentPlayerSteamId, player, __instance);
                        
                        // Update possession time - only count time between consecutive touches
                        DateTime now = DateTime.UtcNow;
                        if (_lastPossessionTouchTime.TryGetValue(currentPlayerSteamId, out DateTime lastTouchTime)) {
                            double timeSinceLastTouch = (now - lastTouchTime).TotalSeconds;
                            // If touched again within 5 seconds, add the interval to possession time
                            if (timeSinceLastTouch <= 5.0) {
                                if (!_possessionTimeSeconds.TryGetValue(currentPlayerSteamId, out double _))
                                    _possessionTimeSeconds.Add(currentPlayerSteamId, 0.0);
                                
                                _possessionTimeSeconds[currentPlayerSteamId] += timeSinceLastTouch;
                                
                                // Send updated possession time to clients
                                QueueStatUpdate(Codebase.Constants.POSSESSION_TIME + currentPlayerSteamId, _possessionTimeSeconds[currentPlayerSteamId].ToString("F2"));
                            }
                        }
                        // Update last touch time for next interval calculation
                        _lastPossessionTouchTime[currentPlayerSteamId] = now;
                        
                        // Turnover/takeaway detection is now handled in ValidatePendingTurnoversTakeaways
                        // when the new team reaches their 2nd event
                    }

                    // Start tipped timer.
                    if (!_playersCurrentPuckTouch.TryGetValue(currentPlayerSteamId, out Stopwatch watch)) {
                        watch = new Stopwatch();
                        watch.Start();
                        _playersCurrentPuckTouch.Add(currentPlayerSteamId, watch);
                    }

                    string lastPlayerOnPuckTipIncludedSteamId = _lastPlayerOnPuckTipIncludedSteamId[_lastTeamOnPuckTipIncluded].SteamId;

                    if (!_lastTimeOnCollisionStayOrExitWasCalled.TryGetValue(currentPlayerSteamId, out Stopwatch lastTimeCollisionExitWatch)) {
                        lastTimeCollisionExitWatch = new Stopwatch();
                        lastTimeCollisionExitWatch.Start();
                        _lastTimeOnCollisionStayOrExitWasCalled.Add(currentPlayerSteamId, lastTimeCollisionExitWatch);
                    }
                    else if (lastTimeCollisionExitWatch.ElapsedMilliseconds > ModServerConfig.MaxPossessionMilliseconds || (!string.IsNullOrEmpty(lastPlayerOnPuckTipIncludedSteamId) && lastPlayerOnPuckTipIncludedSteamId != currentPlayerSteamId)) {
                        watch.Restart();

                        if (!string.IsNullOrEmpty(lastPlayerOnPuckTipIncludedSteamId) && lastPlayerOnPuckTipIncludedSteamId != currentPlayerSteamId) {
                            if (_playersCurrentPuckTouch.TryGetValue(lastPlayerOnPuckTipIncludedSteamId, out Stopwatch lastPlayerWatch))
                                lastPlayerWatch.Reset();
                        }
                    }

                    PlayerTeam otherTeam = TeamFunc.GetOtherTeam(player.Team);

                    if (_puckRaycast.PuckIsGoingToNet[player.Team]) {
                        if (PlayerFunc.IsGoalie(player) && Math.Abs(player.PlayerBody.Rigidbody.transform.position.z) > 13.5) {
                            PlayerTeam shooterTeam = otherTeam;
                            string shooterSteamId = _lastPlayerOnPuckTipIncludedSteamId[shooterTeam].SteamId;
                            if (!string.IsNullOrEmpty(shooterSteamId)) {
                                // Track whether the save involved the stick or body.
                                // Body contact takes priority: if body was touched at ANY point, classify as body save.
                                // Only classify as stick save when the stick is the sole contact throughout.
                                bool hitStick = stick != null;
                                if (_checkIfPuckWasSaved.TryGetValue(player.Team, out SaveCheck existingCheck) && existingCheck.HasToCheck) {
                                    // AND logic: both current AND previous contact must be stick-only for HitStick to stay true
                                    hitStick = existingCheck.HitStick && hitStick;
                                }
                                
                                _checkIfPuckWasSaved[player.Team] = new SaveCheck {
                                    HasToCheck = true,
                                    ShooterSteamId = shooterSteamId,
                                    ShooterTeam = shooterTeam,
                                    HitStick = hitStick,
                                };
                                
                                // Shot should already be recorded when raycast confirmed in ServerManager Update
                                // Save event will be recorded separately when raycast goes false
                            }
                        }
                        else {
                            PlayerTeam shooterTeam = otherTeam;
                            string shooterSteamId = _lastPlayerOnPuckTipIncludedSteamId[shooterTeam].SteamId;
                            if (!string.IsNullOrEmpty(shooterSteamId)) {
                                _checkIfPuckWasBlocked[player.Team] = new BlockCheck {
                                    HasToCheck = true,
                                    BlockerSteamId = player.SteamId.Value.Value,
                                    ShooterTeam = shooterTeam,
                                    BlockGameTime = GetCurrentGameTime(), // Store the game time when block check is set up
                                };
                            }
                        }
                    }
                    else {
                        if (_lastTeamOnPuckTipIncluded == otherTeam && PlayerFunc.IsGoalie(player) && Math.Abs(player.PlayerBody.Rigidbody.transform.position.z) > 13.5) {
                            if ((player.Team == PlayerTeam.Blue && _puckZCoordinateDifference > ModServerConfig.GoalieSaveCreaseSystemZDelta) || (player.Team == PlayerTeam.Red && _puckZCoordinateDifference < -ModServerConfig.GoalieSaveCreaseSystemZDelta)) {
                                (double startX, double endX) = (0, 0);
                                (double startZ, double endZ) = (0, 0);
                                if (player.Team == PlayerTeam.Blue) {
                                    (startX, endX) = ZoneFunc.ICE_X_POSITIONS[IceElement.BlueTeam_BluePaint];
                                    (startZ, endZ) = ZoneFunc.ICE_Z_POSITIONS[IceElement.BlueTeam_BluePaint];
                                }
                                else {
                                    (startX, endX) = ZoneFunc.ICE_X_POSITIONS[IceElement.RedTeam_BluePaint];
                                    (startZ, endZ) = ZoneFunc.ICE_Z_POSITIONS[IceElement.RedTeam_BluePaint];
                                }

                                bool goalieIsInHisCrease = true;
                                if (player.PlayerBody.Rigidbody.transform.position.x - ModServerConfig.GoalieRadius < startX ||
                                    player.PlayerBody.Rigidbody.transform.position.x + ModServerConfig.GoalieRadius > endX ||
                                    player.PlayerBody.Rigidbody.transform.position.z - ModServerConfig.GoalieRadius < startZ ||
                                    player.PlayerBody.Rigidbody.transform.position.z + ModServerConfig.GoalieRadius > endZ) {
                                    goalieIsInHisCrease = false;
                                }

                                if (goalieIsInHisCrease) {
                                    PlayerTeam shooterTeam = TeamFunc.GetOtherTeam(player.Team);
                                    string shooterSteamId = _lastPlayerOnPuckTipIncludedSteamId[shooterTeam].SteamId;
                                    if (!string.IsNullOrEmpty(shooterSteamId)) {
                                        // Body contact takes priority over stick: only a stick save when stick is sole contact throughout.
                                        bool hitStick = stick != null;
                                        if (_checkIfPuckWasSaved.TryGetValue(player.Team, out SaveCheck existingCheck) && existingCheck.HasToCheck) {
                                            // AND logic: body touching at any point overrides stick classification
                                            hitStick = existingCheck.HitStick && hitStick;
                                        }
                                        
                                        _checkIfPuckWasSaved[player.Team] = new SaveCheck {
                                            HasToCheck = true,
                                            ShooterSteamId = shooterSteamId,
                                            ShooterTeam = shooterTeam,
                                            HitStick = hitStick,
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in Puck_OnCollisionEnter_Patch Postfix().\n{ex}", ModServerConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the OnCollisionStay event from Puck.
        /// </summary>
        [HarmonyPatch(typeof(Puck), "OnCollisionStay")]
        public class Puck_OnCollisionStay_Patch {
            [HarmonyPostfix]
            public static void Postfix(Collision collision) {
                try {
                    // If this is not the server or game is not started, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Play || !_logic)
                        return;

                    Player player;

                    Stick stick = SystemFunc.GetStick(collision.gameObject);
                    if (!stick) {
                        PlayerBody playerBody = SystemFunc.GetPlayerBody(collision.gameObject);
                        if (!playerBody || !playerBody.Player)
                            return;

                        player = playerBody.Player;
                    }
                    else {
                        if (!stick.Player)
                            return;

                        player = stick.Player;
                    }

                    string playerSteamId = player.SteamId.Value.Value;

                    // Possession time is now only tracked in OnCollisionEnter (between consecutive touches)
                    // No need to track in OnCollisionStay

                    if (!_lastTimeOnCollisionStayOrExitWasCalled.TryGetValue(playerSteamId, out Stopwatch lastTimeCollisionWatch)) {
                        lastTimeCollisionWatch = new Stopwatch();
                        lastTimeCollisionWatch.Start();
                        _lastTimeOnCollisionStayOrExitWasCalled.Add(playerSteamId, lastTimeCollisionWatch);
                    }
                    lastTimeCollisionWatch.Restart();

                    string lastPlayerOnPuckTipIncluded = _lastPlayerOnPuckTipIncludedSteamId[player.Team].SteamId;

                    // Note: Pass/Reception events are now created in real-time in ProcessPuckTouch
                    // when a new player on the same team touches the puck, converting the previous
                    // player's last Touch to a Pass and recording the current touch as a Reception
                    
                    if (playerSteamId != lastPlayerOnPuckTipIncluded) {
                        _lastPlayerOnPuckTipIncludedSteamId[player.Team] = (playerSteamId, DateTime.UtcNow);
                    }

                    _lastTeamOnPuckTipIncluded = player.Team;

                    // Puck battle tracking - detect when puck is tipped (multiple sticks contacting simultaneously)
                    bool isTipped = PuckFunc.PuckIsTipped(playerSteamId, ModServerConfig.MaxTippedMilliseconds, _playersCurrentPuckTouch, _lastTimeOnCollisionStayOrExitWasCalled);
                    
                    // Detect puck battle when tip starts (two sticks contacting puck simultaneously)
                    if (isTipped && !_wasTippedLastFrame) {
                        // Check if there are players from opposing teams touching the puck simultaneously
                        // A player is actively touching if their stopwatch is running and recent
                        List<string> playersTouchingPuck = new List<string>();
                        HashSet<PlayerTeam> teamsTouchingPuck = new HashSet<PlayerTeam>();
                        
                        DateTime now = DateTime.UtcNow;
                        foreach (var kvp in _playersCurrentPuckTouch) {
                            string touchingSteamId = kvp.Key;
                            Player touchingPlayer = PlayerManager.Instance.GetPlayerBySteamId(touchingSteamId);
                            if (touchingPlayer != null && touchingPlayer && !PlayerFunc.IsGoalie(touchingPlayer)) {
                                // Check if this player is actively touching (stopwatch is running and recent)
                                // If the stopwatch elapsed time is very small, they're actively touching
                                if (kvp.Value.IsRunning && kvp.Value.ElapsedMilliseconds < ModServerConfig.MaxTippedMilliseconds * 2) {
                                    // Also check if they haven't exited recently
                                    if (_lastTimeOnCollisionStayOrExitWasCalled.TryGetValue(touchingSteamId, out Stopwatch exitWatch)) {
                                        if (exitWatch.IsRunning && exitWatch.ElapsedMilliseconds < ModServerConfig.MaxTippedMilliseconds * 2) {
                                            playersTouchingPuck.Add(touchingSteamId);
                                            teamsTouchingPuck.Add(touchingPlayer.Team);
                                        }
                                    } else {
                                        // If no exit watch, assume they're touching
                                        playersTouchingPuck.Add(touchingSteamId);
                                        teamsTouchingPuck.Add(touchingPlayer.Team);
                                    }
                                }
                            }
                        }
                        
                        // Check if we have players from opposing teams
                        bool hasOpposingTeams = teamsTouchingPuck.Contains(PlayerTeam.Blue) && teamsTouchingPuck.Contains(PlayerTeam.Red);
                        
                        if (hasOpposingTeams && playersTouchingPuck.Count >= 2) {
                            // Cooldown check to prevent duplicate battles
                            if ((now - _lastPuckBattleTime).TotalMilliseconds >= PUCK_BATTLE_COOLDOWN_MS) {
                                // Determine who had possession (defending team) vs who didn't (contesting team)
                                // Use _currentTeamInPossession as primary source since it's the most accurate
                                PlayerTeam defendingTeam = PlayerTeam.None;
                                PlayerTeam contestingTeam = PlayerTeam.None;
                                
                                if (_currentTeamInPossession != PlayerTeam.None) {
                                    defendingTeam = _currentTeamInPossession;
                                    contestingTeam = defendingTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
                                } else if (_lastPossession.Team != PlayerTeam.None) {
                                    defendingTeam = _lastPossession.Team;
                                    contestingTeam = defendingTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
                                } else {
                                    // If no clear possession, use the team that was last on puck
                                    if (_lastTeamOnPuck != PlayerTeam.None) {
                                        defendingTeam = _lastTeamOnPuck;
                                        contestingTeam = defendingTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
                                    }
                                }
                                
                                // Record puck battle for each player involved
                                Puck battlePuck = PuckManager.Instance?.GetPuck();
                                if (battlePuck != null) {
                                    Vector3 puckPos = battlePuck.transform.position;
                                    Vector3 puckVel = battlePuck.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
                                    
                                    // Determine team in possession string for override (defending team has possession)
                                    string defendingTeamInPossession = defendingTeam != PlayerTeam.None 
                                        ? (defendingTeam == PlayerTeam.Blue ? "Blue" : "Red") 
                                        : null;
                                    
                                    foreach (string battleSteamId in playersTouchingPuck) {
                                        Player battlePlayer = PlayerManager.Instance.GetPlayerBySteamId(battleSteamId);
                                        if (battlePlayer != null && battlePlayer) {
                                            // Determine flag: "defending" if on defending team, "contesting" if on contesting team
                                            string battleFlag = battlePlayer.Team == defendingTeam ? "defending" : "contesting";
                                            
                                            // Record puck battle event - this does NOT reset possession chains
                                            // Pass defending team as teamInPossessionOverride to ensure correct TeamInPossession for both players
                                            RecordPlayByPlayEventInternal(PlayByPlayEventType.PuckBattle, battlePlayer, puckPos, puckVel, "neutral", battleFlag, null, skipPossessionReset: true, teamInPossessionOverride: defendingTeamInPossession);
                                        }
                                    }
                                    
                                    _lastPuckBattleTime = now;
                                }
                            }
                        }
                    }
                    
                    if (!isTipped) {
                        _lastTeamOnPuck = player.Team;
                        _lastPlayerOnPuckSteamId[player.Team] = (playerSteamId, DateTime.UtcNow);
                    }
                    
                    _wasTippedLastFrame = isTipped;

                    if (!_playersLastTimePuckPossession.TryGetValue(playerSteamId, out Stopwatch watch)) {
                        watch = new Stopwatch();
                        watch.Start();
                        _playersLastTimePuckPossession.Add(playerSteamId, watch);
                    }

                    watch.Restart();

                    // Update possession tracking (turnover/takeaway detection moved to OnCollisionEnter)
                    // Don't update _lastPossession if the last event was a failed touch from an opposing team
                    // This prevents failed touches from breaking the possession chain
                    bool shouldUpdatePossession = true;
                    if (_lastEvent != null && _lastEvent.EventType == PlayByPlayEventType.Touch && 
                        _lastEvent.Outcome == "failed" && 
                        _lastEvent.PlayerTeam != (int)player.Team &&
                        _lastPossession.Team != PlayerTeam.None && 
                        _lastPossession.Team != player.Team) {
                        // Last event was a failed touch from opposing team - don't update possession
                        // Keep the previous team's possession until a successful touch occurs
                        shouldUpdatePossession = false;
                    }
                    
                    if (shouldUpdatePossession) {
                        string currentPossessionSteamId = PlayerFunc.GetPlayerSteamIdInPossession(ModServerConfig.MinPossessionMilliseconds, ModServerConfig.MaxPossessionMilliseconds,
                        ModServerConfig.MaxTippedMilliseconds, _playersLastTimePuckPossession, _playersCurrentPuckTouch, true);
                        if (!string.IsNullOrEmpty(currentPossessionSteamId)) {
                            // Track possession start time for new possession
                            if (!_possessionStartTime.ContainsKey(currentPossessionSteamId)) {
                                _possessionStartTime[currentPossessionSteamId] = DateTime.UtcNow;
                            }

                            _lastPossession = new Possession {
                                SteamId = currentPossessionSteamId,
                                Team = player.Team,
                                Date = DateTime.UtcNow,
                            };
                        } else {
                            // Clear possession start time if no possession
                            _possessionStartTime.Remove(playerSteamId);
                        }
                    }
                    
                    // Update continuous team possession time (runs regardless of individual touches)
                    UpdateTeamPossessionTime(_lastPossession.Team);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in Puck_OnCollisionStay_Patch Postfix().\n{ex}", ModServerConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the OnCollisionExit event from Puck.
        /// </summary>
        [HarmonyPatch(typeof(Puck), "OnCollisionExit")]
        public class Puck_OnCollisionExit_Patch {
            [HarmonyPostfix]
            public static void Postfix(Puck __instance, Collision collision) {
                try {
                    // If this is not the server or game is not started, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Play || !_logic)
                        return;

                    Stick stick = SystemFunc.GetStick(collision.gameObject);
                    if (!stick)
                        return;

                    string playerSteamId = stick.Player.SteamId.Value.Value;
                    Player player = stick.Player;

                    // Record shot attempts when puck leaves a player's stick (if it meets shot criteria)
                    // Only reset _lastShotWasCounted for non-goalies releasing the puck (new shot attempts)
                    // This prevents resetting the flag when goalie touches puck after a save, which would cause duplicate save counting
                    if (!__instance.IsTouchingStick && !PlayerFunc.IsGoalie(player)) {
                        _lastShotWasCounted[stick.Player.Team] = false;
                        _lastBlockWasCounted[stick.Player.Team] = false;
                        _savePercDuringGoalProcessed[stick.Player.Team] = false; // new shot cycle — allow next goal to update save%
                        
                        Puck shotPuck = PuckManager.Instance?.GetPuck();
                        if (player != null && player && shotPuck != null) {
                            Vector3 puckPos = shotPuck.transform.position;
                            Vector3 puckVel = shotPuck.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                            Vector3 shooterPos = player.PlayerBody.Rigidbody.transform.position;
                            float puckSpeed = puckVel.magnitude;
                            
                            // Determine if this is likely a shot attempt (not a pass)
                            // Criteria: sufficient velocity (>= 10 m/s) and moving towards opponent's net
                            // Note: Zone filter removed - shots can come from anywhere
                            // Blue team shoots from negative Z toward Red's goal at z=-40 (moving more negative)
                            // Red team shoots from positive Z toward Blue's goal at z=40 (moving more positive)
                            bool isBlueTeam = (player.Team == PlayerTeam.Blue);
                            bool movingTowardsNet = isBlueTeam ? (puckVel.z < 0) : (puckVel.z > 0);
                            
                            // Issue #3: Verify shot is going towards OPPONENT's net, not own net
                            // Blue team's net is at z=40, Red team's net is at z=-40
                            // Blue shoots towards z=-40 (negative), Red shoots towards z=40 (positive)
                            bool movingTowardsOpponentNet = true;
                            if (isBlueTeam) {
                                // Blue team: puck should be moving towards negative Z (towards Red's net at z=-40)
                                // If puck is already past z=-40 or moving away from it, it's going towards own net
                                movingTowardsOpponentNet = puckVel.z < 0 && puckPos.z > -40;
                            } else {
                                // Red team: puck should be moving towards positive Z (towards Blue's net at z=40)
                                // If puck is already past z=40 or moving away from it, it's going towards own net
                                movingTowardsOpponentNet = puckVel.z > 0 && puckPos.z < 40;
                            }
                            
                            // Calculate distance to net (needed for both angle check and force requirement)
                            // Net center at goal height (center of 0-2 range)
                            const float NET_CENTER_Y = 1.0f; // Fixed goal height at center of net (0-2 range, center = 1.0)
                            Vector3 netCenter = new Vector3(0f, NET_CENTER_Y, isBlueTeam ? -40f : 40f);
                            Vector3 directionToNet = netCenter - puckPos;
                            float distanceToNet = directionToNet.magnitude;
                            
                            // Angle-based check: Verify puck velocity is aligned with direction to net center
                            // This filters out cross-ice passes that are initially headed towards net but won't reach it
                            // Net centers: Blue shoots at Red's net (0, y, -40), Red shoots at Blue's net (0, y, 40)
                            // Note: Use weighted combination - Y component is factored in but given less weight than X/Z
                            bool velocityAlignedWithNet = true; // Default to true, will be set by angle check
                            if (puckSpeed > 0.1f && distanceToNet > 0.1f) { // Only check if puck has meaningful velocity and distance
                                // Weighted combination: scale down Y component to give it less weight than X/Z
                                // Lower Y_WEIGHT = less Y influence (0.5 = Y has half the weight of X/Z)
                                const float Y_WEIGHT = 0.5f; // Adjustable: 0.0 = ignore Y, 1.0 = full Y weight
                                
                                // Create weighted vectors with reduced Y influence
                                Vector3 weightedDirectionToNet = new Vector3(
                                    directionToNet.x,
                                    directionToNet.y * Y_WEIGHT,
                                    directionToNet.z
                                );
                                Vector3 weightedVelocity = new Vector3(
                                    puckVel.x,
                                    puckVel.y * Y_WEIGHT,
                                    puckVel.z
                                );
                                
                                // Normalize the weighted vectors
                                float weightedDirectionMag = weightedDirectionToNet.magnitude;
                                float weightedVelocityMag = weightedVelocity.magnitude;
                                
                                if (weightedDirectionMag > 0.001f && weightedVelocityMag > 0.001f) {
                                    Vector3 normalizedDirectionToNet = weightedDirectionToNet / weightedDirectionMag;
                                    Vector3 normalizedVelocity = weightedVelocity / weightedVelocityMag;
                                    
                                    // Calculate dot product (cosine of angle between weighted vectors)
                                    float dotProduct = Vector3.Dot(normalizedDirectionToNet, normalizedVelocity);
                                    
                                    // Use tighter angle threshold for shots beyond 50 units
                                    // Standard threshold: 18-degree threshold: cos(18?) ? 0.951
                                    // Tighter threshold for distance shots: 12-degree threshold: cos(12?) ? 0.978
                                    const float ANGLE_DISTANCE_THRESHOLD = 50.0f; // Distance threshold for tighter angle requirement
                                    const float MIN_ALIGNMENT_DOT_STANDARD = 0.951f; // cos(18?) - for shots within 50 units
                                    const float MIN_ALIGNMENT_DOT_DISTANCE = 0.978f; // cos(12?) - for shots beyond 50 units
                                    
                                    float minAlignmentDot = distanceToNet > ANGLE_DISTANCE_THRESHOLD 
                                        ? MIN_ALIGNMENT_DOT_DISTANCE 
                                        : MIN_ALIGNMENT_DOT_STANDARD;
                                    
                                    // If dot product >= threshold, angle is within acceptable range, meaning velocity is aligned with net direction
                                    velocityAlignedWithNet = dotProduct >= minAlignmentDot;
                                }
                            }
                            
                            const float MIN_SHOT_VELOCITY = 12f; // m/s - minimum velocity to be considered a shot attempt
                            const float DISTANCE_THRESHOLD = 50.0f; // Distance threshold for higher force requirement
                            const float MIN_FORCE_FOR_DISTANCE_SHOTS = 25.0f; // m/s - minimum force magnitude for shots beyond 50 units
                            
                            // Determine minimum required velocity based on distance
                            // Shots beyond 50 units require higher force (25 m/s), closer shots use standard threshold (12 m/s)
                            float minRequiredVelocity = MIN_SHOT_VELOCITY;
                            if (distanceToNet > DISTANCE_THRESHOLD) {
                                minRequiredVelocity = MIN_FORCE_FOR_DISTANCE_SHOTS;
                            }
                            
                            // Issue #4: Only count if player releases possession (no further touch events)
                            // Check if this player was the last one to touch the puck - if so, they're releasing it
                            bool playerReleasingPossession = false;
                            if (_lastPlayerOnPuckTipIncludedSteamId.TryGetValue(player.Team, out var lastTouchInfo)) {
                                // If this player was the last to touch, they're releasing possession
                                playerReleasingPossession = (lastTouchInfo.SteamId == playerSteamId);
                            }
                            
                            // Record shot attempt if: sufficient velocity AND moving towards opponent's net AND velocity aligned with net direction AND cooldown expired AND player is releasing possession
                            // (Raycast will confirm if it's actually on net, regardless of velocity)
                            const float SHOT_ATTEMPT_COOLDOWN_SECONDS = 1.0f; // Prevent duplicate shot attempts within 1 second per player
                            float currentGameTime = GetCurrentGameTime();
                            float lastShotGameTime = _lastShotAttemptGameTime.TryGetValue(playerSteamId, out float lastTime) ? lastTime : -1f;
                            float secondsSinceLastShot = currentGameTime - lastShotGameTime;
                            
                            if (puckSpeed >= minRequiredVelocity && movingTowardsNet && movingTowardsOpponentNet && velocityAlignedWithNet && playerReleasingPossession && (lastShotGameTime < 0f || secondsSinceLastShot >= SHOT_ATTEMPT_COOLDOWN_SECONDS)) {
                                // Determine shot flag (HomePlate or Outside) based on puck position
                                string shotFlag = DetermineShotFlag(puckPos, player.Team);
                                // Record shot attempt with "attempt" outcome (will be updated to "on net" or "missed" based on raycast)
                                RecordPlayByPlayEventInternal(PlayByPlayEventType.Shot, player, puckPos, puckVel, "attempt", shotFlag);
                                
                                // Track shot attempt stats for syncing
                                if (!_shotAttempts.TryGetValue(playerSteamId, out int _))
                                    _shotAttempts.Add(playerSteamId, 0);
                                _shotAttempts[playerSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.SHOT_ATTEMPTS + playerSteamId, _shotAttempts[playerSteamId].ToString());
                                
                                // Track team shot attempts
                                if (!_teamShotAttempts.TryGetValue(player.Team, out int _))
                                    _teamShotAttempts.Add(player.Team, 0);
                                _teamShotAttempts[player.Team] += 1;
                                QueueStatUpdate(Codebase.Constants.TEAM_SHOT_ATTEMPTS + player.Team.ToString(), _teamShotAttempts[player.Team].ToString());
                                
                                // Update last shot attempt game time for cooldown (per player)
                                _lastShotAttemptGameTime[playerSteamId] = currentGameTime;
                            }
                            
                            // Store release info - will be used if raycast confirms (for on-net shots)
                            // Only store if we actually recorded a shot attempt, to prevent duplicates
                            if (puckSpeed >= MIN_SHOT_VELOCITY && movingTowardsNet && movingTowardsOpponentNet && velocityAlignedWithNet && playerReleasingPossession && (lastShotGameTime < 0f || secondsSinceLastShot >= SHOT_ATTEMPT_COOLDOWN_SECONDS)) {
                                _pendingShotReleases[player.Team] = (playerSteamId, shooterPos, puckPos, puckVel, DateTime.UtcNow);
                            }
                        }
                    }

                    if (!__instance.IsTouchingStick)
                        return;

                    // Possession time is only tracked between consecutive touches, so no cleanup needed here

                    if (!_lastTimeOnCollisionStayOrExitWasCalled.TryGetValue(playerSteamId, out Stopwatch lastTimeCollisionWatch)) {
                        lastTimeCollisionWatch = new Stopwatch();
                        lastTimeCollisionWatch.Start();
                        _lastTimeOnCollisionStayOrExitWasCalled.Add(playerSteamId, lastTimeCollisionWatch);
                    }
                    lastTimeCollisionWatch.Restart();

                    _lastPlayerOnPuckTipIncludedSteamId[stick.Player.Team] = (playerSteamId, DateTime.UtcNow);
                    _lastTeamOnPuckTipIncluded = stick.Player.Team;

                    if (!PuckFunc.PuckIsTipped(playerSteamId, ModServerConfig.MaxTippedMilliseconds, _playersCurrentPuckTouch, _lastTimeOnCollisionStayOrExitWasCalled)) {
                        _lastTeamOnPuck = stick.Player.Team;
                        _lastPlayerOnPuckSteamId[stick.Player.Team] = (playerSteamId, DateTime.UtcNow);
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in Puck_OnCollisionExit_Patch Postfix().\n{ex}", ModServerConfig);
                }
            }
        }

        #region PlayerBodyV2_OnCollision
        /// <summary>
        /// Class that patches the OnCollisionEnter event from PlayerBodyV2.
        /// </summary>
        [HarmonyPatch(typeof(PlayerBody), "OnCollisionEnter")]
        public class PlayerBodyV2_OnCollisionEnter_Patch {
            [HarmonyPostfix]
            public static void Postfix(PlayerBody __instance, Collision collision) {
                // If this is not the server or game is not started, do not use the patch.
                if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Play || !_logic)
                    return;

                try {
                    if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
                        return;

                    PlayerBody collisionPlayerBody = SystemFunc.GetPlayerBody(collision.gameObject);

                    if (!collisionPlayerBody || !collisionPlayerBody.Player || !collisionPlayerBody.Player.IsCharacterSpawned)
                        return;

                    if (!__instance || !__instance.Player || !__instance.Player.IsCharacterSpawned)
                        return;

                    //float force = Utils.GetCollisionForce(collision);

                    // If the player has been hit by the same team, return;
                    if (collisionPlayerBody.Player.Team == __instance.Player.Team)
                        return;

                    string collisionPlayerBodySteamId = collisionPlayerBody.Player.SteamId.Value.Value;
                    if (!_playerIsDown.TryGetValue(collisionPlayerBodySteamId, out bool collisionPlayerBodyIsDown))
                        collisionPlayerBodyIsDown = false;

                    string instancePlayerSteamId = __instance.Player.SteamId.Value.Value;

                    if (!collisionPlayerBodyIsDown && (collisionPlayerBody.HasFallen || collisionPlayerBody.HasSlipped)) {
                        if (_playerIsDown.TryGetValue(collisionPlayerBodySteamId, out bool _))
                            _playerIsDown[collisionPlayerBodySteamId] = true;
                        else
                            _playerIsDown.Add(collisionPlayerBodySteamId, true);

                        if (__instance.Player.PlayerBody.HasFallen || __instance.Player.PlayerBody.HasSlipped) {
                            if (_playerIsDown.TryGetValue(instancePlayerSteamId, out bool _))
                                _playerIsDown[instancePlayerSteamId] = true;
                            else
                                _playerIsDown.Add(instancePlayerSteamId, true);

                            return;
                        }

                        ProcessHit(__instance.Player.SteamId.Value.Value, collisionPlayerBody.Player.SteamId.Value.Value);
                    }

                    if (__instance.Player.PlayerBody.HasFallen || __instance.Player.PlayerBody.HasSlipped) {
                        if (_playerIsDown.TryGetValue(instancePlayerSteamId, out bool _))
                            _playerIsDown[instancePlayerSteamId] = true;
                        else
                            _playerIsDown.Add(instancePlayerSteamId, true);
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(PlayerBodyV2_OnCollisionEnter_Patch)} Postfix().\n{ex}", ModServerConfig);
                }

                return;
            }
        }
        #endregion

        /// <summary>
        /// Class that patches the OnStandUp event from PlayerBodyV2.
        /// </summary>
        [HarmonyPatch(typeof(PlayerBody), nameof(PlayerBody.OnStandUp))]
        public class PlayerBodyV2_OnStandUp_Patch {
            [HarmonyPostfix]
            public static void Postfix(PlayerBody __instance) {
                // If this is not the server or game is not started, do not use the patch.
                if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Play || !_logic)
                    return;

                try {
                    string playerSteamId = __instance.Player.SteamId.Value.Value;
                    if (_playerIsDown.TryGetValue(playerSteamId, out bool _))
                        _playerIsDown[playerSteamId] = false;
                    else
                        _playerIsDown.Add(playerSteamId, false);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(PlayerBodyV2_OnStandUp_Patch)} Postfix().\n{ex}", ModServerConfig);
                }

                return;
            }
        }

        /// <summary>
        /// Event handler called when the game state changes.
        /// Replaces the Server_SetPhase Harmony patch (method removed in B310); handles
        /// phase transitions for play-by-play tracking, early-export logic, and cleanup.
        /// </summary>
        public static void Event_Everyone_OnGameStateChanged(Dictionary<string, object> message) {
            try {
                if (!ServerFunc.IsDedicatedServer() || !_logic)
                    return;

                GameState newGameState = (GameState)message["newGameState"];
                GamePhase phase = newGameState.Phase;
#if DEBUG_MODE
                if (phase != _lastRecordedPhase) {
                    DebugTrace.Section($"GAME_STATE  {_lastRecordedPhase} -> {phase}");
                    DebugTrace.Write("GAME_STATE", $"Phase changed: {_lastRecordedPhase} -> {phase}  pbpEvents={_playByPlayEvents.Count}  goals={_goals.Count}  RedScore={newGameState.RedScore}  BlueScore={newGameState.BlueScore}");
                    DebugTrace.Flush();
                }
#endif
                if (phase == GamePhase.PreGame && _lastRecordedPhase != GamePhase.PreGame) {
                    // PreGame always marks a new game (vote reset, lobby countdown, FaceOff -> PreGame, etc.).
                    // Server-side wipe only; clients sync on PreGame -> FaceOff (no broadcast here).
                    if (_lastRecordedPhase == GamePhase.Play)
                        TryExportAbortedGameStats("Game ended early.");

                    ResetAllServerStatsAndPlayByPlay();
                    _gameOverBlockEntered = false;
                    _exportedAtGameOver = false;
                    _exportedAtResetGameState = false;

                    _lastRecordedPhase = GamePhase.PreGame;
                }
                else if (phase == GamePhase.FaceOff || phase == GamePhase.Warmup || phase == GamePhase.GameOver) {
                    ResetPuckWasSavedOrBlockedChecks();

                    _puckLastCoordinate = Vector3.zero;
                    _puckZCoordinateDifference = 0;

                    // Initialize play-by-play tracking
                    if (phase == GamePhase.FaceOff) {
                        // New game = Warmup -> FaceOff, GameOver -> FaceOff, or Playing -> FaceOff (restart without replay/intermission). In-game faceoff after goal = Playing -> Replay -> FaceOff.
                        bool isNewGame = (_lastRecordedPhase == GamePhase.Warmup || _lastRecordedPhase == GamePhase.GameOver || _lastRecordedPhase == GamePhase.Play);
                        if (isNewGame) {
                            // GameOver -> FaceOff safety net: the GameOver block should have already exported
                            // (and set _exportedAtGameOver = true) before we reach here. But if it didn't —
                            // e.g. an exception fired in the GameOver handler, or the phase transitioned so
                            // fast that the GameOver event never fired independently — we attempt a recovery
                            // export here before RESET_ALL wipes the data below.
                            if (_lastRecordedPhase == GamePhase.GameOver && !_exportedAtGameOver && !_exportedAtResetGameState && _playByPlayEvents.Count > 0) {
                                int uniquePlayerCount = 0;
                                int eventCount = _playByPlayEvents.Count;
                                try {
                                    uniquePlayerCount = _playByPlayEvents.Where(e => !string.IsNullOrEmpty(e.PlayerSteamId)).Select(e => e.PlayerSteamId).Distinct().Count();
                                } catch { }
                                bool meetsCriteria = !ModServerConfig.EnableExportLimit || (uniquePlayerCount >= 8 && eventCount >= 300);
                                if (meetsCriteria) {
                                    Logging.Log($"GameOver->FaceOff safety net: GameOver block did not export — exporting now before reset", ModServerConfig);
                                    bool hasGameEndEvent = _playByPlayEvents.Any(e => e.EventType == PlayByPlayEventType.GameEnd);
                                    if (!hasGameEndEvent) {
                                        int finalPeriod = GetCurrentPeriod();
                                        float maxGameTime = _playByPlayEvents.Count > 0 ? _playByPlayEvents.Max(e => e.GameTime) : 0f;
                                        float gameEndGameTime = maxGameTime > 0f ? maxGameTime : GetCurrentGameTime();
                                        var gameEndEvent = new PlayByPlayEvent { EventId = _nextPlayByPlayEventId++, EventType = PlayByPlayEventType.GameEnd, GameTime = gameEndGameTime, Period = finalPeriod, PlayerSteamId = "", PlayerName = "", PlayerTeam = 0, PlayerPosition = "", PlayerJersey = 0, PlayerSpeed = 0f, Zone = EventZone.Neutral, Position = Vector3.zero, Velocity = Vector3.zero, ForceMagnitude = 0f, Outcome = "end", Flags = "", Team = "", TeamInPossession = _currentTeamInPossession != PlayerTeam.None ? (_currentTeamInPossession == PlayerTeam.Blue ? "Blue" : "Red") : "", CurrentPlayInPossession = _currentPlayInPossession.ToString(), ScoreState = GetScoreState(PlayerTeam.None), Timestamp = DateTime.UtcNow };
                                        CaptureTeamRosterData(gameEndEvent);
                                        _playByPlayEvents.Add(gameEndEvent);
                                    }
                                    ExportGameStats(forceExport: false);
                                    _exportedAtGameOver = true;
                                    string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                                    string sanitizedFileHeader = StripHtmlTags(ModServerConfig.FileHeaderName);
                                    string fullFileName = $"{sanitizedFileHeader}_{gameReferenceId}_stats";
                                    NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage($"Game ended. Stats exported - {fullFileName}");
                                } else {
                                    if (ModServerConfig.EnableExportLimit) {
                                        Logging.Log($"GameOver->FaceOff safety net: export skipped — {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)", ModServerConfig);
                                    }
                                }
                            }

                            // Playing -> FaceOff without PreGame (legacy path). PreGame entry normally resets first.
                            if (_lastRecordedPhase == GamePhase.Play)
                                TryExportAbortedGameStats("Game ended early.");

                            SafeSendDataToAll(RESET_ALL, "1", NetworkDelivery.ReliableSequenced);
                            ResetAllServerStatsAndPlayByPlay();
                        }
                        else if (_lastRecordedPhase == GamePhase.PreGame) {
                            // PreGame entry already wiped server stats (no broadcast). Sync clients at lobby faceoff.
                            SafeSendDataToAll(RESET_ALL, "1", NetworkDelivery.ReliableSequenced);
                        }
                        // Don't update period here - FaceOff happens after every goal, not just period transitions
                        // Period is read directly from GameState when needed

                        // Reset faceoff flag so each faceoff (including after goals) gets recorded
                        _faceoffRecordedForCurrentPeriod = false;

                        _lastRecordedPhase = phase;
                    }
                    else if (phase == GamePhase.Warmup) {
                        // Don't send RESET_ALL here - keep stats visible until next game begins (FaceOff).
                        // Fallback export: fires when the GameOver block couldn't export (e.g. criteria check threw).
                        // cameFromGameOver distinguishes a normal game end (GameOver→Warmup) from an interrupted game (FaceOff→Warmup).
                        bool cameFromGameOver = (_lastRecordedPhase == GamePhase.GameOver);
                        bool shouldTryEarlyExport = !_exportedAtResetGameState
                            && _lastRecordedPhase != GamePhase.None && _lastRecordedPhase != GamePhase.Warmup
                            && (!cameFromGameOver || !_exportedAtGameOver);
                        if (shouldTryEarlyExport) {
                            if (_playByPlayEvents.Count > 0) {
                                int uniquePlayerCount = 0;
                                int eventCount = _playByPlayEvents.Count;

                                try {
                                    var uniqueSteamIds = _playByPlayEvents
                                        .Where(e => !string.IsNullOrEmpty(e.PlayerSteamId))
                                        .Select(e => e.PlayerSteamId)
                                        .Distinct()
                                        .Count();
                                    uniquePlayerCount = uniqueSteamIds;
                                }
                                catch (Exception ex) {
                                    Logging.LogError($"Error counting unique players for early game end export: {ex}", ModServerConfig);
                                }

                                bool meetsCriteria = !ModServerConfig.EnableExportLimit || (uniquePlayerCount >= 8 && eventCount >= 300);
                                if (meetsCriteria) {
                                    if (!ModServerConfig.EnableExportLimit) {
                                        Logging.Log($"Warmup fallback export (from {_lastRecordedPhase}) - exporting stats (export limit disabled)", ModServerConfig);
                                    } else {
                                        Logging.Log($"Warmup fallback export (from {_lastRecordedPhase}) - exporting stats", ModServerConfig);
                                    }

                                    // Record GameEnd event if not already present
                                    bool hasGameEndEvent = _playByPlayEvents.Any(e => e.EventType == PlayByPlayEventType.GameEnd);
                                    if (!hasGameEndEvent) {
                                        int finalPeriod = GetCurrentPeriod();
                                        float maxGameTime = _playByPlayEvents.Count > 0 ? _playByPlayEvents.Max(e => e.GameTime) : 0f;
                                        float gameEndGameTime = maxGameTime > 0f ? maxGameTime : GetCurrentGameTime();

                                        var gameEndEvent = new PlayByPlayEvent {
                                            EventId = _nextPlayByPlayEventId++,
                                            EventType = PlayByPlayEventType.GameEnd,
                                            GameTime = gameEndGameTime,
                                            Period = finalPeriod,
                                            PlayerSteamId = "",
                                            PlayerName = "",
                                            PlayerTeam = 0,
                                            PlayerPosition = "",
                                            PlayerJersey = 0,
                                            PlayerSpeed = 0f,
                                            Zone = EventZone.Neutral,
                                            Position = Vector3.zero,
                                            Velocity = Vector3.zero,
                                            ForceMagnitude = 0f,
                                            Outcome = "end",
                                            Flags = "",
                                            Team = "",
                                            TeamInPossession = _currentTeamInPossession != PlayerTeam.None ? (_currentTeamInPossession == PlayerTeam.Blue ? "Blue" : "Red") : "",
                                            CurrentPlayInPossession = _currentPlayInPossession.ToString(),
                                            ScoreState = GetScoreState(PlayerTeam.None),
                                            Timestamp = DateTime.UtcNow
                                        };

                                        CaptureTeamRosterData(gameEndEvent);

                                        int insertIndex = _playByPlayEvents.Count;
                                        for (int i = 0; i < _playByPlayEvents.Count; i++) {
                                            if (_playByPlayEvents[i].GameTime >= gameEndGameTime) {
                                                insertIndex = i;
                                                break;
                                            }
                                        }
                                        _playByPlayEvents.Insert(insertIndex, gameEndEvent);
                                    }

                                    ExportGameStats(forceExport: false);

                                    // Announce unconditionally — no GameManager.Instance null-check here.
                                    // Previously this broadcast was wrapped in `if (GameManager.Instance != null)`,
                                    // which silently swallowed the message whenever GameManager was null at the
                                    // GameOver → Warmup transition (files were written to disk but no chat fired).
                                    // ChatManager is all that's needed for a broadcast; GameManager is irrelevant.
                                    // "Game ended." = normal end that fell through from GameOver;
                                    // "Game ended early." = interrupted game (e.g. vote-to-warmup mid-match).
                                    string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                                    string sanitizedFileHeader = StripHtmlTags(ModServerConfig.FileHeaderName);
                                    string fullFileName = $"{sanitizedFileHeader}_{gameReferenceId}_stats";
                                    string endedMsg = cameFromGameOver ? "Game ended." : "Game ended early.";
                                    NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage($"{endedMsg} Stats exported - {fullFileName}");
                                } else {
                                    if (ModServerConfig.EnableExportLimit) {
                                        Logging.Log($"Warmup fallback export (from {_lastRecordedPhase}) - Export skipped: {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)", ModServerConfig);
                                    }
                                }
                            }
                        }

                        _playByPlayEvents.Clear();
                        _nextPlayByPlayEventId = 0;
                        _lastProcessedZoneFlagEventId = -1; // Reset zone flag scanner
                        _currentTeamInPossession = PlayerTeam.None;
                        _currentPlayInPossession = 0;
                        _gameStartTime = 0f; // Reset so it initializes on next game start
                        _currentPeriod = 1;
                        _lastTrackedPeriod = 0; // Reset period tracking
                        _lastRecordedPhase = phase;

                        // Reset game time tracking for fractional precision
                        _lastWholeSecondGameTime = 0f;
                        _lastUnityTimeForGameTime = 0f;
                        _lastCountdownValue = -1;

                        // Clear both GameOver export flags so the next game starts clean.
                        // _gameOverBlockEntered resets so the GameOver block can run again;
                        // _exportedAtGameOver resets so the Warmup fallback will attempt export
                        // if (and only if) the GameOver block doesn't set it to true first.
                        _gameOverBlockEntered = false;
                        _exportedAtGameOver   = false;
                        _exportedAtResetGameState = false;

                        // Clear pending shot releases
                        foreach (PlayerTeam team in new List<PlayerTeam>(_pendingShotReleases.Keys)) {
                            _pendingShotReleases[team] = ("", Vector3.zero, Vector3.zero, Vector3.zero, DateTime.MinValue);
                        }

                        // Reset raycast state tracking
                        foreach (PlayerTeam team in new List<PlayerTeam>(_previousRaycastState.Keys)) {
                            _previousRaycastState[team] = false;
                        }

                        // Reset raycast frame counters
                        foreach (PlayerTeam team in new List<PlayerTeam>(_raycastTrueFrames.Keys)) {
                            _raycastTrueFrames[team] = 0;
                        }

                        // Reset shot recorded flags
                        foreach (PlayerTeam team in new List<PlayerTeam>(_shotRecordedForRaycast.Keys)) {
                            _shotRecordedForRaycast[team] = false;
                        }
                    }
                    else if (phase == GamePhase.Play) {
                        // Ensure tracking is initialized even if we missed FaceOff
                        if (_gameStartTime == 0f) {
                            _gameStartTime = Time.time;
                            if (string.IsNullOrEmpty(_currentGameReferenceId)) {
                                _currentGameReferenceId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                            }
                        }

                        // Detect period transition and resolve pending faceoff
                        int currentPeriod = GetCurrentPeriod();

                        // Initialize _lastTrackedPeriod if it's 0 (first time in Playing phase)
                        if (_lastTrackedPeriod == 0 && currentPeriod > 0) {
                            _lastTrackedPeriod = currentPeriod;
                        }

                        if (currentPeriod > 0 && currentPeriod != _lastTrackedPeriod && _lastTrackedPeriod > 0) {
                            // Period has changed - resolve any pending faceoff
                            ResolvePendingFaceoffOnTrackingStop();
                            Logging.Log($"Period transition detected: {_lastTrackedPeriod} -> {currentPeriod}. Resolved pending faceoff.", ModServerConfig);
                        }
                        _lastTrackedPeriod = currentPeriod;

                        // If we somehow missed recording faceoff in FaceOff phase, record it here as fallback
                        if (!_faceoffRecordedForCurrentPeriod && (_lastRecordedPhase == GamePhase.FaceOff || _lastRecordedPhase == GamePhase.None || _lastRecordedPhase == GamePhase.Warmup)) {
                            // Resolve any pending faceoff before starting a new one
                            ResolvePendingFaceoffOnTrackingStop();

                            RecordFaceoffEvent();
                            _faceoffRecordedForCurrentPeriod = true;

                            // Start tracking faceoff outcome
                            _trackingFaceoffOutcome = true;
                            _faceoffPossessionChainCount = 0;
                            _faceoffPossessionTeam = PlayerTeam.None;
                            _faceoffTotalIncremented = false; // Reset flag for new faceoff
                        }

                        _lastRecordedPhase = phase;
                    }
                    else {
                        _lastRecordedPhase = phase;
                    }

                    // Reset player on puck.
                    foreach (PlayerTeam key in new List<PlayerTeam>(_lastPlayerOnPuckTipIncludedSteamId.Keys))
                        _lastPlayerOnPuckTipIncludedSteamId[key] = ("", DateTime.MinValue);

                    foreach (PlayerTeam key in new List<PlayerTeam>(_lastPlayerOnPuckSteamId.Keys))
                        _lastPlayerOnPuckSteamId[key] = ("", DateTime.MinValue);

                    // Reset shot counted states.
                    foreach (PlayerTeam key in new List<PlayerTeam>(_lastShotWasCounted.Keys))
                        _lastShotWasCounted[key] = true;

                    // Reset block counted states.
                    foreach (PlayerTeam key in new List<PlayerTeam>(_lastBlockWasCounted.Keys))
                        _lastBlockWasCounted[key] = true;

                    // Reset goal save-perc guard — each faceoff (including after goals) starts a fresh cycle.
                    foreach (PlayerTeam key in new List<PlayerTeam>(_savePercDuringGoalProcessed.Keys))
                        _savePercDuringGoalProcessed[key] = false;

                    // Reset possession times.
                    foreach (Stopwatch watch in _playersLastTimePuckPossession.Values)
                        watch.Stop();
                    _playersLastTimePuckPossession.Clear();

                    // Reset last possession to prevent false turnovers/takeaways at period start
                    _lastPossession = new Possession();

                    // Reset possession chain tracking to prevent false turnovers/takeaways at period start
                    _currentTeamInPossession = PlayerTeam.None;
                    _currentPlayInPossession = 0;

                    // Reset puck collision stay or exit times.
                    foreach (Stopwatch watch in _lastTimeOnCollisionStayOrExitWasCalled.Values)
                        watch.Stop();
                    _lastTimeOnCollisionStayOrExitWasCalled.Clear();

                    // Reset tipped times.
                    foreach (Stopwatch watch in _playersCurrentPuckTouch.Values)
                        watch.Stop();
                    _playersCurrentPuckTouch.Clear();

                    if (phase == GamePhase.GameOver) {
                        // _gameOverBlockEntered prevents this block from running more than once per game.
                        // The game-state event fires on every engine tick while the GameOver/podium screen
                        // is displayed, so without this guard the export and broadcast would repeat every tick.
                        // We set the flag immediately (before any work below) so that even if an exception
                        // is thrown partway through, repeated ticks are still blocked.
                        if (_gameOverBlockEntered) {
                            // Already handled for this game — nothing to do.
                        } else {
                            _gameOverBlockEntered = true;

                        // Record GameEnd event when game concludes
                        // Check if we already have a GameEnd event
                        bool hasGameEndEvent = _playByPlayEvents.Any(e =>
                            e.EventType == PlayByPlayEventType.GameEnd);

                        if (!hasGameEndEvent) {
                            int finalPeriod = GetCurrentPeriod();

                            // If game ended in regulation (3 periods), use exactly 900.0
                            // Check both finalPeriod == 3 and if max gameTime is close to 900 (within 5 seconds)
                            // This handles cases where period might be 4+ but game actually ended in regulation
                            float maxGameTime = _playByPlayEvents.Count > 0 ? _playByPlayEvents.Max(e => e.GameTime) : 0f;
                            float gameEndGameTime;
                            if (finalPeriod == 3 || (maxGameTime >= 895f && maxGameTime <= 905f)) {
                                // Game ended in regulation - use exactly 900.0 seconds
                                gameEndGameTime = 900.0f;
                            }
                            else {
                                // Game went to overtime or ended early - use actual max gameTime
                                gameEndGameTime = maxGameTime > 0f ? maxGameTime : GetCurrentGameTime();
                            }

                            var gameEndEvent = new PlayByPlayEvent {
                                EventId = _nextPlayByPlayEventId++,
                                EventType = PlayByPlayEventType.GameEnd,
                                GameTime = gameEndGameTime,
                                Period = finalPeriod,
                                PlayerSteamId = "",
                                PlayerName = "",
                                PlayerTeam = 0,
                                PlayerPosition = "",
                                PlayerJersey = 0,
                                PlayerSpeed = 0f,
                                Zone = EventZone.Neutral,
                                Position = Vector3.zero,
                                Velocity = Vector3.zero,
                                ForceMagnitude = 0f,
                                Outcome = "end",
                                Flags = "",
                                Team = "", // Game end has no team
                                TeamInPossession = _currentTeamInPossession != PlayerTeam.None ? (_currentTeamInPossession == PlayerTeam.Blue ? "Blue" : "Red") : "",
                                CurrentPlayInPossession = _currentPlayInPossession.ToString(),
                                ScoreState = GetScoreState(PlayerTeam.None),
                                Timestamp = DateTime.UtcNow
                            };

                            // Capture roster data from current game state if available
                            CaptureTeamRosterData(gameEndEvent);

                            // Insert the game end event at the correct chronological position
                            int insertIndex = _playByPlayEvents.Count;
                            for (int i = 0; i < _playByPlayEvents.Count; i++) {
                                if (_playByPlayEvents[i].GameTime >= gameEndGameTime) {
                                    insertIndex = i;
                                    break;
                                }
                            }
                            _playByPlayEvents.Insert(insertIndex, gameEndEvent);
                            Logging.Log($"Recorded GameEnd event at gameTime {gameEndGameTime:F3} (period {finalPeriod}).", ModServerConfig);
                        }

                        string gwgSteamId = "";
                        PlayerTeam winningTeam = PlayerTeam.None;
                        try {
                            // Derive score from our own goal log — newGameState scores are already
                            // zeroed out by the time the GameOver phase event fires.
                            int blueScore = _goals.Count(g => g.Team == "Blue");
                            int redScore  = _goals.Count(g => g.Team == "Red");

                            if (blueScore > redScore) {
                                winningTeam = PlayerTeam.Blue;
                                // Find the goal where Blue first took the lead
                                int blueGoalsScored = 0;
                                int redGoalsScored = 0;
                                foreach (GoalInfo goal in _goals.OrderBy(g => g.GameTime)) {
                                    if (goal.Team == "Blue") {
                                        blueGoalsScored++;
                                    } else {
                                        redGoalsScored++;
                                    }

                                    if (goal.Team == "Blue" && blueGoalsScored > redGoalsScored && blueGoalsScored == redScore + 1) {
                                        gwgSteamId = goal.Scorer;
                                        break;
                                    }
                                }
                            }
                            else if (redScore > blueScore) {
                                winningTeam = PlayerTeam.Red;
                                // Find the goal where Red first took the lead
                                int blueGoalsScored = 0;
                                int redGoalsScored = 0;
                                foreach (GoalInfo goal in _goals.OrderBy(g => g.GameTime)) {
                                    if (goal.Team == "Blue") {
                                        blueGoalsScored++;
                                    } else {
                                        redGoalsScored++;
                                    }

                                    if (goal.Team == "Red" && redGoalsScored > blueGoalsScored && redGoalsScored == blueScore + 1) {
                                        gwgSteamId = goal.Scorer;
                                        break;
                                    }
                                }
                            }

                            LogGWG(gwgSteamId);
                        }
                        catch { } // Shootout goal or something, so no GWG.

                        Dictionary<string, double> starPoints = new Dictionary<string, double>();
                        foreach (Player player in PlayerManager.Instance.GetPlayers()) {
                            if (player == null || !player)
                                continue;

                            string steamId = player.SteamId.Value.Value;
                            starPoints.Add(steamId, 0);

                            double gwgModifier = gwgSteamId == player.SteamId.Value.Value ? 0.5d : 0;
                            double teamModifier = winningTeam == player.Team ? 1.1d : 1d;

                            if (PlayerFunc.IsGoalie(player)) {
                                // Simplified goalie point system
                                const double GOAL_ALLOWED_PENALTY = -10d;
                                const double SHOT_FACED_POINTS = 10d;
                                const double GOALIE_GOAL_MODIFIER = 175d;
                                const double GOALIE_ASSIST_MODIFIER = 30d;
                                const double SHUTOUT_BONUS = 100d;

                                if (_savePerc.TryGetValue(steamId, out var saveValues)) {
                                    // Goals allowed: -10 points each
                                    int goalsAllowed = saveValues.Shots - saveValues.Saves;
                                    starPoints[steamId] += ((double)goalsAllowed) * GOAL_ALLOWED_PENALTY;

                                    // Shots faced: 10 points each
                                    starPoints[steamId] += ((double)saveValues.Shots) * SHOT_FACED_POINTS;

                                    // Shutout bonus: 100 points if goalie allowed 0 goals and faced at least 1 shot
                                    if (goalsAllowed == 0 && saveValues.Shots > 0) {
                                        starPoints[steamId] += SHUTOUT_BONUS;
                                    }
                                }

                                if (_passes.TryGetValue(steamId, out int passes))
                                    starPoints[steamId] += ((double)passes) * 2.5d;

                                starPoints[steamId] += GOALIE_GOAL_MODIFIER * gwgModifier;
                                starPoints[steamId] += ((double)player.Goals.Value) * GOALIE_GOAL_MODIFIER;
                                starPoints[steamId] += ((double)player.Assists.Value) * GOALIE_ASSIST_MODIFIER;
                            }
                            else {
                                if (_sog.TryGetValue(steamId, out int shots)) {
                                    starPoints[steamId] += ((double)shots) * 7.5d;
                                }

                                if (_passes.TryGetValue(steamId, out int passes))
                                    starPoints[steamId] += ((double)passes) * 2.5d;

                                if (_blocks.TryGetValue(steamId, out int blocks))
                                    starPoints[steamId] += ((double)blocks) * 5d;

                                const double SKATER_GOAL_MODIFIER = 70d;
                                const double SKATER_ASSIST_MODIFIER = 30d;

                                starPoints[steamId] += SKATER_GOAL_MODIFIER * gwgModifier;
                                starPoints[steamId] += ((double)player.Goals.Value) * SKATER_GOAL_MODIFIER;
                                starPoints[steamId] += ((double)player.Assists.Value) * SKATER_ASSIST_MODIFIER;
                            }

                            // Updated skater stat multipliers
                            if (_hits.TryGetValue(steamId, out int hits))
                                starPoints[steamId] += ((double)hits) * 2.5d;

                            if (_takeaways.TryGetValue(steamId, out int takeaways))
                                starPoints[steamId] += ((double)takeaways) * 5d;

                            if (_turnovers.TryGetValue(steamId, out int turnovers))
                                starPoints[steamId] -= ((double)turnovers) * 5d;

                            // DZ Exits and OZ Entries: 1 point each
                            if (_exits.TryGetValue(steamId, out int exits))
                                starPoints[steamId] += ((double)exits) * 1d;

                            if (_entries.TryGetValue(steamId, out int entries))
                                starPoints[steamId] += ((double)entries) * 1d;

                            starPoints[steamId] *= teamModifier;
                        }

                        starPoints = starPoints.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                        if (starPoints.Count >= 1)
                            _stars[1] = starPoints.ElementAt(0).Key;
                        else
                            _stars[1] = "";

                        if (starPoints.Count >= 2)
                            _stars[2] = starPoints.ElementAt(1).Key;
                        else
                            _stars[2] = "";

                        if (starPoints.Count >= 3)
                            _stars[3] = starPoints.ElementAt(2).Key;
                        else
                            _stars[3] = "";

                        // Build a single stars line: "🥇 Name1, 🥈 Name2, 🥉 Name3"
                        var starParts = new List<string>();
                        foreach (KeyValuePair<int, string> star in _stars.OrderBy(x => x.Key)) {
                            if (!string.IsNullOrEmpty(star.Value)) {
                                Player player = PlayerManager.Instance.GetPlayerBySteamId(star.Value);
                                if (player != null && player) {
                                    starParts.Add($"{GetStarMedalGlyph(star.Key)} {player.Username.Value}");
                                }
                                SafeSendDataToAll(STAR, $"{star.Value};{star.Key}");
                                LogStar(star.Value, star.Key);
                            }
                        }
                        if (starParts.Count > 0)
                            NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage(string.Join(", ", starParts));

                        // Mark phase immediately so repeated GameOver ticks are ignored even if the export block throws.
                        _lastRecordedPhase = phase;

                        // Check play-by-play data before creating JSON - only export if game has sufficient players and events.
                        // Use a snapshot list to avoid InvalidOperationException if the collection is modified concurrently.
                        int uniquePlayerCount = 0;
                        List<PlayByPlayEvent> pbpSnapshot = null;
                        try { pbpSnapshot = _playByPlayEvents.ToList(); } catch { pbpSnapshot = new List<PlayByPlayEvent>(_playByPlayEvents); }
                        int eventCount = pbpSnapshot.Count;

                        try {
                            uniquePlayerCount = pbpSnapshot
                                .Where(e => !string.IsNullOrEmpty(e.PlayerSteamId))
                                .Select(e => e.PlayerSteamId)
                                .Distinct()
                                .Count();
                        }
                        catch (Exception ex) {
                            Logging.LogError($"Error counting unique players for export: {ex}", ModServerConfig);
                        }

                        // Only create and export JSON and CSV if pbp check passes (8+ players and 300+ events) or if limit is disabled
                        bool meetsCriteria = !ModServerConfig.EnableExportLimit || (uniquePlayerCount >= 8 && eventCount >= 300);
#if DEBUG_MODE
                        DebugTrace.Write("GAME_STATE", $"GameOver export criteria: uniquePlayers={uniquePlayerCount} events={eventCount} meetsCriteria={meetsCriteria} EnableExportLimit={ModServerConfig.EnableExportLimit}");
                        DebugTrace.Flush();
#endif
                        if (meetsCriteria) {
                            ExportGameStats();

                            // Set _exportedAtGameOver only after the export actually ran.
                            // The Warmup fallback path checks this flag — if true it skips its own
                            // export so we don't write duplicate files. If the export threw above,
                            // this line is never reached, _exportedAtGameOver stays false, and
                            // Warmup will still attempt a recovery export.
                            _exportedAtGameOver = true;

                            string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                            string sanitizedFileHeader = StripHtmlTags(ModServerConfig.FileHeaderName);
                            string fullFileName = $"{sanitizedFileHeader}_{gameReferenceId}_stats";
                            NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage($"Game ended. Stats exported - {fullFileName}");
                        }
                        else {
                            if (ModServerConfig.EnableExportLimit) {
                                Logging.Log($"Export skipped (JSON and CSV): {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)", ModServerConfig);
                            }
                        }

                        } // end else (_gameOverBlockEntered guard)
                    }
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in Event_Everyone_OnGameStateChanged.\n{ex}", ModServerConfig);
            }
        }
        /// <summary>
        /// Class that patches WrapPlayerUsername event from UIChat.
        /// </summary>
        // WrapPlayerUsername may have been renamed in B323; using string literal ? verify at runtime
        [HarmonyPatch(typeof(UIChat), "GetChatMessagePrefix")]
        public static class UIChat_GetChatMessagePrefix_Patch {
            public static void Postfix(ChatMessage chatMessage, ref string __result) {
                if (!chatMessage.SteamID.HasValue)
                    return;

                string steamId = chatMessage.SteamID.Value.Value;
                if (string.IsNullOrEmpty(steamId) || !_stars.Values.Contains(steamId))
                    return;

                __result = GetStarTagForChat(steamId) + __result;
            }
        }

        /// <summary>
        /// Patches Application.OpenURL so that Workshop/steamcommunity URLs open in the Steam overlay browser instead of the system browser.
        /// </summary>
        [HarmonyPatch(typeof(Application), nameof(Application.OpenURL))]
        public static class Application_OpenURL_Patch {
            public static bool Prefix(string url) {
                if (string.IsNullOrEmpty(url) || !url.Contains("steamcommunity.com"))
                    return true;

                try {
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                        Type steamFriends = asm.GetType("Steamworks.SteamFriends");
                        if (steamFriends == null)
                            continue;

                        MethodInfo method = steamFriends.GetMethod("ActivateGameOverlayToWebPage", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                        if (method != null) {
                            method.Invoke(null, new object[] { url });
                            return false;
                        }

                        method = steamFriends.GetMethod("OpenWebOverlay", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(bool) }, null);
                        if (method != null) {
                            method.Invoke(null, new object[] { url, false });
                            return false;
                        }
                    }
                }
                catch { }

                return true;
            }
        }

        /// <summary>
        /// Method that launches when the mod is being enabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully enabled.</returns>
        public bool OnEnable() {
            try {
                if (_harmonyPatched)
                    return true;

                bool isServer = ServerFunc.IsDedicatedServer();
#if DEBUG_MODE
                DebugTrace.Init(isServer);
                DebugTrace.Section("LIFECYCLE");
                DebugTrace.Write("LIFECYCLE", $"OnEnable called. IsDedicatedServer={isServer} Version={MOD_VERSION}");
#endif
#if PUCK_API_DUMP
                if (isServer)
                    DumpGameAPI();
#endif
                Logging.Log($"Enabling...", ModServerConfig, true);

                // ── Pre-patch reflection validation ──────────────────────────────────
                // Verify every patched method/property exists BEFORE Harmony touches it.
                // This produces a clear "method not found" message instead of a cryptic Harmony error.
                var missingTargets = new List<string>();
                void CheckMethod(Type t, string methodName) {
                    if (t == null) { missingTargets.Add($"(null type).{methodName}()"); return; }
                    var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                    if (t.GetMethod(methodName, flags) == null)
                        missingTargets.Add($"{t.FullName}.{methodName}()");
                }
                void CheckProp(Type t, string propName) {
                    if (t == null) { missingTargets.Add($"(null type).{propName}"); return; }
                    var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                    if (t.GetProperty(propName, flags) == null)
                        missingTargets.Add($"{t.FullName}.{propName} (property)");
                }
                // Puck collision callbacks
                CheckMethod(typeof(Puck), "OnCollisionEnter");
                CheckMethod(typeof(Puck), "OnCollisionStay");
                CheckMethod(typeof(Puck), "OnCollisionExit");
                // Properties used at runtime inside those patches
                CheckProp(typeof(Puck), "IsTouchingStick");
                // PlayerBody collision
                CheckMethod(typeof(PlayerBody), "OnCollisionEnter");

                if (missingTargets.Count > 0) {
                    string missingReport = string.Join("\n", missingTargets.Select(m => "  MISSING: " + m));
                    Logging.LogError($"Pre-patch validation failed — {missingTargets.Count} target(s) not found in current game build:\n{missingReport}", ModServerConfig);
#if DEBUG_MODE
                    DebugTrace.Section("HARMONY");
                    foreach (var m in missingTargets)
                        DebugTrace.Write("HARMONY", $"PRE-PATCH MISSING: {m}");
                    DebugTrace.Flush();
#endif
                }
                else {
                    Logging.Log("Pre-patch validation OK — all target methods/properties found.", ModServerConfig, true);
#if DEBUG_MODE
                    DebugTrace.Section("HARMONY");
                    DebugTrace.Write("HARMONY", "Pre-patch validation OK");
#endif
                }

                // ── Harmony patching ────────────────────────────────────────────────
                _harmonyPatchTargets.Clear();
                var patchErrors = new List<string>();
                var patchSuccess = new List<string>();
                foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
                    try {
                        var patchMethods = _harmony.CreateClassProcessor(type).Patch();
                        if (patchMethods != null && patchMethods.Count > 0) {
                            patchSuccess.Add(type.Name);
                            _harmonyPatchTargets.AddRange(patchMethods);
                        }
                    }
                    catch (Exception patchEx) {
                        string inner = patchEx.InnerException != null ? $" → {patchEx.InnerException.Message}" : "";
                        patchErrors.Add($"  {type.Name}: {patchEx.Message}{inner}");
                    }
                }

                string patchSummary = $"Harmony: {patchSuccess.Count} OK, {patchErrors.Count} FAILED";
                Logging.Log(patchSummary, ModServerConfig, true);
                foreach (var ok in patchSuccess)
                    Logging.Log($"  PATCHED: {ok}", ModServerConfig, true);
                if (patchErrors.Count > 0)
                    Logging.LogError($"Harmony patch failures ({patchErrors.Count}):\n{string.Join("\n", patchErrors)}", ModServerConfig);

#if DEBUG_MODE
                DebugTrace.Write("HARMONY", patchSummary);
                foreach (var ok in patchSuccess)
                    DebugTrace.Write("HARMONY", $"  OK: {ok}");
                foreach (var err in patchErrors)
                    DebugTrace.Write("HARMONY", $"  FAIL: {err}");
                DebugTrace.Flush();
#endif
                // Don't hard-fail on patch errors — log them and continue so partial functionality works.
                // Critical patches (collision/saves) failing will surface naturally through missing stats.

                Logging.Log($"Enabled.", ModServerConfig, true);

                Logging.Log("Stats mod loaded", _clientConfig);

#if DEBUG_MODE
                DebugTrace.Write("LIFECYCLE", "Registering network handlers...");
#endif
                NetworkCommunication.AddToNotLogList(DATA_NAMES_TO_IGNORE);

                if (ServerFunc.IsDedicatedServer()) {
#if DEBUG_MODE
                    DebugTrace.Write("LIFECYCLE", "Server path: registering named message handlers...");
#endif
                    Server_RegisterNamedMessageHandler();

                    Logging.Log("Setting server sided config.", ModServerConfig, true);
#if DEBUG_MODE
                    DebugTrace.Write("LIFECYCLE", "Reading server config...");
#endif
                    ModServerConfig = ModServerConfig.ReadConfig();
                    
                    // Derive FileHeaderName from the server name, unless admin has opted into a custom name.
                    // Format: "[Bracket Tag] | League Name | City"  →  second pipe-segment used (e.g. "PHL_Official_#")
                    try {
                        if (ServerManager.Instance != null && !ModServerConfig.UseCustomFileHeaderName) {
                            string serverName = ServerManager.Instance.Server.Value.Name.Value;
                            if (!string.IsNullOrEmpty(serverName)) {
                                string[] segments = serverName.Split('|');
                                if (segments.Length >= 2) {
                                    // Use the second segment (index 1) — the league/event name portion
                                    serverName = segments[1].Trim();
                                }
                                else {
                                    serverName = serverName.Trim();
                                }
                                // Sanitize for filename use
                                serverName = StripHtmlTags(serverName);
                                serverName = serverName.Replace("[", "").Replace("]", "");
                                serverName = serverName.Replace("(", "").Replace(")", "");
                                serverName = serverName.Replace(" ", "_");
                                char[] invalidChars = Path.GetInvalidFileNameChars();
                                foreach (char c in invalidChars) {
                                    serverName = serverName.Replace(c.ToString(), "");
                                }
                                while (serverName.Contains("__"))
                                    serverName = serverName.Replace("__", "_");
                                serverName = serverName.Trim('_');
                                if (string.IsNullOrEmpty(serverName)) {
                                    serverName = "puck";
                                }
                                ModServerConfig.FileHeaderName = serverName;
                                Logging.Log($"FileHeaderName set from server name: {ModServerConfig.FileHeaderName}", ModServerConfig, true);
                                
                                // Write updated config back to file
                                try {
                                    string rootPath = Path.GetFullPath(".");
                                    string configPath = Path.Combine(rootPath, Constants.MOD_NAME + "_serverconfig.json");
                                    File.WriteAllText(configPath, ModServerConfig.ToString());
                                    Logging.Log($"Updated server config file with FileHeaderName: {ModServerConfig.FileHeaderName}", ModServerConfig, true);
                                }
                                catch (Exception writeEx) {
                                    Logging.LogError($"Can't write the server config file after updating FileHeaderName. (Permission error ?)\n{writeEx}", ModServerConfig);
                                }
                            }
                        }
                    }
                    catch (Exception ex) {
                        Logging.LogError($"Error setting FileHeaderName from server name: {ex}", ModServerConfig);
                    }
                }
                else {
                    Logging.Log("Setting client sided config.", ModServerConfig, true);
#if DEBUG_MODE
                    DebugTrace.Write("LIFECYCLE", "Client path: reading client config...");
#endif
                    _clientConfig = ClientConfig.ReadConfig();
#if DEBUG_MODE
                    DebugTrace.Write("LIFECYCLE", "Client config loaded.");
#endif
                }

                Logging.Log("Subscribing to events.", ModServerConfig, true);
#if DEBUG_MODE
                DebugTrace.Write("LIFECYCLE", "Subscribing to EventManager events...");
#endif

                if (ServerFunc.IsDedicatedServer()) {
                    SubscribeEvent("Event_Everyone_OnClientConnected",       Event_OnClientConnected);
                    SubscribeEvent("Event_Everyone_OnClientDisconnected",    Event_OnClientDisconnected);
                    SubscribeEvent("Event_Everyone_OnPlayerGameStateChanged",Event_OnPlayerRoleChanged);
                    SubscribeEvent("Event_Everyone_OnGameStateChanged",      Event_Everyone_OnGameStateChanged);
                    SubscribeEvent(Codebase.Constants.STATS_MOD_NAME,       Event_OnStatsTrigger);
                    SubscribeEvent(Codebase.Constants.RULESET_MOD_NAME,     Event_OnRulesetTrigger);
                }
                else {
                    SubscribeEvent("Event_OnClientStopped", Event_Client_OnClientStopped);
                }

                _harmonyPatched = true;
                _logic = true;
#if DEBUG_MODE
                DebugTrace.Write("LIFECYCLE", $"OnEnable complete. IsDedicatedServer={isServer} Version={MOD_VERSION}");
                DebugTrace.Flush();
#endif
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to enable.\n{ex}", ModServerConfig);
#if DEBUG_MODE
                DebugTrace.Write("LIFECYCLE", $"OnEnable EXCEPTION: {ex}");
                DebugTrace.Flush();
#endif
                return false;
            }
        }

        /// <summary>
        /// Wraps EventManager.AddEventListener with trace logging so any broken/renamed
        /// event names surface immediately in the debug log instead of silently doing nothing.
        /// </summary>
        private static void SubscribeEvent(string eventName, Action<Dictionary<string, object>> handler) {
            try {
                EventManager.AddEventListener(eventName, handler);
                Logging.Log($"  Subscribed: {eventName}", ModServerConfig, true);
#if DEBUG_MODE
                DebugTrace.Write("LIFECYCLE", $"  Event OK: {eventName}");
#endif
            }
            catch (Exception ex) {
                Logging.LogError($"  Failed to subscribe to event '{eventName}': {ex.Message}", ModServerConfig);
#if DEBUG_MODE
                DebugTrace.Write("LIFECYCLE", $"  Event FAIL: {eventName} — {ex.Message}");
                DebugTrace.Flush();
#endif
            }
        }

        /// <summary>
        /// Method that launches when the mod is being disabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully disabled.</returns>
        public bool OnDisable() {
            try {
                if (!_harmonyPatched)
                    return true;

                Logging.Log($"Disabling...", ModServerConfig, true);

                Logging.Log("Unsubscribing from events.", ModServerConfig, true);
                NetworkCommunication.RemoveFromNotLogList(DATA_NAMES_TO_IGNORE);
                if (ServerFunc.IsDedicatedServer()) {
                    EventManager.RemoveEventListener("Event_Everyone_OnClientConnected", Event_OnClientConnected);
                    EventManager.RemoveEventListener("Event_Everyone_OnClientDisconnected", Event_OnClientDisconnected);
                    EventManager.RemoveEventListener("Event_Everyone_OnPlayerGameStateChanged", Event_OnPlayerRoleChanged);
                    EventManager.RemoveEventListener("Event_Everyone_OnGameStateChanged", Event_Everyone_OnGameStateChanged);
                    EventManager.RemoveEventListener(Codebase.Constants.STATS_MOD_NAME, Event_OnStatsTrigger);
                    EventManager.RemoveEventListener(Codebase.Constants.RULESET_MOD_NAME, Event_OnRulesetTrigger);
                    NetworkManager.Singleton?.CustomMessagingManager?.UnregisterNamedMessageHandler(Constants.FROM_CLIENT_TO_SERVER);
                }
                else {
                    EventManager.RemoveEventListener("Event_OnClientStopped", Event_Client_OnClientStopped);
                    Event_Client_OnClientStopped(new Dictionary<string, object>());
                    NetworkManager.Singleton?.CustomMessagingManager?.UnregisterNamedMessageHandler(Constants.FROM_SERVER_TO_CLIENT);
                }

                _hasRegisteredWithNamedMessageHandler = false;
                _rulesetModEnabled = null;
                _serverHasResponded = false;
                _askServerForStartupDataCount = 0;

                ScoreboardModifications(false);

                UnpatchHarmonySafely();

                Logging.Log($"Disabled.", ModServerConfig, true);
#if DEBUG_MODE
                DebugTrace.Section("LIFECYCLE");
                DebugTrace.Write("LIFECYCLE", "OnDisable called ? final flush.");
                DebugTrace.Flush();
#endif
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to disable.\n{ex}", ModServerConfig);
                return true;
            }
            finally {
                _harmonyPatched = false;
                _logic = true;
            }
        }

        /// <summary>
        /// Removes Harmony patches. UnpatchSelf can throw on open generic targets (e.g. BaseGameMode`1);
        /// fall back to per-type unpatch so leaving a server always clears client patches.
        /// </summary>
        private static void UnpatchHarmonySafely() {
            try {
                _harmony.UnpatchSelf();
                return;
            }
            catch (Exception ex) {
                Logging.LogError($"Harmony UnpatchSelf failed, falling back to per-type unpatch: {ex.Message}", ModServerConfig);
            }

            foreach (var target in _harmonyPatchTargets.ToList()) {
                try {
                    _harmony.Unpatch(target, HarmonyPatchType.All, Constants.MOD_NAME);
                }
                catch (Exception ex) {
                    Logging.LogError($"Failed to unpatch {target?.DeclaringType?.Name}.{target?.Name}: {ex.Message}", ModServerConfig);
                }
            }
            _harmonyPatchTargets.Clear();
        }
        #endregion

        #region Events
        public static void Event_OnStatsTrigger(Dictionary<string, object> message) {
            try {
                foreach (KeyValuePair<string, object> kvp in message) {
                    string value = (string)kvp.Value;

                    switch (kvp.Key) {
                        case Codebase.Constants.SOG:
                            // Goalie save% on goals is updated by GameManager_Server_GoalScored_Patch only.
                            // Do not schedule SendSavePercDuringGoal here — the game fires SOG on saves
                            // too, which produced phantom shots faced (SF > saves + GA).
                            break;

                        case Codebase.Constants.LOGIC:
                            _logic = bool.Parse(value);
                            break;
                    }
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnStatsTrigger)}.\n{ex}", ModServerConfig);
            }
        }

        public static void Event_OnRulesetTrigger(Dictionary<string, object> message) {
            try {
                foreach (KeyValuePair<string, object> kvp in message) {
                    string value = (string)kvp.Value;

                    switch (kvp.Key) {
                        case Codebase.Constants.PAUSE:
                            _paused = bool.Parse(value);
                            break;
                    }
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnRulesetTrigger)}.\n{ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Method called when a client has connected (joined a server) on the server-side.
        /// Used to set server-sided stuff after the game has loaded.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_OnClientConnected(Dictionary<string, object> message) {
            if (!ServerFunc.IsDedicatedServer())
                return;

            //Logging.Log("Event_Everyone_OnClientConnected", ModServerConfig);

            try {
                Server_RegisterNamedMessageHandler();

                ulong clientId = (ulong)message["clientId"];
                Player connectedPlayer = PlayerManager.Instance.GetPlayerByClientId(clientId);
                string clientSteamId = connectedPlayer?.SteamId.Value.Value ?? "";
                try {
                    _players_ClientId_SteamId.Add(clientId, "");
                }
                catch {
                    _players_ClientId_SteamId.Remove(clientId);
                    _players_ClientId_SteamId.Add(clientId, "");
                }

                // Send the required client version so the client can compare and notify the user if outdated
                NetworkCommunication.SendData(Constants.MOD_NAME + "_" + nameof(MOD_VERSION), COMPATIBLE_CLIENT_VERSION, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);

                CheckForRulesetMod();
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnClientConnected)}.\n{ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Method called when a client has disconnect (left a server) on the server-side.
        /// Used to unset data linked to the player like rule status.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_OnClientDisconnected(Dictionary<string, object> message) {
            if (!ServerFunc.IsDedicatedServer())
                return;

            //Logging.Log("Event_Everyone_OnClientDisconnected", ModServerConfig);

            try {
                ulong clientId = (ulong)message["clientId"];
                string clientSteamId;
                try {
                    clientSteamId = _players_ClientId_SteamId[clientId];
                }
                catch {
                    Logging.LogError($"Client Id {clientId} steam Id not found in {nameof(_players_ClientId_SteamId)}.", ModServerConfig);
                    return;
                }

                _playerIsDown.Remove(clientSteamId);
                _playersCurrentPuckTouch.Remove(clientSteamId);
                _playersLastTimePuckPossession.Remove(clientSteamId);
                _lastTimeOnCollisionStayOrExitWasCalled.Remove(clientSteamId);

                _players_ClientId_SteamId.Remove(clientId);
                _pendingVersionMismatch.Remove(clientId);
                _clientReportedModVersions.Remove(clientId);
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnClientDisconnected)}.\n{ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Method called when the client has stopped on the client-side.
        /// Used to reset the config so that it doesn't carry over between servers.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_Client_OnClientStopped(Dictionary<string, object> message) {
            if (NetworkManager.Singleton == null || ServerFunc.IsDedicatedServer())
                return;

            //Logging.Log("Event_OnClientStopped", _clientConfig);

            try {
                ModServerConfig = new ModServerConfig();

                _serverHasResponded = false;
                _askServerForStartupDataCount = 0;

                foreach (int key in new List<int>(_stars.Keys))
                    _stars[key] = "";
                _stickSaves.Clear();
                _bodySaves.Clear();
                _homePlateSaves.Clear();
                _homePlateShots.Clear();
                _passes.Clear();
                _blocks.Clear();
                _hits.Clear();
                _takeaways.Clear();
                _turnovers.Clear();
                _exits.Clear();
                _entries.Clear();
                _shotAttempts.Clear();
                _puckBattleWins.Clear();
                _puckBattleLosses.Clear();
                _goals.Clear();
                // Reset SOG and save percentage
                Client_ResetSOG();
                Client_ResetSavePerc();
                // Reset puck touches
                Client_ResetPuckTouches();
                // Reset possession time and related tracking
                Client_ResetPossessionTime();
                // Reset puck battles
                Client_ResetPuckBattles();
                // Reset home plate SOGs
                Client_ResetHomePlateSogs();
                // Reset team stats
                Client_ResetTeamStats();

                ScoreboardModifications(false);
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_Client_OnClientStopped)}.\n{ex}", _clientConfig);
            }
        }

        public static void Event_OnPlayerRoleChanged(Dictionary<string, object> message) {
            // Use the event to link client Ids to Steam Ids.
            Dictionary<ulong, string> players_ClientId_SteamId_ToChange = new Dictionary<ulong, string>();
            foreach (var kvp in _players_ClientId_SteamId) {
                if (string.IsNullOrEmpty(kvp.Value)) {
                    Player p = PlayerManager.Instance.GetPlayerByClientId(kvp.Key);
                    if (p != null)
                        players_ClientId_SteamId_ToChange.Add(kvp.Key, p.SteamId.Value.Value);
                }
            }

            foreach (var kvp in players_ClientId_SteamId_ToChange) {
                if (!string.IsNullOrEmpty(kvp.Value)) {
                    _players_ClientId_SteamId[kvp.Key] = kvp.Value;
                    Logging.Log($"Added clientId {kvp.Key} linked to Steam Id {kvp.Value}.", ModServerConfig);
                }

                // Fire any pending version-mismatch broadcast now that the player's name is populated.
                if (_pendingVersionMismatch.TryGetValue(kvp.Key, out string pendingClientVersion)) {
                    _pendingVersionMismatch.Remove(kvp.Key);
                    if (IsClientModVersionOutdated(pendingClientVersion, COMPATIBLE_CLIENT_VERSION)) {
                        try {
                            Player outdatedPlayer = PlayerManager.Instance?.GetPlayerByClientId(kvp.Key);
                            string outdatedName = (outdatedPlayer != null && !string.IsNullOrEmpty(outdatedPlayer.Username.Value.Value))
                                ? outdatedPlayer.Username.Value.Value
                                : $"Client {kvp.Key}";
                            string clientVersionDisplay = pendingClientVersion == "1" ? "unknown/legacy" : $"v{pendingClientVersion}";
                            NetworkBehaviourSingleton<ChatManager>.Instance?.Server_BroadcastChatMessage(
                                $"{outdatedName} has an outdated stats mod! ({clientVersionDisplay}, server expects v{COMPATIBLE_CLIENT_VERSION})");
                        } catch (Exception broadcastEx) {
                            Logging.LogError($"Error broadcasting deferred version mismatch for clientId={kvp.Key}: {broadcastEx.Message}", ModServerConfig);
                        }
                    }
                }
            }

            Player player = (Player)message["player"];

            string playerSteamId = player.SteamId.Value.Value;

            if (string.IsNullOrEmpty(playerSteamId))
                return;

            // Event_Everyone_OnPlayerGameStateChanged fires for any game state change (phase/team/role).
            // Extract role from the new game state and only proceed if the role actually changed.
            PlayerGameState newGameState = (PlayerGameState)message["newGameState"];
            PlayerGameState oldGameState = (PlayerGameState)message["oldGameState"];
            PlayerRole newRole = newGameState.Role;

            if (oldGameState.Role == newRole)
                return;

            if (newRole != PlayerRole.Goalie) {
                if (!_sog.TryGetValue(playerSteamId, out int _))
                    _sog.Add(playerSteamId, 0);

                QueueStatUpdate(Codebase.Constants.SOG + playerSteamId, _sog[playerSteamId].ToString());
            }
            else {
                if (!_savePerc.TryGetValue(playerSteamId, out var _))
                    _savePerc.Add(playerSteamId, (0, 0));

                QueueStatUpdate(Codebase.Constants.SAVEPERC + playerSteamId, _savePerc[playerSteamId].ToString());
            }
        }
        #endregion

        #region Methods/Functions
        /// <summary>
        /// Method that processes a hit by a player.
        /// </summary>
        /// <param name="hitterSteamId">String, steam Id of the player that made a hit.</param>
        /// <param name="hitteeSteamId">String, steam Id of the player that was hit.</param>
        private static void ProcessHit(string hitterSteamId, string hitteeSteamId) {
            if (!_hits.TryGetValue(hitterSteamId, out int _))
                _hits.Add(hitterSteamId, 0);

            _hits[hitterSteamId] += 1;
            QueueStatUpdate(Codebase.Constants.HIT + hitterSteamId, _hits[hitterSteamId].ToString());
            LogHit(hitterSteamId, _hits[hitterSteamId]);

            // Record play-by-play events: success for hitter, failure for hittee
            Player hitter = PlayerManager.Instance.GetPlayerBySteamId(hitterSteamId);
            Player hittee = PlayerManager.Instance.GetPlayerBySteamId(hitteeSteamId);
            if (hitter != null && hitter) {
                Vector3 playerPos = hitter.transform.position;
                Vector3 playerVel = hitter.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
                RecordPlayByPlayEventInternal(PlayByPlayEventType.Hit, hitter, playerPos, playerVel, "successful");
            }
            if (hittee != null && hittee) {
                Vector3 playerPos = hittee.transform.position;
                Vector3 playerVel = hittee.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
                RecordPlayByPlayEventInternal(PlayByPlayEventType.Hit, hittee, playerPos, playerVel, "failed");
            }
        }

        /// <summary>
        /// Method that processes a blocked shot by a player.
        /// </summary>
        /// <param name="blockerSteamId">String, steam Id of the player that blocked a shot.</param>
        /// <param name="shooterSteamId">String, steam Id of the player whose shot was blocked.</param>
        /// <param name="shooterTeam">PlayerTeam, team of the shooter.</param>
        /// <param name="blockGameTime">Float, game time when the block occurred (when block check was set up).</param>
        private static void ProcessBlock(string blockerSteamId, string shooterSteamId, PlayerTeam shooterTeam, float blockGameTime = 0f) {
            if (!_blocks.TryGetValue(blockerSteamId, out int _))
                _blocks.Add(blockerSteamId, 0);

            _blocks[blockerSteamId] += 1;
            QueueStatUpdate(Codebase.Constants.BLOCK + blockerSteamId, _blocks[blockerSteamId].ToString());
            LogBlock(blockerSteamId, _blocks[blockerSteamId]);

            // Find and update the corresponding shot event
            // Check if a save occurred for this shot - if so, this is a failed block
            string blockOutcome = "successful";
            if (!string.IsNullOrEmpty(shooterSteamId)) {
                float currentGameTime = GetCurrentGameTime();
                var shotEvent = _playByPlayEvents.LastOrDefault(e => 
                    e.PlayerSteamId == shooterSteamId && 
                    e.EventType == PlayByPlayEventType.Shot && 
                    e.PlayerTeam == (int)shooterTeam &&
                    (e.Outcome == "on net" || e.Outcome == "attempt" || e.Outcome == "blocked") &&
                    e.GameTime >= currentGameTime - 5f); // Check within last 5 seconds to catch delayed blocks
                
                if (shotEvent != null) {
                    // Check if a save or goal occurred for this shot (within 5 seconds after the shot)
                    bool saveOrGoalOccurred = _playByPlayEvents.Any(e =>
                        (e.EventType == PlayByPlayEventType.Save || e.EventType == PlayByPlayEventType.Goal) &&
                        e.GameTime >= shotEvent.GameTime &&
                        e.GameTime <= shotEvent.GameTime + 5f);
                    
                    if (saveOrGoalOccurred) {
                        // Shot went on net despite block attempt - this is a failed block
                        blockOutcome = "failed";
                        // Update shot outcome to "on net" since it reached the goalie
                        shotEvent.Outcome = "on net";
                        // Always set the flag based on shot position (HomePlate or Outside)
                        // The flag was cleared when marked as blocked, so restore it now
                        if (string.IsNullOrEmpty(shotEvent.Flags)) {
                            string shotFlag = DetermineShotFlag(shotEvent.Position, (PlayerTeam)shotEvent.PlayerTeam);
                            shotEvent.Flags = shotFlag;
                        }
                    } else {
                        // No save/goal occurred - this is a successful block
                        shotEvent.Outcome = "blocked";
                        
                        // Clear home plate flag for blocked shots - they shouldn't count as home plate SOGs
                        // (Home plate SOGs should only be tracked when save/goal occurs)
                        if (shotEvent.Flags == "HomePlate") {
                            shotEvent.Flags = ""; // Clear flag for blocked shots
                        }
                    }
                }
            }

            // Record play-by-play event as part of unified tracking
            Player blocker = PlayerManager.Instance.GetPlayerBySteamId(blockerSteamId);
            Puck blockPuck = PuckManager.Instance?.GetPuck();
            if (blocker != null && blocker && blockPuck != null) {
                Vector3 puckPos = blockPuck.transform.position;
                Vector3 puckVel = blockPuck.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
                // Use the block game time when the block check was set up, not current time
                // This ensures the block event is recorded at the correct time, not when processing happens
                float gameTimeToUse = blockGameTime > 0f ? blockGameTime : GetCurrentGameTime();
                RecordPlayByPlayEventInternal(PlayByPlayEventType.Block, blocker, puckPos, puckVel, blockOutcome, null, null, false, gameTimeToUse);
            }

            // Successful block = puck never became a scoring chance for the goalie. Wipe any
            // save/SF credit that was recorded before the block was finalized (e.g. brief goalie
            // contact then teammate block, or phantom ++shots from legacy SOG triggers).
            if (blockOutcome == "successful")
                RevertGoalieStatsForBlockedShot(shooterTeam, shooterSteamId, blockGameTime);
        }

        private static void ProcessTakeaways(string takeawaySteamId) {
            DateTime now = DateTime.UtcNow;
            
            // Check cooldown - only count if 1 second has passed since last takeaway
            if (_lastTakeawayTime.TryGetValue(takeawaySteamId, out DateTime lastTakeawayTime)) {
                double timeSinceLastTakeaway = (now - lastTakeawayTime).TotalSeconds;
                if (timeSinceLastTakeaway < 1.0) {
                    return; // Still in cooldown, don't count this takeaway
                }
            }
            _lastTakeawayTime[takeawaySteamId] = now;

            if (!_takeaways.TryGetValue(takeawaySteamId, out int _))
                _takeaways.Add(takeawaySteamId, 0);

            _takeaways[takeawaySteamId] += 1;
            QueueStatUpdate(Codebase.Constants.TAKEAWAY + takeawaySteamId, _takeaways[takeawaySteamId].ToString());
            LogTakeaways(takeawaySteamId, _takeaways[takeawaySteamId]);
            
            // Track team takeaway stat
            Player takeawayPlayer = PlayerManager.Instance.GetPlayerBySteamId(takeawaySteamId);
            if (takeawayPlayer != null && takeawayPlayer) {
                PlayerTeam playerTeam = takeawayPlayer.Team;
                if (!_teamTakeaways.TryGetValue(playerTeam, out int _))
                    _teamTakeaways.Add(playerTeam, 0);
                _teamTakeaways[playerTeam] += 1;
                QueueStatUpdate(Codebase.Constants.TEAM_TAKEAWAYS + playerTeam.ToString(), _teamTakeaways[playerTeam].ToString());
            }
        }

        private static void ProcessPuckTouch(string playerSteamId, Player player = null, Puck puck = null) {
            DateTime now = DateTime.UtcNow;
            
            // Check cooldown - only count if 0.4 seconds have passed since last touch (for both stats and play-by-play)
            bool shouldProcess = true;
            if (_lastPuckTouchTime.TryGetValue(playerSteamId, out DateTime lastTouchTime)) {
                double timeSinceLastTouch = (now - lastTouchTime).TotalSeconds;
                if (timeSinceLastTouch < 0.4) {
                    shouldProcess = false; // Still in cooldown, don't process this touch
                }
            }
            
            // Always check for zone changes and update zone tracking, even during cooldown
            // This ensures zone exits/entries are detected even if touches are spaced out
            if (player != null && player && puck != null) {
                Vector3 puckPos = puck.transform.position;
                Vector3 puckVel = puck.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
                
                // Determine current zone (for tracking purposes only - flags added retroactively)
                EventZone currentZone = DetermineEventZone(puckPos, player.Team);
                
                // Update last zone for this player (always update, even if moving backwards or in cooldown)
                // This ensures we track zone changes even when touches are on cooldown
                _lastPlayerZone[playerSteamId] = currentZone;
                
                // Record regular touch events only when not in cooldown (to avoid spam)
                if (shouldProcess) {
                        // Check if this should be a pass/reception instead of a touch
                        // If last event was a Touch from a different player on the same team within 6 seconds, convert to Pass/Reception
                        bool shouldRecordAsPassReception = false;
                        string passerSteamId = null;
                        
                        if (_lastEvent != null && 
                            _lastEvent.EventType == PlayByPlayEventType.Touch &&
                            _lastEvent.PlayerSteamId != playerSteamId &&
                            _lastEvent.PlayerTeam == (int)player.Team) {
                            // Check time window (6 seconds, same as pass detection)
                            double timeSinceLastTouchMs = (DateTime.UtcNow - _lastEvent.Timestamp).TotalMilliseconds;
                            if (timeSinceLastTouchMs < 6000) {
                                shouldRecordAsPassReception = true;
                                passerSteamId = _lastEvent.PlayerSteamId;
                            }
                        }
                        
                        if (shouldRecordAsPassReception && !string.IsNullOrEmpty(passerSteamId)) {
                            // Prevent double-counting: Only convert if the event hasn't already been converted to a Pass
                            // This guards against the same touch event being processed multiple times
                            if (_lastEvent.EventType == PlayByPlayEventType.Touch) {
                                // Convert last Touch to Pass
                                _lastEvent.EventType = PlayByPlayEventType.Pass;
                                _lastEvent.Outcome = "successful";
                                
                                // Get passer for stats tracking
                                Player passer = PlayerManager.Instance.GetPlayerBySteamId(passerSteamId);
                                
                                // Update passer stats
                                if (!_passes.TryGetValue(passerSteamId, out int _))
                                    _passes.Add(passerSteamId, 0);
                                _passes[passerSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.PASS + passerSteamId, _passes[passerSteamId].ToString());
                                LogPass(passerSteamId, _passes[passerSteamId]);
                                
                                // Track team pass stat
                                if (passer != null && passer && !PlayerFunc.IsGoalie(passer)) {
                                    PlayerTeam playerTeam = passer.Team;
                                    if (!_teamPasses.TryGetValue(playerTeam, out int _))
                                        _teamPasses.Add(playerTeam, 0);
                                    _teamPasses[playerTeam] += 1;
                                    QueueStatUpdate(Codebase.Constants.TEAM_PASSES + playerTeam.ToString(), _teamPasses[playerTeam].ToString());
                                }
                                
                                // Update last zone for receiver
                                _lastPlayerZone[playerSteamId] = currentZone;
                            }
                        }
                        
                        // Record regular Touch event (zone flags added retroactively by periodic scanner)
                        // The receiver's first touch is NOT converted to Reception - it stays as whatever it is
                        RecordPlayByPlayEventInternal(PlayByPlayEventType.Touch, player, puckPos, puckVel, "successful", "", null, false, null, null);
                        
                        // Update last touch time and stat tracking
                        _lastPuckTouchTime[playerSteamId] = now;
                        
                        // Update stat tracking
                        if (!_puckTouches.TryGetValue(playerSteamId, out int _))
                            _puckTouches.Add(playerSteamId, 0);

                        _puckTouches[playerSteamId] += 1;
                        QueueStatUpdate(Codebase.Constants.PUCK_TOUCH + playerSteamId, _puckTouches[playerSteamId].ToString());
                        LogPuckTouch(playerSteamId, _puckTouches[playerSteamId]);
                    }
                }
            }

        // Removed CheckAndUpdatePossession and EndPossession - possession time is now calculated
        // only between consecutive touches in OnCollisionEnter

        /// <summary>
        /// Formats time in seconds as "M:SS" (e.g., 213 seconds = "3:33")
        /// </summary>
        /// <param name="timeSeconds">Time in seconds</param>
        /// <returns>Formatted string as "M:SS"</returns>
        private static string FormatTimeAsMinutesSeconds(double timeSeconds) {
            int totalSeconds = (int)timeSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:D2}";
        }

        /// <summary>
        /// Determines if a shot position is within the home plate region.
        /// Home plate is defined as a polygon with:
        /// - Square: y from 18 to 30, x from -11 to 11
        /// - Triangle top: from base (-11,30) to (11,30) up to posts (-2,40) and (2,40)
        /// For Blue team: positive Y (18 to 40)
        /// For Red team: negative Y (-18 to -40)
        /// </summary>
        /// <param name="position">The shot position (x, y, z coordinates)</param>
        /// <param name="playerTeam">The team taking the shot</param>
        /// <returns>"HomePlate" if in home plate region, "Outside" otherwise</returns>
        private static string DetermineShotFlag(Vector3 position, PlayerTeam playerTeam) {
            float x = position.x;
            float z = position.z; // In Unity, z is the length axis (equivalent to Y in the diagram)
            
            bool isBlueTeam = (playerTeam == PlayerTeam.Blue);
            
            // Home plate region boundaries
            const float minX = -11f;
            const float maxX = 11f;
            const float squareMinZ = 18f;
            const float squareMaxZ = 30f;
            const float triangleTopZ = 40f;
            const float leftPostX = -2f;
            const float rightPostX = 2f;
            const float EPSILON = 0.01f; // Small epsilon for floating point precision
            
            if (isBlueTeam) {
                // Blue team shoots from negative Z (their offensive zone) toward Red's goal at z=-40
                // Use absolute value for Z since we're checking distance from goal
                float absZ = Mathf.Abs(z);
                
                if (absZ < squareMinZ || absZ > triangleTopZ) {
                    return "Outside";
                }
                
                // If in square region (18 <= absZ <= 30), check X bounds
                if (absZ >= squareMinZ && absZ <= squareMaxZ) {
                    if (x >= (minX - EPSILON) && x <= (maxX + EPSILON)) {
                        return "HomePlate";
                    }
                }
                
                // If in triangle region (30 < absZ <= 40), check if within triangle
                if (absZ > squareMaxZ && absZ <= triangleTopZ) {
                    // Triangle: base from (-11,30) to (11,30), top at posts (-2,40) and (2,40)
                    // Linear interpolation: at absZ=30, x from -11 to 11; at absZ=40, x from -2 to 2
                    float t = (absZ - squareMaxZ) / (triangleTopZ - squareMaxZ); // 0 at absZ=30, 1 at absZ=40
                    float leftBoundAtZ = -11f + t * (leftPostX - (-11f)); // -11 at absZ=30, -2 at absZ=40
                    float rightBoundAtZ = 11f + t * (rightPostX - 11f); // 11 at absZ=30, 2 at absZ=40
                    
                    if (x >= (leftBoundAtZ - EPSILON) && x <= (rightBoundAtZ + EPSILON)) {
                        return "HomePlate";
                    }
                }
            }
            else {
                // Red team shoots from positive Z (their offensive zone) toward Blue's goal at z=40
                // Use absolute value for Z since we're checking distance from goal
                float absZ = Mathf.Abs(z);
                
                if (absZ < squareMinZ || absZ > triangleTopZ) {
                    return "Outside";
                }
                
                // If in square region (18 <= absZ <= 30), check X bounds
                if (absZ >= squareMinZ && absZ <= squareMaxZ) {
                    if (x >= (minX - EPSILON) && x <= (maxX + EPSILON)) {
                        return "HomePlate";
                    }
                }
                
                // If in triangle region (30 < absZ <= 40), check if within triangle
                if (absZ > squareMaxZ && absZ <= triangleTopZ) {
                    // Triangle: base from (-11,30) to (11,30), top at posts (-2,40) and (2,40)
                    // Linear interpolation: at absZ=30, x from -11 to 11; at absZ=40, x from -2 to 2
                    float t = (absZ - squareMaxZ) / (triangleTopZ - squareMaxZ); // 0 at absZ=30, 1 at absZ=40
                    float leftBoundAtZ = -11f + t * (leftPostX - (-11f)); // -11 at absZ=30, -2 at absZ=40
                    float rightBoundAtZ = 11f + t * (rightPostX - 11f); // 11 at absZ=30, 2 at absZ=40
                    
                    if (x >= (leftBoundAtZ - EPSILON) && x <= (rightBoundAtZ + EPSILON)) {
                        return "HomePlate";
                    }
                }
            }
            
            return "Outside";
        }

        /// <summary>
        /// Updates continuous team possession time. Team possession time runs continuously
        /// while a team has possession, but stops if no events occur within 5 seconds.
        /// </summary>
        /// <param name="currentTeam">The team that currently has possession (or None if no possession)</param>
        private static void UpdateTeamPossessionTime(PlayerTeam currentTeam) {
            DateTime now = DateTime.UtcNow;
            
            // If team possession has changed
            if (_currentTeamPossession != currentTeam) {
                // Finalize previous team's possession time (only if gap was <= 5 seconds)
                if (_currentTeamPossession != PlayerTeam.None) {
                    if (_teamLastEventTime.TryGetValue(_currentTeamPossession, out DateTime lastEventTime)) {
                        double gapSinceLastEvent = (now - lastEventTime).TotalSeconds;
                        if (gapSinceLastEvent <= 5.0) {
                            // Only accumulate time if gap was <= 5 seconds
                            double elapsedTime = (now - _teamPossessionStartTime).TotalSeconds;
                            if (!_teamPossessionTime.TryGetValue(_currentTeamPossession, out double _))
                                _teamPossessionTime.Add(_currentTeamPossession, 0.0);
                            _teamPossessionTime[_currentTeamPossession] += elapsedTime;
                            QueueStatUpdate(Codebase.Constants.TEAM_POSSESSION_TIME + _currentTeamPossession.ToString(), _teamPossessionTime[_currentTeamPossession].ToString("F2"));
                        }
                        // If gap > 5 seconds, don't accumulate (clock stopped)
                    }
                }
                
                // Start new team's possession timer
                _currentTeamPossession = currentTeam;
                _teamPossessionStartTime = now;
                if (currentTeam != PlayerTeam.None) {
                    _teamLastEventTime[currentTeam] = now;
                }
            }
            // If same team still has possession, check for timeout
            else if (_currentTeamPossession != PlayerTeam.None) {
                // Check if gap since last event is > 5 seconds (timeout)
                if (_teamLastEventTime.TryGetValue(_currentTeamPossession, out DateTime lastEventTime)) {
                    double gapSinceLastEvent = (now - lastEventTime).TotalSeconds;
                    if (gapSinceLastEvent > 5.0) {
                        // Timeout: stop the clock, don't accumulate time
                        // Reset possession start time to now (will start fresh when next event occurs)
                        // Don't update last event time - it stays old until a new event updates it
                        _teamPossessionStartTime = now;
                        return; // Don't accumulate time for this gap
                    }
                }
                
                // Gap is <= 5 seconds, accumulate time continuously
                double elapsedTime = (now - _teamPossessionStartTime).TotalSeconds;
                if (!_teamPossessionTime.TryGetValue(_currentTeamPossession, out double _))
                    _teamPossessionTime.Add(_currentTeamPossession, 0.0);
                _teamPossessionTime[_currentTeamPossession] += elapsedTime;
                QueueStatUpdate(Codebase.Constants.TEAM_POSSESSION_TIME + _currentTeamPossession.ToString(), _teamPossessionTime[_currentTeamPossession].ToString("F2"));
                // Reset start time to now to avoid double-counting
                _teamPossessionStartTime = now;
            }
        }

        /// <summary>
        /// Checks for and processes turnovers/takeaways when the new team reaches their 2nd event
        /// This combines detection and validation - checks if possession changed and processes immediately
        /// </summary>
        private static void ValidatePendingTurnoversTakeaways(Player player) {
            if (player == null || !player || PlayerFunc.IsGoalie(player))
                return;
            
            // Only check when new team hits their 2nd event (_currentPlayInPossession == 1)
            if (_currentPlayInPossession != 1 || _currentTeamInPossession != player.Team)
                return;
            
            // Prevent re-entrancy - if we're already validating, don't validate again
            // This prevents infinite recursion when recording turnover/takeaway events
            if (_isValidatingTurnoversTakeaways)
                return;
            
            _isValidatingTurnoversTakeaways = true;
            try {
                // Check if we've already recorded a turnover/takeaway for this possession change
                // Look for existing turnover/takeaway events from the new team to prevent duplicates
                if (_playByPlayEvents.Count > 0) {
                    // Check the last few events to see if a turnover/takeaway was already recorded
                    int checkCount = Math.Min(5, _playByPlayEvents.Count);
                    for (int i = _playByPlayEvents.Count - 1; i >= _playByPlayEvents.Count - checkCount; i--) {
                        if (_playByPlayEvents[i].PlayerTeam == (int)player.Team &&
                            (_playByPlayEvents[i].EventType == PlayByPlayEventType.Takeaway ||
                             _playByPlayEvents[i].EventType == PlayByPlayEventType.Turnover)) {
                            // Already recorded a turnover/takeaway for this team - skip to prevent duplicates
                            return;
                        }
                    }
                }
                
                // Detect possession change by looking at play-by-play events
                // Find the last event from a different team before the current team's possession started
                PlayerTeam previousTeam = PlayerTeam.None;
                if (_playByPlayEvents.Count > 1) {
                    // Look backwards from the current event to find when team changed
                    for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        if (_playByPlayEvents[i].PlayerTeam != (int)player.Team) {
                            previousTeam = (PlayerTeam)_playByPlayEvents[i].PlayerTeam;
                            break;
                        }
                    }
                }
                
                // If no previous team found, try using _lastPossession as fallback
                if (previousTeam == PlayerTeam.None && _lastPossession.Team != PlayerTeam.None && _lastPossession.Team != player.Team) {
                    previousTeam = _lastPossession.Team;
                }
                
                // Check if possession changed (different team than current)
                if (previousTeam == PlayerTeam.None || previousTeam == player.Team)
                    return; // No possession change or same team
                
                string currentPlayerSteamId = player.SteamId.Value.Value;
                
                // Find the first successful touch from the new team (the takeaway player)
                // Look for the first event from the new team in the current possession (excluding hits)
                // Use GameTime to find the chronologically first event, not list order
                // Also count successful events to ensure we have 2+ successful events before validating
                string takeawaySteamId = currentPlayerSteamId; // Default to current player
                PlayByPlayEvent firstNewTeamEvent = null; // Track the first successful event from new team (by GameTime)
                int newTeamSuccessfulEventCount = 0; // Count successful events from new team
                if (_playByPlayEvents.Count > 0) {
                    // Find the last previous team event's GameTime to establish a cutoff
                    float lastPreviousTeamGameTime = 0f;
                    for (int i = _playByPlayEvents.Count - 1; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        if (_playByPlayEvents[i].PlayerTeam == (int)previousTeam) {
                            lastPreviousTeamGameTime = _playByPlayEvents[i].GameTime;
                            break;
                        }
                    }
                    
                    // Search through all events to find the chronologically first new team event (by GameTime)
                    // Also count successful events to ensure we have 2+ before validating
                    // The 1-second delay below ensures retroactive updates have time to occur
                    float earliestNewTeamGameTime = float.MaxValue;
                    for (int i = 0; i < _playByPlayEvents.Count; i++) {
                        if (_playByPlayEvents[i].PlayerTeam == (int)player.Team) {
                            // Only consider events that occur after the last previous team event
                            if (_playByPlayEvents[i].GameTime > lastPreviousTeamGameTime) {
                                string outcome = _playByPlayEvents[i].Outcome ?? "";
                                // Ignore hits - they don't indicate possession change
                                bool isHit = _playByPlayEvents[i].EventType == PlayByPlayEventType.Hit;
                                // Include successful/neutral events OR shot events (shots don't use "successful" outcome)
                                bool isShot = _playByPlayEvents[i].EventType == PlayByPlayEventType.Shot;
                                bool isValidOutcome = (outcome == "successful" || outcome == "neutral" || outcome == "") || isShot;
                                if (!isHit && isValidOutcome) {
                                    newTeamSuccessfulEventCount++; // Count successful events
                                    // Track the event with the earliest GameTime
                                    if (_playByPlayEvents[i].GameTime < earliestNewTeamGameTime) {
                                        earliestNewTeamGameTime = _playByPlayEvents[i].GameTime;
                                        takeawaySteamId = _playByPlayEvents[i].PlayerSteamId;
                                        firstNewTeamEvent = _playByPlayEvents[i];
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Only proceed if new team has 2+ successful events (not just 2+ total events)
                // Since validation is triggered when _currentPlayInPossession == 1 (2nd event) and only for successful events,
                // and the current event is already in _playByPlayEvents when validation runs, we should count it
                // So we need at least 2 successful events in the list
                if (newTeamSuccessfulEventCount < 2) {
                    return; // Not enough successful events to validate turnover/takeaway
                }
                
                // Remove the delay check - it was preventing valid turnovers from being recorded
                // The successful events requirement and other checks should be sufficient
                // Retroactive updates will be handled by the outcome checks in the counting logic above
                
                // Check if previous team had 2+ events in possession chain
                // Find the last successful play from the previous team (before the new team's possession started)
                PlayByPlayEvent lastSuccessfulPlay = null;
                if (_playByPlayEvents.Count > 1) {
                    bool inPreviousTeamSegment = false;
                    for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        int eventTeam = _playByPlayEvents[i].PlayerTeam;
                        if (eventTeam == (int)previousTeam) {
                            inPreviousTeamSegment = true;
                            string outcome = _playByPlayEvents[i].Outcome ?? "";
                            if (outcome == "successful" || outcome == "neutral" || outcome == "") {
                                lastSuccessfulPlay = _playByPlayEvents[i];
                                break;
                            }
                        } else if (inPreviousTeamSegment) {
                            break;
                        }
                    }
                }
                
                // Check if last successful play was recent (within 6 seconds)
                const float POSSESSION_CHANGE_WINDOW_SECONDS = 6.0f;
                bool recentPossessionChange = false;
                if (lastSuccessfulPlay != null && _playByPlayEvents.Count > 0) {
                    float newTeamTouchTime = _playByPlayEvents[_playByPlayEvents.Count - 1].GameTime;
                    float timeSinceLastSuccessfulPlay = newTeamTouchTime - lastSuccessfulPlay.GameTime;
                    recentPossessionChange = timeSinceLastSuccessfulPlay <= POSSESSION_CHANGE_WINDOW_SECONDS;
                } else {
                    recentPossessionChange = (DateTime.UtcNow - _lastPossession.Date).TotalMilliseconds < POSSESSION_CHANGE_WINDOW_SECONDS * 1000;
                }
                
                if (!recentPossessionChange)
                    return; // Too much time has passed, not a valid turnover/takeaway
                
                // Get the previous team's possession length
                int previousPlayCount = 0;
                if (lastSuccessfulPlay != null) {
                    if (int.TryParse(lastSuccessfulPlay.CurrentPlayInPossession, out int parsedCount)) {
                        previousPlayCount = parsedCount;
                    }
                }
                
                // Fallback: count consecutive successful events from previous team's last possession only
                if (previousPlayCount == 0 && _playByPlayEvents.Count > 1) {
                    int count = 0;
                    bool inPreviousTeamSegment = false;
                    for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        int eventTeam = _playByPlayEvents[i].PlayerTeam;
                        if (eventTeam == (int)previousTeam) {
                            inPreviousTeamSegment = true;
                            string outcome = _playByPlayEvents[i].Outcome ?? "";
                            if (outcome == "successful" || outcome == "neutral" || outcome == "") {
                                count++;
                            } else {
                                break;
                            }
                        } else if (inPreviousTeamSegment) {
                            break;
                        }
                    }
                    previousPlayCount = count;
                }
                
                // Only process if previous team had 2+ events
                // Also skip if the last successful play was a shot attempt - shot attempts should never lead to turnovers
                if (previousPlayCount >= 2) {
                    // Check if last successful play was a shot attempt
                    if (lastSuccessfulPlay != null && lastSuccessfulPlay.EventType == PlayByPlayEventType.Shot) {
                        return; // Shot attempts should never lead to turnovers
                    }
                    
                    // Get the turnover player from the last failed play from the previous team (the play that caused the turnover)
                    // If no failed play found, fall back to the last successful play
                    string turnoverSteamId = "";
                    PlayByPlayEvent lastFailedPlay = null;
                    
                    // First, try to find the last failed play from the previous team
                    if (_playByPlayEvents.Count > 1) {
                        for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                            if (_playByPlayEvents[i].PlayerTeam == (int)previousTeam) {
                                string outcome = _playByPlayEvents[i].Outcome ?? "";
                                if (outcome == "failed") {
                                    lastFailedPlay = _playByPlayEvents[i];
                                    break;
                                }
                            } else if (_playByPlayEvents[i].PlayerTeam != (int)previousTeam) {
                                // Stop when we hit a different team's event
                                break;
                            }
                        }
                    }
                    
                    // Use the failed play if found, otherwise fall back to last successful play
                    if (lastFailedPlay != null) {
                        turnoverSteamId = lastFailedPlay.PlayerSteamId;
                    } else if (lastSuccessfulPlay != null) {
                        turnoverSteamId = lastSuccessfulPlay.PlayerSteamId;
                    } else {
                        // Fallback: find last event from previous team (successful or failed)
                        for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                            if (_playByPlayEvents[i].PlayerTeam == (int)previousTeam) {
                                turnoverSteamId = _playByPlayEvents[i].PlayerSteamId;
                                break;
                            } else if (_playByPlayEvents[i].PlayerTeam != (int)previousTeam) {
                                break;
                            }
                        }
                    }
                    
                    if (string.IsNullOrEmpty(turnoverSteamId))
                        return; // Can't determine turnover player
                    
                    // Calculate GameTime for the turnover (before checking for shots)
                    float turnoverGameTime = 0f;
                    if (lastSuccessfulPlay != null && firstNewTeamEvent != null) {
                        // Place turnover/takeaway events between the two events
                        // Use midpoint, or slightly after the previous team's event (e.g., 0.01 seconds after)
                        float previousTime = lastSuccessfulPlay.GameTime;
                        float newTeamTime = firstNewTeamEvent.GameTime;
                        turnoverGameTime = previousTime + 0.01f; // Slightly after previous team's last event
                        
                        // Ensure it's before the new team's first event
                        if (turnoverGameTime >= newTeamTime) {
                            turnoverGameTime = (previousTime + newTeamTime) / 2f; // Use midpoint if needed
                        }
                    } else if (lastSuccessfulPlay != null) {
                        // Fallback: use previous team's time + small offset
                        turnoverGameTime = lastSuccessfulPlay.GameTime + 0.01f;
                    } else if (firstNewTeamEvent != null) {
                        // Fallback: use new team's time - small offset
                        turnoverGameTime = firstNewTeamEvent.GameTime - 0.01f;
                    } else {
                        // Fallback: use current game time
                        turnoverGameTime = GetCurrentGameTime();
                    }
                    
                    // Guard against turnovers that occur right before or after shots
                    // Check if there's a shot event from the turnover player that occurs within 5 seconds of the turnover time (before or after)
                    // This handles cases where shot tracking has a delay and the shot event is created after the turnover validation,
                    // or where shots are recorded retroactively with earlier GameTimes
                    const float SHOT_CHECK_WINDOW_SECONDS = 5.0f;
                    bool shotFoundNearTurnover = false;
                    if (turnoverGameTime > 0f) {
                        // Check existing events for shots from the turnover player that occur within the window (before or after)
                        foreach (var existingEvent in _playByPlayEvents) {
                            if (existingEvent.PlayerSteamId == turnoverSteamId &&
                                existingEvent.EventType == PlayByPlayEventType.Shot) {
                                float timeDiff = Math.Abs(existingEvent.GameTime - turnoverGameTime);
                                if (timeDiff <= SHOT_CHECK_WINDOW_SECONDS) {
                                    shotFoundNearTurnover = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    // If a shot was found near the turnover time, don't record the turnover
                    if (shotFoundNearTurnover) {
                        return; // Shot occurred, don't record turnover
                    }
                    
                    // Process stats
                    ProcessTakeaways(takeawaySteamId);
                    ProcessTurnovers(turnoverSteamId);
                    
                    // Record play-by-play events simultaneously (paired events)
                    Player takeawayPlayer = PlayerManager.Instance.GetPlayerBySteamId(takeawaySteamId);
                    Player turnoverPlayer = PlayerManager.Instance.GetPlayerBySteamId(turnoverSteamId);
                    Puck pairedPuck = PuckManager.Instance?.GetPuck();
                    
                    if (takeawayPlayer != null && takeawayPlayer && turnoverPlayer != null && turnoverPlayer && pairedPuck != null) {
                        Vector3 puckPos = pairedPuck.transform.position;
                        Vector3 puckVel = pairedPuck.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
                        
                        // Use the team of each player directly - takeaway player's team (new team), turnover player's team (old team)
                        string takeawayTeamInPossession = (takeawayPlayer.Team == PlayerTeam.Blue ? "Blue" : "Red");
                        string turnoverTeamInPossession = (turnoverPlayer.Team == PlayerTeam.Blue ? "Blue" : "Red");
                        
                        // Record both events with calculated GameTime (placed between last previous team event and first new team event)
                        RecordPlayByPlayEventInternal(PlayByPlayEventType.Takeaway, takeawayPlayer, puckPos, puckVel, "successful", "", null, skipPossessionReset: true, gameTimeOverride: turnoverGameTime > 0 ? turnoverGameTime : (float?)null, teamInPossessionOverride: takeawayTeamInPossession);
                        RecordPlayByPlayEventInternal(PlayByPlayEventType.Turnover, turnoverPlayer, puckPos, puckVel, "failed", "", null, skipPossessionReset: true, gameTimeOverride: turnoverGameTime > 0 ? turnoverGameTime : (float?)null, teamInPossessionOverride: turnoverTeamInPossession);
                        
                        // Track this turnover so we can cancel it if a shot appears shortly after
                        // Get the event IDs from the events that were just added
                        if (turnoverGameTime > 0f && _playByPlayEvents.Count >= 2) {
                            var takeawayEvent = _playByPlayEvents[_playByPlayEvents.Count - 2];
                            var turnoverEvent = _playByPlayEvents[_playByPlayEvents.Count - 1];
                            
                            if (takeawayEvent.EventType == PlayByPlayEventType.Takeaway && turnoverEvent.EventType == PlayByPlayEventType.Turnover) {
                                if (!_recentTurnovers.TryGetValue(turnoverSteamId, out List<(float, int, int)> turnovers)) {
                                    turnovers = new List<(float, int, int)>();
                                    _recentTurnovers.Add(turnoverSteamId, turnovers);
                                }
                                turnovers.Add((turnoverGameTime, turnoverEvent.EventId, takeawayEvent.EventId));
                                
                                // Clean up old turnovers (older than 1 second) to prevent memory buildup
                                turnovers.RemoveAll(t => turnoverGameTime - t.Item1 > 1.0f);
                            }
                        }
                    }
                }
            } finally {
                _isValidatingTurnoversTakeaways = false;
            }
        }

        private static void ProcessTurnovers(string turnoverSteamId) {
            DateTime now = DateTime.UtcNow;
            
            // Check cooldown - only count if 1 second has passed since last turnover
            if (_lastTurnoverTime.TryGetValue(turnoverSteamId, out DateTime lastTurnoverTime)) {
                double timeSinceLastTurnover = (now - lastTurnoverTime).TotalSeconds;
                if (timeSinceLastTurnover < 1.0) {
                    return; // Still in cooldown, don't count this turnover
                }
            }
            _lastTurnoverTime[turnoverSteamId] = now;

            if (!_turnovers.TryGetValue(turnoverSteamId, out int _))
                _turnovers.Add(turnoverSteamId, 0);

            _turnovers[turnoverSteamId] += 1;
            QueueStatUpdate(Codebase.Constants.TURNOVER + turnoverSteamId, _turnovers[turnoverSteamId].ToString());
            LogTurnovers(turnoverSteamId, _turnovers[turnoverSteamId]);
            
            // Track team turnover stat
            Player turnoverPlayer = PlayerManager.Instance.GetPlayerBySteamId(turnoverSteamId);
            if (turnoverPlayer != null && turnoverPlayer) {
                PlayerTeam playerTeam = turnoverPlayer.Team;
                if (!_teamTurnovers.TryGetValue(playerTeam, out int _))
                    _teamTurnovers.Add(playerTeam, 0);
                _teamTurnovers[playerTeam] += 1;
                QueueStatUpdate(Codebase.Constants.TEAM_TURNOVERS + playerTeam.ToString(), _teamTurnovers[playerTeam].ToString());
            }
            
            // Note: Play-by-play events for turnovers are now recorded in the paired event handler
            // (when possession change is validated), not here, to ensure they're paired with takeaways
        }

        /// <summary>
        /// Cancels a turnover if a shot occurred shortly after it.
        /// This handles cases where shot tracking has a delay and the shot event is created after the turnover validation.
        /// </summary>
        private static void CancelTurnoverIfShotOccurred(string playerSteamId, float shotGameTime) {
            if (string.IsNullOrEmpty(playerSteamId) || shotGameTime <= 0f)
                return;
            
            if (!_recentTurnovers.TryGetValue(playerSteamId, out List<(float GameTime, int TurnoverEventId, int TakeawayEventId)> turnovers))
                return;
            
            const float SHOT_CHECK_WINDOW_SECONDS = 5.0f;
            
            // Find turnovers that occurred within the time window of the shot (before or after, 5 seconds)
            // This handles cases where shots are recorded retroactively with earlier GameTimes
            var turnoversToCancel = turnovers.Where(t => 
                Math.Abs(shotGameTime - t.GameTime) <= SHOT_CHECK_WINDOW_SECONDS
            ).ToList();
            
            foreach (var turnover in turnoversToCancel) {
                // Find the takeaway event to get the takeaway player
                var takeawayEvent = _playByPlayEvents.FirstOrDefault(e => e.EventId == turnover.TakeawayEventId);
                string takeawaySteamId = takeawayEvent?.PlayerSteamId;
                
                // Decrement turnover stats
                if (_turnovers.TryGetValue(playerSteamId, out int turnoverCount) && turnoverCount > 0) {
                    _turnovers[playerSteamId] = turnoverCount - 1;
                    QueueStatUpdate(Codebase.Constants.TURNOVER + playerSteamId, _turnovers[playerSteamId].ToString());
                    LogTurnovers(playerSteamId, _turnovers[playerSteamId]);
                }
                
                // Decrement team turnover stat
                Player player = PlayerManager.Instance.GetPlayerBySteamId(playerSteamId);
                if (player != null && player) {
                    PlayerTeam playerTeam = player.Team;
                    if (_teamTurnovers.TryGetValue(playerTeam, out int teamTurnoverCount) && teamTurnoverCount > 0) {
                        _teamTurnovers[playerTeam] = teamTurnoverCount - 1;
                        QueueStatUpdate(Codebase.Constants.TEAM_TURNOVERS + playerTeam.ToString(), _teamTurnovers[playerTeam].ToString());
                    }
                }
                
                // Decrement takeaway stats if takeaway event was found
                if (!string.IsNullOrEmpty(takeawaySteamId)) {
                    if (_takeaways.TryGetValue(takeawaySteamId, out int takeawayCount) && takeawayCount > 0) {
                        _takeaways[takeawaySteamId] = takeawayCount - 1;
                        QueueStatUpdate(Codebase.Constants.TAKEAWAY + takeawaySteamId, _takeaways[takeawaySteamId].ToString());
                        LogTakeaways(takeawaySteamId, _takeaways[takeawaySteamId]);
                    }
                    
                    // Decrement team takeaway stat
                    Player takeawayPlayer = PlayerManager.Instance.GetPlayerBySteamId(takeawaySteamId);
                    if (takeawayPlayer != null && takeawayPlayer) {
                        PlayerTeam takeawayTeam = takeawayPlayer.Team;
                        if (_teamTakeaways.TryGetValue(takeawayTeam, out int teamTakeawayCount) && teamTakeawayCount > 0) {
                            _teamTakeaways[takeawayTeam] = teamTakeawayCount - 1;
                            QueueStatUpdate(Codebase.Constants.TEAM_TAKEAWAYS + takeawayTeam.ToString(), _teamTakeaways[takeawayTeam].ToString());
                        }
                    }
                }
                
                // Remove turnover and takeaway events from play-by-play list
                _playByPlayEvents.RemoveAll(e => e.EventId == turnover.TurnoverEventId || e.EventId == turnover.TakeawayEventId);
                
                // Remove from tracking list
                turnovers.Remove(turnover);
            }
        }

        /// <summary>
        /// Calculates Time On Ice (TOI) for all players by analyzing play-by-play events.
        /// Tracks when players appear in roster data and sums up time intervals.
        /// </summary>
        private static void CalculateTimeOnIce() {
            // Clear existing TOI data
            _timeOnIceSeconds.Clear();
            
            if (_playByPlayEvents == null || _playByPlayEvents.Count == 0)
                return;
            
            // Dictionary to track when each player started being on ice (GameTime)
            Dictionary<string, float> playerOnIceStartTime = new Dictionary<string, float>();
            
            // Sort events by GameTime to process chronologically
            var sortedEvents = _playByPlayEvents.OrderBy(e => e.GameTime).ToList();
            
            // Track the first event's gameTime to identify players who were on ice from the start
            float firstEventGameTime = sortedEvents.Count > 0 && sortedEvents[0].GameTime > 0f ? sortedEvents[0].GameTime : 0f;
            
            // Process each event chronologically
            for (int i = 0; i < sortedEvents.Count; i++) {
                var currentEvent = sortedEvents[i];
                float currentGameTime = currentEvent.GameTime;
                
                // Skip events with invalid game time (negative, but allow 0 for first event)
                if (currentGameTime < 0f)
                    continue;
                
                // Skip GameEnd event - we'll handle it separately at the end
                // GameEnd's roster might be incomplete or empty, so we don't want to process it in the loop
                if (currentEvent.EventType == PlayByPlayEventType.GameEnd)
                    continue;
                
                // Extract all players currently on ice from roster fields
                HashSet<string> playersOnIce = new HashSet<string>();
                
                // Add players from all roster fields
                playersOnIce.UnionWith(ExtractSteamIdsFromRosterField(currentEvent.TeamForwardsSteamID));
                playersOnIce.UnionWith(ExtractSteamIdsFromRosterField(currentEvent.TeamDefencemenSteamID));
                playersOnIce.UnionWith(ExtractSteamIdsFromRosterField(currentEvent.TeamGoalieSteamID));
                playersOnIce.UnionWith(ExtractSteamIdsFromRosterField(currentEvent.OpposingTeamForwardsSteamID));
                playersOnIce.UnionWith(ExtractSteamIdsFromRosterField(currentEvent.OpposingTeamDefencemenSteamID));
                playersOnIce.UnionWith(ExtractSteamIdsFromRosterField(currentEvent.OpposingTeamGoalieSteamID));
                
                // Check for players who are no longer on ice (were on ice before, but not now)
                var playersToRemove = playerOnIceStartTime.Keys.Where(steamId => !playersOnIce.Contains(steamId)).ToList();
                
                foreach (string steamId in playersToRemove) {
                    float startTime = playerOnIceStartTime[steamId];
                    float timeOnIce = currentGameTime - startTime;
                    
                    // Add to total TOI
                    if (!_timeOnIceSeconds.TryGetValue(steamId, out double totalTOI))
                        _timeOnIceSeconds.Add(steamId, 0.0);
                    _timeOnIceSeconds[steamId] += timeOnIce;
                    
                    // Remove from tracking
                    playerOnIceStartTime.Remove(steamId);
                }
                
                // Check for players who just came on ice (not tracked yet, but now on ice)
                foreach (string steamId in playersOnIce) {
                    if (!playerOnIceStartTime.ContainsKey(steamId)) {
                        // Player just came on ice
                        // If this is the first event with roster data (FaceoffOutcome), assume player started at gameTime 0
                        // This accounts for players who were on ice from the start but roster wasn't captured until first event
                        float startTime = (i == 0) ? 0f : currentGameTime;
                        playerOnIceStartTime[steamId] = startTime;
                    }
                }
            }
            
            // Handle players still on ice at the end of the game
            // Look for GameEnd event first, if not found use the maximum gameTime from all events
            var gameEndEvent = sortedEvents
                .Where(e => e.EventType == PlayByPlayEventType.GameEnd)
                .OrderByDescending(e => e.GameTime)
                .FirstOrDefault();
            
            float finalGameTime;
            if (gameEndEvent != null) {
                // Use GameEnd event's GameTime
                finalGameTime = gameEndEvent.GameTime;
            }
            else {
                // Fallback: use the maximum game time from all events if GameEnd doesn't exist
                finalGameTime = sortedEvents.Count > 0 ? sortedEvents.Max(e => e.GameTime) : 0f;
            }
            
            foreach (var kvp in playerOnIceStartTime) {
                string steamId = kvp.Key;
                float startTime = kvp.Value;
                float timeOnIce = finalGameTime - startTime;
                
                // Add to total TOI
                if (!_timeOnIceSeconds.TryGetValue(steamId, out double totalTOI))
                    _timeOnIceSeconds.Add(steamId, 0.0);
                _timeOnIceSeconds[steamId] += timeOnIce;
            }
        }

        /// <summary>
        /// Extracts Steam IDs from a roster field string (semicolon-separated, formatted as ="steamId")
        /// </summary>
        private static HashSet<string> ExtractSteamIdsFromRosterField(string rosterField) {
            HashSet<string> steamIds = new HashSet<string>();
            if (string.IsNullOrEmpty(rosterField))
                return steamIds;

            // Split by semicolon and extract SteamIDs
            string[] parts = rosterField.Split(';');
            foreach (string part in parts) {
                string trimmed = part.Trim();
                // Remove `="` prefix and `"` suffix
                if (trimmed.StartsWith("=\"") && trimmed.EndsWith("\"")) {
                    string steamId = trimmed.Substring(2, trimmed.Length - 3);
                    if (!string.IsNullOrEmpty(steamId)) {
                        steamIds.Add(steamId);
                    }
                }
            }
            return steamIds;
        }

        /// <summary>
        /// Calculates Plus/Minus for all skaters based on goal events
        /// Skaters get +1 for being on ice when their team scores, -1 when opponent scores
        /// Goalies do not have +/- (only skaters)
        /// </summary>
        private static void CalculatePlusMinus() {
            // Clear existing +/- stats
            _plusMinus.Clear();
            
            // Get all goal events (excluding own goals)
            var goalEvents = _playByPlayEvents
                .Where(e => e.EventType == PlayByPlayEventType.Goal)
                .OrderBy(e => e.GameTime)
                .ToList();
            
            foreach (var goalEvent in goalEvents) {
                // Get the scoring team from the goal event
                PlayerTeam scoringTeam = (PlayerTeam)goalEvent.PlayerTeam;
                if (scoringTeam == PlayerTeam.None)
                    continue;

                PlayerTeam opposingTeam = scoringTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
                
                // Extract all skaters on ice from roster data
                HashSet<string> scoringTeamSkaters = new HashSet<string>();
                HashSet<string> opposingTeamSkaters = new HashSet<string>();
                
                // Get skaters from scoring team (forwards + defencemen, exclude goalies)
                // Note: TeamForwardsSteamID/TeamDefencemenSteamID are from the goal scorer's team perspective
                // OpposingTeamForwardsSteamID/OpposingTeamDefencemenSteamID are from the opposing team
                // Since goalEvent.PlayerTeam is the scoring team, TeamForwardsSteamID is always the scoring team
                scoringTeamSkaters.UnionWith(ExtractSteamIdsFromRosterField(goalEvent.TeamForwardsSteamID));
                scoringTeamSkaters.UnionWith(ExtractSteamIdsFromRosterField(goalEvent.TeamDefencemenSteamID));
                
                opposingTeamSkaters.UnionWith(ExtractSteamIdsFromRosterField(goalEvent.OpposingTeamForwardsSteamID));
                opposingTeamSkaters.UnionWith(ExtractSteamIdsFromRosterField(goalEvent.OpposingTeamDefencemenSteamID));
                
                // Filter out goalies - only count skaters
                foreach (string steamId in scoringTeamSkaters.ToList()) {
                    Player player = PlayerManager.Instance?.GetPlayerBySteamId(steamId);
                    if (player != null && player && PlayerFunc.IsGoalie(player)) {
                        scoringTeamSkaters.Remove(steamId);
                    }
                }
                
                foreach (string steamId in opposingTeamSkaters.ToList()) {
                    Player player = PlayerManager.Instance?.GetPlayerBySteamId(steamId);
                    if (player != null && player && PlayerFunc.IsGoalie(player)) {
                        opposingTeamSkaters.Remove(steamId);
                    }
                }
                
                // Give +1 to all skaters on scoring team
                foreach (string steamId in scoringTeamSkaters) {
                    if (string.IsNullOrEmpty(steamId))
                        continue;
                    
                    if (!_plusMinus.TryGetValue(steamId, out int _))
                        _plusMinus.Add(steamId, 0);
                    _plusMinus[steamId] += 1;
                }
                
                // Give -1 to all skaters on opposing team
                foreach (string steamId in opposingTeamSkaters) {
                    if (string.IsNullOrEmpty(steamId))
                        continue;
                    
                    if (!_plusMinus.TryGetValue(steamId, out int _))
                        _plusMinus.Add(steamId, 0);
                    _plusMinus[steamId] -= 1;
                }
            }
        }

        // OLD METHOD REMOVED: ProcessPuckBattleWin is no longer used
        // Puck battles are now recorded in the new detection code path (lines 1531-1607)
        // which records them with "neutral" outcome and proper flags (defending/contesting)
        // This method is kept for backwards compatibility with stats tracking only
        private static void ProcessPuckBattleWin(string winnerSteamId, string loserSteamId) {
            // Stats tracking only - no play-by-play events recorded here
            // Play-by-play events are handled by the new puck battle detection system
            if (!_puckBattleWins.TryGetValue(winnerSteamId, out int _))
                _puckBattleWins.Add(winnerSteamId, 0);

            _puckBattleWins[winnerSteamId] += 1;
            QueueStatUpdate(Codebase.Constants.PUCK_BATTLE_WINS + winnerSteamId, _puckBattleWins[winnerSteamId].ToString());
            
            // Track team stat
            Player winnerPlayer = PlayerManager.Instance.GetPlayerBySteamId(winnerSteamId);
            if (winnerPlayer != null && winnerPlayer && !PlayerFunc.IsGoalie(winnerPlayer)) {
                PlayerTeam winnerTeam = winnerPlayer.Team;
                if (!_teamPuckBattleWins.TryGetValue(winnerTeam, out int _))
                    _teamPuckBattleWins.Add(winnerTeam, 0);
                _teamPuckBattleWins[winnerTeam] += 1;
                QueueStatUpdate(Codebase.Constants.TEAM_PUCK_BATTLE_WINS + winnerTeam.ToString(), _teamPuckBattleWins[winnerTeam].ToString());
            }

            // Process loss for loser
            ProcessPuckBattleLoss(loserSteamId);
            
            // NO PLAY-BY-PLAY EVENTS RECORDED HERE - handled by new detection system
        }

        private static void ProcessPuckBattleLoss(string loserSteamId) {
            if (!_puckBattleLosses.TryGetValue(loserSteamId, out int _))
                _puckBattleLosses.Add(loserSteamId, 0);

            _puckBattleLosses[loserSteamId] += 1;
            QueueStatUpdate(Codebase.Constants.PUCK_BATTLE_LOSSES + loserSteamId, _puckBattleLosses[loserSteamId].ToString());
            
            // Track team stat
            Player loserPlayer = PlayerManager.Instance.GetPlayerBySteamId(loserSteamId);
            if (loserPlayer != null && loserPlayer && !PlayerFunc.IsGoalie(loserPlayer)) {
                PlayerTeam loserTeam = loserPlayer.Team;
                if (!_teamPuckBattleLosses.TryGetValue(loserTeam, out int _))
                    _teamPuckBattleLosses.Add(loserTeam, 0);
                _teamPuckBattleLosses[loserTeam] += 1;
                QueueStatUpdate(Codebase.Constants.TEAM_PUCK_BATTLE_LOSSES + loserTeam.ToString(), _teamPuckBattleLosses[loserTeam].ToString());
            }
        }

        private static void Server_RegisterNamedMessageHandler() {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null && !_hasRegisteredWithNamedMessageHandler) {
                Logging.Log($"RegisterNamedMessageHandler {Constants.FROM_CLIENT_TO_SERVER}.", ModServerConfig);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(Constants.FROM_CLIENT_TO_SERVER, ReceiveData);

                _hasRegisteredWithNamedMessageHandler = true;
            }
        }

        private static void CheckForRulesetMod() {
            if (_rulesetModEnabled != null && (bool)_rulesetModEnabled)
                return;

            _rulesetModEnabled = ModManager.GetModById("3501446576") != null ||
                                 ModManager.GetModById("3500559233") != null;
            Logging.Log($"Ruleset mod is enabled : {_rulesetModEnabled}.", ModServerConfig, true);
        }

        /// <summary>
        /// Method that manages received data from client-server communications.
        /// </summary>
        /// <param name="clientId">Ulong, Id of the client that sent the data. (0 if the server sent the data)</param>
        /// <param name="reader">FastBufferReader, stream containing the received data.</param>
        public static void ReceiveData(ulong clientId, FastBufferReader reader) {
            try {
                string dataName, dataStr;
                if (clientId == NetworkManager.ServerClientId) // If client Id is 0, we received data from the server, so we are client-sided.
                    (dataName, dataStr) = NetworkCommunication.GetData(clientId, reader, _clientConfig);
                else
                    (dataName, dataStr) = NetworkCommunication.GetData(clientId, reader, ModServerConfig);

                // Normalize corrupted RESET_ALL (defense-in-depth if send uses ReliableSequenced)
                bool isClientFromServer = clientId == NetworkManager.ServerClientId;
                bool isShortUnstructuredPayload = dataStr != null && dataStr.Length <= 4 && !dataStr.Contains(';');
                bool couldBeCorruptedReset = isClientFromServer && dataName != null && dataName.StartsWith(Constants.MOD_NAME) && dataName != RESET_ALL
                    && dataName.Length <= Constants.MOD_NAME.Length + 5
                    && (dataStr == "1" || isShortUnstructuredPayload);
                if (couldBeCorruptedReset) {
                    dataName = RESET_ALL;
                    dataStr = "1";
                }

                switch (dataName) {
                    case Constants.MOD_NAME + "_" + nameof(MOD_VERSION): // CLIENT-SIDE: server sends the required client version; compare and notify if outdated.
                        _serverHasResponded = true;
#if DEBUG_MODE
                        DebugTrace.Section("NETWORK HANDSHAKE");
                        DebugTrace.Write("NETWORK", $"CLIENT received required version from server. required='{dataStr}' ours='{MOD_VERSION}' match={MOD_VERSION == dataStr}");
#endif
                        // Always activate the mod regardless of version — outdated clients still get
                        // partial functionality. A mismatch just shows an in-chat update nudge.
                        if (MonoBehaviourSingleton<UIManager>.Instance.Scoreboard != null) {
                            VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "scoreboard");
#if DEBUG_MODE
                            DebugTrace.Write("NETWORK", $"Scoreboard init: scoreboardContainer={scoreboardContainer != null} headerAlreadySet={_hasUpdatedUIScoreboard.Contains("header")} teamTooltipCount={_teamTooltips.Count}");
#endif
                            if (scoreboardContainer != null && !_hasUpdatedUIScoreboard.Contains("header")) {
                                ScoreboardModifications(true);
                            } else if (scoreboardContainer != null && _hasUpdatedUIScoreboard.Contains("header") && _teamTooltips.Count == 0) {
                                SetupTeamTooltips(scoreboardContainer);
                            }
                            if (_hasUpdatedUIScoreboard.Contains("header") && scoreboardContainer != null) {
                                scoreboardContainer.schedule.Execute(() => {
                                    ScoreboardModifications(true);
                                }).ExecuteLater(100);
                            }
                        }
#if DEBUG_MODE
                        else {
                            DebugTrace.Write("NETWORK", "Scoreboard is NULL on version receive — tooltips will init on next StylePlayer patch.");
                        }
#endif
                        break;

                    case Constants.MOD_NAME + "_kick": // SERVER-SIDE: old clients (pre-graceful-update code) send this when their version doesn't match.
                        // Do NOT kick — just broadcast a public notice so the server is aware and the player knows to update.
                        if (dataStr != "1")
                            break;
                        try {
                            Player outdatedPlayer = PlayerManager.Instance.GetPlayerByClientId(clientId);
                            if (outdatedPlayer != null && !string.IsNullOrEmpty(outdatedPlayer.Username.Value.Value))
                                Logging.Log($"Client {clientId} ({outdatedPlayer.Username.Value.Value}) has outdated Stats mod — ignoring kick request.", ModServerConfig);
                        } catch { }
                        break;

                    case Constants.ASK_SERVER_FOR_STARTUP_DATA: // SERVER-SIDE : client sends its MOD_VERSION; respond with our required version + batch stats.
                        if (string.IsNullOrEmpty(dataStr))
                            break;
#if DEBUG_MODE
                        DebugTrace.Section("NETWORK HANDSHAKE");
                        DebugTrace.Write("NETWORK", $"SERVER received ASK_SERVER_FOR_STARTUP_DATA from clientId={clientId} clientVersion='{dataStr}'. Sending COMPATIBLE_CLIENT_VERSION={COMPATIBLE_CLIENT_VERSION}");
                        DebugTrace.Flush();
#endif
                        NetworkCommunication.SendData(Constants.MOD_NAME + "_" + nameof(MOD_VERSION), COMPATIBLE_CLIENT_VERSION, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);

                        if (_clientReportedModVersions.TryGetValue(clientId, out string _))
                            _clientReportedModVersions[clientId] = dataStr;
                        else
                            _clientReportedModVersions.Add(clientId, dataStr);

                        // Version mismatch — defer the broadcast until Event_OnPlayerRoleChanged where
                        // the player's Username is guaranteed to be populated.
                        // Skip when the client is on a newer build than this server (outdated server, not outdated client).
                        if (IsClientModVersionOutdated(dataStr, COMPATIBLE_CLIENT_VERSION)) {
                            try {
                                if (_pendingVersionMismatch.ContainsKey(clientId))
                                    _pendingVersionMismatch.Remove(clientId);
                                _pendingVersionMismatch.Add(clientId, dataStr);
                            } catch (Exception versionEx) {
                                Logging.LogError($"Error queuing version mismatch for clientId={clientId}: {versionEx.Message}", ModServerConfig);
                            }
                        }

                        if (_sog.Count != 0) {
                            string batchSOG = "";
                            foreach (string key in new List<string>(_sog.Keys))
                                batchSOG += key + ';' + _sog[key].ToString() + ';';
                            batchSOG = batchSOG.Remove(batchSOG.Length - 1);
                            NetworkCommunication.SendData(BATCH_SOG, batchSOG, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_savePerc.Count != 0) {
                            string batchSavePerc = "";
                            foreach (string key in new List<string>(_savePerc.Keys))
                                batchSavePerc += key + ';' + _savePerc[key].ToString() + ';';
                            batchSavePerc = batchSavePerc.Remove(batchSavePerc.Length - 1);
                            NetworkCommunication.SendData(BATCH_SAVEPERC, batchSavePerc, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_hits.Count != 0) {
                            string batchHits = "";
                            foreach (string key in new List<string>(_hits.Keys))
                                batchHits += key + ';' + _hits[key].ToString() + ';';
                            batchHits = batchHits.Remove(batchHits.Length - 1);
                            NetworkCommunication.SendData(BATCH_HIT, batchHits, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_turnovers.Count != 0) {
                            string batchTurnovers = "";
                            foreach (string key in new List<string>(_turnovers.Keys))
                                batchTurnovers += key + ';' + _turnovers[key].ToString() + ';';
                            batchTurnovers = batchTurnovers.Remove(batchTurnovers.Length - 1);
                            NetworkCommunication.SendData(BATCH_TURNOVER, batchTurnovers, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_takeaways.Count != 0) {
                            string batchTakeaways = "";
                            foreach (string key in new List<string>(_takeaways.Keys))
                                batchTakeaways += key + ';' + _takeaways[key].ToString() + ';';
                            batchTakeaways = batchTakeaways.Remove(batchTakeaways.Length - 1);
                            NetworkCommunication.SendData(BATCH_TAKEAWAY, batchTakeaways, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_passes.Count != 0) {
                            string batchPasses = "";
                            foreach (string key in new List<string>(_passes.Keys))
                                batchPasses += key + ';' + _passes[key].ToString() + ';';
                            batchPasses = batchPasses.Remove(batchPasses.Length - 1);
                            NetworkCommunication.SendData(BATCH_PASS, batchPasses, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_puckTouches.Count != 0) {
                            string batchPuckTouches = "";
                            foreach (string key in new List<string>(_puckTouches.Keys))
                                batchPuckTouches += key + ';' + _puckTouches[key].ToString() + ';';
                            batchPuckTouches = batchPuckTouches.Remove(batchPuckTouches.Length - 1);
                            NetworkCommunication.SendData(BATCH_PUCK_TOUCH, batchPuckTouches, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_exits.Count != 0) {
                            string batchExits = "";
                            foreach (string key in new List<string>(_exits.Keys))
                                batchExits += key + ';' + _exits[key].ToString() + ';';
                            batchExits = batchExits.Remove(batchExits.Length - 1);
                            NetworkCommunication.SendData(BATCH_EXIT, batchExits, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_entries.Count != 0) {
                            string batchEntries = "";
                            foreach (string key in new List<string>(_entries.Keys))
                                batchEntries += key + ';' + _entries[key].ToString() + ';';
                            batchEntries = batchEntries.Remove(batchEntries.Length - 1);
                            NetworkCommunication.SendData(BATCH_ENTRY, batchEntries, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_possessionTimeSeconds.Count != 0) {
                            string batchPossessionTime = "";
                            foreach (string key in new List<string>(_possessionTimeSeconds.Keys))
                                batchPossessionTime += key + ';' + _possessionTimeSeconds[key].ToString("F2") + ';';
                            batchPossessionTime = batchPossessionTime.Remove(batchPossessionTime.Length - 1);
                            NetworkCommunication.SendData(BATCH_POSSESSION_TIME, batchPossessionTime, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_puckBattleWins.Count != 0) {
                            string batchPuckBattleWins = "";
                            foreach (string key in new List<string>(_puckBattleWins.Keys))
                                batchPuckBattleWins += key + ';' + _puckBattleWins[key].ToString() + ';';
                            batchPuckBattleWins = batchPuckBattleWins.Remove(batchPuckBattleWins.Length - 1);
                            NetworkCommunication.SendData(BATCH_PUCK_BATTLE_WINS, batchPuckBattleWins, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_puckBattleLosses.Count != 0) {
                            string batchPuckBattleLosses = "";
                            foreach (string key in new List<string>(_puckBattleLosses.Keys))
                                batchPuckBattleLosses += key + ';' + _puckBattleLosses[key].ToString() + ';';
                            batchPuckBattleLosses = batchPuckBattleLosses.Remove(batchPuckBattleLosses.Length - 1);
                            NetworkCommunication.SendData(BATCH_PUCK_BATTLE_LOSSES, batchPuckBattleLosses, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_shotAttempts.Count != 0) {
                            string batchShotAttempts = "";
                            foreach (string key in new List<string>(_shotAttempts.Keys))
                                batchShotAttempts += key + ';' + _shotAttempts[key].ToString() + ';';
                            batchShotAttempts = batchShotAttempts.Remove(batchShotAttempts.Length - 1);
                            NetworkCommunication.SendData(BATCH_SHOT_ATTEMPTS, batchShotAttempts, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_homePlateSogs.Count != 0) {
                            string batchHomePlateSogs = "";
                            foreach (string key in new List<string>(_homePlateSogs.Keys))
                                batchHomePlateSogs += key + ';' + _homePlateSogs[key].ToString() + ';';
                            batchHomePlateSogs = batchHomePlateSogs.Remove(batchHomePlateSogs.Length - 1);
                            NetworkCommunication.SendData(BATCH_HOME_PLATE_SOGS, batchHomePlateSogs, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        // Send team stats
                        if (_teamShots.Count != 0) {
                            string batchTeamShots = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamShots.Keys))
                                batchTeamShots += team.ToString() + ';' + _teamShots[team].ToString() + ';';
                            batchTeamShots = batchTeamShots.Remove(batchTeamShots.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_SHOTS, batchTeamShots, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamShotAttempts.Count != 0) {
                            string batchTeamShotAttempts = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamShotAttempts.Keys))
                                batchTeamShotAttempts += team.ToString() + ';' + _teamShotAttempts[team].ToString() + ';';
                            batchTeamShotAttempts = batchTeamShotAttempts.Remove(batchTeamShotAttempts.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_SHOT_ATTEMPTS, batchTeamShotAttempts, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamHomePlateSogs.Count != 0) {
                            string batchTeamHomePlateSogs = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamHomePlateSogs.Keys))
                                batchTeamHomePlateSogs += team.ToString() + ';' + _teamHomePlateSogs[team].ToString() + ';';
                            batchTeamHomePlateSogs = batchTeamHomePlateSogs.Remove(batchTeamHomePlateSogs.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_HOME_PLATE_SOGS, batchTeamHomePlateSogs, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamFaceoffWins.Count != 0) {
                            string batchTeamFaceoffWins = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamFaceoffWins.Keys))
                                batchTeamFaceoffWins += team.ToString() + ';' + _teamFaceoffWins[team].ToString() + ';';
                            batchTeamFaceoffWins = batchTeamFaceoffWins.Remove(batchTeamFaceoffWins.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_FACEOFF_WINS, batchTeamFaceoffWins, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamFaceoffTotal.Count != 0) {
                            string batchTeamFaceoffTotal = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamFaceoffTotal.Keys))
                                batchTeamFaceoffTotal += team.ToString() + ';' + _teamFaceoffTotal[team].ToString() + ';';
                            batchTeamFaceoffTotal = batchTeamFaceoffTotal.Remove(batchTeamFaceoffTotal.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_FACEOFF_TOTAL, batchTeamFaceoffTotal, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamTakeaways.Count != 0) {
                            string batchTeamTakeaways = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamTakeaways.Keys))
                                batchTeamTakeaways += team.ToString() + ';' + _teamTakeaways[team].ToString() + ';';
                            batchTeamTakeaways = batchTeamTakeaways.Remove(batchTeamTakeaways.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_TAKEAWAYS, batchTeamTakeaways, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamTurnovers.Count != 0) {
                            string batchTeamTurnovers = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamTurnovers.Keys))
                                batchTeamTurnovers += team.ToString() + ';' + _teamTurnovers[team].ToString() + ';';
                            batchTeamTurnovers = batchTeamTurnovers.Remove(batchTeamTurnovers.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_TURNOVERS, batchTeamTurnovers, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamExits.Count != 0) {
                            string batchTeamExits = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamExits.Keys))
                                batchTeamExits += team.ToString() + ';' + _teamExits[team].ToString() + ';';
                            batchTeamExits = batchTeamExits.Remove(batchTeamExits.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_EXITS, batchTeamExits, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamEntries.Count != 0) {
                            string batchTeamEntries = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamEntries.Keys))
                                batchTeamEntries += team.ToString() + ';' + _teamEntries[team].ToString() + ';';
                            batchTeamEntries = batchTeamEntries.Remove(batchTeamEntries.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_ENTRIES, batchTeamEntries, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamPasses.Count != 0) {
                            string batchTeamPasses = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPasses.Keys))
                                batchTeamPasses += team.ToString() + ';' + _teamPasses[team].ToString() + ';';
                            batchTeamPasses = batchTeamPasses.Remove(batchTeamPasses.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_PASSES, batchTeamPasses, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamPossessionTime.Count != 0) {
                            string batchTeamPossessionTime = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPossessionTime.Keys))
                                batchTeamPossessionTime += team.ToString() + ';' + _teamPossessionTime[team].ToString("F2") + ';';
                            batchTeamPossessionTime = batchTeamPossessionTime.Remove(batchTeamPossessionTime.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_POSSESSION_TIME, batchTeamPossessionTime, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamPuckBattleWins.Count != 0) {
                            string batchTeamPuckBattleWins = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPuckBattleWins.Keys))
                                batchTeamPuckBattleWins += team.ToString() + ';' + _teamPuckBattleWins[team].ToString() + ';';
                            batchTeamPuckBattleWins = batchTeamPuckBattleWins.Remove(batchTeamPuckBattleWins.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_PUCK_BATTLE_WINS, batchTeamPuckBattleWins, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        if (_teamPuckBattleLosses.Count != 0) {
                            string batchTeamPuckBattleLosses = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPuckBattleLosses.Keys))
                                batchTeamPuckBattleLosses += team.ToString() + ';' + _teamPuckBattleLosses[team].ToString() + ';';
                            batchTeamPuckBattleLosses = batchTeamPuckBattleLosses.Remove(batchTeamPuckBattleLosses.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_PUCK_BATTLE_LOSSES, batchTeamPuckBattleLosses, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        // Send stick saves
                        if (_stickSaves.Count != 0) {
                            string batchStickSaves = "";
                            foreach (string key in new List<string>(_stickSaves.Keys))
                                batchStickSaves += key + ';' + _stickSaves[key].ToString() + ';';
                            batchStickSaves = batchStickSaves.Remove(batchStickSaves.Length - 1);
                            NetworkCommunication.SendData(BATCH_STICK_SAVES, batchStickSaves, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        // Send body saves
                        if (_bodySaves.Count != 0) {
                            string batchBodySaves = "";
                            foreach (string key in new List<string>(_bodySaves.Keys))
                                batchBodySaves += key + ';' + _bodySaves[key].ToString() + ';';
                            batchBodySaves = batchBodySaves.Remove(batchBodySaves.Length - 1);
                            NetworkCommunication.SendData(BATCH_BODY_SAVES, batchBodySaves, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        // Send home plate saves
                        if (_homePlateSaves.Count != 0) {
                            string batchHomePlateSaves = "";
                            foreach (string key in new List<string>(_homePlateSaves.Keys))
                                batchHomePlateSaves += key + ';' + _homePlateSaves[key].ToString() + ';';
                            batchHomePlateSaves = batchHomePlateSaves.Remove(batchHomePlateSaves.Length - 1);
                            NetworkCommunication.SendData(BATCH_HOME_PLATE_SAVES, batchHomePlateSaves, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        // Send home plate shots faced
                        if (_homePlateShots.Count != 0) {
                            string batchHomePlateShots = "";
                            foreach (string key in new List<string>(_homePlateShots.Keys))
                                batchHomePlateShots += key + ';' + _homePlateShots[key].ToString() + ';';
                            batchHomePlateShots = batchHomePlateShots.Remove(batchHomePlateShots.Length - 1);
                            NetworkCommunication.SendData(BATCH_HOME_PLATE_SHOTS_FACED, batchHomePlateShots, clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        }

                        foreach (int key in new List<int>(_stars.Keys))
                            NetworkCommunication.SendData(STAR, $"{_stars[key]};{key}", clientId, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig);
                        break;

                    /*case RESET_SOG:
                        if (dataStr != "1")
                            break;

                        Client_ResetSOG();
                        break;

                    case RESET_SAVEPERC:
                        if (dataStr != "1")
                            break;

                        Client_ResetSavePerc();
                        break;*/

                    case RESET_ALL:
                        if (dataStr != "1")
                            break;

                        Client_ResetSOG();
                        Client_ResetSavePerc();
                        Client_ResetPasses();
                        Client_ResetBlocks();
                        Client_ResetHits();
                        Client_ResetTakeaways();
                        Client_ResetTurnovers();
                        Client_ResetExits();
                        Client_ResetEntries();
                        Client_ResetShotAttempts();
                        Client_ResetHomePlateSogs();
                        Client_ResetPuckTouches();
                        Client_ResetPossessionTime();
                        Client_ResetPuckBattles();
                        Client_ResetStickSaves();
                        Client_ResetBodySaves();
                        Client_ResetHomePlateSaves();
                        Client_ResetHomePlateShots();
                        Client_ResetTeamStats();
                        Client_RefreshTooltipsAndLabelsAfterReset();
                        break;

                    case BATCH_SOG:
                        string[] splittedSOG = dataStr.Split(';');
                        string steamIdSOG = "";
                        for (int i = 0; i < splittedSOG.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdSOG = splittedSOG[i];
                            else // SOG
                                ReceiveData_SOG(steamIdSOG, splittedSOG[i]);
                        }
                        break;

                    case BATCH_SAVEPERC:
                        string[] splittedSavePerc = dataStr.Split(';');
                        string steamIdSavePerc = "";
                        for (int i = 0; i < splittedSavePerc.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdSavePerc = splittedSavePerc[i];
                            else // SavePerc
                                ReceiveData_SavePerc(steamIdSavePerc, splittedSavePerc[i]);
                        }
                        break;

                    case STAR:
                        string[] splittedStar = dataStr.Split(';');
                        string steamIdStar = "";
                        for (int i = 0; i < splittedStar.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdStar = splittedStar[i];
                            else // Star index
                                ReceiveData_Star(steamIdStar, splittedStar[i]);
                        }
                        break;

                    case BATCH_BLOCK:
                        string[] splittedBlocks = dataStr.Split(';');
                        string steamIdBlock = "";
                        for (int i = 0; i < splittedBlocks.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdBlock = splittedBlocks[i];
                            else // Blocks
                                ReceiveData_Block(steamIdBlock, splittedBlocks[i]);
                        }
                        break;

                    case BATCH_HIT:
                        string[] splittedHits = dataStr.Split(';');
                        string steamIdHit = "";
                        for (int i = 0; i < splittedHits.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdHit = splittedHits[i];
                            else // Hits
                                ReceiveData_Hit(steamIdHit, splittedHits[i]);
                        }
                        break;

                    case BATCH_TURNOVER:
                        string[] splittedTurnovers = dataStr.Split(';');
                        string steamIdTurnover = "";
                        for (int i = 0; i < splittedTurnovers.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdTurnover = splittedTurnovers[i];
                            else // Turnovers
                                ReceiveData_Turnover(steamIdTurnover, splittedTurnovers[i]);
                        }
                        break;

                    case BATCH_TAKEAWAY:
                        string[] splittedTakeaways = dataStr.Split(';');
                        string steamIdTakeaway = "";
                        for (int i = 0; i < splittedTakeaways.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdTakeaway = splittedTakeaways[i];
                            else // Takeaways
                                ReceiveData_Takeaway(steamIdTakeaway, splittedTakeaways[i]);
                        }
                        break;

                    case BATCH_PASS:
                        string[] splittedPasses = dataStr.Split(';');
                        string steamIdPass = "";
                        for (int i = 0; i < splittedPasses.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdPass = splittedPasses[i];
                            else // Passes
                                ReceiveData_Pass(steamIdPass, splittedPasses[i]);
                        }
                        break;

                    case BATCH_PUCK_TOUCH:
                        string[] splittedPuckTouches = dataStr.Split(';');
                        string steamIdPuckTouch = "";
                        for (int i = 0; i < splittedPuckTouches.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdPuckTouch = splittedPuckTouches[i];
                            else // Puck Touches
                                ReceiveData_PuckTouch(steamIdPuckTouch, splittedPuckTouches[i]);
                        }
                        break;

                    case BATCH_EXIT:
                        string[] splittedExits = dataStr.Split(';');
                        string steamIdExit = "";
                        for (int i = 0; i < splittedExits.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdExit = splittedExits[i];
                            else // Exits
                                ReceiveData_Exit(steamIdExit, splittedExits[i]);
                        }
                        break;

                    case BATCH_ENTRY:
                        string[] splittedEntries = dataStr.Split(';');
                        string steamIdEntry = "";
                        for (int i = 0; i < splittedEntries.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdEntry = splittedEntries[i];
                            else // Entries
                                ReceiveData_Entry(steamIdEntry, splittedEntries[i]);
                        }
                        break;

                    case BATCH_POSSESSION_TIME:
                        string[] splittedPossessionTime = dataStr.Split(';');
                        string steamIdPossessionTime = "";
                        for (int i = 0; i < splittedPossessionTime.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdPossessionTime = splittedPossessionTime[i];
                            else // Possession Time
                                ReceiveData_PossessionTime(steamIdPossessionTime, splittedPossessionTime[i]);
                        }
                        break;

                    case BATCH_PUCK_BATTLE_WINS:
                        string[] splittedPuckBattleWins = dataStr.Split(';');
                        string steamIdPuckBattleWins = "";
                        for (int i = 0; i < splittedPuckBattleWins.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdPuckBattleWins = splittedPuckBattleWins[i];
                            else // Puck Battle Wins
                                ReceiveData_PuckBattleWins(steamIdPuckBattleWins, splittedPuckBattleWins[i]);
                        }
                        break;

                    case BATCH_PUCK_BATTLE_LOSSES:
                        string[] splittedPuckBattleLosses = dataStr.Split(';');
                        string steamIdPuckBattleLosses = "";
                        for (int i = 0; i < splittedPuckBattleLosses.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdPuckBattleLosses = splittedPuckBattleLosses[i];
                            else // Puck Battle Losses
                                ReceiveData_PuckBattleLosses(steamIdPuckBattleLosses, splittedPuckBattleLosses[i]);
                        }
                        break;

                    case BATCH_SHOT_ATTEMPTS:
                        string[] splittedShotAttempts = dataStr.Split(';');
                        string steamIdShotAttempts = "";
                        for (int i = 0; i < splittedShotAttempts.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdShotAttempts = splittedShotAttempts[i];
                            else // Shot Attempts
                                ReceiveData_ShotAttempts(steamIdShotAttempts, splittedShotAttempts[i]);
                        }
                        break;


                    case BATCH_TEAM_SHOTS:
                        string[] splittedTeamShots = dataStr.Split(';');
                        string teamStrShots = "";
                        for (int i = 0; i < splittedTeamShots.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrShots = splittedTeamShots[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrShots, out PlayerTeam teamShots)) // Shots
                                ReceiveData_TeamShots(teamShots, splittedTeamShots[i]);
                        }
                        break;

                    case BATCH_TEAM_SHOT_ATTEMPTS:
                        string[] splittedTeamShotAttempts = dataStr.Split(';');
                        string teamStrShotAttempts = "";
                        for (int i = 0; i < splittedTeamShotAttempts.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrShotAttempts = splittedTeamShotAttempts[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrShotAttempts, out PlayerTeam teamShotAttempts)) // Shot Attempts
                                ReceiveData_TeamShotAttempts(teamShotAttempts, splittedTeamShotAttempts[i]);
                        }
                        break;


                    case BATCH_HOME_PLATE_SOGS:
                        string[] splittedHomePlateSogs = dataStr.Split(';');
                        string steamIdHomePlateSogs = "";
                        for (int i = 0; i < splittedHomePlateSogs.Length; i++) {
                            if (i % 2 == 0) // SteamId
                                steamIdHomePlateSogs = splittedHomePlateSogs[i];
                            else // Home Plate SOGs
                                ReceiveData_HomePlateSogs(steamIdHomePlateSogs, splittedHomePlateSogs[i]);
                        }
                        break;

                    case BATCH_TEAM_HOME_PLATE_SOGS:
                        string[] splittedTeamHomePlateSogs = dataStr.Split(';');
                        string teamStrHomePlateSogs = "";
                        for (int i = 0; i < splittedTeamHomePlateSogs.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrHomePlateSogs = splittedTeamHomePlateSogs[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrHomePlateSogs, out PlayerTeam teamHomePlateSogs)) // Home Plate SOGs
                                ReceiveData_TeamHomePlateSogs(teamHomePlateSogs, splittedTeamHomePlateSogs[i]);
                        }
                        break;

                    case BATCH_TEAM_PASSES:
                        string[] splittedTeamPasses = dataStr.Split(';');
                        string teamStrPasses = "";
                        for (int i = 0; i < splittedTeamPasses.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrPasses = splittedTeamPasses[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrPasses, out PlayerTeam teamPasses)) // Passes
                                ReceiveData_TeamPasses(teamPasses, splittedTeamPasses[i]);
                        }
                        break;

                    case BATCH_TEAM_FACEOFF_WINS:
                        string[] splittedTeamFaceoffWins = dataStr.Split(';');
                        string teamStrFaceoffWins = "";
                        for (int i = 0; i < splittedTeamFaceoffWins.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrFaceoffWins = splittedTeamFaceoffWins[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrFaceoffWins, out PlayerTeam teamFaceoffWins)) // Faceoff Wins
                                ReceiveData_TeamFaceoffWins(teamFaceoffWins, splittedTeamFaceoffWins[i]);
                        }
                        break;

                    case BATCH_TEAM_FACEOFF_TOTAL:
                        string[] splittedTeamFaceoffTotal = dataStr.Split(';');
                        string teamStrFaceoffTotal = "";
                        for (int i = 0; i < splittedTeamFaceoffTotal.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrFaceoffTotal = splittedTeamFaceoffTotal[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrFaceoffTotal, out PlayerTeam teamFaceoffTotal)) // Faceoff Total
                                ReceiveData_TeamFaceoffTotal(teamFaceoffTotal, splittedTeamFaceoffTotal[i]);
                        }
                        break;

                    case BATCH_TEAM_TAKEAWAYS:
                        string[] splittedTeamTakeaways = dataStr.Split(';');
                        string teamStrTakeaways = "";
                        for (int i = 0; i < splittedTeamTakeaways.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrTakeaways = splittedTeamTakeaways[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrTakeaways, out PlayerTeam teamTakeaways)) // Takeaways
                                ReceiveData_TeamTakeaways(teamTakeaways, splittedTeamTakeaways[i]);
                        }
                        break;

                    case BATCH_TEAM_TURNOVERS:
                        string[] splittedTeamTurnovers = dataStr.Split(';');
                        string teamStrTurnovers = "";
                        for (int i = 0; i < splittedTeamTurnovers.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrTurnovers = splittedTeamTurnovers[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrTurnovers, out PlayerTeam teamTurnovers)) // Turnovers
                                ReceiveData_TeamTurnovers(teamTurnovers, splittedTeamTurnovers[i]);
                        }
                        break;

                    case BATCH_TEAM_EXITS:
                        string[] splittedTeamExits = dataStr.Split(';');
                        string teamStrExits = "";
                        for (int i = 0; i < splittedTeamExits.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrExits = splittedTeamExits[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrExits, out PlayerTeam teamExits)) // Exits
                                ReceiveData_TeamExits(teamExits, splittedTeamExits[i]);
                        }
                        break;

                    case BATCH_TEAM_ENTRIES:
                        string[] splittedTeamEntries = dataStr.Split(';');
                        string teamStrEntries = "";
                        for (int i = 0; i < splittedTeamEntries.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrEntries = splittedTeamEntries[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrEntries, out PlayerTeam teamEntries)) // Entries
                                ReceiveData_TeamEntries(teamEntries, splittedTeamEntries[i]);
                        }
                        break;

                    case BATCH_TEAM_POSSESSION_TIME:
                        string[] splittedTeamPossessionTime = dataStr.Split(';');
                        string teamStrPossessionTime = "";
                        for (int i = 0; i < splittedTeamPossessionTime.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrPossessionTime = splittedTeamPossessionTime[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrPossessionTime, out PlayerTeam teamPossessionTime)) // Possession Time
                                ReceiveData_TeamPossessionTime(teamPossessionTime, splittedTeamPossessionTime[i]);
                        }
                        break;

                    case BATCH_TEAM_PUCK_BATTLE_WINS:
                        string[] splittedTeamPuckBattleWins = dataStr.Split(';');
                        string teamStrPuckBattleWins = "";
                        for (int i = 0; i < splittedTeamPuckBattleWins.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrPuckBattleWins = splittedTeamPuckBattleWins[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrPuckBattleWins, out PlayerTeam teamPuckBattleWins)) // Wins
                                ReceiveData_TeamPuckBattleWins(teamPuckBattleWins, splittedTeamPuckBattleWins[i]);
                        }
                        break;

                    case BATCH_TEAM_PUCK_BATTLE_LOSSES:
                        string[] splittedTeamPuckBattleLosses = dataStr.Split(';');
                        string teamStrPuckBattleLosses = "";
                        for (int i = 0; i < splittedTeamPuckBattleLosses.Length; i++) {
                            if (i % 2 == 0) // Team
                                teamStrPuckBattleLosses = splittedTeamPuckBattleLosses[i];
                            else if (Enum.TryParse<PlayerTeam>(teamStrPuckBattleLosses, out PlayerTeam teamPuckBattleLosses)) // Losses
                                ReceiveData_TeamPuckBattleLosses(teamPuckBattleLosses, splittedTeamPuckBattleLosses[i]);
                        }
                        break;

                    case BATCH_STICK_SAVES:
                        string[] splittedStickSaves = dataStr.Split(';');
                        for (int i = 0; i < splittedStickSaves.Length; i += 2) {
                            if (i + 1 < splittedStickSaves.Length) {
                                string playerSteamId = splittedStickSaves[i];
                                string value = splittedStickSaves[i + 1];
                                ReceiveData_StickSaves(playerSteamId, value);
                            }
                        }
                        break;

                    case BATCH_BODY_SAVES:
                        string[] splittedBodySaves = dataStr.Split(';');
                        for (int i = 0; i < splittedBodySaves.Length; i += 2) {
                            if (i + 1 < splittedBodySaves.Length) {
                                string playerSteamId = splittedBodySaves[i];
                                string value = splittedBodySaves[i + 1];
                                ReceiveData_BodySaves(playerSteamId, value);
                            }
                        }
                        break;

                    case BATCH_HOME_PLATE_SAVES:
                        string[] splittedHomePlateSaves = dataStr.Split(';');
                        for (int i = 0; i < splittedHomePlateSaves.Length; i += 2) {
                            if (i + 1 < splittedHomePlateSaves.Length) {
                                string playerSteamId = splittedHomePlateSaves[i];
                                string value = splittedHomePlateSaves[i + 1];
                                ReceiveData_HomePlateSaves(playerSteamId, value);
                            }
                        }
                        break;

                    case BATCH_HOME_PLATE_SHOTS_FACED:
                        string[] splittedHomePlateShotsFaced = dataStr.Split(';');
                        for (int i = 0; i < splittedHomePlateShotsFaced.Length; i += 2) {
                            if (i + 1 < splittedHomePlateShotsFaced.Length) {
                                string playerSteamId = splittedHomePlateShotsFaced[i];
                                string value = splittedHomePlateShotsFaced[i + 1];
                                ReceiveData_HomePlateShotsFaced(playerSteamId, value);
                            }
                        }
                        break;

                    default:
                        // Fallback: handle corrupted RESET_ALL when normalization missed it
                        bool fromServer = clientId == NetworkManager.ServerClientId;
                        bool shortPayload = dataStr != null && dataStr.Length <= 4 && !dataStr.Contains(';');
                        if (fromServer && shortPayload && dataName != null && dataName.StartsWith(Constants.MOD_NAME)) {
                            Client_ResetSOG();
                            Client_ResetSavePerc();
                            Client_ResetPasses();
                            Client_ResetBlocks();
                            Client_ResetHits();
                            Client_ResetTakeaways();
                            Client_ResetTurnovers();
                            Client_ResetExits();
                            Client_ResetEntries();
                            Client_ResetShotAttempts();
                            Client_ResetHomePlateSogs();
                            Client_ResetPuckTouches();
                            Client_ResetPossessionTime();
                            Client_ResetPuckBattles();
                            Client_ResetStickSaves();
                            Client_ResetBodySaves();
                            Client_ResetHomePlateSaves();
                            Client_ResetHomePlateShots();
                            Client_ResetTeamStats();
                            Client_RefreshTooltipsAndLabelsAfterReset();
                        }
                        else if (dataName.StartsWith(Codebase.Constants.SOG)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.SOG, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_SOG(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.SAVEPERC)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.SAVEPERC, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_SavePerc(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.BLOCK)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.BLOCK, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_Block(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.HIT)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.HIT, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_Hit(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.TURNOVER)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.TURNOVER, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_Turnover(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.TAKEAWAY)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.TAKEAWAY, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_Takeaway(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.PASS)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.PASS, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_Pass(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.PUCK_TOUCH)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.PUCK_TOUCH, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_PuckTouch(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.EXIT)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.EXIT, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_Exit(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.ENTRY)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.ENTRY, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_Entry(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.POSSESSION_TIME)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.POSSESSION_TIME, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_PossessionTime(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.PUCK_BATTLE_WINS)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.PUCK_BATTLE_WINS, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_PuckBattleWins(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.PUCK_BATTLE_LOSSES)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.PUCK_BATTLE_LOSSES, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;

                            ReceiveData_PuckBattleLosses(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.SHOT_ATTEMPTS)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.SHOT_ATTEMPTS, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;
                            ReceiveData_ShotAttempts(playerSteamId, dataStr);
                        }


                        if (dataName.StartsWith(Codebase.Constants.HOME_PLATE_SOGS)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.HOME_PLATE_SOGS, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;
                            ReceiveData_HomePlateSogs(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.STICK_SAVES)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.STICK_SAVES, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;
                            ReceiveData_StickSaves(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.BODY_SAVES)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.BODY_SAVES, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;
                            ReceiveData_BodySaves(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.HOME_PLATE_SAVES)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.HOME_PLATE_SAVES, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;
                            ReceiveData_HomePlateSaves(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.HOME_PLATE_SHOTS_FACED)) {
                            string playerSteamId = dataName.Replace(Codebase.Constants.HOME_PLATE_SHOTS_FACED, "");
                            if (string.IsNullOrEmpty(playerSteamId))
                                return;
                            ReceiveData_HomePlateShotsFaced(playerSteamId, dataStr);
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_SHOTS)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_SHOTS, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamShots(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_SHOT_ATTEMPTS)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_SHOT_ATTEMPTS, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamShotAttempts(team, dataStr);
                            }
                        }


                        if (dataName.StartsWith(Codebase.Constants.TEAM_HOME_PLATE_SOGS)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_HOME_PLATE_SOGS, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamHomePlateSogs(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_PASSES)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_PASSES, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamPasses(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_TAKEAWAYS)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_TAKEAWAYS, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamTakeaways(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_TURNOVERS)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_TURNOVERS, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamTurnovers(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_EXITS)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_EXITS, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamExits(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_ENTRIES)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_ENTRIES, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamEntries(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_POSSESSION_TIME)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_POSSESSION_TIME, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamPossessionTime(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_PUCK_BATTLE_WINS)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_PUCK_BATTLE_WINS, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamPuckBattleWins(team, dataStr);
                            }
                        }

                        if (dataName.StartsWith(Codebase.Constants.TEAM_PUCK_BATTLE_LOSSES)) {
                            string teamStr = dataName.Replace(Codebase.Constants.TEAM_PUCK_BATTLE_LOSSES, "");
                            if (string.IsNullOrEmpty(teamStr))
                                return;
                            if (Enum.TryParse<PlayerTeam>(teamStr, out PlayerTeam team)) {
                                ReceiveData_TeamPuckBattleLosses(team, dataStr);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in ReceiveData.\n{ex}", ModServerConfig);
            }
        }

        private static void ReceiveData_SOG(string playerSteamId, string dataStr) {
            int sog = int.Parse(dataStr);

            if (_sog.TryGetValue(playerSteamId, out int _)) {
                _sog[playerSteamId] = sog;
                Player currentPlayer = PlayerManager.Instance.GetPlayerBySteamId(playerSteamId);
                if (currentPlayer != null && currentPlayer && !PlayerFunc.IsGoalie(currentPlayer))
                    _sogLabels[playerSteamId].text = sog.ToString();
            }
            else
                _sog.Add(playerSteamId, sog);
        }

        private static void ReceiveData_SavePerc(string playerSteamId, string dataStr) {
            string[] dataStrSplitted = SystemFunc.RemoveWhitespace(dataStr.Replace("(", "").Replace(")", "")).Split(',');
            int saves = int.Parse(dataStrSplitted[0]);
            int shots = int.Parse(dataStrSplitted[1]);

            if (_savePerc.TryGetValue(playerSteamId, out var _)) {
                _savePerc[playerSteamId] = (saves, shots);
                Player currentPlayer = PlayerManager.Instance.GetPlayerBySteamId(playerSteamId);
                if (currentPlayer && PlayerFunc.IsGoalie(currentPlayer))
                    _sogLabels[playerSteamId].text = GetGoalieSavePerc(saves, shots);
            }
            else
                _savePerc.Add(playerSteamId, (saves, shots));
        }

        private static void ReceiveData_Star(string playerSteamId, string dataStr) {
            int starIndex = int.Parse(dataStr);

            if (_stars.TryGetValue(starIndex, out string _))
                _stars[starIndex] = playerSteamId;
            else
                _stars.Add(starIndex, playerSteamId);
        }

        private static void ReceiveData_Block(string playerSteamId, string dataStr) {
            int blocks = int.Parse(dataStr);

            if (_blocks.TryGetValue(playerSteamId, out int _)) {
                _blocks[playerSteamId] = blocks;
            }
            else {
                _blocks.Add(playerSteamId, blocks);
            }
        }

        private static void ReceiveData_Hit(string playerSteamId, string dataStr) {
            int hits = int.Parse(dataStr);

            if (_hits.TryGetValue(playerSteamId, out int _)) {
                _hits[playerSteamId] = hits;
            }
            else {
                _hits.Add(playerSteamId, hits);
            }
        }

        private static void ReceiveData_Turnover(string playerSteamId, string dataStr) {
            int turnovers = int.Parse(dataStr);

            if (_turnovers.TryGetValue(playerSteamId, out int _)) {
                _turnovers[playerSteamId] = turnovers;
            }
            else {
                _turnovers.Add(playerSteamId, turnovers);
            }
        }

        private static void ReceiveData_Takeaway(string playerSteamId, string dataStr) {
            int takeaways = int.Parse(dataStr);

            if (_takeaways.TryGetValue(playerSteamId, out int _)) {
                _takeaways[playerSteamId] = takeaways;
            }
            else {
                _takeaways.Add(playerSteamId, takeaways);
            }
        }

        private static void ReceiveData_Pass(string playerSteamId, string dataStr) {
            int passes = int.Parse(dataStr);

            if (_passes.TryGetValue(playerSteamId, out int _)) {
                _passes[playerSteamId] = passes;
            }
            else {
                _passes.Add(playerSteamId, passes);
            }
        }

        private static void ReceiveData_PuckTouch(string playerSteamId, string dataStr) {
            int puckTouches = int.Parse(dataStr);

            if (_puckTouches.TryGetValue(playerSteamId, out int _)) {
                _puckTouches[playerSteamId] = puckTouches;
            }
            else {
                _puckTouches.Add(playerSteamId, puckTouches);
            }
        }

        private static void ReceiveData_Exit(string playerSteamId, string dataStr) {
            int exits = int.Parse(dataStr);

            if (_exits.TryGetValue(playerSteamId, out int _)) {
                _exits[playerSteamId] = exits;
            }
            else {
                _exits.Add(playerSteamId, exits);
            }
        }

        private static void ReceiveData_Entry(string playerSteamId, string dataStr) {
            int entries = int.Parse(dataStr);

            if (_entries.TryGetValue(playerSteamId, out int _)) {
                _entries[playerSteamId] = entries;
            }
            else {
                _entries.Add(playerSteamId, entries);
            }
        }

        private static void ReceiveData_PossessionTime(string playerSteamId, string dataStr) {
            double possessionTime = double.Parse(dataStr);

            if (_possessionTimeSeconds.TryGetValue(playerSteamId, out double _)) {
                _possessionTimeSeconds[playerSteamId] = possessionTime;
            }
            else {
                _possessionTimeSeconds.Add(playerSteamId, possessionTime);
            }
        }

        private static void ReceiveData_PuckBattleWins(string playerSteamId, string dataStr) {
            int wins = int.Parse(dataStr);

            if (_puckBattleWins.TryGetValue(playerSteamId, out int _)) {
                _puckBattleWins[playerSteamId] = wins;
            }
            else {
                _puckBattleWins.Add(playerSteamId, wins);
            }
        }

        private static void ReceiveData_PuckBattleLosses(string playerSteamId, string dataStr) {
            int losses = int.Parse(dataStr);

            if (_puckBattleLosses.TryGetValue(playerSteamId, out int _)) {
                _puckBattleLosses[playerSteamId] = losses;
            }
            else {
                _puckBattleLosses.Add(playerSteamId, losses);
            }
        }

        private static void ReceiveData_ShotAttempts(string playerSteamId, string dataStr) {
            int attempts = int.Parse(dataStr);
            if (_shotAttempts.TryGetValue(playerSteamId, out int _))
                _shotAttempts[playerSteamId] = attempts;
            else
                _shotAttempts.Add(playerSteamId, attempts);
        }


        private static void ReceiveData_HomePlateSogs(string playerSteamId, string dataStr) {
            int homePlateSog = int.Parse(dataStr);
            if (_homePlateSogs.TryGetValue(playerSteamId, out int _))
                _homePlateSogs[playerSteamId] = homePlateSog;
            else
                _homePlateSogs.Add(playerSteamId, homePlateSog);
        }

        private static void ReceiveData_TeamShots(PlayerTeam team, string dataStr) {
            int shots = int.Parse(dataStr);
            if (_teamShots.TryGetValue(team, out int _)) {
                _teamShots[team] = shots;
            } else {
                _teamShots.Add(team, shots);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamShotAttempts(PlayerTeam team, string dataStr) {
            int attempts = int.Parse(dataStr);
            if (_teamShotAttempts.TryGetValue(team, out int _))
                _teamShotAttempts[team] = attempts;
            else
                _teamShotAttempts.Add(team, attempts);
            WriteClientTeamStatsToFile();
        }


        private static void ReceiveData_TeamHomePlateSogs(PlayerTeam team, string dataStr) {
            int homePlateSog = int.Parse(dataStr);
            if (_teamHomePlateSogs.TryGetValue(team, out int _))
                _teamHomePlateSogs[team] = homePlateSog;
            else
                _teamHomePlateSogs.Add(team, homePlateSog);
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamPasses(PlayerTeam team, string dataStr) {
            int passes = int.Parse(dataStr);
            if (_teamPasses.TryGetValue(team, out int _)) {
                _teamPasses[team] = passes;
            } else {
                _teamPasses.Add(team, passes);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamFaceoffWins(PlayerTeam team, string dataStr) {
            int wins = int.Parse(dataStr);
            if (_teamFaceoffWins.TryGetValue(team, out int _)) {
                _teamFaceoffWins[team] = wins;
            } else {
                _teamFaceoffWins.Add(team, wins);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamFaceoffTotal(PlayerTeam team, string dataStr) {
            int total = int.Parse(dataStr);
            if (_teamFaceoffTotal.TryGetValue(team, out int _)) {
                _teamFaceoffTotal[team] = total;
            } else {
                _teamFaceoffTotal.Add(team, total);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamTakeaways(PlayerTeam team, string dataStr) {
            int takeaways = int.Parse(dataStr);
            if (_teamTakeaways.TryGetValue(team, out int _)) {
                _teamTakeaways[team] = takeaways;
            } else {
                _teamTakeaways.Add(team, takeaways);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamTurnovers(PlayerTeam team, string dataStr) {
            int turnovers = int.Parse(dataStr);
            if (_teamTurnovers.TryGetValue(team, out int _)) {
                _teamTurnovers[team] = turnovers;
            } else {
                _teamTurnovers.Add(team, turnovers);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamExits(PlayerTeam team, string dataStr) {
            int exits = int.Parse(dataStr);
            if (_teamExits.TryGetValue(team, out int _)) {
                _teamExits[team] = exits;
            } else {
                _teamExits.Add(team, exits);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamEntries(PlayerTeam team, string dataStr) {
            int entries = int.Parse(dataStr);
            if (_teamEntries.TryGetValue(team, out int _)) {
                _teamEntries[team] = entries;
            } else {
                _teamEntries.Add(team, entries);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamPossessionTime(PlayerTeam team, string dataStr) {
            double possessionTime = double.Parse(dataStr);
            if (_teamPossessionTime.TryGetValue(team, out double _)) {
                _teamPossessionTime[team] = possessionTime;
            } else {
                _teamPossessionTime.Add(team, possessionTime);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamPuckBattleWins(PlayerTeam team, string dataStr) {
            int wins = int.Parse(dataStr);
            if (_teamPuckBattleWins.TryGetValue(team, out int _)) {
                _teamPuckBattleWins[team] = wins;
            } else {
                _teamPuckBattleWins.Add(team, wins);
            }
            WriteClientTeamStatsToFile();
        }

        private static void ReceiveData_TeamPuckBattleLosses(PlayerTeam team, string dataStr) {
            int losses = int.Parse(dataStr);
            if (_teamPuckBattleLosses.TryGetValue(team, out int _)) {
                _teamPuckBattleLosses[team] = losses;
            } else {
                _teamPuckBattleLosses.Add(team, losses);
            }
            WriteClientTeamStatsToFile();
        }

        /// <summary>
        /// Writes all team-level stats to a single client-side CSV when LogClientSideStats is enabled.
        /// Includes goals (from game score) and saves (sum of goalie saves per team).
        /// </summary>
        private static void WriteClientTeamStatsToFile() {
            if (!_clientConfig.LogClientSideStats)
                return;
            try {
                int blueGoals = 0, redGoals = 0;
                if (GameManager.Instance != null) {
                    blueGoals = GameManager.Instance.BlueScore;
                    redGoals = GameManager.Instance.RedScore;
                }
                // Sum saves from all positions (so pulled goalie / backup goalie / skater saves count)
                int blueSaves = 0, redSaves = 0;
                if (PlayerManager.Instance != null) {
                    foreach (Player p in PlayerManager.Instance.GetPlayers()) {
                        if (p == null || !p)
                            continue;
                        string steamId = p.SteamId.Value.Value;
                        if (!_savePerc.TryGetValue(steamId, out var sp))
                            continue;
                        if (p.Team == PlayerTeam.Blue)
                            blueSaves += sp.Saves;
                        else if (p.Team == PlayerTeam.Red)
                            redSaves += sp.Saves;
                    }
                }
                string statsFolderPath = Path.Combine(Path.GetFullPath("."), "stats");
                if (!Directory.Exists(statsFolderPath))
                    Directory.CreateDirectory(statsFolderPath);
                int blueShots = _teamShots.TryGetValue(PlayerTeam.Blue, out int bs) ? bs : 0;
                int redShots = _teamShots.TryGetValue(PlayerTeam.Red, out int rs) ? rs : 0;
                int bluePasses = _teamPasses.TryGetValue(PlayerTeam.Blue, out int bp) ? bp : 0;
                int redPasses = _teamPasses.TryGetValue(PlayerTeam.Red, out int rp) ? rp : 0;
                int blueFaceoffWins = _teamFaceoffWins.TryGetValue(PlayerTeam.Blue, out int bfw) ? bfw : 0;
                int redFaceoffWins = _teamFaceoffWins.TryGetValue(PlayerTeam.Red, out int rfw) ? rfw : 0;
                int blueExits = _teamExits.TryGetValue(PlayerTeam.Blue, out int be) ? be : 0;
                int redExits = _teamExits.TryGetValue(PlayerTeam.Red, out int re) ? re : 0;
                int blueEntries = _teamEntries.TryGetValue(PlayerTeam.Blue, out int ben) ? ben : 0;
                int redEntries = _teamEntries.TryGetValue(PlayerTeam.Red, out int ren) ? ren : 0;
                double bluePossession = _teamPossessionTime.TryGetValue(PlayerTeam.Blue, out double bpt) ? bpt : 0;
                double redPossession = _teamPossessionTime.TryGetValue(PlayerTeam.Red, out double rpt) ? rpt : 0;
                string path = Path.Combine(statsFolderPath, "PHL_stats_team.csv");
                var sb = new StringBuilder();
                sb.AppendLine("team,shots,shotAttempts,homePlateSogs,passes,faceoffWins,faceoffTotal,takeaways,turnovers,exits,entries,possessionTime,puckBattleWins,puckBattleLosses,goals,saves");
                foreach (PlayerTeam team in new[] { PlayerTeam.Blue, PlayerTeam.Red }) {
                    int shots = team == PlayerTeam.Blue ? blueShots : redShots;
                    int shotAttempts = _teamShotAttempts.TryGetValue(team, out int sa) ? sa : 0;
                    int homePlateSogs = _teamHomePlateSogs.TryGetValue(team, out int hps) ? hps : 0;
                    int passes = team == PlayerTeam.Blue ? bluePasses : redPasses;
                    int faceoffWins = team == PlayerTeam.Blue ? blueFaceoffWins : redFaceoffWins;
                    int faceoffTotal = _teamFaceoffTotal.TryGetValue(team, out int ft) ? ft : 0;
                    int takeaways = _teamTakeaways.TryGetValue(team, out int tk) ? tk : 0;
                    int turnovers = _teamTurnovers.TryGetValue(team, out int to) ? to : 0;
                    int exits = team == PlayerTeam.Blue ? blueExits : redExits;
                    int entries = team == PlayerTeam.Blue ? blueEntries : redEntries;
                    double possessionTime = team == PlayerTeam.Blue ? bluePossession : redPossession;
                    int puckBattleWins = _teamPuckBattleWins.TryGetValue(team, out int pbw) ? pbw : 0;
                    int puckBattleLosses = _teamPuckBattleLosses.TryGetValue(team, out int pbl) ? pbl : 0;
                    int goals = team == PlayerTeam.Blue ? blueGoals : redGoals;
                    int saves = team == PlayerTeam.Blue ? blueSaves : redSaves;
                    sb.AppendLine($"{team},{shots},{shotAttempts},{homePlateSogs},{passes},{faceoffWins},{faceoffTotal},{takeaways},{turnovers},{exits},{entries},{possessionTime.ToString("R", CultureInfo.InvariantCulture)},{puckBattleWins},{puckBattleLosses},{goals},{saves}");
                }
                File.WriteAllText(path, sb.ToString());
                // Individual text files per stat per team for OBS/overlays
                File.WriteAllText(Path.Combine(statsFolderPath, "bluegoals.txt"), blueGoals.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "redgoals.txt"), redGoals.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "blueshots.txt"), blueShots.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "redshots.txt"), redShots.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "bluefaceoffwins.txt"), blueFaceoffWins.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "redfaceoffwins.txt"), redFaceoffWins.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "blueexits.txt"), blueExits.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "redexits.txt"), redExits.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "blueentries.txt"), blueEntries.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "redentries.txt"), redEntries.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "bluepossession.txt"), bluePossession.ToString("R", CultureInfo.InvariantCulture));
                File.WriteAllText(Path.Combine(statsFolderPath, "redpossession.txt"), redPossession.ToString("R", CultureInfo.InvariantCulture));
                File.WriteAllText(Path.Combine(statsFolderPath, "bluepasses.txt"), bluePasses.ToString());
                File.WriteAllText(Path.Combine(statsFolderPath, "redpasses.txt"), redPasses.ToString());
            }
            catch (Exception ex) {
                Logging.LogError($"Error writing client team stats file: {ex}", _clientConfig);
            }
        }

        private static void ReceiveData_StickSaves(string playerSteamId, string dataStr) {
            int stickSaves = int.Parse(dataStr);
            if (_stickSaves.TryGetValue(playerSteamId, out int _)) {
                _stickSaves[playerSteamId] = stickSaves;
            } else {
                _stickSaves.Add(playerSteamId, stickSaves);
            }
        }

        private static void ReceiveData_BodySaves(string playerSteamId, string dataStr) {
            int bodySaves = int.Parse(dataStr);
            if (_bodySaves.TryGetValue(playerSteamId, out int _)) {
                _bodySaves[playerSteamId] = bodySaves;
            } else {
                _bodySaves.Add(playerSteamId, bodySaves);
            }
        }

        private static void ReceiveData_HomePlateSaves(string playerSteamId, string dataStr) {
            int hpSaves = int.Parse(dataStr);
            if (_homePlateSaves.TryGetValue(playerSteamId, out int _)) {
                _homePlateSaves[playerSteamId] = hpSaves;
            } else {
                _homePlateSaves.Add(playerSteamId, hpSaves);
            }
        }

        private static void ReceiveData_HomePlateShotsFaced(string playerSteamId, string dataStr) {
            int hpShots = int.Parse(dataStr);
            if (_homePlateShots.TryGetValue(playerSteamId, out int _)) {
                _homePlateShots[playerSteamId] = hpShots;
            } else {
                _homePlateShots.Add(playerSteamId, hpShots);
            }
        }

        private static EventCallback<GeometryChangedEvent> _columnLayoutGeometryCallback;

        private static void HideAllPlayerTooltips() {
            foreach (var kvp in new List<KeyValuePair<string, VisualElement>>(_playerTooltips)) {
                if (kvp.Value != null)
                    kvp.Value.style.display = DisplayStyle.None;
            }
            foreach (var kvp in new List<KeyValuePair<PlayerTeam, VisualElement>>(_teamTooltips)) {
                if (kvp.Value != null)
                    kvp.Value.style.display = DisplayStyle.None;
            }
        }

        private static void UnregisterPlayerTooltipPointerHandlers(string playerSteamId) {
            if (!_playerTooltipPointerHandlers.TryGetValue(playerSteamId, out PlayerTooltipPointerHandlers handlers))
                return;
            if (_playerTooltipContainers.TryGetValue(playerSteamId, out VisualElement container) && container != null) {
                if (handlers.Enter != null)
                    container.UnregisterCallback(handlers.Enter);
                if (handlers.Leave != null)
                    container.UnregisterCallback(handlers.Leave);
            }
            _playerTooltipPointerHandlers.Remove(playerSteamId);
        }

        private static void UnregisterScoreboardColumnSync() {
            if (_sogColHeaderRow != null && _columnLayoutGeometryCallback != null)
                _sogColHeaderRow.UnregisterCallback(_columnLayoutGeometryCallback);
            _scoreboardColumnSyncRegistered = false;
            _sogColHeaderRow = null;
            _columnLayoutGeometryCallback = null;
        }

        private static float ResolveGoalsColumnWidth(VisualElement colHeaderRow, VisualElement playerContainer = null) {
            float w = colHeaderRow?.Q("Goals")?.resolvedStyle.width ?? 0;
            if (w <= 1) {
                Label goalsHdrL = colHeaderRow?.Q<Label>("GoalsLabel");
                if (goalsHdrL != null)
                    w = goalsHdrL.resolvedStyle.width;
            }
            if (w <= 1 && playerContainer != null) {
                w = playerContainer.Q("Goals")?.resolvedStyle.width ?? 0;
                if (w <= 1) {
                    Label goalsRowL = playerContainer.Q<Label>("GoalsLabel");
                    if (goalsRowL != null)
                        w = goalsRowL.resolvedStyle.width;
                }
            }
            return w > 1 ? Mathf.Max(w, SOG_COLUMN_MIN_WIDTH) : 0;
        }

        private static void MirrorStatCellLayout(VisualElement targetCell, Label targetLabel, VisualElement refGoalsCell, Label refGoalsLabel, float width) {
            if (targetCell == null)
                return;
            targetCell.style.width = width;
            targetCell.style.minWidth = width;
            targetCell.style.maxWidth = width;
            if (refGoalsCell != null) {
                targetCell.style.flexGrow = refGoalsCell.style.flexGrow;
                targetCell.style.flexShrink = refGoalsCell.style.flexShrink;
                targetCell.style.flexBasis = refGoalsCell.style.flexBasis;
                targetCell.style.alignItems = refGoalsCell.style.alignItems;
                targetCell.style.justifyContent = refGoalsCell.style.justifyContent;
            }
            if (targetLabel != null && refGoalsLabel != null) {
                if (refGoalsLabel.resolvedStyle.fontSize > 0)
                    targetLabel.style.fontSize = refGoalsLabel.resolvedStyle.fontSize;
                // Color is synced on hover via pointer callbacks + StylePlayer — not here, or stale
                // GoalsLabel colors from geometry/layout passes cause multi-second hover lag.
                targetLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
        }

        private static void SyncSogColumnLayout(VisualElement scoreboardContainer) {
            if (scoreboardContainer == null)
                return;
            VisualElement colHeaderRow = _sogColHeaderRow ?? scoreboardContainer.Q("Content")?.Q("Header");
            if (colHeaderRow == null)
                return;

            float w = ResolveGoalsColumnWidth(colHeaderRow);
            if (w <= 1)
                return;

            VisualElement refGoalsHdrCell = colHeaderRow.Q("Goals");
            Label refGoalsHdrLabel = colHeaderRow.Q<Label>("GoalsLabel");
            VisualElement hdrCell = colHeaderRow.Q(SOG_CELL_NAME + "_ColHdr");
            Label hdrLabel = colHeaderRow.Q<Label>(SOG_HEADER_LABEL_NAME);
            if (hdrCell != null)
                MirrorStatCellLayout(hdrCell, hdrLabel, refGoalsHdrCell, refGoalsHdrLabel, w);

            var playerMap = SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(
                typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "playerVisualElementMap");
            if (playerMap == null)
                return;

            foreach (var kvp in playerMap) {
                VisualElement playerContainer = kvp.Value?.Q("Player");
                if (playerContainer == null)
                    continue;
                VisualElement sogCell = playerContainer.Q(SOG_CELL_NAME);
                if (sogCell == null)
                    continue;
                VisualElement refGoalsCell = playerContainer.Q("Goals");
                Label refGoalsLabel = playerContainer.Q<Label>("GoalsLabel");
                Label sogLabel = sogCell.Q<Label>(SOG_LABEL);
                MirrorStatCellLayout(sogCell, sogLabel, refGoalsCell, refGoalsLabel, w);
            }
        }

        private static void RegisterScoreboardColumnSync(VisualElement scoreboardContainer) {
            if (_scoreboardColumnSyncRegistered || scoreboardContainer == null)
                return;
            VisualElement colHeaderRow = scoreboardContainer.Q("Content")?.Q("Header");
            if (colHeaderRow == null)
                return;
            _sogColHeaderRow = colHeaderRow;
            _columnLayoutGeometryCallback = _ => SyncSogColumnLayout(scoreboardContainer);
            colHeaderRow.RegisterCallback(_columnLayoutGeometryCallback);
            _scoreboardColumnSyncRegistered = true;
            colHeaderRow.schedule.Execute(() => SyncSogColumnLayout(scoreboardContainer)).ExecuteLater(50);
        }

        private static Label FindPlayerNameLabel(VisualElement playerContainer, Player player) {
            if (playerContainer == null)
                return null;
            foreach (VisualElement child in playerContainer.Children()) {
                if (child is Label label) {
                    string childName = label.name?.ToLower() ?? "";
                    if (childName.Contains("username") || childName.Contains("name") || childName == "usernamelabel" || childName == "namelabel")
                        return label;
                }
            }
            try {
                var queryResult = playerContainer.Query<Label>("UsernameLabel");
                if (queryResult != null)
                    return queryResult.First();
            } catch { }
            foreach (var label in playerContainer.Query<Label>().ToList()) {
                if (label.text != null && player != null && player && label.text.Contains(player.Username.Value.Value))
                    return label;
            }
            return null;
        }

        /// <summary>
        /// Keeps the custom S/Sv column text color in sync with GoalsLabel.
        /// </summary>
        private static void SyncSogLabelColorForContainer(VisualElement playerContainer) {
            if (playerContainer == null)
                return;
            Label goalsL = playerContainer.Q<Label>("GoalsLabel");
            Label sogL = playerContainer.Q<Label>(SOG_LABEL);
            if (goalsL != null && sogL != null && goalsL.resolvedStyle.color.a > 0)
                sogL.style.color = goalsL.resolvedStyle.color;
        }

        private static void ScheduleSogLabelColorSync(VisualElement scheduleOn, VisualElement playerContainer) {
            if (scheduleOn == null || playerContainer == null)
                return;
            // Same-frame tail — after USS applies hover styles to GoalsLabel.
            scheduleOn.schedule.Execute(() => SyncSogLabelColorForContainer(playerContainer)).ExecuteLater(0);
        }

        private static void RegisterSogHoverColorSync(VisualElement hoverTarget, VisualElement playerContainer, string playerSteamId) {
            if (hoverTarget == null || playerContainer == null || _sogHoverCallbacksRegistered.Contains(playerSteamId))
                return;
            hoverTarget.RegisterCallback<PointerEnterEvent>(_ => ScheduleSogLabelColorSync(hoverTarget, playerContainer));
            hoverTarget.RegisterCallback<PointerLeaveEvent>(_ => ScheduleSogLabelColorSync(hoverTarget, playerContainer));
            _sogHoverCallbacksRegistered.Add(playerSteamId);
        }

        private static void EnsureScoreboardColumnInfrastructure(VisualElement scoreboardContainer, VisualElement headerRow) {
            if (scoreboardContainer == null)
                return;

            if (!_hasUpdatedUIScoreboard.Contains("header")) {
                if (headerRow != null) {
                    VisualElement staleHeader = headerRow.Children().FirstOrDefault(x => x.name == SOG_HEADER_LABEL_NAME);
                    if (staleHeader != null)
                        headerRow.Remove(staleHeader);
                }
                _hasUpdatedUIScoreboard.Add("header");
            }

            if (!_hasUpdatedUIScoreboard.Contains("colHeader")) {
                VisualElement colHeaderRow = scoreboardContainer.Q("Content")?.Q("Header");
                if (colHeaderRow != null) {
                    VisualElement staleColHdr = colHeaderRow.Children().FirstOrDefault(x => x.name == SOG_CELL_NAME + "_ColHdr");
                    if (staleColHdr != null)
                        colHeaderRow.Remove(staleColHdr);

                    VisualElement sogHdrCell = new VisualElement { name = SOG_CELL_NAME + "_ColHdr" };
                    sogHdrCell.pickingMode = PickingMode.Ignore;
                    Label sogHdrLabel = new Label("S/Sv") { name = SOG_HEADER_LABEL_NAME };
                    sogHdrLabel.pickingMode = PickingMode.Ignore;
                    sogHdrLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    sogHdrCell.Add(sogHdrLabel);

                    VisualElement pingHdr = colHeaderRow.Q("Ping");
                    VisualElement goalsHdr = colHeaderRow.Q("Goals");
                    VisualElement hdrAnchor = pingHdr ?? goalsHdr;
                    if (hdrAnchor != null)
                        colHeaderRow.Insert(colHeaderRow.IndexOf(hdrAnchor), sogHdrCell);
                    else
                        colHeaderRow.Add(sogHdrCell);

                    _hasUpdatedUIScoreboard.Add("colHeader");
                }
            }

            RegisterScoreboardColumnSync(scoreboardContainer);
        }

        private static void EnsurePlayerScoreboardRow(Player player, VisualElement rowElement, VisualElement scoreboardContainer) {
            if (player == null || !player || rowElement == null || scoreboardContainer == null)
                return;

            string playerSteamId = player.SteamId.Value.Value;
            if (string.IsNullOrEmpty(playerSteamId))
                return;

            VisualElement playerContainer = rowElement.Q("Player");
            if (playerContainer == null)
                return;

            bool isGoalie = GetPlayerPosition(player) == "G";
            bool sogExists = playerContainer.Q(SOG_CELL_NAME) != null || playerContainer.Children().Any(x => x.name == SOG_LABEL);

            if (!sogExists && !_hasUpdatedUIScoreboard.Contains(playerSteamId)) {
                VisualElement existingSogCell = playerContainer.Children().FirstOrDefault(x => x.name == SOG_CELL_NAME);
                if (existingSogCell != null)
                    playerContainer.Remove(existingSogCell);
                VisualElement existingSogLabel = playerContainer.Children().FirstOrDefault(x => x.name == SOG_LABEL);
                if (existingSogLabel != null)
                    playerContainer.Remove(existingSogLabel);

                VisualElement sogCell = new VisualElement { name = SOG_CELL_NAME };
                sogCell.pickingMode = PickingMode.Ignore;
                Label sogLabel = new Label(isGoalie ? GetGoalieSavePerc(0, 0) : "0") { name = SOG_LABEL };
                sogLabel.pickingMode = PickingMode.Ignore;
                sogLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                sogCell.Add(sogLabel);

                VisualElement pingLabel = playerContainer.Children().FirstOrDefault(x => x.name == "Ping");
                VisualElement goalsAnchor = playerContainer.Children().FirstOrDefault(x => x.name == "Goals");
                VisualElement anchor = pingLabel ?? goalsAnchor;
                if (anchor != null)
                    playerContainer.Insert(playerContainer.IndexOf(anchor), sogCell);
                else
                    playerContainer.Add(sogCell);

                _sogLabels[playerSteamId] = sogLabel;

                if (!_sog.TryGetValue(playerSteamId, out int _))
                    _sog.Add(playerSteamId, 0);

                _hasUpdatedUIScoreboard.Add(playerSteamId);
            } else if (_sogLabels.TryGetValue(playerSteamId, out Label existingLabel) && existingLabel != null) {
                bool wasGoalie = _playerTooltipIsGoalie.TryGetValue(playerSteamId, out bool wg) && wg;
                if (wasGoalie != isGoalie) {
                    if (isGoalie && _savePerc.TryGetValue(playerSteamId, out (int saves, int shots) sp))
                        existingLabel.text = GetGoalieSavePerc(sp.saves, sp.shots);
                    else if (isGoalie)
                        existingLabel.text = GetGoalieSavePerc(0, 0);
                    else
                        existingLabel.text = _sog.TryGetValue(playerSteamId, out int sogVal) ? sogVal.ToString() : "0";
                }
            }

            if (!_savePerc.TryGetValue(playerSteamId, out (int, int) _))
                _savePerc.Add(playerSteamId, (0, 0));
            if (!_hits.TryGetValue(playerSteamId, out int _))
                _hits.Add(playerSteamId, 0);
            if (!_turnovers.TryGetValue(playerSteamId, out int _))
                _turnovers.Add(playerSteamId, 0);
            if (!_takeaways.TryGetValue(playerSteamId, out int _))
                _takeaways.Add(playerSteamId, 0);
            if (!_passes.TryGetValue(playerSteamId, out int _))
                _passes.Add(playerSteamId, 0);

            if (_serverHasResponded) {
                Label nameLabel = FindPlayerNameLabel(playerContainer, player);
                if (nameLabel != null) {
                    nameLabel.pickingMode = PickingMode.Position;
                    playerContainer.pickingMode = PickingMode.Position;
                    bool needsTooltip = !_playerTooltips.ContainsKey(playerSteamId);
                    bool roleChanged = _playerTooltipIsGoalie.TryGetValue(playerSteamId, out bool wasGoalieRole) && wasGoalieRole != isGoalie;
                    if (needsTooltip || roleChanged)
                        SetupPlayerTooltip(nameLabel, playerContainer, playerSteamId, player, nameLabel);
                }
            }

            RegisterSogHoverColorSync(rowElement, playerContainer, playerSteamId);
        }

        private static void RemovePlayerScoreboardRow(string playerSteamId, VisualElement rowElement) {
            VisualElement playerContainer = rowElement?.Q("Player");
            if (playerContainer != null) {
                VisualElement sogCell = playerContainer.Q(SOG_CELL_NAME);
                if (sogCell != null)
                    playerContainer.Remove(sogCell);
                VisualElement legacyLabel = playerContainer.Children().FirstOrDefault(x => x.name == SOG_LABEL);
                if (legacyLabel != null)
                    playerContainer.Remove(legacyLabel);
            }
            UnregisterPlayerTooltipPointerHandlers(playerSteamId);
            _sogHoverCallbacksRegistered.Remove(playerSteamId);
            if (_playerTooltips.TryGetValue(playerSteamId, out VisualElement tooltip)) {
                tooltip.parent?.Remove(tooltip);
                _playerTooltips.Remove(playerSteamId);
            }
            _playerTooltipNameLabels.Remove(playerSteamId);
            _playerTooltipContainers.Remove(playerSteamId);
            _playerTooltipIsGoalie.Remove(playerSteamId);
            _sogLabels.Remove(playerSteamId);
            _hasUpdatedUIScoreboard.Remove(playerSteamId);
        }

        private static void OnClientStylePlayer(UIScoreboard scoreboard, Player player) {
            if (scoreboard == null || player == null || !player)
                return;

            VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), scoreboard, "scoreboard");
            VisualElement headerRow = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), scoreboard, "header");
            if (scoreboardContainer == null)
                return;

            EnsureScoreboardColumnInfrastructure(scoreboardContainer, headerRow);

            // Retry team tooltip setup if the initial handshake attempt failed (e.g. scoreboard was hidden).
            if (_serverHasResponded && !_teamTooltipsSetup && !_teamTooltipSetupScheduled
                && scoreboardContainer.worldBound.width > 0) {
                SetupTeamTooltips(scoreboardContainer);
            }

            var playerMap = SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), scoreboard, "playerVisualElementMap");
            if (playerMap != null && playerMap.TryGetValue(player, out VisualElement rowElement)) {
                EnsurePlayerScoreboardRow(player, rowElement, scoreboardContainer);
                SyncSogLabelColorForContainer(rowElement.Q("Player"));
                ScheduleSogLabelColorSync(rowElement, rowElement.Q("Player"));
            }

            scoreboardContainer.schedule.Execute(() => SyncSogColumnLayout(scoreboardContainer)).ExecuteLater(0);
            ScheduleScoreboardReorder(scoreboardContainer);

            string playerSteamId = player.SteamId.Value.Value;
            if (!string.IsNullOrEmpty(playerSteamId) && _stars.Values.Contains(playerSteamId) && playerMap != null && playerMap.TryGetValue(player, out VisualElement starRow))
                ApplyScoreboardStarTag(starRow.Query<Label>("UsernameLabel"), playerSteamId);
        }

        /// <summary>
        /// Method used to modify the scoreboard to add additional stats.
        /// </summary>
        /// <param name="enable">Bool, true if new stats scoreboard has to added to the scoreboard. False if they need to be removed.</param>

        private static void ScoreboardModifications(bool enable) {
            if (MonoBehaviourSingleton<UIManager>.Instance.Scoreboard == null) {
                #if DEBUG_MODE
                DebugTrace.Write("ScoreboardMod", $"ABORT - UIManager.Instance.Scoreboard is null.");
                #endif
                return;
            }

            VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "scoreboard");
            VisualElement headerRow = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "header");

            #if DEBUG_MODE
            DebugTrace.Write("ScoreboardMod", $"enable={enable} scoreboardContainer={scoreboardContainer != null} headerRow={headerRow != null} serverHasResponded={_serverHasResponded}");
            #endif

            if (!enable) {
                HideAllPlayerTooltips();
                UnregisterScoreboardColumnSync();

                if (headerRow != null) {
                    VisualElement stale = headerRow.Children().FirstOrDefault(x => x.name == SOG_HEADER_LABEL_NAME);
                    if (stale != null)
                        headerRow.Remove(stale);
                }

                var playerMapDisable = SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "playerVisualElementMap");
                if (playerMapDisable != null) {
                    foreach (var kvp in playerMapDisable)
                        RemovePlayerScoreboardRow(kvp.Key.SteamId.Value.Value, kvp.Value);
                }

                _sog.Clear();
                _savePerc.Clear();
                _sogLabels.Clear();
                _playerTooltips.Clear();
                _playerTooltipNameLabels.Clear();
                _playerTooltipContainers.Clear();
                _playerTooltipIsGoalie.Clear();
                _playerTooltipPointerHandlers.Clear();
                _sogHoverCallbacksRegistered.Clear();
                _hasUpdatedUIScoreboard.Clear();
                _teamTooltipsSetup = false;
                _teamTooltipSetupScheduled = false;
                foreach (var kvp in new List<KeyValuePair<PlayerTeam, VisualElement>>(_teamHitAreas)) {
                    kvp.Value?.parent?.Remove(kvp.Value);
                }
                _teamHitAreas.Clear();
                _teamTooltips.Clear();
                return;
            }

            EnsureScoreboardColumnInfrastructure(scoreboardContainer, headerRow);

            if (_serverHasResponded) {
                SetupTeamTooltips(scoreboardContainer);
                RefreshTeamHitAreaPositions(scoreboardContainer);
            }

            var playerMap = SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "playerVisualElementMap");
            if (playerMap != null) {
                foreach (var kvp in playerMap)
                    EnsurePlayerScoreboardRow(kvp.Key, kvp.Value, scoreboardContainer);
            }

            SyncSogColumnLayout(scoreboardContainer);
            scoreboardContainer?.schedule.Execute(() => SyncSogColumnLayout(scoreboardContainer)).ExecuteLater(100);

            ReorderScoreboardPlayers(scoreboardContainer);
        }

        /// <summary>
        /// Helper function to check if a player is a goalie using both role and position as fallback.
        /// </summary>
        private static bool IsPlayerGoalie(Player player) {
            if (player == null || !player)
                return false;
            
            // Primary check: role
            if (PlayerFunc.IsGoalie(player))
                return true;
            
            // Fallback check: position string
            string position = GetPlayerPosition(player);
            return position == "G";
        }

        /// <summary>
        /// Helper function to create a label with consistent styling.
        /// </summary>
        private static Label CreateTooltipLabel(string text, string name, string playerSteamId, Label referenceLabel, float defaultFontSize = 18f) {
            Label label = new Label(text) { name = name + "_" + playerSteamId };
            label.text = text;
            // Use fixed styling — never inherit from referenceLabel, whose resolvedStyle varies
            // per player and returns 0 for mid-game joiners causing inconsistent tooltip appearance.
            label.style.fontSize = defaultFontSize;
            label.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
            label.style.unityTextAlign = TextAnchor.UpperLeft;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 2;
            label.style.width = Length.Percent(100);
            label.style.minHeight = 18;
            label.style.height = StyleKeyword.Auto;
            label.style.display = DisplayStyle.Flex;
            label.style.visibility = Visibility.Visible;
            label.style.opacity = 1f;
            label.pickingMode = PickingMode.Ignore;
            label.style.textOverflow = TextOverflow.Clip;
            label.style.overflow = Overflow.Visible;
            label.schedule.Execute(() => {
                label.text = text;
                label.MarkDirtyRepaint();
            });
            return label;
        }

        /// <summary>
        /// Generates a consistent color based on SteamID using a hash function.
        /// </summary>
        private static Color GetColorFromSteamId(string steamId) {
            if (string.IsNullOrEmpty(steamId))
                return new Color(0.1f, 0.1f, 0.1f, 0.95f); // Default dark gray
            
            // Trim whitespace for comparison
            string trimmedSteamId = steamId.Trim();
            
            // Special case: Pink for specific SteamIDs
            if (trimmedSteamId == "76561198050995236" ||
                trimmedSteamId == "76561198068597258" ||
                trimmedSteamId == "76561198022179232" ||
                trimmedSteamId == "76561198155889632" ||
                trimmedSteamId == "76561198980346669" ||
                trimmedSteamId == "76561199122116162") {
                return new Color(0.8f, 0.4f, 0.7f, 0.95f); // Pink color
            }
            
            // Add more specific SteamIDs here as needed
            // Example:
            // if (trimmedSteamId == "12345678901234567") {
            //     return new Color(1.0f, 0.5f, 0.0f, 0.95f); // Orange color
            // }
            
            // Default dark gray for all other SteamIDs
            return new Color(0.1f, 0.1f, 0.1f, 0.95f);
        }

        /// <summary>
        /// Sets up a tooltip that appears when hovering over a player's name.
        /// </summary>
        private static void SetupPlayerTooltip(Label nameLabel, VisualElement playerContainer, string playerSteamId, Player player, Label referenceLabel, float defaultFontSize = 13f) {
            UnregisterPlayerTooltipPointerHandlers(playerSteamId);

            // Remove existing tooltip if it exists
            if (_playerTooltips.TryGetValue(playerSteamId, out VisualElement existingTooltip)) {
                existingTooltip.style.display = DisplayStyle.None;
                existingTooltip.parent?.Remove(existingTooltip);
                _playerTooltips.Remove(playerSteamId);
            }
            // Cleanup stored references (will be recreated below)
            _playerTooltipNameLabels.Remove(playerSteamId);
            _playerTooltipContainers.Remove(playerSteamId);
            _playerTooltipIsGoalie.Remove(playerSteamId);
            
            VisualElement tooltip = new VisualElement {
                name = "PlayerTooltip_" + playerSteamId
            };
            tooltip.style.position = Position.Absolute;
            tooltip.style.backgroundColor = GetColorFromSteamId(playerSteamId);
            tooltip.style.borderTopWidth = 2;
            tooltip.style.borderBottomWidth = 2;
            tooltip.style.borderLeftWidth = 2;
            tooltip.style.borderRightWidth = 2;
            tooltip.style.borderTopColor = Color.white;
            tooltip.style.borderBottomColor = Color.white;
            tooltip.style.borderLeftColor = Color.white;
            tooltip.style.borderRightColor = Color.white;
            tooltip.style.paddingTop = 10;
            tooltip.style.paddingBottom = 10;
            tooltip.style.paddingLeft = 15;
            tooltip.style.paddingRight = 15;
            tooltip.style.minWidth = 280;
            tooltip.style.width = 280;
            tooltip.style.maxWidth = 280;
            tooltip.style.height = StyleKeyword.Auto;
            tooltip.style.display = DisplayStyle.None;
            // Ensure the tooltip can be measured and content fits
            tooltip.style.overflow = Overflow.Visible;
            tooltip.style.flexShrink = 0;
            tooltip.style.flexDirection = FlexDirection.Column;
            tooltip.style.alignItems = Align.FlexStart;
            tooltip.style.flexWrap = Wrap.NoWrap;
            tooltip.style.visibility = Visibility.Visible;
            tooltip.style.opacity = 1f;
            

            // Fixed constants for all player tooltips — never inherit from the reference label
            // (referenceLabel.resolvedStyle varies per player and returns 0 for mid-game joiners,
            // which caused inconsistent font sizes and text alignment across tooltips).
            const float TOOLTIP_TITLE_FONT_SIZE = 22f;
            const float TOOLTIP_STAT_FONT_SIZE  = 18f;
            defaultFontSize = TOOLTIP_STAT_FONT_SIZE; // ensure all CreateTooltipLabel calls use the fixed size

            Label titleLabel = new Label(player.Username.Value.Value);
            titleLabel.text = player.Username.Value.Value;
            titleLabel.style.fontSize = TOOLTIP_TITLE_FONT_SIZE;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
            titleLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            
            titleLabel.style.marginBottom = 8;
            titleLabel.style.width = Length.Percent(100);
            titleLabel.style.minHeight = 20;
            titleLabel.style.height = StyleKeyword.Auto;
            titleLabel.style.display = DisplayStyle.Flex;
            titleLabel.style.visibility = Visibility.Visible;
            titleLabel.style.opacity = 1f;
            titleLabel.pickingMode = PickingMode.Ignore;
            titleLabel.style.textOverflow = TextOverflow.Clip;
            titleLabel.style.overflow = Overflow.Visible;
            
            // Force text to be set again after all styles are applied
            titleLabel.schedule.Execute(() => {
                titleLabel.text = player.Username.Value.Value;
                titleLabel.MarkDirtyRepaint();
            });
            
            tooltip.Add(titleLabel);

            // Check if player is a goalie - use GetPlayerPosition to match play-by-play detection method
            bool isGoalie = GetPlayerPosition(player) == "G";

            if (isGoalie) {
                // Create goalie-specific labels
                // SV%: saves/shots (percentage)
                Label svPercentLabel = CreateTooltipLabel("SV%: 0/0 (0%)", "TooltipSVPercent", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(svPercentLabel);

                // Stick Saves: number of stick saves
                Label stickSavesLabel = CreateTooltipLabel("Stick Saves: 0", "TooltipStickSaves", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(stickSavesLabel);

                // Body Saves: number of body saves
                Label bodySavesLabel = CreateTooltipLabel("Body Saves: 0", "TooltipBodySaves", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(bodySavesLabel);

                // Poss Time: possession time in MM:SS format
                Label possessionTimeLabel = CreateTooltipLabel("Poss Time: 0:00", "TooltipPossessionTime", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(possessionTimeLabel);

                // Passes: number of passes
                Label passesLabel = CreateTooltipLabel("Passes: 0", "TooltipPasses", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(passesLabel);
            } else {
                // Create stat labels for skaters
                // Shots (SOGs)
                Label shotsLabel = CreateTooltipLabel("Shots: 0", "TooltipShots", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(shotsLabel);

                Label passesLabel = CreateTooltipLabel("Passes: 0", "TooltipPasses", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(passesLabel);

                Label exitsEntriesLabel = CreateTooltipLabel("Exits & Entries: 0 | 0", "TooltipExitsEntries", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(exitsEntriesLabel);

                Label possessionTimeLabel = CreateTooltipLabel("Possession: 0:00", "TooltipPossessionTime", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(possessionTimeLabel);

                // Takeaways (separate line)
                Label takeawaysLabel = CreateTooltipLabel("Takeaways:", "TooltipTakeaways", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(takeawaysLabel);

                // Turnovers (separate line)
                Label turnoversLabel = CreateTooltipLabel("Turnovers:", "TooltipTurnovers", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(turnoversLabel);

                // Blocks (before hits)
                Label blocksLabel = CreateTooltipLabel("Blocks: 0", "TooltipBlocks", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(blocksLabel);

                // Hits (at the bottom)
                Label hitsLabel = CreateTooltipLabel("Hits: 0", "TooltipHits", playerSteamId, referenceLabel, defaultFontSize);
                tooltip.Add(hitsLabel);
            }
            
            // Add star points label at the bottom for all players
            Label starPointsLabel = CreateTooltipLabel("Star Points: 0", "TooltipStarPoints", playerSteamId, referenceLabel, defaultFontSize);
            starPointsLabel.style.marginTop = 8;
            starPointsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            tooltip.Add(starPointsLabel);

            // Add tooltip to the scoreboard container (same parent as scoreboard elements) to ensure proper rendering context
            try {
                VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "scoreboard");
                #if DEBUG_MODE
                DebugTrace.Write("SetupTooltip", $"player={playerSteamId} scoreboardContainer={scoreboardContainer != null}");
                #endif
                if (scoreboardContainer != null) {
                    scoreboardContainer.Add(tooltip);
                    scoreboardContainer.style.overflow = Overflow.Visible;
                    #if DEBUG_MODE
                    DebugTrace.Write("SetupTooltip", $"Tooltip added to scoreboardContainer. Tooltip name={tooltip.name}");
                    #endif
                } else {
                    // Fallback to root
                    var root = MonoBehaviourSingleton<UIManager>.Instance.Scoreboard?.GetComponent<UnityEngine.UIElements.UIDocument>()?.rootVisualElement;
                    #if DEBUG_MODE
                    DebugTrace.Write("SetupTooltip", $"scoreboardContainer null - trying UIDocument root={root != null}");
                    #endif
                    if (root != null) {
                        root.Add(tooltip);
                        root.style.overflow = Overflow.Visible;
                    } else {
                        #if DEBUG_MODE
                        DebugTrace.Write("SetupTooltip", $"FAILED - Could not find container or root for tooltip");
                        #endif
                    }
                }
            } catch (Exception ex) {
                #if DEBUG_MODE
                DebugTrace.Write("SetupTooltip", $"Exception adding tooltip: {ex}");
                #endif
            }
            
            _playerTooltips.Add(playerSteamId, tooltip);
            
            // Store references for tooltip recreation when position changes
            _playerTooltipNameLabels[playerSteamId] = nameLabel;
            _playerTooltipContainers[playerSteamId] = playerContainer;
            _playerTooltipIsGoalie[playerSteamId] = isGoalie;
            
            // Ensure tooltip is above other elements and visible
            tooltip.BringToFront();

            // Enable picking on tooltip as well
            tooltip.pickingMode = PickingMode.Ignore; // Tooltip shouldn't block mouse events
            
            // Register events on both the label AND the container for better coverage
            // Tooltip never intercepts pointer events — must not block the row beneath it
            tooltip.pickingMode = PickingMode.Ignore;
            foreach (var child in tooltip.Children())
                child.pickingMode = PickingMode.Ignore;

            bool tooltipPositioned = false;

            Action showTooltip = () => {
                try {
                    if (!_serverHasResponded && !_playerTooltips.ContainsKey(playerSteamId))
                        return;

                    // Recreate tooltip if the player switched positions (skater ↔ goalie)
                    if (player != null && player) {
                        bool currentIsGoalie = GetPlayerPosition(player) == "G";
                        if (_playerTooltipIsGoalie.TryGetValue(playerSteamId, out bool wasGoalie) && wasGoalie != currentIsGoalie) {
                            if (_playerTooltipNameLabels.TryGetValue(playerSteamId, out Label storedNameLabel) &&
                                _playerTooltipContainers.TryGetValue(playerSteamId, out VisualElement storedContainer) &&
                                storedNameLabel != null && storedContainer != null) {
                                SetupPlayerTooltip(storedNameLabel, storedContainer, playerSteamId, player, storedNameLabel);
                                if (_playerTooltips.TryGetValue(playerSteamId, out VisualElement newTooltip))
                                    tooltip = newTooltip;
                                tooltipPositioned = false; // force reposition for new tooltip
                            }
                        }
                    }

                    UpdateTooltipStats(tooltip, playerSteamId, player);
                    tooltip.style.display = DisplayStyle.Flex;

                    // Only position once per hover session. Repositioning on every re-enter
                    // (which layout passes can trigger) causes upward drift because
                    // resolvedStyle.height grows as content is measured after the first frame.
                    if (!tooltipPositioned) {
                        tooltipPositioned = true;
                        // Defer until after the first layout pass so resolvedStyle.height is real.
                        tooltip.schedule.Execute(() => {
                            if (tooltip.style.display == DisplayStyle.Flex)
                                UpdateTooltipPosition(tooltip, nameLabel);
                        }).ExecuteLater(50);
                    }

                    // Stats are frozen while hovering — no periodic updates.
                    // Periodic text changes trigger layout passes which cause Unity UI Toolkit
                    // to re-evaluate pointer state, producing spurious PointerLeave/Enter pairs
                    // every 500ms (audible tick + drift). Stats update on the next hover instead.
                } catch (Exception ex) {
                    Logging.LogError($"Error showing tooltip: {ex}", _clientConfig);
                }
            };

            // Use a tiny hide delay so that moving between child elements within the row
            // doesn't fire PointerLeave → hide → PointerEnter → show in an infinite loop.
            bool hideScheduled = false;
            Action hideTooltip = () => {
                if (hideScheduled) return;
                hideScheduled = true;
                playerContainer.schedule.Execute(() => {
                    hideScheduled = false;
                    tooltipPositioned = false; // reset so next hover gets a fresh position
                    tooltip.style.display = DisplayStyle.None;
                }).ExecuteLater(80);
            };

            // Only listen on the container row — NOT on nameLabel individually.
            var pointerHandlers = new PlayerTooltipPointerHandlers {
                Enter = _ => {
                    hideScheduled = false;
                    showTooltip();
                },
                Leave = _ => hideTooltip()
            };
            _playerTooltipPointerHandlers[playerSteamId] = pointerHandlers;
            playerContainer.RegisterCallback(pointerHandlers.Enter);
            playerContainer.RegisterCallback(pointerHandlers.Leave);
        }

        /// <summary>
        /// Updates faceoff wins and totals dictionaries by scanning play-by-play events (server-side only).
        /// Scans for FaceoffOutcome events: successful = win, failed = loss.
        /// Each faceoff has 2 outcome events (one per team), so totals are calculated accordingly.
        /// </summary>
        private static void UpdateFaceoffStatsFromPbp() {
            if (!ServerFunc.IsDedicatedServer())
                return;

            try {
                // Scan all faceoff outcome events
                var faceoffOutcomeEvents = _playByPlayEvents
                    .Where(e => e.EventType == PlayByPlayEventType.FaceoffOutcome)
                    .OrderBy(e => e.GameTime) // Process in chronological order
                    .ToList();

                // Reset dictionaries and rebuild from pbp
                _teamFaceoffWins.Clear();
                _teamFaceoffTotal.Clear();

                // Group faceoff outcomes by GameTime (each faceoff has 2 outcome events at same time)
                var faceoffGroups = faceoffOutcomeEvents
                    .GroupBy(e => e.GameTime)
                    .ToList();

                foreach (var faceoffGroup in faceoffGroups) {
                    // Each faceoff has 2 outcome events (one per team)
                    var outcomes = faceoffGroup.ToList();
                    
                    // Count wins: successful outcomes (only winning team gets a win)
                    foreach (var outcome in outcomes) {
                        if (outcome.Outcome == "successful") {
                            PlayerTeam team = (PlayerTeam)outcome.PlayerTeam;
                            if (team != PlayerTeam.None) {
                                if (!_teamFaceoffWins.TryGetValue(team, out int _))
                                    _teamFaceoffWins.Add(team, 0);
                                _teamFaceoffWins[team]++;
                            }
                        }
                    }
                    
                    // Count totals: each faceoff = 1 total for each team
                    // Since each faceoff has 2 outcome events (one per team), we count each event once
                    // This gives us 1 total per team per faceoff
                    foreach (var outcome in outcomes) {
                        PlayerTeam team = (PlayerTeam)outcome.PlayerTeam;
                        if (team != PlayerTeam.None) {
                            if (!_teamFaceoffTotal.TryGetValue(team, out int _))
                                _teamFaceoffTotal.Add(team, 0);
                            _teamFaceoffTotal[team]++;
                        }
                    }
                }

                // Queue stat updates for all teams
                foreach (var kvp in _teamFaceoffWins) {
                    QueueStatUpdate(Codebase.Constants.TEAM_FACEOFF_WINS + kvp.Key.ToString(), kvp.Value.ToString());
                }
                foreach (var kvp in _teamFaceoffTotal) {
                    QueueStatUpdate(Codebase.Constants.TEAM_FACEOFF_TOTAL + kvp.Key.ToString(), kvp.Value.ToString());
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error updating faceoff stats from pbp: {ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Updates body saves and stick saves dictionaries by scanning play-by-play events (server-side only).
        /// This ensures dictionaries stay in sync with pbp events and can be synced to clients.
        /// </summary>
        private static void UpdateBodyStickSavesFromPbp() {
            if (!ServerFunc.IsDedicatedServer())
                return;

            try {
                // Track which goalies we've processed to avoid double-counting
                var processedGoalies = new HashSet<string>();

                // Scan all save events
                var saveEvents = _playByPlayEvents
                    .Where(e => 
                        !string.IsNullOrEmpty(e.PlayerSteamId) &&
                        e.EventType == PlayByPlayEventType.Save &&
                        e.Outcome == "successful") // Only count successful saves
                    .ToList();

                // Reset dictionaries and rebuild from pbp
                _stickSaves.Clear();
                _bodySaves.Clear();

                foreach (var saveEvent in saveEvents) {
                    string goalieSteamId = saveEvent.PlayerSteamId;
                    
                    // Check if the save event has a "Stick" flag
                    bool isStickSave = !string.IsNullOrEmpty(saveEvent.Flags) && saveEvent.Flags.Contains("Stick");
                    
                    if (isStickSave) {
                        if (!_stickSaves.TryGetValue(goalieSteamId, out int _))
                            _stickSaves.Add(goalieSteamId, 0);
                        _stickSaves[goalieSteamId]++;
                    } else {
                        if (!_bodySaves.TryGetValue(goalieSteamId, out int _))
                            _bodySaves.Add(goalieSteamId, 0);
                        _bodySaves[goalieSteamId]++;
                    }
                }

                // Queue stat updates for all goalies
                foreach (var kvp in _stickSaves) {
                    QueueStatUpdate(Codebase.Constants.STICK_SAVES + kvp.Key, kvp.Value.ToString());
                }
                foreach (var kvp in _bodySaves) {
                    QueueStatUpdate(Codebase.Constants.BODY_SAVES + kvp.Key, kvp.Value.ToString());
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error updating body/stick saves from pbp: {ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Updates the tooltip statistics display.
        /// </summary>
        private static void UpdateTooltipStats(VisualElement tooltip, string playerSteamId, Player player) {
            if (tooltip == null) return;
            
            // Check if player is valid
            if (player == null || !player) {
                return;
            }
            
            // Check if player is a goalie - use GetPlayerPosition to match play-by-play detection method
            // Note: Position change detection is handled in UpdateUIScoreboard, not here
            bool isGoalie = GetPlayerPosition(player) == "G";
            
            if (isGoalie) {
                // Update goalie-specific labels
                // SV%: saves/shots (percentage)
                Label svPercentLabel = null;
                try {
                    svPercentLabel = tooltip.Query<Label>("TooltipSVPercent_" + playerSteamId).First();
                } catch { }
                if (svPercentLabel != null) {
                    // Use _savePerc which tracks (Saves, Shots) for goalies
                    int saves = 0;
                    int shots = 0;
                    if (_savePerc.TryGetValue(playerSteamId, out var savePercValue)) {
                        saves = savePercValue.Saves;
                        shots = savePercValue.Shots;
                    }
                    double svPercent = shots > 0 ? (double)saves / shots * 100.0 : 0.0;
                    svPercentLabel.text = $"SV%: {saves}/{shots} ({svPercent:F1}%)";
                }

                // Stick Saves: number of stick saves (from synced dictionary)
                Label stickSavesLabel = null;
                try {
                    stickSavesLabel = tooltip.Query<Label>("TooltipStickSaves_" + playerSteamId).First();
                } catch { }
                if (stickSavesLabel != null) {
                    int stickSaves = _stickSaves.TryGetValue(playerSteamId, out int ss) ? ss : 0;
                    stickSavesLabel.text = $"Stick Saves: {stickSaves}";
                }

                // Body Saves: number of body saves (from synced dictionary)
                Label bodySavesLabel = null;
                try {
                    bodySavesLabel = tooltip.Query<Label>("TooltipBodySaves_" + playerSteamId).First();
                } catch { }
                if (bodySavesLabel != null) {
                    int bodySaves = _bodySaves.TryGetValue(playerSteamId, out int bs) ? bs : 0;
                    bodySavesLabel.text = $"Body Saves: {bodySaves}";
                }

                // Poss Time: possession time in MM:SS format
                Label possessionTimeLabel = null;
                try {
                    possessionTimeLabel = tooltip.Query<Label>("TooltipPossessionTime_" + playerSteamId).First();
                } catch { }
                if (possessionTimeLabel != null) {
                    double possessionTime = _possessionTimeSeconds.TryGetValue(playerSteamId, out double pt) ? pt : 0.0;
                    possessionTimeLabel.text = $"Poss Time: {FormatTimeAsMinutesSeconds(possessionTime)}";
                }

                // Passes: number of passes
                Label passesLabel = null;
                try {
                    passesLabel = tooltip.Query<Label>("TooltipPasses_" + playerSteamId).First();
                } catch { }
                if (passesLabel != null) {
                    int passes = _passes.TryGetValue(playerSteamId, out int p) ? p : 0;
                    passesLabel.text = $"Passes: {passes}";
                }
            } else {
                // Update skater labels
                // Use synced stats from server (these are available on client)
                // Shots (SOGs) - use _sog dictionary which is synced from server
                int shots = _sog.TryGetValue(playerSteamId, out int s) ? s : 0;

                // Shots (SOGs)
                Label shotsLabel = null;
                try {
                    shotsLabel = tooltip.Query<Label>("TooltipShots_" + playerSteamId).First();
                } catch { }
                if (shotsLabel != null) {
                    shotsLabel.text = $"Shots: {shots}";
                }

                // Passes
                Label passesLabel = null;
                try {
                    passesLabel = tooltip.Query<Label>("TooltipPasses_" + playerSteamId).First();
                } catch { }
                if (passesLabel != null) {
                    int passes = _passes.TryGetValue(playerSteamId, out int p) ? p : 0;
                    passesLabel.text = $"Passes: {passes}";
                }

                // Exits & Entries
                Label exitsEntriesLabel = null;
                try {
                    exitsEntriesLabel = tooltip.Query<Label>("TooltipExitsEntries_" + playerSteamId).First();
                } catch { }
                if (exitsEntriesLabel != null) {
                    int exits = _exits.TryGetValue(playerSteamId, out int e) ? e : 0;
                    int entries = _entries.TryGetValue(playerSteamId, out int en) ? en : 0;
                    exitsEntriesLabel.text = $"Exits & Entries: {exits} | {entries}";
                }

                // Possession
                Label possessionTimeLabel = null;
                try {
                    possessionTimeLabel = tooltip.Query<Label>("TooltipPossessionTime_" + playerSteamId).First();
                } catch { }
                if (possessionTimeLabel != null) {
                    double possessionTime = _possessionTimeSeconds.TryGetValue(playerSteamId, out double pt) ? pt : 0.0;
                    possessionTimeLabel.text = $"Possession: {FormatTimeAsMinutesSeconds(possessionTime)}";
                }

                // Takeaways
                Label takeawaysLabel = null;
                try {
                    takeawaysLabel = tooltip.Query<Label>("TooltipTakeaways_" + playerSteamId).First();
                } catch { }
                if (takeawaysLabel != null) {
                    int takeaways = _takeaways.TryGetValue(playerSteamId, out int t) ? t : 0;
                    takeawaysLabel.text = $"Takeaways: {takeaways}";
                }

                // Turnovers
                Label turnoversLabel = null;
                try {
                    turnoversLabel = tooltip.Query<Label>("TooltipTurnovers_" + playerSteamId).First();
                } catch { }
                if (turnoversLabel != null) {
                    int turnovers = _turnovers.TryGetValue(playerSteamId, out int to) ? to : 0;
                    turnoversLabel.text = $"Turnovers: {turnovers}";
                }

                // Blocks
                Label blocksLabel = null;
                try {
                    blocksLabel = tooltip.Query<Label>("TooltipBlocks_" + playerSteamId).First();
                } catch { }
                if (blocksLabel != null) {
                    int blocks = _blocks.TryGetValue(playerSteamId, out int b) ? b : 0;
                    blocksLabel.text = $"Blocks: {blocks}";
                }

                // Hits
                Label hitsLabel = null;
                try {
                    hitsLabel = tooltip.Query<Label>("TooltipHits_" + playerSteamId).First();
                } catch { }
                if (hitsLabel != null) {
                    int hits = _hits.TryGetValue(playerSteamId, out int h) ? h : 0;
                    hitsLabel.text = $"Hits: {hits}";
                }
            }
            
            // Update star points at the bottom
            Label starPointsLabel = null;
            try {
                starPointsLabel = tooltip.Query<Label>("TooltipStarPoints_" + playerSteamId).First();
            } catch { }
            if (starPointsLabel != null) {
                double starPoints = CalculateCurrentStarPoints(playerSteamId, player);
                starPointsLabel.text = $"Star Points: {starPoints:F1}";
            }
        }
        
        /// <summary>
        /// Calculates current star points for a player based on current game stats.
        /// </summary>
        private static double CalculateCurrentStarPoints(string playerSteamId, Player player) {
            if (player == null || !player)
                return 0.0;
            
            double starPoints = 0.0;
            
            // Get current game state for team modifier and GWG
            string gwgSteamId = "";
            PlayerTeam winningTeam = PlayerTeam.None;
            try {
                if (GameManager.Instance != null) {
                    int blueScore = GameManager.Instance.BlueScore;
                    int redScore = GameManager.Instance.RedScore;
                    
                    if (blueScore > redScore) {
                        winningTeam = PlayerTeam.Blue;
                        // Find the goal where Blue first took the lead
                        int blueGoalsScored = 0;
                        int redGoalsScored = 0;
                        foreach (GoalInfo goal in _goals.OrderBy(g => g.GameTime)) {
                            if (goal.Team == "Blue") {
                                blueGoalsScored++;
                            } else {
                                redGoalsScored++;
                            }
                            
                            if (goal.Team == "Blue" && blueGoalsScored > redGoalsScored && blueGoalsScored == redScore + 1) {
                                gwgSteamId = goal.Scorer;
                                break;
                            }
                        }
                    }
                    else if (redScore > blueScore) {
                        winningTeam = PlayerTeam.Red;
                        // Find the goal where Red first took the lead
                        int blueGoalsScored = 0;
                        int redGoalsScored = 0;
                        foreach (GoalInfo goal in _goals.OrderBy(g => g.GameTime)) {
                            if (goal.Team == "Blue") {
                                blueGoalsScored++;
                            } else {
                                redGoalsScored++;
                            }
                            
                            if (goal.Team == "Red" && redGoalsScored > blueGoalsScored && redGoalsScored == blueScore + 1) {
                                gwgSteamId = goal.Scorer;
                                break;
                            }
                        }
                    }
                }
            } catch { }
            
            double gwgModifier = gwgSteamId == playerSteamId ? 0.5d : 0;
            double teamModifier = winningTeam == player.Team ? 1.1d : 1d;
            
            if (PlayerFunc.IsGoalie(player)) {
                // Simplified goalie point system
                const double GOAL_ALLOWED_PENALTY = -10d;
                const double SHOT_FACED_POINTS = 10d;
                const double GOALIE_GOAL_MODIFIER = 175d;
                const double GOALIE_ASSIST_MODIFIER = 30d;
                const double SHUTOUT_BONUS = 100d;

                if (_savePerc.TryGetValue(playerSteamId, out var saveValues)) {
                    // Goals allowed: -10 points each
                    int goalsAllowed = saveValues.Shots - saveValues.Saves;
                    starPoints += ((double)goalsAllowed) * GOAL_ALLOWED_PENALTY;
                    
                    // Shots faced: 10 points each
                    starPoints += ((double)saveValues.Shots) * SHOT_FACED_POINTS;
                    
                    // Shutout bonus: 100 points if goalie allowed 0 goals and faced at least 1 shot
                    if (goalsAllowed == 0 && saveValues.Shots > 0) {
                        starPoints += SHUTOUT_BONUS;
                    }
                }

                if (_passes.TryGetValue(playerSteamId, out int passes))
                    starPoints += ((double)passes) * 2.5d;

                starPoints += GOALIE_GOAL_MODIFIER * gwgModifier;
                starPoints += ((double)player.Goals.Value) * GOALIE_GOAL_MODIFIER;
                starPoints += ((double)player.Assists.Value) * GOALIE_ASSIST_MODIFIER;
            }
            else {
                if (_sog.TryGetValue(playerSteamId, out int shots)) {
                    starPoints += ((double)shots) * 7.5d;
                }

                if (_passes.TryGetValue(playerSteamId, out int passes))
                    starPoints += ((double)passes) * 2.5d;

                if (_blocks.TryGetValue(playerSteamId, out int blocks))
                    starPoints += ((double)blocks) * 5d;

                const double SKATER_GOAL_MODIFIER = 70d;
                const double SKATER_ASSIST_MODIFIER = 30d;

                starPoints += SKATER_GOAL_MODIFIER * gwgModifier;
                starPoints += ((double)player.Goals.Value) * SKATER_GOAL_MODIFIER;
                starPoints += ((double)player.Assists.Value) * SKATER_ASSIST_MODIFIER;
            }

            // Updated skater stat multipliers
            if (_hits.TryGetValue(playerSteamId, out int hits))
                starPoints += ((double)hits) * 2.5d;

            if (_takeaways.TryGetValue(playerSteamId, out int takeaways))
                starPoints += ((double)takeaways) * 5d;

            if (_turnovers.TryGetValue(playerSteamId, out int turnovers))
                starPoints -= ((double)turnovers) * 5d;

            // DZ Exits and OZ Entries: 1 point each
            if (_exits.TryGetValue(playerSteamId, out int exits))
                starPoints += ((double)exits) * 1d;

            if (_entries.TryGetValue(playerSteamId, out int entries))
                starPoints += ((double)entries) * 1d;

            // Apply team modifier only when game is over
            if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.GameOver) {
                starPoints *= teamModifier;
            }
            
            return starPoints;
        }

        /// <summary>
        /// Sets up team tooltips for team score labels.
        /// </summary>
        /// <summary>
        /// Refreshes the screen-space positions of team score hit areas every frame while the
        /// scoreboard is visible. This corrects any stale 0,0 positions that result from the
        /// 600 ms deferred setup firing while the scoreboard was temporarily hidden.
        /// </summary>
        private static void RefreshTeamHitAreaPositions(VisualElement scoreboardContainer) {
            if (_teamHitAreas.Count == 0) return;
            try {
                Rect containerWorld = scoreboardContainer.worldBound;
                if (containerWorld.width <= 0) return;

                UIGameState gameStatePanel = MonoBehaviourSingleton<UIManager>.Instance?.GameState;
                if (gameStatePanel == null) return;

                Label blueScoreLabel = SystemFunc.GetPrivateField<Label>(typeof(UIGameState), gameStatePanel, "blueScoreLabel");
                Label redScoreLabel  = SystemFunc.GetPrivateField<Label>(typeof(UIGameState), gameStatePanel, "redScoreLabel");

                foreach (var kvp in _teamHitAreas) {
                    Label scoreLabel = kvp.Key == PlayerTeam.Blue ? blueScoreLabel : redScoreLabel;
                    if (scoreLabel == null) continue;
                    VisualElement hitArea = kvp.Value;
                    if (hitArea == null) continue;

                    Rect labelWorld = scoreLabel.worldBound;
                    if (labelWorld.width <= 0) continue;

                    float newLeft   = labelWorld.x - containerWorld.x;
                    float newTop    = labelWorld.y - containerWorld.y;
                    float newWidth  = Mathf.Max(labelWorld.width,  50f);
                    float newHeight = Mathf.Max(labelWorld.height, 30f);

                    // Only write if the values actually changed to avoid unnecessary style dirtying
                    if (Mathf.Abs(hitArea.style.left.value.value   - newLeft)   > 0.5f ||
                        Mathf.Abs(hitArea.style.top.value.value    - newTop)    > 0.5f ||
                        Mathf.Abs(hitArea.style.width.value.value  - newWidth)  > 0.5f ||
                        Mathf.Abs(hitArea.style.height.value.value - newHeight) > 0.5f) {
                        hitArea.style.left   = newLeft;
                        hitArea.style.top    = newTop;
                        hitArea.style.width  = newWidth;
                        hitArea.style.height = newHeight;
                    }
                }
            } catch { }
        }

        private static void SetupTeamTooltips(VisualElement scoreboardContainer) {
            try {
                if (_teamTooltipsSetup)
                    return;

                // Score labels live in UIGameState, a DIFFERENT UIDocument panel from UIScoreboard.
                // UIScoreboard is rendered on top and absorbs all mouse events before they reach UIGameState.
                // Solution: get the score labels for worldBound only; create invisible hit-area elements
                // INSIDE scoreboardContainer (same panel as UIScoreboard) positioned over the score labels.
                UIGameState gameStatePanel = MonoBehaviourSingleton<UIManager>.Instance?.GameState;
                #if DEBUG_MODE
                DebugTrace.Write("TeamSetup", $"SetupTeamTooltips called. gameStatePanel={gameStatePanel != null}");
                #endif
                if (gameStatePanel == null) {
                    #if DEBUG_MODE
                    DebugTrace.Write("TeamSetup", $"ABORT - UIGameState panel not available.");
                    #endif
                    return;
                }

                if (_teamTooltipSetupScheduled)
                    return;
                _teamTooltipSetupScheduled = true;

                scoreboardContainer.schedule.Execute(() => {
                    try {
                        if (_teamTooltipsSetup)
                            return;

                        // Bail if the scoreboard was closed while this deferred was pending.
                        // Without this guard, hit areas are created with zero worldBound, their
                        // 100 ms position-update also skips, and _teamTooltipsSetup is set to true
                        // with stale 0,0 hit areas that never trigger hover events.
                        if (scoreboardContainer.worldBound.width <= 0)
                            return;

                        Label blueScoreLabel = SystemFunc.GetPrivateField<Label>(typeof(UIGameState), gameStatePanel, "blueScoreLabel");
                        Label redScoreLabel  = SystemFunc.GetPrivateField<Label>(typeof(UIGameState), gameStatePanel, "redScoreLabel");

                        #if DEBUG_MODE
                        DebugTrace.Write("TeamSetup", $"Scheduled: blueScoreLabel={blueScoreLabel != null} text='{blueScoreLabel?.text}' redScoreLabel={redScoreLabel != null} text='{redScoreLabel?.text}'");
                        #endif

                        if (blueScoreLabel == null || redScoreLabel == null) {
                            #if DEBUG_MODE
                            DebugTrace.Write("TeamSetup", $"ABORT - score labels not found (blue={blueScoreLabel != null}, red={redScoreLabel != null}).");
                            #endif
                            return;
                        }

                        // Create transparent hit areas in scoreboardContainer so mouse events are
                        // received by the UIScoreboard panel (which sits on top of UIGameState).
                        VisualElement blueHitArea = CreateTeamScoreHitArea(scoreboardContainer, blueScoreLabel, PlayerTeam.Blue);
                        VisualElement redHitArea  = CreateTeamScoreHitArea(scoreboardContainer, redScoreLabel,  PlayerTeam.Red);

                        SetupTeamTooltip(blueHitArea, blueScoreLabel, scoreboardContainer, PlayerTeam.Blue);
                        SetupTeamTooltip(redHitArea,  redScoreLabel,  scoreboardContainer, PlayerTeam.Red);

                        _teamTooltipsSetup = true;
                        #if DEBUG_MODE
                        DebugTrace.Write("TeamSetup", $"Team tooltips registered successfully (hit-area approach).");
                        #endif

                        // Keep hit-area positions in sync with the score labels continuously.
                        // RefreshTeamHitAreaPositions is also called from ScoreboardModifications, but that
                        // only fires when player stat updates arrive. After game-end, stats stop flowing so
                        // we need this independent loop to keep tooltips working during the post-game period.
                        void KeepHitAreasAligned() {
                            // Stop looping once tooltips have been torn down (new game reset).
                            if (_teamHitAreas.Count == 0 || !_teamTooltipsSetup)
                                return;
                            RefreshTeamHitAreaPositions(scoreboardContainer);
                            scoreboardContainer.schedule.Execute(KeepHitAreasAligned).ExecuteLater(500);
                        }
                        scoreboardContainer.schedule.Execute(KeepHitAreasAligned).ExecuteLater(500);
                    } catch (Exception ex) {
                        #if DEBUG_MODE
                        DebugTrace.Write("TeamSetup", $"Exception in scheduled callback: {ex}");
                        #endif
                    } finally {
                        _teamTooltipSetupScheduled = false;
                    }
                }).ExecuteLater(600);
            } catch (Exception ex) {
                Logging.LogError($"Error setting up team tooltips: {ex}", _clientConfig);
            }
        }

        /// <summary>
        /// Creates a transparent VisualElement in scoreboardContainer that covers the given score label's
        /// screen area, giving us a reliable hit target in the UIScoreboard panel.
        /// </summary>
        private static VisualElement CreateTeamScoreHitArea(VisualElement scoreboardContainer, Label scoreLabel, PlayerTeam team) {
            // Remove stale hit area if any
            if (_teamHitAreas.TryGetValue(team, out VisualElement stale)) {
                stale.parent?.Remove(stale);
                _teamHitAreas.Remove(team);
            }

            VisualElement hitArea = new VisualElement {
                name = $"TeamScoreHitArea_{team}",
                pickingMode = PickingMode.Position
            };
            hitArea.style.position   = Position.Absolute;
            hitArea.style.backgroundColor = new StyleColor(Color.clear);
            scoreboardContainer.Add(hitArea);
            scoreboardContainer.style.overflow = Overflow.Visible;

            // Scheduled so layout is resolved before we read worldBound
            hitArea.schedule.Execute(() => {
                Rect labelWorld     = scoreLabel.worldBound;
                Rect containerWorld = scoreboardContainer.worldBound;
                if (labelWorld.width > 0 && containerWorld.width > 0) {
                    hitArea.style.left   = labelWorld.x      - containerWorld.x;
                    hitArea.style.top    = labelWorld.y      - containerWorld.y;
                    hitArea.style.width  = Mathf.Max(labelWorld.width,  50f);
                    hitArea.style.height = Mathf.Max(labelWorld.height, 30f);
                    #if DEBUG_MODE
                    DebugTrace.Write("TeamSetup", $"HitArea {team}: label world={labelWorld} container world={containerWorld} ? local left={hitArea.style.left.value} top={hitArea.style.top.value} w={hitArea.style.width.value} h={hitArea.style.height.value}");
                    #endif
                }
            }).ExecuteLater(100);

            _teamHitAreas.Add(team, hitArea);
            return hitArea;
        }

        /// <summary>
        /// Sets up a tooltip that appears when hovering over a team score hit area.
        /// hitArea is a transparent overlay inside scoreboardContainer (UIScoreboard panel).
        /// scoreLabel is the actual label in UIGameState, used only for reference/font.
        /// </summary>
        private static void SetupTeamTooltip(VisualElement hitArea, Label scoreLabel, VisualElement scoreboardContainer, PlayerTeam team) {
            Label referenceLabel = scoreLabel;
            string teamName = team == PlayerTeam.Blue ? "Blue Team" : "Red Team";
            string teamKey = team.ToString();
            
            // Remove existing tooltip if it exists
            if (_teamTooltips.TryGetValue(team, out VisualElement existingTooltip)) {
                existingTooltip.parent?.Remove(existingTooltip);
                _teamTooltips.Remove(team);
            }
            
            VisualElement tooltip = new VisualElement {
                name = "TeamTooltip_" + teamKey
            };
            tooltip.style.position = Position.Absolute;
            // Set team-specific colors: muted blue for blue team, muted red for red team
            if (team == PlayerTeam.Blue) {
                tooltip.style.backgroundColor = new Color(0.15f, 0.25f, 0.35f, 0.95f); // Muted blue
            } else {
                tooltip.style.backgroundColor = new Color(0.35f, 0.2f, 0.2f, 0.95f); // Muted red
            }
            tooltip.style.borderTopWidth = 2;
            tooltip.style.borderBottomWidth = 2;
            tooltip.style.borderLeftWidth = 2;
            tooltip.style.borderRightWidth = 2;
            tooltip.style.borderTopColor = Color.white;
            tooltip.style.borderBottomColor = Color.white;
            tooltip.style.borderLeftColor = Color.white;
            tooltip.style.borderRightColor = Color.white;
            tooltip.style.paddingTop = 10;
            tooltip.style.paddingBottom = 10;
            tooltip.style.paddingLeft = 15;
            tooltip.style.paddingRight = 15;
            tooltip.style.minWidth = 280;
            tooltip.style.width = 280;
            tooltip.style.maxWidth = 280;
            tooltip.style.height = StyleKeyword.Auto;
            tooltip.style.display = DisplayStyle.None;
            tooltip.style.overflow = Overflow.Visible;
            tooltip.style.flexShrink = 0;
            tooltip.style.flexDirection = FlexDirection.Column;
            tooltip.style.alignItems = Align.FlexStart;
            tooltip.style.flexWrap = Wrap.NoWrap;
            tooltip.style.visibility = Visibility.Visible;
            tooltip.style.opacity = 1f;
            
            // Use a smaller fixed font size for team tooltips (team score labels are usually large)
            float defaultFontSize = 13f;
            float titleFontSize = 16f;
            
            // Title label
            Label titleLabel = new Label(teamName);
            titleLabel.text = teamName;
            titleLabel.style.fontSize = titleFontSize;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
            titleLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.width = Length.Percent(100);
            titleLabel.style.minHeight = 20;
            titleLabel.style.height = StyleKeyword.Auto;
            titleLabel.style.display = DisplayStyle.Flex;
            titleLabel.style.visibility = Visibility.Visible;
            titleLabel.style.opacity = 1f;
            titleLabel.pickingMode = PickingMode.Ignore;
            tooltip.Add(titleLabel);
            
            // Create stat labels (comparative format: hover team - opp team)
            CreateTeamStatLabel(tooltip, "SOGs", "TooltipTeamSOGs_" + teamKey, referenceLabel, defaultFontSize);
            CreateTeamStatLabel(tooltip, "Faceoffs", "TooltipTeamFaceoffPct_" + teamKey, referenceLabel, defaultFontSize);
            CreateTeamStatLabel(tooltip, "Passes", "TooltipTeamPasses_" + teamKey, referenceLabel, defaultFontSize);
            CreateTeamStatLabel(tooltip, "Takeaways", "TooltipTeamTakeaways_" + teamKey, referenceLabel, defaultFontSize);
            CreateTeamStatLabel(tooltip, "DZ Exits", "TooltipTeamDZExits_" + teamKey, referenceLabel, defaultFontSize);
            CreateTeamStatLabel(tooltip, "OZ Entries", "TooltipTeamOZEntries_" + teamKey, referenceLabel, defaultFontSize);
            CreateTeamStatLabel(tooltip, "Possession", "TooltipTeamPossession_" + teamKey, referenceLabel, defaultFontSize);
            
            // Add tooltip to scoreboardContainer (UIScoreboard panel) ? same panel as hit areas
            // and player tooltips so it renders correctly above everything.
            scoreboardContainer.Add(tooltip);
            scoreboardContainer.style.overflow = Overflow.Visible;
            
            _teamTooltips.Add(team, tooltip);

            #if DEBUG_MODE
            DebugTrace.Write("TeamTooltip", $"SetupTeamTooltip done for team={team} hitArea={hitArea?.name} scoreboardContainer={scoreboardContainer?.name}");
            #endif

            Action showTooltip = () => {
                #if DEBUG_MODE
                DebugTrace.Write("TeamHover", $"HOVER FIRED for team={team} serverHasResponded={_serverHasResponded}");
                #endif
                if (!_serverHasResponded) return;
                UpdateTeamTooltipStats(tooltip, team);
                tooltip.style.display = DisplayStyle.Flex;
                tooltip.BringToFront();
                // Anchor to the hit area's fixed position, not the cursor.
                UpdateTeamTooltipPositionAtHitArea(tooltip, scoreboardContainer, hitArea, team);

                void UpdatePeriodically() {
                    if (tooltip.style.display == DisplayStyle.Flex) {
                        UpdateTeamTooltipStats(tooltip, team);
                        tooltip.schedule.Execute(UpdatePeriodically).ExecuteLater(500);
                    }
                }
                tooltip.schedule.Execute(UpdatePeriodically).ExecuteLater(500);
            };

            Action hideTooltip = () => {
                tooltip.style.display = DisplayStyle.None;
            };

            hitArea.RegisterCallback<MouseEnterEvent>(evt => showTooltip());
            hitArea.RegisterCallback<PointerEnterEvent>(evt => showTooltip());
            hitArea.RegisterCallback<MouseLeaveEvent>(evt => hideTooltip());
            hitArea.RegisterCallback<PointerLeaveEvent>(evt => hideTooltip());
        }

        private static void CreateTeamStatLabel(VisualElement tooltip, string labelText, string labelName, Label referenceLabel, float defaultFontSize) {
            Label statLabel = new Label(labelText + ": 0") { name = labelName };
            statLabel.text = labelText + ": 0";
            // Use the provided defaultFontSize (13px) instead of copying from referenceLabel
            statLabel.style.fontSize = defaultFontSize;
            statLabel.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
            statLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            // Try to copy font asset from reference label if available
            if (referenceLabel != null) {
                try {
                    var fontAssetField = typeof(Label).GetField("fontAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fontAssetField != null) {
                        var fontAsset = fontAssetField.GetValue(referenceLabel) as UnityEngine.Font;
                        if (fontAsset != null) {
                            fontAssetField.SetValue(statLabel, fontAsset);
                        }
                    }
                } catch { }
            }
            statLabel.style.marginBottom = 2;
            statLabel.style.width = Length.Percent(100);
            statLabel.style.minHeight = 18;
            statLabel.style.height = StyleKeyword.Auto;
            statLabel.style.display = DisplayStyle.Flex;
            statLabel.style.visibility = Visibility.Visible;
            statLabel.style.opacity = 1f;
            statLabel.pickingMode = PickingMode.Ignore;
            statLabel.style.textOverflow = TextOverflow.Clip;
            statLabel.style.overflow = Overflow.Visible;
            tooltip.Add(statLabel);
        }

        /// <summary>
        /// Updates the team tooltip statistics display with comparative stats.
        /// Uses team-level stats (stats stay with team when players switch).
        /// </summary>
        private static void UpdateTeamTooltipStats(VisualElement tooltip, PlayerTeam team) {
            // Get opposing team
            PlayerTeam oppTeam = team == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
            
            // Get team stats (these stay with the team even when players switch)
            int hoverPasses = _teamPasses.TryGetValue(team, out int hp) ? hp : 0;
            double hoverPossessionTime = _teamPossessionTime.TryGetValue(team, out double hpt) ? hpt : 0.0;
            
            int oppPasses = _teamPasses.TryGetValue(oppTeam, out int op) ? op : 0;
            double oppPossessionTime = _teamPossessionTime.TryGetValue(oppTeam, out double opt) ? opt : 0.0;
            
            // Use synced team stats from server (these are available on client)
            // SOGs - use _teamShots dictionary which is synced from server
            int hoverSOGs = _teamShots.TryGetValue(team, out int hs) ? hs : 0;
            int oppSOGs = _teamShots.TryGetValue(oppTeam, out int os) ? os : 0;
            
            // Calculate faceoff percentage from synced team stats
            int hoverFaceoffWins = _teamFaceoffWins.TryGetValue(team, out int hfw) ? hfw : 0;
            int totalFaceoffs = _teamFaceoffTotal.TryGetValue(team, out int tft) ? tft : 0;
            double hoverFaceoffPct = totalFaceoffs > 0 ? (double)hoverFaceoffWins / totalFaceoffs * 100.0 : 0.0;
            
            // Get takeaways from team-level dictionaries
            int hoverTakeaways = _teamTakeaways.TryGetValue(team, out int htk) ? htk : 0;
            int oppTakeaways = _teamTakeaways.TryGetValue(oppTeam, out int otk) ? otk : 0;
            
            // Get DZ exits from team-level dictionaries
            int hoverDZExits = _teamExits.TryGetValue(team, out int hte) ? hte : 0;
            int oppDZExits = _teamExits.TryGetValue(oppTeam, out int ote) ? ote : 0;
            
            // Get OZ entries from team-level dictionaries
            int hoverOZEntries = _teamEntries.TryGetValue(team, out int hten) ? hten : 0;
            int oppOZEntries = _teamEntries.TryGetValue(oppTeam, out int oten) ? oten : 0;
            
            string teamKey = team.ToString();
            
            // SOGs: hover - opp
            Label sogsLabel = tooltip.Query<Label>("TooltipTeamSOGs_" + teamKey).First();
            if (sogsLabel != null) {
                sogsLabel.text = $"SOGs: {hoverSOGs}-{oppSOGs}";
            }
            
            // Faceoffs: hover% (hover wins/total faceoffs)
            Label faceoffPctLabel = tooltip.Query<Label>("TooltipTeamFaceoffPct_" + teamKey).First();
            if (faceoffPctLabel != null) {
                faceoffPctLabel.text = $"Faceoffs: {hoverFaceoffPct:F0}% ({hoverFaceoffWins}/{totalFaceoffs})";
            }
            
            // Passes: hover - opp
            Label passesLabel = tooltip.Query<Label>("TooltipTeamPasses_" + teamKey).First();
            if (passesLabel != null) {
                passesLabel.text = $"Passes: {hoverPasses}-{oppPasses}";
            }
            
            // Takeaways: hover - opp
            Label takeawaysLabel = tooltip.Query<Label>("TooltipTeamTakeaways_" + teamKey).First();
            if (takeawaysLabel != null) {
                takeawaysLabel.text = $"Takeaways: {hoverTakeaways}-{oppTakeaways}";
            }
            
            // DZ Exits: hover - opp
            Label dzExitsLabel = tooltip.Query<Label>("TooltipTeamDZExits_" + teamKey).First();
            if (dzExitsLabel != null) {
                dzExitsLabel.text = $"DZ Exits: {hoverDZExits}-{oppDZExits}";
            }
            
            // OZ Entries: hover - opp
            Label ozEntriesLabel = tooltip.Query<Label>("TooltipTeamOZEntries_" + teamKey).First();
            if (ozEntriesLabel != null) {
                ozEntriesLabel.text = $"OZ Entries: {hoverOZEntries}-{oppOZEntries}";
            }
            
            // Possession: percentage% (hoverTeam time - oppTeam time) (formatted as M:SS)
            Label possessionLabel = tooltip.Query<Label>("TooltipTeamPossession_" + teamKey).First();
            if (possessionLabel != null) {
                string hoverTimeStr = FormatTimeAsMinutesSeconds(hoverPossessionTime);
                string oppTimeStr = FormatTimeAsMinutesSeconds(oppPossessionTime);
                
                // Calculate percentage
                double totalPossessionTime = hoverPossessionTime + oppPossessionTime;
                double possessionPercentage = totalPossessionTime > 0 ? (hoverPossessionTime / totalPossessionTime) * 100.0 : 0.0;
                
                possessionLabel.text = $"Possession: {possessionPercentage:F0}% ({hoverTimeStr} - {oppTimeStr})";
            }
        }

        /// <summary>
        /// Positions a team tooltip anchored to the cursor world position.
        /// Blue:  right edge at cursor ? extends LEFT  of cursor.
        /// Red:   left  edge at cursor ? extends RIGHT of cursor.
        /// Both appear just below the cursor.
        /// </summary>
        /// <summary>
        /// Positions the team tooltip at a fixed location relative to the score hit area,
        /// regardless of where the mouse entered. Blue tooltip appears to the left of the
        /// label, red tooltip appears to the right.
        /// </summary>
        private static void UpdateTeamTooltipPositionAtHitArea(VisualElement tooltip, VisualElement scoreboardContainer, VisualElement hitArea, PlayerTeam team) {
            try {
                Rect parentWorld = scoreboardContainer.worldBound;
                if (parentWorld.width <= 0) return;

                Rect hitWorld = hitArea.worldBound;
                if (hitWorld.width <= 0) return;

                // Width is always the explicit fixed value set on creation — don't use resolvedStyle
                // which may be unresolved immediately after DisplayStyle.Flex is set.
                const float tooltipWidth = 280f;

                // Blue: top-right corner of tooltip anchored at hit area right edge → tooltip grows left
                // Red:  top-left  corner of tooltip anchored at hit area left  edge → tooltip grows right
                float xScreen = team == PlayerTeam.Blue
                    ? hitWorld.xMax - tooltipWidth
                    : hitWorld.xMin;
                float yScreen = hitWorld.yMax + 8f;

                tooltip.style.left = xScreen - parentWorld.x;
                tooltip.style.top  = yScreen - parentWorld.y;
                tooltip.BringToFront();

                #if DEBUG_MODE
                DebugTrace.Write("TeamTooltipPos", $"team={team} hitArea={hitWorld} local=({tooltip.style.left.value.value:F0},{tooltip.style.top.value.value:F0})");
                #endif
            } catch (Exception ex) {
                Logging.LogError($"Error positioning team tooltip: {ex}", _clientConfig);
            }
        }

        /// <summary>
        /// Updates the tooltip position relative to the name label.
        /// </summary>
        private static void UpdateTooltipPosition(VisualElement tooltip, Label nameLabel) {
            try {
                VisualElement parent = tooltip.parent;
                if (parent == null) return;

                // Use worldBound so we are independent of intermediate coordinate spaces.
                // nameLabel may be nested several elements deep inside the scoreboardContainer,
                // so layout-space coordinates would require manual accumulation. worldBound
                // gives us real screen pixels and is always correct regardless of nesting.
                Rect nameWorld   = nameLabel.worldBound;
                Rect parentWorld = parent.worldBound;

                // Guard: layout not yet computed
                if (parentWorld.width <= 0) return;

                float tooltipWidth  = tooltip.resolvedStyle.width  > 1 ? tooltip.resolvedStyle.width  : 280f;
                float tooltipHeight = tooltip.resolvedStyle.height > 1 ? tooltip.resolvedStyle.height : 200f;

                // Horizontal: place to the right of the scoreboard panel with a small gap.
                // We use the parent (scoreboardContainer) right edge as the anchor so it
                // sits consistently next to the PING column on every screen resolution.
                float xScreen = parentWorld.xMax + 12f;

                // If that would go off the screen's right side, flip it to the left of the panel.
                // We compare against the parent's own parent width as a proxy for screen width.
                float screenW = parent.parent != null && parent.parent.worldBound.width > 0
                    ? parent.parent.worldBound.xMax
                    : xScreen + tooltipWidth + 20f;  // safe fallback ? won't flip
                if (xScreen + tooltipWidth > screenW - 8f)
                    xScreen = parentWorld.xMin - tooltipWidth - 12f;

                // Vertical: center the tooltip relative to the scoreboard panel.
                float yScreen = parentWorld.yMin + (parentWorld.height / 2f) - (tooltipHeight / 2f);

                // Convert from screen space back to scoreboardContainer local space.
                float xLocal = xScreen - parentWorld.x;
                float yLocal = yScreen - parentWorld.y;

                tooltip.style.left = xLocal;
                tooltip.style.top  = yLocal;

                #if DEBUG_MODE
                DebugTrace.Write("TooltipPos", $"screen=({xScreen:F0},{yScreen:F0}) local=({xLocal:F0},{yLocal:F0}) parentWorld={parentWorld} nameWorld={nameWorld}");
                #endif
            } catch (Exception ex) {
                Logging.LogError($"Error updating tooltip position: {ex}", _clientConfig);
            }
        }

        /// <summary>
        /// Function that sends and sets the SOG for a player when a goal is scored.
        /// </summary>
        /// <param name="player">Player, player that scored.</param>
        /// <returns>Bool, true if it was already sent and set.</returns>
        private static bool SendSOGDuringGoal(Player player) {
            ResetPuckWasSavedOrBlockedChecks();

            if (!_lastShotWasCounted[player.Team]) {
                string playerSteamId = player.SteamId.Value.Value;

                if (string.IsNullOrEmpty(playerSteamId))
                    return true;

                if (!_sog.TryGetValue(playerSteamId, out int _))
                    _sog.Add(playerSteamId, 0);

                _sog[playerSteamId] += 1;
                int sog = _sog[playerSteamId];
                QueueStatUpdate(Codebase.Constants.SOG + playerSteamId, sog.ToString());
                LogSOG(playerSteamId, sog);
                
                                // Track team stat
                                if (!_teamShots.TryGetValue(player.Team, out int _))
                                    _teamShots.Add(player.Team, 0);
                _teamShots[player.Team] += 1;
                QueueStatUpdate(Codebase.Constants.TEAM_SHOTS + player.Team.ToString(), _teamShots[player.Team].ToString());

                _lastShotWasCounted[player.Team] = true;

                // Ensure shot was recorded before recording goal (retroactively if needed)
                float currentGameTime = GetCurrentGameTime();
                
                // Check if there's a recent shot attempt event (even if outcome is still "attempt")
                var existingShotAttempt = _playByPlayEvents.LastOrDefault(e => 
                    e.PlayerSteamId == playerSteamId && 
                    e.EventType == PlayByPlayEventType.Shot && 
                    (e.Outcome == "attempt" || e.Outcome == "on net" || e.Outcome == "goal") &&
                    e.GameTime >= currentGameTime - 3f);
                
                bool shotRecorded = existingShotAttempt != null;
                
                // Check if shot attempt was already recorded (to avoid double counting)
                // This handles cases where shot attempt was recorded but shot event wasn't found due to timing
                bool shotAttemptAlreadyRecorded = existingShotAttempt != null || 
                    (_shotAttempts.TryGetValue(playerSteamId, out int existingAttempts) && existingAttempts > 0);
                
                if (!shotRecorded) {
                    // Record shot retroactively - use goal scorer's last touch position, not puck position
                    PlayerTeam attackingTeam = player.Team;
                    
                    // Find the last touch event for this goal scorer (within last 12 seconds)
                    var lastTouchEvent = _playByPlayEvents.LastOrDefault(e => 
                        e.PlayerSteamId == playerSteamId && 
                        e.EventType == PlayByPlayEventType.Touch &&
                        e.GameTime >= currentGameTime - 12f);
                    
                    // If no touch found for this specific player, find last touch by any player on the scoring team (within 12 seconds)
                    if (lastTouchEvent == null) {
                        lastTouchEvent = _playByPlayEvents.LastOrDefault(e => 
                            e.PlayerTeam == (int)attackingTeam && 
                            e.EventType == PlayByPlayEventType.Touch &&
                            e.GameTime >= currentGameTime - 12f);
                    }
                    
                    // Use position from touch event if found, otherwise use blank/zero coordinates
                    Vector3 shotPosition = lastTouchEvent != null ? lastTouchEvent.Position : Vector3.zero;
                    Vector3 shotVelocity = lastTouchEvent != null ? lastTouchEvent.Velocity : Vector3.zero;
                    float? shotGameTime = lastTouchEvent != null ? (float?)lastTouchEvent.GameTime : null;
                    float? shotPlayerSpeed = lastTouchEvent != null ? (float?)lastTouchEvent.PlayerSpeed : null;
                    
                    // If position is still zero, try to find ANY recent touch by this goal scorer (even older)
                    if (shotPosition == Vector3.zero) {
                        var fallbackTouchEvent = _playByPlayEvents.LastOrDefault(e => 
                            e.PlayerSteamId == playerSteamId && 
                            e.EventType == PlayByPlayEventType.Touch);
                        
                        if (fallbackTouchEvent != null && fallbackTouchEvent.Position != Vector3.zero) {
                            shotPosition = fallbackTouchEvent.Position;
                            shotVelocity = fallbackTouchEvent.Velocity;
                            shotGameTime = fallbackTouchEvent.GameTime;
                            shotPlayerSpeed = fallbackTouchEvent.PlayerSpeed;
                        }
                    }
                    
                    // Determine flag based on position (only if position is valid)
                    string shotFlag = (shotPosition != Vector3.zero) ? DetermineShotFlag(shotPosition, player.Team) : "";
                    
                    // If there's an existing shot attempt event, update it to "goal" instead of creating a new one
                    if (existingShotAttempt != null) {
                        existingShotAttempt.Outcome = "goal";
                        if (string.IsNullOrEmpty(existingShotAttempt.Flags)) {
                            existingShotAttempt.Flags = shotFlag;
                        }
                        // Update GameTime, PlayerSpeed, Velocity, Position, and ForceMagnitude from touch event if available
                        if (shotGameTime.HasValue) {
                            existingShotAttempt.GameTime = shotGameTime.Value;
                        }
                        if (shotPlayerSpeed.HasValue) {
                            existingShotAttempt.PlayerSpeed = shotPlayerSpeed.Value;
                        }
                        if (shotPosition != Vector3.zero) {
                            existingShotAttempt.Position = shotPosition;
                            existingShotAttempt.Velocity = shotVelocity;
                            existingShotAttempt.ForceMagnitude = shotVelocity.magnitude;
                        }
                    } else {
                        // No existing shot event - create new one with touch event data (GameTime, PlayerSpeed, Velocity, Position)
                        // ForceMagnitude will be automatically calculated from velocity.magnitude
                        RecordPlayByPlayEventInternal(PlayByPlayEventType.Shot, player, shotPosition, shotVelocity, "goal", shotFlag, shotPlayerSpeed, false, shotGameTime);
                    }
                    
                    // Only track shot attempt stats if shot attempt wasn't already recorded
                    // This prevents double counting for close shots where attempt was already recorded
                    if (!shotAttemptAlreadyRecorded) {
                        // Track shot attempt stats for retroactive shot (player and team)
                        if (!_shotAttempts.TryGetValue(playerSteamId, out int _))
                            _shotAttempts.Add(playerSteamId, 0);
                        _shotAttempts[playerSteamId] += 1;
                        QueueStatUpdate(Codebase.Constants.SHOT_ATTEMPTS + playerSteamId, _shotAttempts[playerSteamId].ToString());
                        
                        // Track team shot attempts
                        if (!_teamShotAttempts.TryGetValue(player.Team, out int _))
                            _teamShotAttempts.Add(player.Team, 0);
                        _teamShotAttempts[player.Team] += 1;
                        QueueStatUpdate(Codebase.Constants.TEAM_SHOT_ATTEMPTS + player.Team.ToString(), _teamShotAttempts[player.Team].ToString());
                    }
                    
                    // Track home plate SOGs for retroactive shots (if shot was from home plate and touch was found)
                    // Check both touch event flag and pending release info to avoid duplicates
                    bool homePlateTracked = false;
                    if (lastTouchEvent != null && shotFlag == "HomePlate") {
                        if (!_homePlateSogs.TryGetValue(playerSteamId, out int _))
                            _homePlateSogs.Add(playerSteamId, 0);
                        _homePlateSogs[playerSteamId] += 1;
                        QueueStatUpdate(Codebase.Constants.HOME_PLATE_SOGS + playerSteamId, _homePlateSogs[playerSteamId].ToString());
                        
                        // Track team home plate SOGs
                        if (!_teamHomePlateSogs.TryGetValue(player.Team, out int _))
                            _teamHomePlateSogs.Add(player.Team, 0);
                        _teamHomePlateSogs[player.Team] += 1;
                        QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + player.Team.ToString(), _teamHomePlateSogs[player.Team].ToString());
                        homePlateTracked = true;
                    }
                    
                    // Also check pending release info if touch event didn't have home plate flag
                    if (!homePlateTracked) {
                        if (_pendingShotReleases.TryGetValue(attackingTeam, out var releaseInfo) && !string.IsNullOrEmpty(releaseInfo.ShooterSteamId) && releaseInfo.ShooterSteamId == playerSteamId) {
                            string releaseShotFlag = DetermineShotFlag(releaseInfo.PuckPosition, player.Team);
                            if (releaseShotFlag == "HomePlate") {
                                if (!_homePlateSogs.TryGetValue(playerSteamId, out int _))
                                    _homePlateSogs.Add(playerSteamId, 0);
                                _homePlateSogs[playerSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.HOME_PLATE_SOGS + playerSteamId, _homePlateSogs[playerSteamId].ToString());
                                
                                // Track team home plate SOGs
                                if (!_teamHomePlateSogs.TryGetValue(player.Team, out int _))
                                    _teamHomePlateSogs.Add(player.Team, 0);
                                _teamHomePlateSogs[player.Team] += 1;
                                QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + player.Team.ToString(), _teamHomePlateSogs[player.Team].ToString());
                            }
                        }
                    }
                } else {
                    // Update existing shot to "goal" if it exists (shot resulted in goal)
                    // Use existingShotAttempt if we found it earlier, otherwise search again
                    var shotEvent = existingShotAttempt ?? _playByPlayEvents.LastOrDefault(e => 
                        e.PlayerSteamId == playerSteamId && 
                        e.EventType == PlayByPlayEventType.Shot && 
                        e.GameTime >= GetCurrentGameTime() - 3f);
                    if (shotEvent != null) {
                        // Only proceed if shot was on net (not "missed")
                        // If shot was "missed", it shouldn't result in a goal, but handle edge cases
                        if (shotEvent.Outcome == "missed") {
                            // Shot was marked as missed but goal was scored - update to goal
                            // This can happen due to timing issues with raycast confirmation
                            shotEvent.Outcome = "goal";
                        } else {
                            shotEvent.Outcome = "goal"; // Shot resulted in goal
                        }
                        
                        // Set home plate flag only for shots on goal (not attempts)
                        // If flag is empty (was an attempt), set it now that it's confirmed as a goal
                        if (string.IsNullOrEmpty(shotEvent.Flags)) {
                            string shotFlag = DetermineShotFlag(shotEvent.Position, (PlayerTeam)shotEvent.PlayerTeam);
                            shotEvent.Flags = shotFlag;
                        }
                        
                        // Track home plate SOGs (only for shots on goal, not missed)
                        // Only track if shot wasn't originally "missed" (edge case handling above)
                        if (shotEvent.Flags == "HomePlate" && shotEvent.Outcome == "goal") {
                            if (!_homePlateSogs.TryGetValue(playerSteamId, out int _))
                                _homePlateSogs.Add(playerSteamId, 0);
                            _homePlateSogs[playerSteamId] += 1;
                            QueueStatUpdate(Codebase.Constants.HOME_PLATE_SOGS + playerSteamId, _homePlateSogs[playerSteamId].ToString());
                            
                            // Track team home plate SOGs
                            if (!_teamHomePlateSogs.TryGetValue(player.Team, out int _))
                                _teamHomePlateSogs.Add(player.Team, 0);
                            _teamHomePlateSogs[player.Team] += 1;
                            QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + player.Team.ToString(), _teamHomePlateSogs[player.Team].ToString());
                        }
                    }
                }

                // Track home plate shots for goalie when goal is scored on home plate shot
                Player goalie = PlayerFunc.GetOtherTeamGoalie(player.Team);
                if (goalie != null) {
                    string goalieSteamId = goalie.SteamId.Value.Value;
                    bool isHomePlateGoal = false;
                    
                    // Check if the shot was a home plate shot
                    var lastShot = _playByPlayEvents.LastOrDefault(e => 
                        e.PlayerSteamId == playerSteamId && 
                        e.EventType == PlayByPlayEventType.Shot && 
                        e.GameTime >= GetCurrentGameTime() - 3f);
                    if (lastShot != null) {
                        // Use shot event's position to determine flag (more reliable than Flags field)
                        // Flags might not be set yet for "attempt" shots (before raycast confirmation)
                        string shotFlag = DetermineShotFlag(lastShot.Position, (PlayerTeam)lastShot.PlayerTeam);
                        if (shotFlag == "HomePlate") {
                            isHomePlateGoal = true;
                        }
                    } else if (!shotRecorded) {
                        // Check pending shot release (if shot event doesn't exist yet)
                        PlayerTeam attackingTeam = player.Team;
                        if (_pendingShotReleases.TryGetValue(attackingTeam, out var releaseInfo) && 
                            !string.IsNullOrEmpty(releaseInfo.ShooterSteamId) && 
                            releaseInfo.ShooterSteamId == playerSteamId) {
                            string shotFlag = DetermineShotFlag(releaseInfo.PuckPosition, player.Team);
                            if (shotFlag == "HomePlate") {
                                isHomePlateGoal = true;
                            }
                        }
                    }
                    
                    // Track home plate shot on goal (for HP SV% calculation)
                    if (isHomePlateGoal) {
                        if (!_homePlateShots.TryGetValue(goalieSteamId, out int hpShotValue)) {
                            _homePlateShots.Add(goalieSteamId, 0);
                            hpShotValue = 0;
                        }
                        int hpShots = _homePlateShots[goalieSteamId] = ++hpShotValue;
                        QueueStatUpdate(Codebase.Constants.HOME_PLATE_SHOTS_FACED + goalieSteamId, hpShots.ToString());
                    }
                }

                // Goal PBP event recording is handled exclusively by the Harmony Postfix
                // (GameManager_Server_GoalScored_Patch.Postfix) which has proper dedup logic.
                // Recording it here as well (before the Postfix runs) produced triple Goal PBP
                // entries per real goal, inflating plus/minus by 3× and corrupting per-game CSVs.
                return false;
            }
            else {
                // Shot already counted — Goal PBP event is handled by the Harmony Postfix.
            }

            return true;
        }

        private static void ResetPuckWasSavedOrBlockedChecks() {
            // Reset puck was saved states.
            foreach (PlayerTeam key in new List<PlayerTeam>(_checkIfPuckWasSaved.Keys))
                _checkIfPuckWasSaved[key] = new SaveCheck();

            // Reset puck was blocked states.
            foreach (PlayerTeam key in new List<PlayerTeam>(_checkIfPuckWasBlocked.Keys))
                _checkIfPuckWasBlocked[key] = new BlockCheck();
        }

        /// <summary>
        /// Who was in net for the defending team when this goal was scored (from live role + PBP roster snapshot).
        /// Empty net goals return isEmptyNet=true and no goalie steam id.
        /// </summary>
        private static (string goalieSteamId, bool isEmptyNet) ResolveDefendingGoalieForGoal(PlayByPlayEvent goalPbp, PlayerTeam scoringTeam) {
            Player defendingGoalie = PlayerFunc.GetOtherTeamGoalie(scoringTeam);
            string roleGoalieId = defendingGoalie != null && defendingGoalie ? defendingGoalie.SteamId.Value.Value : "";

            string rosterGoalieId = "";
            if (goalPbp != null) {
                var rosterIds = ExtractSteamIdsFromRosterField(goalPbp.OpposingTeamGoalieSteamID);
                if (rosterIds.Count > 0)
                    rosterGoalieId = rosterIds.First();
            }

            if (string.IsNullOrEmpty(roleGoalieId) && string.IsNullOrEmpty(rosterGoalieId))
                return ("", true);

            if (!string.IsNullOrEmpty(roleGoalieId))
                return (roleGoalieId, false);

            return (rosterGoalieId, false);
        }

        /// <summary>
        /// Resolves defending goalie for a stored goal (supports legacy goals recorded before per-goalie attribution).
        /// </summary>
        private static string ResolveDefendingGoalieSteamIdForGoal(GoalInfo goal) {
            if (goal == null)
                return "";
            if (goal.IsEmptyNet)
                return "";
            if (!string.IsNullOrEmpty(goal.DefendingGoalieSteamId))
                return goal.DefendingGoalieSteamId;

            PlayByPlayEvent pbpGoal = _playByPlayEvents.FirstOrDefault(e =>
                e.EventType == PlayByPlayEventType.Goal &&
                e.PlayerSteamId == goal.Scorer &&
                e.Period == goal.Period &&
                Math.Abs(e.GameTime - goal.GameTime) < 1.0f);

            if (pbpGoal == null)
                return "";

            var rosterIds = ExtractSteamIdsFromRosterField(pbpGoal.OpposingTeamGoalieSteamID);
            return rosterIds.Count > 0 ? rosterIds.First() : "";
        }

        private static bool IsGoalEmptyNet(GoalInfo goal) {
            if (goal == null)
                return false;
            if (goal.IsEmptyNet)
                return true;
            if (!string.IsNullOrEmpty(goal.DefendingGoalieSteamId))
                return false;
            return string.IsNullOrEmpty(ResolveDefendingGoalieSteamIdForGoal(goal));
        }

        /// <summary>
        /// Goals charged to a specific goalie (excludes empty-net and other goalies' stints).
        /// </summary>
        private static int CountGoalsAllowedForGoalie(string goalieSteamId, int additionalGoalsAllowed = 0) {
            if (string.IsNullOrEmpty(goalieSteamId))
                return 0;

            int count = 0;
            foreach (GoalInfo g in _goals) {
                if (IsGoalEmptyNet(g))
                    continue;
                if (ResolveDefendingGoalieSteamIdForGoal(g) == goalieSteamId)
                    count++;
            }
            return count + additionalGoalsAllowed;
        }

        /// <summary>
        /// Enforces shots faced = saves + goals allowed. Fixes phantom ++shots on _savePerc
        /// that never became a save or goal (e.g. blocked shots, legacy SOG stat triggers).
        /// </summary>
        /// <param name="additionalGoalsAllowed">Include a goal being scored now but not yet appended to <see cref="_goals"/>.</param>
        private static void ReconcileGoalieShotsFaced(string goalieSteamId, int additionalGoalsAllowed = 0) {
            if (string.IsNullOrEmpty(goalieSteamId) || !ServerFunc.IsDedicatedServer())
                return;

            int goalsAllowed = CountGoalsAllowedForGoalie(goalieSteamId, additionalGoalsAllowed);
            if (!_savePerc.TryGetValue(goalieSteamId, out var v)) {
                _savePerc.Add(goalieSteamId, (0, goalsAllowed));
                QueueStatUpdate(Codebase.Constants.SAVEPERC + goalieSteamId, _savePerc[goalieSteamId].ToString());
                return;
            }

            int canonicalShots = v.Saves + goalsAllowed;
            if (v.Shots == canonicalShots)
                return;

            _savePerc[goalieSteamId] = (v.Saves, canonicalShots);
            QueueStatUpdate(Codebase.Constants.SAVEPERC + goalieSteamId, _savePerc[goalieSteamId].ToString());
            LogSavePerc(goalieSteamId, v.Saves, canonicalShots);
        }

        /// <summary>
        /// When a shot is fully blocked (no goal), remove any goalie save/SF that was credited
        /// for that attempt, then reconcile SF to saves + goals allowed.
        /// </summary>
        private static void RevertGoalieStatsForBlockedShot(PlayerTeam shooterTeam, string shooterSteamId, float blockGameTime) {
            Player goalie = PlayerFunc.GetOtherTeamGoalie(shooterTeam);
            if (goalie == null || !goalie)
                return;

            string goalieSteamId = goalie.SteamId.Value.Value;
            PlayerTeam defendingTeam = goalie.Team;
            float eventTime = blockGameTime > 0f ? blockGameTime : GetCurrentGameTime();

            if (!string.IsNullOrEmpty(shooterSteamId)) {
                var saveEvent = _playByPlayEvents.LastOrDefault(e =>
                    e.PlayerSteamId == goalieSteamId &&
                    e.EventType == PlayByPlayEventType.Save &&
                    e.Outcome == "successful" &&
                    e.GameTime >= eventTime - 4f &&
                    e.GameTime <= eventTime + 1f);

                var shotEvent = _playByPlayEvents.LastOrDefault(e =>
                    e.PlayerSteamId == shooterSteamId &&
                    e.EventType == PlayByPlayEventType.Shot &&
                    e.GameTime >= eventTime - 5f &&
                    e.GameTime <= eventTime + 1f);

                if (saveEvent != null && shotEvent != null && shotEvent.Outcome == "blocked") {
                    saveEvent.Outcome = "failed";
                    if (_savePerc.TryGetValue(goalieSteamId, out var sp)) {
                        int saves = Math.Max(0, sp.Saves - 1);
                        int shots = Math.Max(0, sp.Shots - 1);
                        _savePerc[goalieSteamId] = (saves, shots);
                        UpdateBodyStickSavesFromPbp();
                        QueueStatUpdate(Codebase.Constants.SAVEPERC + goalieSteamId, _savePerc[goalieSteamId].ToString());
                    }
                }
            }

            ReconcileGoalieShotsFaced(goalieSteamId);
        }

        /// <summary>
        /// Function that sends and sets the s% for a goalie when a goal is scored.
        /// </summary>
        /// <param name="team">PlayerTeam, team that scored the goal.</param>
        /// <param name="saveWasCounted">Bool, true if a save was already counted for that shot.</param>
        private static void SendSavePercDuringGoal(PlayerTeam team, bool saveWasCounted) {
            // Guard: both the Harmony Prefix patch and the Event_OnStatsTrigger SOG path call this
            // function for every goal. Without this check the second call decrements saves again,
            // producing negative save percentages (-100 %, etc.).
            if (_savePercDuringGoalProcessed.TryGetValue(team, out bool alreadyProcessed) && alreadyProcessed)
                return;
            _savePercDuringGoalProcessed[team] = true;

            // Get other team goalie — null when net is empty (pulled goalie).
            Player goalie = PlayerFunc.GetOtherTeamGoalie(team);
            if (goalie == null)
                return;

            string _goaliePlayerSteamId = goalie.SteamId.Value.Value;
            if (!_savePerc.TryGetValue(_goaliePlayerSteamId, out var _savePercValue)) {
                _savePerc.Add(_goaliePlayerSteamId, (0, 0));
                _savePercValue = (0, 0);
            }

            // If a save was counted, we need to check if it should be marked as "failed"
            // A save should be "failed" only if there's NO follow-up touch by the attacking team before the goal
            // If there IS a follow-up touch, it's a rebound goal and the save remains "successful"
            if (saveWasCounted) {
                float currentGameTime = GetCurrentGameTime();
                var lastSaveEvent = _playByPlayEvents
                    .Where(e => 
                        e.PlayerSteamId == _goaliePlayerSteamId && 
                        e.EventType == PlayByPlayEventType.Save &&
                        e.GameTime >= currentGameTime - 3f) // Within last 3 seconds
                    .OrderByDescending(e => e.GameTime) // Most recent first
                    .FirstOrDefault();
                
                    if (lastSaveEvent != null) {
                    // Check if there's a touch event by the attacking team between the save and the goal
                    bool hasFollowUpTouch = _playByPlayEvents.Any(e =>
                        e.EventType == PlayByPlayEventType.Touch &&
                        e.PlayerTeam == (int)team && // Attacking team (team that scored)
                        e.GameTime > lastSaveEvent.GameTime &&
                        e.GameTime < currentGameTime &&
                        e.Outcome == "successful"); // Only count successful touches
                    
                    // A goal was scored regardless — the save is not credited.
                    // Mark the PBP save event as "failed" in all cases (direct or rebound goal)
                    // so that UpdateBodyStickSavesFromPbp only counts saves where no goal followed.
                    // This keeps stick_saves + body_saves == _savePerc.Saves at all times.
                    if (lastSaveEvent.Outcome == "successful") {
                        lastSaveEvent.Outcome = "failed";
                    }
                    
                    // Rebuild body/stick saves from PBP (excluding the now-failed save event above)
                    UpdateBodyStickSavesFromPbp();
                    
                    // Decrement home plate saves only for direct goals (no rebound touch).
                    // For rebound goals the original save-attempt didn't come from a second home-plate shot.
                    if (!hasFollowUpTouch) {
                        bool wasHomePlateSave = lastSaveEvent.Flags != null && lastSaveEvent.Flags.Contains("HomePlate");
                        if (wasHomePlateSave) {
                            if (_homePlateSaves.TryGetValue(_goaliePlayerSteamId, out int hpSaveValue) && hpSaveValue > 0) {
                                int hpSaves = _homePlateSaves[_goaliePlayerSteamId] = --hpSaveValue;
                                QueueStatUpdate(Codebase.Constants.HOME_PLATE_SAVES + _goaliePlayerSteamId, hpSaves.ToString());
                            }
                        }
                    }
                }
            }

            _savePerc[_goaliePlayerSteamId] = saveWasCounted ? (--_savePercValue.Saves, _savePercValue.Shots) : (_savePercValue.Saves, ++_savePercValue.Shots);
            // Goal is not in _goals yet (Prefix runs before Postfix) — count it when reconciling SF for this goalie only.
            ReconcileGoalieShotsFaced(_goaliePlayerSteamId, additionalGoalsAllowed: 1);
            QueueStatUpdate(Codebase.Constants.SAVEPERC + _goaliePlayerSteamId, _savePerc[_goaliePlayerSteamId].ToString());
            LogSavePerc(_goaliePlayerSteamId, _savePerc[_goaliePlayerSteamId].Saves, _savePerc[_goaliePlayerSteamId].Shots);
        }

        /// <summary>
        /// Method that logs the save percentage of a goalie.
        /// </summary>
        /// <param name="goaliePlayerSteamId">String, steam Id of the goalie.</param>
        /// <param name="saves">Int, number of saves.</param>
        /// <param name="sog">Int, number of shots on goal on the goalie.</param>
        private static void LogSavePerc(string goaliePlayerSteamId, int saves, int sog) {
            Logging.Log($"playerSteamId:{goaliePlayerSteamId},saveperc:{GetGoalieSavePerc(saves, sog)},saves:{saves},sog:{sog}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the stick saves of a goalie.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="stickSaves">Int, number of stick saves.</param>
        private static void LogStickSave(string playerSteamId, int stickSaves) {
            Logging.Log($"playerSteamId:{playerSteamId},sticksv:{stickSaves}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the shots on goal of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="sog">Int, number of shots on goal.</param>
        private static void LogSOG(string playerSteamId, int sog) {
            Logging.Log($"playerSteamId:{playerSteamId},sog:{sog}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the blocked shots of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="block">Int, number of blocked shots.</param>
        private static void LogBlock(string playerSteamId, int block) {
            Logging.Log($"playerSteamId:{playerSteamId},block:{block}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the hits of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="hit">Int, number of hits.</param>
        private static void LogHit(string playerSteamId, int hit) {
            Logging.Log($"playerSteamId:{playerSteamId},hit:{hit}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the takeaways of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="takeaway">Int, number of takeaways.</param>
        private static void LogTakeaways(string playerSteamId, int takeaway) {
            Logging.Log($"playerSteamId:{playerSteamId},takeaway:{takeaway}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the turnovers of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="turnover">Int, number of turnovers.</param>
        private static void LogTurnovers(string playerSteamId, int turnover) {
            Logging.Log($"playerSteamId:{playerSteamId},turnover:{turnover}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the passes of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="pass">Int, number of passes.</param>
        private static void LogPass(string playerSteamId, int pass) {
            Logging.Log($"playerSteamId:{playerSteamId},pass:{pass}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the puck touches of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="puckTouch">Int, number of puck touches.</param>
        private static void LogPuckTouch(string playerSteamId, int puckTouch) {
            Logging.Log($"playerSteamId:{playerSteamId},pucktouch:{puckTouch}", ModServerConfig);
        }

        /// <summary>
        /// Method that logs the game winning goal of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        private static void LogGWG(string playerSteamId) {
            Logging.Log($"playerSteamId:{playerSteamId},gwg:1", ModServerConfig);
        }

        /// <summary>
        /// Computes star points from the already-built playersList (uses our own tracked data,
        /// not live PlayerManager objects which may be unavailable at game end).
        /// Updates _stars[1/2/3] so the in-game display is also correct, then returns _stars.
        /// </summary>
        private static LockDictionary<int, string> ComputeAndSetStars(List<Dictionary<string, object>> playersList, string gwgSteamId) {
            try {
                int blueScore = _goals.Count(g => g.Team == "Blue");
                int redScore  = _goals.Count(g => g.Team == "Red");
                string winningTeam = blueScore > redScore ? "Blue" : (redScore > blueScore ? "Red" : "");

                const double GOAL_ALLOWED_PENALTY  = -10d;
                const double SHOT_FACED_POINTS     =  10d;
                const double SHUTOUT_BONUS         = 100d;
                const double GOALIE_GOAL_MODIFIER  = 175d;
                const double GOALIE_ASSIST_MODIFIER =  30d;
                const double SKATER_GOAL_MODIFIER  =  70d;
                const double SKATER_ASSIST_MODIFIER =  30d;

                var starPoints = new Dictionary<string, double>();

                foreach (var p in playersList) {
                    string steamId  = p.TryGetValue("steamId",  out object sid)  ? (string)sid  : "";
                    string team     = p.TryGetValue("team",     out object tm)   ? (string)tm   : "";
                    string position = p.TryGetValue("position", out object pos)  ? (string)pos  : "";

                    if (string.IsNullOrEmpty(steamId) || team == "spectator")
                        continue;

                    int goals    = p.TryGetValue("goals",     out object g)  ? (int)g  : 0;
                    int assists  = p.TryGetValue("assists",   out object a)  ? (int)a  : 0;
                    int sog      = p.TryGetValue("sog",       out object s)  ? (int)s  : 0;
                    int passes   = p.TryGetValue("passes",    out object pa) ? (int)pa : 0;
                    int blocks   = p.TryGetValue("blocks",    out object bl) ? (int)bl : 0;
                    int hits     = p.TryGetValue("hits",      out object h)  ? (int)h  : 0;
                    int takeaways= p.TryGetValue("takeaways", out object ta) ? (int)ta : 0;
                    int turnovers= p.TryGetValue("turnovers", out object to) ? (int)to : 0;
                    int exits    = p.TryGetValue("exits",     out object ex) ? (int)ex : 0;
                    int entries  = p.TryGetValue("entries",   out object en) ? (int)en : 0;

                    double pts = 0d;
                    double gwgMod    = gwgSteamId == steamId ? 0.5d : 0d;
                    double teamMod   = (winningTeam == team) ? 1.1d : 1d;

                    bool isGoalie = position == "G";
                    if (isGoalie) {
                        int shotsFaced = p.TryGetValue("shotsFaced", out object sf) ? (int)sf : 0;
                        int saves      = p.TryGetValue("saves",      out object sv) ? (int)sv : 0;
                        int ga         = p.TryGetValue("goalsAllowed", out object gaVal) ? (int)gaVal : 0;
                        pts += ga * GOAL_ALLOWED_PENALTY;
                        pts += shotsFaced * SHOT_FACED_POINTS;
                        if (ga == 0 && shotsFaced > 0) pts += SHUTOUT_BONUS;
                        pts += passes * 2.5d;
                        pts += GOALIE_GOAL_MODIFIER  * gwgMod;
                        pts += goals   * GOALIE_GOAL_MODIFIER;
                        pts += assists * GOALIE_ASSIST_MODIFIER;
                    } else {
                        pts += sog     * 7.5d;
                        pts += passes  * 2.5d;
                        pts += blocks  * 5d;
                        pts += SKATER_GOAL_MODIFIER  * gwgMod;
                        pts += goals   * SKATER_GOAL_MODIFIER;
                        pts += assists * SKATER_ASSIST_MODIFIER;
                    }

                    pts += hits      * 2.5d;
                    pts += takeaways * 5d;
                    pts -= turnovers * 5d;
                    pts += exits     * 1d;
                    pts += entries   * 1d;
                    pts *= teamMod;

                    starPoints[steamId] = pts;
                }

                var ranked = starPoints.OrderByDescending(x => x.Value).ToList();
                _stars[1] = ranked.Count >= 1 ? ranked[0].Key : "";
                _stars[2] = ranked.Count >= 2 ? ranked[1].Key : "";
                _stars[3] = ranked.Count >= 3 ? ranked[2].Key : "";
            }
            catch (Exception ex) {
                Logging.LogError($"Error computing stars: {ex.Message}", ModServerConfig);
                _stars[1] = _stars[2] = _stars[3] = "";
            }
            return _stars;
        }

        /// <summary>
        /// Method that logs the match star of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="starIndex">Int, star number of the player (1 is first star, etc.).</param>
        private static void LogStar(string playerSteamId, int starIndex) {
            Logging.Log($"playerSteamId:{playerSteamId},star:{starIndex}", ModServerConfig);
        }

        #region Play-by-Play Methods
        /// <summary>
        /// Records a play-by-play event as part of unified stat tracking system
        /// This is called from the same places where stats are updated, ensuring chronological logging
        /// </summary>
        private static void RecordPlayByPlayEventInternal(PlayByPlayEventType eventType, Player player, Vector3 position, Vector3 velocity, string outcome = "successful", string flags = "", float? playerSpeedOverride = null, bool skipPossessionReset = false, float? gameTimeOverride = null, string teamInPossessionOverride = null) {
            // Use same game active check as existing stats mod - only track during Playing phase (FaceOff is just a 3 second delay)
            if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Play || !_logic)
                return;

            if (player == null || !player)
                return;

            // Initialize game tracking if not already initialized (handles single-player scenarios)
            if (_gameStartTime == 0f) {
                _gameStartTime = Time.time;
            }
            if (string.IsNullOrEmpty(_currentGameReferenceId)) {
                _currentGameReferenceId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            }

            try {
                string playerSteamId = player.SteamId.Value.Value;
                if (string.IsNullOrEmpty(playerSteamId))
                    return;

                EventZone zone = DetermineEventZone(position, player.Team);
                float gameTime = gameTimeOverride ?? GetCurrentGameTime();
                int period = GetCurrentPeriod();
                PlayerTeam eventTeam = player.Team;

                // Track play sequence - increment if same team, reset if possession changes
                // Puck battles do NOT reset possession chains - they maintain the current possession
                // Failed touches do NOT reset possession chains - only successful touches can break chains
                // Failed hits do NOT reset possession chains - hits don't affect possession
                // Saves do NOT reset possession chains - they are treated like failed possessions
                bool isPuckBattle = (eventType == PlayByPlayEventType.PuckBattle);
                bool isFailedHit = (eventType == PlayByPlayEventType.Hit && outcome == "failed");
                bool isSave = (eventType == PlayByPlayEventType.Save);
                bool isBlock = (eventType == PlayByPlayEventType.Block);
                
                // Note: Turnover/takeaway event recording is now handled in ValidatePendingTurnoversTakeaways
                // This ensures events are recorded when validation succeeds, regardless of event type
                
                // Check if this is a failed touch from opposing team - if so, don't reset possession chain
                bool isFailedTouch = (eventType == PlayByPlayEventType.Touch && outcome == "failed");
                bool isFailedTouchFromOpposingTeam = isFailedTouch && _currentTeamInPossession != eventTeam && _currentTeamInPossession != PlayerTeam.None;
                
                // Explicitly handle failed touches from opposing team - they should NOT reset possession
                if (isFailedTouchFromOpposingTeam) {
                    // Failed touch from opposing team - don't reset possession, don't increment counter
                    // Just record the event and continue with current possession chain
                    // Skip all possession reset logic for this case
                }
                else if (!skipPossessionReset && !isPuckBattle && !isFailedHit && !isSave && !isBlock && !isFailedTouch && _currentTeamInPossession != eventTeam) {
                    // Possession changed to a different team (and it's not a failed touch)
                    // Possession changed - check if last touch from previous team had a shot
                    if (_lastEvent != null && _lastEvent.PlayerTeam == (int)_currentTeamInPossession) {
                            // Find the last touch from the previous team
                            var lastTouch = _playByPlayEvents.LastOrDefault(e => 
                                e.PlayerTeam == (int)_currentTeamInPossession && 
                                (e.EventType == PlayByPlayEventType.Touch || e.EventType == PlayByPlayEventType.Takeaway));
                            if (lastTouch != null) {
                                // Check if a shot occurred after this touch (look past blocks)
                                // If touch is followed by block, look past the block to determine outcome
                                bool shotOccurred = false;
                                bool teamMaintainedPossessionAfterBlock = false;
                                
                                var eventsAfterTouch = _playByPlayEvents.Where(e => e.EventId > lastTouch.EventId).ToList();
                                bool foundBlockAfterTouch = false;
                                
                                foreach (var evt in eventsAfterTouch) {
                                    // Skip blocks when checking for shots - look past them
                                    if (evt.EventType == PlayByPlayEventType.Block && evt.PlayerTeam != (int)_currentTeamInPossession) {
                                        foundBlockAfterTouch = true;
                                        continue; // Skip blocks, look past them
                                    }
                                    
                                    // Check if shot occurred (even if it was blocked)
                                    if (evt.EventType == PlayByPlayEventType.Shot || evt.EventType == PlayByPlayEventType.Goal) {
                                        shotOccurred = true;
                                        break;
                                    }
                                    
                                    // If we found a block, check if team maintained possession after it
                                    if (foundBlockAfterTouch && evt.PlayerTeam == (int)_currentTeamInPossession) {
                                        if (evt.EventType == PlayByPlayEventType.Touch || 
                                            evt.EventType == PlayByPlayEventType.Pass ||
                                            evt.EventType == PlayByPlayEventType.Shot ||
                                            evt.EventType == PlayByPlayEventType.Goal) {
                                            teamMaintainedPossessionAfterBlock = true;
                                            break;
                                        }
                                    }
                                    
                                    // If we hit a different team's event (not a block or hit), stop looking
                                    // Hits don't affect possession, so ignore them when checking for possession changes
                                    if (evt.PlayerTeam != (int)_currentTeamInPossession && 
                                        evt.EventType != PlayByPlayEventType.Block && 
                                        evt.EventType != PlayByPlayEventType.Hit) {
                                        break;
                                    }
                                }
                                
                                // Touch is successful if: shot occurred OR team maintained possession after block
                                // Only mark as failed if no shot occurred AND team didn't maintain possession after block
                                if (!shotOccurred && !teamMaintainedPossessionAfterBlock && lastTouch.EventType == PlayByPlayEventType.Touch) {
                                    lastTouch.Outcome = "failed";
                                }
                            }
                        }
                        
                    // Track faceoff outcome - possession changed
                    if (_trackingFaceoffOutcome) {
                        // Record failure for team that lost possession if they had started a chain
                        if (_faceoffPossessionTeam != PlayerTeam.None && _faceoffPossessionTeam != eventTeam && _faceoffPossessionChainCount > 0) {
                            RecordFaceoffOutcome(_faceoffPossessionTeam, false);
                        }
                        _faceoffPossessionChainCount = 0;
                        _faceoffPossessionTeam = eventTeam;
                    }
                    
                    // Reset play counter to 0 for first event of new possession
                    _currentTeamInPossession = eventTeam;
                    _currentPlayInPossession = 0;
                    
                    // Reset zone exit/entry flags for new possession
                    _teamHasExitedDZ[eventTeam] = false;
                    _teamHasEnteredOZ[eventTeam] = false;
                }
                // If it's a failed touch, failed hit, save, or block, don't reset possession - just record the event and continue
                // Don't increment counter either - failed touches, failed hits, saves, and blocks from opposing team don't affect possession
                else if (!skipPossessionReset && !isPuckBattle && !isFailedHit && !isSave && !isBlock && _currentTeamInPossession == eventTeam) {
                    // Same team - increment play counter (first event is 0, second is 1, etc.)
                    // Failed touches from same team still increment the counter (they don't break the chain)
                    // Puck battles don't increment - they just record the event
                    // Failed hits don't increment - hits don't affect possession chain
                    // Saves don't increment - saves are treated like failed possessions
                    // Blocks don't increment - blocks don't affect possession chain, only touches after blocks count
                    _currentPlayInPossession++;
                    
                    // Track faceoff outcome - increment chain count for possession-building events
                    if (_trackingFaceoffOutcome) {
                        // Only count events that build possession (touches, passes, shots, etc.)
                        bool countsTowardChain = eventType == PlayByPlayEventType.Touch ||
                                                eventType == PlayByPlayEventType.Takeaway ||
                                                eventType == PlayByPlayEventType.Pass ||
                                                eventType == PlayByPlayEventType.Shot ||
                                                eventType == PlayByPlayEventType.Goal;
                        
                        if (countsTowardChain) {
                            if (_faceoffPossessionTeam == eventTeam) {
                                _faceoffPossessionChainCount++;
                                
                                // Check if team has 2+ chain possession (win faceoff) - requires 1 touch after establishing possession
                                if (_faceoffPossessionChainCount >= 2) {
                                    RecordFaceoffOutcome(eventTeam, true);
                                    _trackingFaceoffOutcome = false; // Stop tracking after outcome is determined
                                    _pendingBlueFaceoffOutcome = null; // Clear after updating
                                    _pendingRedFaceoffOutcome = null; // Clear after updating
                                }
                            }
                            else {
                                // Other team got possession - record failure for previous team if they had started a chain
                                if (_faceoffPossessionTeam != PlayerTeam.None && _faceoffPossessionChainCount > 0) {
                                    RecordFaceoffOutcome(_faceoffPossessionTeam, false);
                                }
                                _faceoffPossessionTeam = eventTeam;
                                _faceoffPossessionChainCount = 1;
                            }
                        }
                    }
                }
                else if (skipPossessionReset && eventType == PlayByPlayEventType.PuckBattle) {
                    // Puck battles don't reset possession - they maintain the current possession state
                    // Don't increment counter either - puck battles themselves don't count toward possession chain
                    // The next touch will increment if same team retains possession
                }
                // If skipPossessionReset is true but it's not a puck battle, don't do anything (event already handled)

                // Puck battles always have neutral outcome
                string finalOutcome = (eventType == PlayByPlayEventType.PuckBattle) ? "neutral" : outcome;
                string finalFlags = flags ?? "";
                
                var pbpEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = eventType,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = playerSteamId,
                    PlayerName = player.Username.Value.Value,
                    PlayerTeam = (int)eventTeam,
                    PlayerPosition = GetPlayerPosition(player),
                    PlayerJersey = player.Number.Value,
                    PlayerSpeed = playerSpeedOverride ?? GetPlayerSpeed(player),
                    Zone = zone,
                    Position = position,
                    Velocity = velocity,
                    ForceMagnitude = velocity.magnitude,
                    Outcome = finalOutcome,
                    Flags = finalFlags,
                    Team = eventTeam == PlayerTeam.Blue ? "Blue" : "Red", // Team of the player performing the event
                    TeamInPossession = teamInPossessionOverride ?? ((skipPossessionReset || (eventType == PlayByPlayEventType.Touch && outcome == "failed" && _currentTeamInPossession != eventTeam)) && _currentTeamInPossession != PlayerTeam.None 
                        ? (_currentTeamInPossession == PlayerTeam.Blue ? "Blue" : "Red") 
                        : (eventTeam == PlayerTeam.Blue ? "Blue" : "Red")),
                    CurrentPlayInPossession = (eventType == PlayByPlayEventType.Takeaway || eventType == PlayByPlayEventType.Turnover)
                        ? "0" // Takeaway is the first event for new team, turnover is the last event for previous team
                        : ((skipPossessionReset || (eventType == PlayByPlayEventType.Touch && outcome == "failed" && _currentTeamInPossession != eventTeam)) && _currentTeamInPossession != PlayerTeam.None
                            ? _currentPlayInPossession.ToString() 
                            : (_currentTeamInPossession == eventTeam ? _currentPlayInPossession.ToString() : "0")),
                    ScoreState = GetScoreState(eventTeam),
                    Timestamp = DateTime.UtcNow
                };

                CaptureTeamRosterData(pbpEvent);
                
                // If gameTimeOverride is provided, insert at the correct position based on GameTime
                // Otherwise, append to the end
                if (gameTimeOverride.HasValue) {
                    // Find the insertion point: first event with GameTime >= our GameTime
                    int insertIndex = _playByPlayEvents.Count;
                    for (int i = 0; i < _playByPlayEvents.Count; i++) {
                        if (_playByPlayEvents[i].GameTime >= gameTime) {
                            insertIndex = i;
                            break;
                        }
                    }
                    _playByPlayEvents.Insert(insertIndex, pbpEvent);
                } else {
                    _playByPlayEvents.Add(pbpEvent);
                }
                
                _lastEvent = pbpEvent; // Update last event for turnover/takeaway detection
#if DEBUG_MODE
                // Log key event types (not every touch ? that would be thousands of lines)
                if (eventType == PlayByPlayEventType.Goal || eventType == PlayByPlayEventType.OwnGoal ||
                    eventType == PlayByPlayEventType.Shot || eventType == PlayByPlayEventType.Save ||
                    eventType == PlayByPlayEventType.Block || eventType == PlayByPlayEventType.Faceoff ||
                    eventType == PlayByPlayEventType.Takeaway || eventType == PlayByPlayEventType.Turnover) {
                    DebugTrace.Write("PBP", $"Event #{_playByPlayEvents.Count} {eventType} player={player?.Username?.Value} outcome={outcome} period={GetCurrentPeriod()} gameTime={GetCurrentGameTime():F1}s");
                }
#endif
                // If this is a shot event, check if we need to cancel any recent turnovers
                // This handles cases where shot tracking has a delay and the shot event is created after the turnover validation
                if (eventType == PlayByPlayEventType.Shot && gameTime > 0f) {
                    CancelTurnoverIfShotOccurred(playerSteamId, gameTime);
                }
                
                // Check for and process turnovers/takeaways when team hits their 2nd SUCCESSFUL possession chain event (_currentPlayInPossession == 1)
                // Only trigger validation for successful events (not failed touches, hits, saves, blocks, or puck battles)
                // This combines detection and validation - checks if possession changed and processes immediately
                // Re-entrancy guard in ValidatePendingTurnoversTakeaways prevents infinite recursion
                bool isSuccessfulEvent = (outcome == "successful" || outcome == "neutral" || outcome == "") || 
                                         eventType == PlayByPlayEventType.Shot; // Shots don't use "successful" outcome
                bool isEventThatCounts = !isPuckBattle && !isFailedHit && !isSave && !isBlock && 
                                        !(eventType == PlayByPlayEventType.Touch && outcome == "failed");
                if (_currentPlayInPossession == 1 && _currentTeamInPossession == eventTeam && isSuccessfulEvent && isEventThatCounts) {
                    ValidatePendingTurnoversTakeaways(player);
                }
                
                // Update last event time for team possession timeout tracking
                DateTime eventTime = DateTime.UtcNow;
                _teamLastEventTime[eventTeam] = eventTime;
                
                // If this team had a timeout (gap > 5 seconds), reset possession start time to now
                // This ensures we start fresh after a timeout, not accumulate from the timeout time
                if (_currentTeamPossession == eventTeam && _teamPossessionStartTime < eventTime.AddSeconds(-5.0)) {
                    _teamPossessionStartTime = eventTime;
                }
                
                // Track exits and entries for tooltip display (check flags instead of event types)
                if (!string.IsNullOrEmpty(flags)) {
                    string[] flagArray = flags.Split(',');
                    foreach (string flag in flagArray) {
                        string trimmedFlag = flag.Trim();
                        if (trimmedFlag == "DZExit") {
                            if (!_exits.TryGetValue(playerSteamId, out int _))
                                _exits.Add(playerSteamId, 0);
                            _exits[playerSteamId] += 1;
                            QueueStatUpdate(Codebase.Constants.EXIT + playerSteamId, _exits[playerSteamId].ToString());
                            
                            // Track team exit stat
                            if (player != null && player) {
                                PlayerTeam playerTeam = player.Team;
                                if (!_teamExits.TryGetValue(playerTeam, out int _))
                                    _teamExits.Add(playerTeam, 0);
                                _teamExits[playerTeam] += 1;
                                QueueStatUpdate(Codebase.Constants.TEAM_EXITS + playerTeam.ToString(), _teamExits[playerTeam].ToString());
                            }
                        }
                        else if (trimmedFlag == "OZEntry") {
                            if (!_entries.TryGetValue(playerSteamId, out int _))
                                _entries.Add(playerSteamId, 0);
                            _entries[playerSteamId] += 1;
                            QueueStatUpdate(Codebase.Constants.ENTRY + playerSteamId, _entries[playerSteamId].ToString());
                            
                            // Track team entry stat
                            if (player != null && player) {
                                PlayerTeam playerTeam = player.Team;
                                if (!_teamEntries.TryGetValue(playerTeam, out int _))
                                    _teamEntries.Add(playerTeam, 0);
                                _teamEntries[playerTeam] += 1;
                                QueueStatUpdate(Codebase.Constants.TEAM_ENTRIES + playerTeam.ToString(), _teamEntries[playerTeam].ToString());
                            }
                        }
                    }
                }
                
                // Reception events removed - passes are determined by converting previous Touch to Pass
                // when a teammate touches the puck, allowing for one-touch passes and shots off passes
                
                _lastEvent = pbpEvent; // Track last event for outcome determination
            }
            catch (Exception ex) {
                Logging.LogError($"Error recording play-by-play event: {ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Records a Faceoff event when Playing phase begins
        /// </summary>
        private static void RecordFaceoffEvent() {
            if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance == null || !_logic)
                return;

            try {
                // Resolve any pending faceoff before creating a new one
                ResolvePendingFaceoffOnTrackingStop();

                if (PlayerManager.Instance == null)
                    return;

                var allPlayers = PlayerManager.Instance.GetPlayers();
                if (allPlayers == null)
                    return;

                float gameTime = GetCurrentGameTime();
                int period = GetCurrentPeriod();
                Vector3 centerIce = Vector3.zero; // Faceoff at center ice

                // Create faceoff event with blank player data
                var faceoffEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = PlayByPlayEventType.Faceoff,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = "", // Blank
                    PlayerName = "", // Blank
                    PlayerTeam = 0, // None
                    PlayerPosition = "", // Blank
                    PlayerJersey = 0, // Blank
                    PlayerSpeed = 0f, // Blank
                    Zone = EventZone.Neutral,
                    Position = centerIce,
                    Velocity = Vector3.zero,
                    ForceMagnitude = 0f,
                    Outcome = "", // Blank
                    Team = "", // Faceoff has no team
                    TeamInPossession = "", // Blank
                    CurrentPlayInPossession = "0",
                    ScoreState = GetScoreState(PlayerTeam.None), // Default to 0-0 for faceoff
                    Timestamp = DateTime.UtcNow
                };

                CaptureTeamRosterData(faceoffEvent);
                _playByPlayEvents.Add(faceoffEvent);
                _lastFaceoffEventId = faceoffEvent.EventId;
                
                // Record placeholder faceoff outcome events immediately after faceoff (will be updated when outcome is determined)
                _pendingBlueFaceoffOutcome = RecordFaceoffOutcomePlaceholder(PlayerTeam.Blue, allPlayers, gameTime, period, centerIce);
                _pendingRedFaceoffOutcome = RecordFaceoffOutcomePlaceholder(PlayerTeam.Red, allPlayers, gameTime, period, centerIce);
                
                // Update faceoff stats from pbp scanning (server-side only)
                UpdateFaceoffStatsFromPbp();
                
                // Reset possession/event logic on faceoff
                _currentTeamInPossession = PlayerTeam.None;
                _currentPlayInPossession = 0;
                _lastEvent = faceoffEvent; // Set last event to faceoff to prevent takeaways
            }
            catch (Exception ex) {
                Logging.LogError($"Error recording faceoff event: {ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Records placeholder FaceoffOutcome events immediately after faceoff (will be updated when outcome is determined)
        /// </summary>
        private static PlayByPlayEvent RecordFaceoffOutcomePlaceholder(PlayerTeam team, System.Collections.Generic.IEnumerable<Player> allPlayers, float gameTime, int period, Vector3 centerIce) {
            // Find center for this team
            Player center = allPlayers.FirstOrDefault(p => 
                p != null && p && 
                p.Team == team && 
                GetPlayerPosition(p) == "C");

            PlayByPlayEvent outcomeEvent;
            if (center != null) {
                outcomeEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = PlayByPlayEventType.FaceoffOutcome,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = center.SteamId.Value.Value,
                    PlayerName = center.Username.Value.Value,
                    PlayerTeam = (int)team,
                    PlayerPosition = "C",
                    PlayerJersey = center.Number.Value,
                    PlayerSpeed = GetPlayerSpeed(center),
                    Zone = EventZone.Neutral,
                    Position = centerIce,
                    Velocity = Vector3.zero,
                    ForceMagnitude = 0f,
                    Outcome = "pending", // Will be updated when outcome is determined
                    Team = team == PlayerTeam.Blue ? "Blue" : "Red", // Team of the center
                    TeamInPossession = "", // Will be updated when outcome is determined (team that won)
                    CurrentPlayInPossession = "0",
                    ScoreState = GetScoreState(team),
                    Timestamp = DateTime.UtcNow
                };
            }
            else {
                // No center - record with blank player data
                outcomeEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = PlayByPlayEventType.FaceoffOutcome,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = "", // Blank
                    PlayerName = "", // Blank
                    PlayerTeam = (int)team,
                    PlayerPosition = "", // Blank
                    PlayerJersey = 0, // Blank
                    PlayerSpeed = 0f, // Blank
                    Zone = EventZone.Neutral,
                    Position = centerIce,
                    Velocity = Vector3.zero,
                    ForceMagnitude = 0f,
                    Outcome = "pending", // Will be updated when outcome is determined
                    Team = team == PlayerTeam.Blue ? "Blue" : "Red", // Team of the center
                    TeamInPossession = "", // Will be updated when outcome is determined (team that won)
                    CurrentPlayInPossession = "0",
                    ScoreState = GetScoreState(team),
                    Timestamp = DateTime.UtcNow
                };
            }

            CaptureTeamRosterData(outcomeEvent);
            _playByPlayEvents.Add(outcomeEvent);
            return outcomeEvent;
        }

        /// <summary>
        /// Resolves a pending faceoff when tracking stops (period transition, new faceoff starting, etc.)
        /// Updates placeholder outcomes and then recalculates stats from pbp scanning
        /// </summary>
        private static void ResolvePendingFaceoffOnTrackingStop() {
            // Only process if we're tracking a faceoff
            if (!_trackingFaceoffOutcome)
                return;

            try {
                // Determine winner: use _faceoffPossessionTeam if set, otherwise use team from _lastEvent, fallback to Blue
                PlayerTeam winnerTeam = _faceoffPossessionTeam;
                if (winnerTeam == PlayerTeam.None) {
                    if (_lastEvent != null && _lastEvent.PlayerTeam > 0) {
                        winnerTeam = (PlayerTeam)_lastEvent.PlayerTeam;
                    } else {
                        winnerTeam = PlayerTeam.Blue; // Fallback
                    }
                }

                // Update placeholder outcomes
                string winnerTeamStr = winnerTeam == PlayerTeam.Blue ? "Blue" : "Red";
                if (_pendingBlueFaceoffOutcome != null) {
                    _pendingBlueFaceoffOutcome.Outcome = (PlayerTeam.Blue == winnerTeam) ? "successful" : "failed";
                    _pendingBlueFaceoffOutcome.TeamInPossession = winnerTeamStr; // Team that won the faceoff
                }
                if (_pendingRedFaceoffOutcome != null) {
                    _pendingRedFaceoffOutcome.Outcome = (PlayerTeam.Red == winnerTeam) ? "successful" : "failed";
                    _pendingRedFaceoffOutcome.TeamInPossession = winnerTeamStr; // Team that won the faceoff
                }

                // Update faceoff stats from pbp scanning (server-side only)
                UpdateFaceoffStatsFromPbp();
            }
            catch (Exception ex) {
                Logging.LogError($"Error resolving pending faceoff on tracking stop: {ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Records a FaceoffOutcome event for the center of each team
        /// </summary>
        /// <param name="winningTeam">The team that won (if won=true) or lost (if won=false)</param>
        /// <param name="won">True if this team won (3+ chain), false if they lost possession</param>
        private static void RecordFaceoffOutcome(PlayerTeam winningTeam, bool won) {
            if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Play || !_logic)
                return;

            try {
                if (PlayerManager.Instance == null)
                    return;

                var allPlayers = PlayerManager.Instance.GetPlayers();
                if (allPlayers == null)
                    return;

                float gameTime = GetCurrentGameTime();
                int period = GetCurrentPeriod();
                Vector3 centerIce = Vector3.zero;

                // Update existing placeholder events instead of creating new ones
                if (won) {
                    // Winner gets success, other team gets failure
                    PlayerTeam losingTeam = winningTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
                    
                    string winningTeamStr = winningTeam == PlayerTeam.Blue ? "Blue" : "Red";
                    if (_pendingBlueFaceoffOutcome != null) {
                        _pendingBlueFaceoffOutcome.Outcome = (PlayerTeam.Blue == winningTeam) ? "successful" : "failed";
                        _pendingBlueFaceoffOutcome.TeamInPossession = winningTeamStr; // Team that won the faceoff
                        _pendingBlueFaceoffOutcome = null;
                    }
                    if (_pendingRedFaceoffOutcome != null) {
                        _pendingRedFaceoffOutcome.Outcome = (PlayerTeam.Red == winningTeam) ? "successful" : "failed";
                        _pendingRedFaceoffOutcome.TeamInPossession = winningTeamStr; // Team that won the faceoff
                        _pendingRedFaceoffOutcome = null;
                    }
                    
                    // Update faceoff stats from pbp scanning (server-side only)
                    UpdateFaceoffStatsFromPbp();
                }
                else {
                    // When won=false, winningTeam parameter is actually the team that LOST
                    // So we need to determine the actual winner (the other team) and update both outcomes
                    PlayerTeam actualWinner = winningTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
                    
                    // Update both outcomes: winner gets success, loser gets failure
                    string actualWinnerStr = actualWinner == PlayerTeam.Blue ? "Blue" : "Red";
                    if (_pendingBlueFaceoffOutcome != null) {
                        _pendingBlueFaceoffOutcome.Outcome = (PlayerTeam.Blue == actualWinner) ? "successful" : "failed";
                        _pendingBlueFaceoffOutcome.TeamInPossession = actualWinnerStr; // Team that won the faceoff
                        _pendingBlueFaceoffOutcome = null;
                    }
                    if (_pendingRedFaceoffOutcome != null) {
                        _pendingRedFaceoffOutcome.Outcome = (PlayerTeam.Red == actualWinner) ? "successful" : "failed";
                        _pendingRedFaceoffOutcome.TeamInPossession = actualWinnerStr; // Team that won the faceoff
                        _pendingRedFaceoffOutcome = null;
                    }
                    
                    // Update faceoff stats from pbp scanning (server-side only)
                    UpdateFaceoffStatsFromPbp();
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error recording faceoff outcome: {ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Helper method to record faceoff outcome for a single team
        /// </summary>
        private static void RecordFaceoffOutcomeForTeam(PlayerTeam team, bool won, System.Collections.Generic.IEnumerable<Player> allPlayers, float gameTime, int period, Vector3 centerIce) {
            // Find center for this team
            Player center = allPlayers.FirstOrDefault(p => 
                p != null && p && 
                p.Team == team && 
                GetPlayerPosition(p) == "C");

            if (center != null) {
                var outcomeEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = PlayByPlayEventType.FaceoffOutcome,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = center.SteamId.Value.Value,
                    PlayerName = center.Username.Value.Value,
                    PlayerTeam = (int)team,
                    PlayerPosition = "C",
                    PlayerJersey = center.Number.Value,
                    PlayerSpeed = GetPlayerSpeed(center),
                    Zone = EventZone.Neutral,
                    Position = centerIce,
                    Velocity = Vector3.zero,
                    ForceMagnitude = 0f,
                    Outcome = won ? "successful" : "failed",
                    Team = team == PlayerTeam.Blue ? "Blue" : "Red", // Team of the center
                    TeamInPossession = won ? (team == PlayerTeam.Blue ? "Blue" : "Red") : (team == PlayerTeam.Blue ? "Red" : "Blue"), // Team that won the faceoff
                    CurrentPlayInPossession = "0",
                    ScoreState = GetScoreState(team),
                    Timestamp = DateTime.UtcNow
                };

                CaptureTeamRosterData(outcomeEvent);
                _playByPlayEvents.Add(outcomeEvent);
            }
            else {
                // No center - record with blank player data
                var outcomeEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = PlayByPlayEventType.FaceoffOutcome,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = "", // Blank
                    PlayerName = "", // Blank
                    PlayerTeam = (int)team,
                    PlayerPosition = "", // Blank
                    PlayerJersey = 0, // Blank
                    PlayerSpeed = 0f, // Blank
                    Zone = EventZone.Neutral,
                    Position = centerIce,
                    Velocity = Vector3.zero,
                    ForceMagnitude = 0f,
                    Outcome = won ? "successful" : "failed",
                    Team = team == PlayerTeam.Blue ? "Blue" : "Red", // Team of the center
                    TeamInPossession = won ? (team == PlayerTeam.Blue ? "Blue" : "Red") : (team == PlayerTeam.Blue ? "Red" : "Blue"), // Team that won the faceoff
                    CurrentPlayInPossession = "0",
                    ScoreState = GetScoreState(team),
                    Timestamp = DateTime.UtcNow
                };

                CaptureTeamRosterData(outcomeEvent);
                _playByPlayEvents.Add(outcomeEvent);
            }
        }

        /// <summary>
        /// Determines which zone an event occurred in
        /// </summary>
        private static EventZone DetermineEventZone(Vector3 position, PlayerTeam playerTeam) {
            float distanceFromCenter = Mathf.Abs(position.z);
            const float blueLineDistance = 13f;

            if (distanceFromCenter <= blueLineDistance) {
                return EventZone.Neutral;
            }
            else {
                // Determine if this is offensive or defensive zone based on player team
                bool isBlueTeam = (playerTeam == PlayerTeam.Blue);
                bool isInHomeHalf = (position.z > 0);

                if (isBlueTeam == isInHomeHalf) {
                    return EventZone.Defensive; // Player in their own half
                }
                else {
                    return EventZone.Offensive; // Player in opponent's half
                }
            }
        }

        /// <summary>
        /// Gets the shot timeout in seconds based on the zone where the shot was taken
        /// OZ (Offensive Zone): 2 seconds
        /// NZ (Neutral Zone): 4 seconds
        /// DZ (Defensive Zone): 6 seconds
        /// </summary>
        private static float GetShotTimeoutByZone(EventZone zone) {
            switch (zone) {
                case EventZone.Offensive:
                    return 2.0f; // OZ: 2 seconds
                case EventZone.Neutral:
                    return 4.0f; // NZ: 4 seconds
                case EventZone.Defensive:
                    return 6.0f; // DZ: 6 seconds
                default:
                    return 2.0f; // Default to 2 seconds if zone is unknown
            }
        }

        /// <summary>
        /// Gets the current game time in seconds with decimal precision (actual game clock time, pauses during stoppages)
        /// </summary>
        private static float GetCurrentGameTime() {
            if (GameManager.Instance == null)
                return 0f;

            try {
                // Use GameManager convenience property: GameManager.Instance.Tick (countdown ticks)
                int timeRemaining = GameManager.Instance.Tick;
                
                // Convert countdown to elapsed time for current period
                float periodElapsedTime = 300f - timeRemaining;
                
                // Get current period to calculate total game time
                int currentPeriod = GetCurrentPeriod();
                float wholeSecondGameTime;
                if (currentPeriod > 0) {
                    // Each period is 300 seconds (5 minutes)
                    // Period 1: 0-300s, Period 2: 300-600s, Period 3: 600-900s, etc.
                    wholeSecondGameTime = ((currentPeriod - 1) * 300f) + periodElapsedTime;
                }
                else {
                    // Period not available, just return period elapsed time
                    wholeSecondGameTime = periodElapsedTime;
                }
                
                // Track fractional seconds using a 1-second timer that pauses when game clock pauses
                float currentUnityTime = UnityEngine.Time.time;
                
                // Check if countdown changed BEFORE updating tracking
                bool countdownChanged = (timeRemaining != _lastCountdownValue);
                
                // If countdown changed (new whole second), reset fractional timer and update tracking
                if (countdownChanged) {
                    _lastWholeSecondGameTime = wholeSecondGameTime;
                    _lastUnityTimeForGameTime = currentUnityTime;
                    _lastCountdownValue = timeRemaining;
                    _fractionalSecondTimer = 0f; // Reset fractional timer when countdown advances
                }
                
                // Always calculate fractional seconds when in Playing phase for consistent decimal precision
                if (GameManager.Instance.Phase == GamePhase.Play) {
                    // Initialize tracking if not already done
                    if (_lastUnityTimeForGameTime <= 0f || _lastCountdownValue == -1) {
                        _lastWholeSecondGameTime = wholeSecondGameTime;
                        _lastUnityTimeForGameTime = currentUnityTime;
                        _lastCountdownValue = timeRemaining;
                        _fractionalSecondTimer = 0f;
                        // Return with minimal fractional precision to ensure decimals are always present
                        return wholeSecondGameTime + 0.001f;
                    }
                    
                    // Calculate fractional seconds from timer with precise pause detection
                    // Timer resets to 0 when countdown changes, then advances based on Time.time advancement
                    // Track time since countdown last changed (not since last call) for accurate fractional seconds
                    float timeSinceCountdownChange = currentUnityTime - _lastUnityTimeForGameTime;
                    
                    // Detect if game is paused: if timeSinceCountdownChange hasn't increased, Time.time hasn't advanced
                    // Store previous timer value to detect if time is advancing
                    float previousTimerValue = _fractionalSecondTimer;
                    float newTimerValue = Mathf.Min(timeSinceCountdownChange, 1.0f); // Clamp to 1.0 maximum
                    
                    // Only update timer if time is actually advancing (newTimerValue > previousTimerValue)
                    // This handles pause/unpause cycles precisely - if Time.time pauses, newTimerValue won't increase
                    if (newTimerValue > previousTimerValue) {
                        // Time.time is advancing - update fractional timer
                        _fractionalSecondTimer = newTimerValue;
                    }
                    // If newTimerValue <= previousTimerValue, game is paused - timer stays at last value
                    
                    // Always return with fractional precision for consistency
                    return _lastWholeSecondGameTime + _fractionalSecondTimer;
                }
                
                // Clock not running, return whole second
                // Initialize tracking if not already done
                if (_lastCountdownValue == -1) {
                    _lastWholeSecondGameTime = wholeSecondGameTime;
                    _lastUnityTimeForGameTime = currentUnityTime;
                    _lastCountdownValue = timeRemaining;
                    _fractionalSecondTimer = 0f;
                }
                return wholeSecondGameTime;
            }
            catch (Exception ex) {
                Logging.LogError($"Error in GetCurrentGameTime: {ex}", ModServerConfig);
                return 0f;
            }
        }

        /// <summary>
        /// Gets the current score state from the perspective of the team in possession
        /// </summary>
        private static string GetScoreState(PlayerTeam teamInPossession) {
            if (GameManager.Instance == null)
                return "0-0";

            try {
                var gameState = GameManager.Instance.GameState.Value;
                int blueScore = gameState.BlueScore;
                int redScore = gameState.RedScore;

                // Format score from the perspective of the team in possession
                if (teamInPossession == PlayerTeam.Blue) {
                    return $"{blueScore}-{redScore}";
                }
                else if (teamInPossession == PlayerTeam.Red) {
                    return $"{redScore}-{blueScore}";
                }
                else {
                    // No team in possession, default to Blue perspective
                    return $"{blueScore}-{redScore}";
                }
            }
            catch {
                return "0-0";
            }
        }

        /// <summary>
        /// Gets the current period
        /// </summary>
        private static int GetCurrentPeriod() {
            if (GameManager.Instance == null)
                return 0;

            try {
                int period = GameManager.Instance.Period;
                if (period > 0)
                    return period;
            }
            catch (Exception ex) {
                Logging.LogError($"Error in GetCurrentPeriod: {ex}", ModServerConfig);
            }

            return 0;
        }

        /// <summary>
        /// Gets player position string
        /// </summary>
        private static string GetPlayerPosition(Player player) {
            if (player == null || !player)
                return "";

            try {
                if (PlayerFunc.IsGoalie(player))
                    return PlayerFunc.GOALIE_POSITION;

                // Player.PlayerPosition is a PlayerPosition NetworkBehaviour; Name holds "C", "LW", etc.
                PlayerPosition positionComponent = player.PlayerPosition;
                if (positionComponent != null && !string.IsNullOrEmpty(positionComponent.Name))
                    return positionComponent.Name;
            }
            catch { }

            return "";
        }

        /// <summary>
        /// Gets the position order priority for sorting (lower number = appears first)
        /// </summary>
        private static int GetPositionOrderPriority(string position, PlayerTeam team) {
            // Position order: C, LW, RW, LD, RD, G, N/A (empty)
            // Red team appears first, then Blue team
            int teamOffset = team == PlayerTeam.Red ? 0 : 1000; // Red team first
            
            switch (position) {
                case "C": return teamOffset + 1;
                case "LW": return teamOffset + 2;
                case "RW": return teamOffset + 3;
                case "LD": return teamOffset + 4;
                case "RD": return teamOffset + 5;
                case "G": return teamOffset + 6;
                default: return teamOffset + 7; // N/A or empty position
            }
        }

        private static void ScheduleScoreboardReorder(VisualElement scoreboardContainer) {
            if (scoreboardContainer == null)
                return;

            scoreboardContainer.schedule.Execute(() => ReorderScoreboardPlayers(scoreboardContainer)).ExecuteLater(0);
        }

        /// <summary>
        /// Reorders players in the scoreboard by position: C, LW, RW, LD, RD, G, N/A
        /// Red team first, then Blue team
        /// </summary>
        private static void ReorderScoreboardPlayers(VisualElement scoreboardContainer) {
            if (scoreboardContainer == null || MonoBehaviourSingleton<UIManager>.Instance.Scoreboard == null)
                return;

            try {
                var playerVisualElementMap = SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), MonoBehaviourSingleton<UIManager>.Instance.Scoreboard, "playerVisualElementMap");
                if (playerVisualElementMap == null || playerVisualElementMap.Count == 0)
                    return;

                // Get all players with their visual elements and sort by position
                var playersWithElements = playerVisualElementMap.Select(kvp => new {
                    Player = kvp.Key,
                    VisualElement = kvp.Value,
                    Position = GetPlayerPosition(kvp.Key),
                    Team = kvp.Key.Team,
                    OrderPriority = GetPositionOrderPriority(GetPlayerPosition(kvp.Key), kvp.Key.Team)
                }).OrderBy(p => p.OrderPriority).ToList();

                // Group by parent container (players might be in different containers)
                var playersByParent = playersWithElements
                    .Where(p => p.VisualElement != null && p.VisualElement.parent != null)
                    .GroupBy(p => p.VisualElement.parent)
                    .ToList();

                // Reorder within each parent container by removing and re-adding in correct order
                foreach (var parentGroup in playersByParent) {
                    VisualElement parent = parentGroup.Key;
                    var sortedPlayers = parentGroup.OrderBy(p => p.OrderPriority).ToList();

                    // Remove all elements from parent first
                    var elementsToReorder = sortedPlayers.Select(p => p.VisualElement).Where(e => e.parent == parent).ToList();
                    foreach (var element in elementsToReorder) {
                        element.RemoveFromHierarchy();
                    }

                    // Re-add them in the correct order
                    foreach (var playerInfo in sortedPlayers) {
                        try {
                            if (playerInfo.VisualElement.parent == null) {
                                parent.Add(playerInfo.VisualElement);
                            }
                        }
                        catch (Exception ex) {
                            Logging.LogError($"Error reordering player {playerInfo.Player.Username.Value}: {ex}", _clientConfig);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error reordering scoreboard players: {ex}", _clientConfig);
            }
        }

        /// <summary>
        /// Gets player speed in m/s
        /// </summary>
        private static float GetPlayerSpeed(Player player) {
            if (player == null || !player)
                return 0f;

            try {
                // Access rigidbody through PlayerBody property
                if (player.PlayerBody != null && player.PlayerBody.Rigidbody != null) {
                    return player.PlayerBody.Rigidbody.linearVelocity.magnitude;
                }
            }
            catch { }

            return 0f;
        }

        /// <summary>
        /// Captures team roster data at the time of an event
        /// </summary>
        private static void CaptureTeamRosterData(PlayByPlayEvent gameEvent) {
            try {
                if (PlayerManager.Instance == null)
                    return;

                PlayerTeam playerTeam = (PlayerTeam)gameEvent.PlayerTeam;
                PlayerTeam opposingTeam;
                
                // For Faceoff and GameEnd events (PlayerTeam = None), use Red as "team" and Blue as "opposing"
                // This ensures all players are captured in roster data
                if (playerTeam == PlayerTeam.None && 
                    (gameEvent.EventType == PlayByPlayEventType.Faceoff || 
                     gameEvent.EventType == PlayByPlayEventType.FaceoffOutcome ||
                     gameEvent.EventType == PlayByPlayEventType.GameEnd)) {
                    playerTeam = PlayerTeam.Red;
                    opposingTeam = PlayerTeam.Blue;
                }
                else {
                    opposingTeam = playerTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;
                }

                var teamPlayers = PlayerManager.Instance.GetSpawnedPlayersByTeam(playerTeam, false);
                var opposingTeamPlayers = PlayerManager.Instance.GetSpawnedPlayersByTeam(opposingTeam, false);

                // Team forwards (C, LW, RW)
                var teamForwards = teamPlayers.Where(p => {
                    string pos = GetPlayerPosition(p);
                    return pos == "C" || pos == "LW" || pos == "RW";
                }).Select(p => $"=\"{p.SteamId.Value}\"").ToList();
                gameEvent.TeamForwardsSteamID = string.Join(";", teamForwards);

                // Team defencemen (LD, RD)
                var teamDefencemen = teamPlayers.Where(p => {
                    string pos = GetPlayerPosition(p);
                    return pos == "LD" || pos == "RD";
                }).Select(p => $"=\"{p.SteamId.Value}\"").ToList();
                gameEvent.TeamDefencemenSteamID = string.Join(";", teamDefencemen);

                // Team goalie (G)
                var teamGoalies = teamPlayers.Where(p => GetPlayerPosition(p) == "G")
                    .Select(p => $"=\"{p.SteamId.Value}\"").ToList();
                gameEvent.TeamGoalieSteamID = string.Join(";", teamGoalies);

                // Opposing team forwards
                var opposingForwards = opposingTeamPlayers.Where(p => {
                    string pos = GetPlayerPosition(p);
                    return pos == "C" || pos == "LW" || pos == "RW";
                }).Select(p => $"=\"{p.SteamId.Value}\"").ToList();
                gameEvent.OpposingTeamForwardsSteamID = string.Join(";", opposingForwards);

                // Opposing team defencemen
                var opposingDefencemen = opposingTeamPlayers.Where(p => {
                    string pos = GetPlayerPosition(p);
                    return pos == "LD" || pos == "RD";
                }).Select(p => $"=\"{p.SteamId.Value}\"").ToList();
                gameEvent.OpposingTeamDefencemenSteamID = string.Join(";", opposingDefencemen);

                // Opposing team goalie
                var opposingGoalies = opposingTeamPlayers.Where(p => GetPlayerPosition(p) == "G")
                    .Select(p => $"=\"{p.SteamId.Value}\"").ToList();
                gameEvent.OpposingTeamGoalieSteamID = string.Join(";", opposingGoalies);
            }
            catch (Exception ex) {
                Logging.LogError($"Error capturing team roster data: {ex}", ModServerConfig);
            }
        }

        // ==================== EARLY DATA EXPORT - FOR USE IN CASE OF FF ====================
        // This method allows exporting both JSON and CSV early via /endgame command (for forfeits)
        /// <summary>
        /// Exports play-by-play data to CSV (internal method, can be called from GameOver)
        /// </summary>
        private static void ExportPlayByPlayCSVInternal() {
            if (_playByPlayEvents.Count == 0) {
                Logging.Log("No play-by-play events to export", ModServerConfig);
                return;
            }
            
            // Before exporting, convert any shot events with "attempt" outcomes to "missed"
            // This ensures all shots have a final outcome (attempt, on net, blocked, goal, or missed)
            int convertedAttempts = 0;
            foreach (var shotEvent in _playByPlayEvents) {
                if (shotEvent.EventType == PlayByPlayEventType.Shot && shotEvent.Outcome == "attempt") {
                    shotEvent.Outcome = "missed";
                    convertedAttempts++;
                }
            }
            if (convertedAttempts > 0) {
                Logging.Log($"Converted {convertedAttempts} pending shot 'attempt' outcomes to 'missed' before CSV export", ModServerConfig);
            }

            try {
                string statsFolderPath = Path.Combine(Path.GetFullPath("."), "stats");
                if (!Directory.Exists(statsFolderPath))
                    Directory.CreateDirectory(statsFolderPath);

                // Use the game reference ID from game start, or generate one if not set
                string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                // Sanitize FileHeaderName to remove any HTML tags that might have been added
                string sanitizedFileHeader = StripHtmlTags(ModServerConfig.FileHeaderName);
                string csvPath = Path.Combine(statsFolderPath, $"{sanitizedFileHeader}_{gameReferenceId}_playbyplay.csv");

                var csv = new StringBuilder();
                csv.AppendLine("sep=,");
                csv.AppendLine("gameReferenceId,id,period,gameTime,team,teamInPossession,currentPlayInPossession,scoreState,name,zone,outcome,flags,xCoord,yCoord,zCoord,playerJersey,playerPosition,playerName,forcemagnitude,PlayerSpeed,PuckVelocity,playerReferenceSteamID,teamForwardsSteamID,teamDefencemenSteamID,teamGoalieSteamID,opposingTeamForwardsSteamID,opposingTeamDefencemenSteamID,opposingTeamGoalieSteamID");

                int eventId = 0;
                foreach (var gameEvent in _playByPlayEvents) {
                    csv.AppendLine(string.Join(",",
                        gameReferenceId,                                    // gameReferenceId
                        eventId++,                                         // id
                        gameEvent.Period,                                  // period
                        gameEvent.GameTime.ToString("F3"),                 // gameTime
                        CsvEscape(gameEvent.Team),                          // team (team of player performing event)
                        CsvEscape(gameEvent.TeamInPossession),              // teamInPossession (team with puck possession)
                        CsvEscape(gameEvent.CurrentPlayInPossession),     // currentPlayInPossession
                        CsvEscape(gameEvent.ScoreState),                   // scoreState
                        GetEventName(gameEvent.EventType),                 // name
                        GetEventZoneString(gameEvent.Zone),                  // zone
                        CsvEscape(gameEvent.Outcome),                       // outcome
                        CsvEscape(gameEvent.Flags),                         // flags (moved to after outcome, before coordinates)
                        gameEvent.Position.x.ToString("F1"),                // xCoord
                        gameEvent.Position.y.ToString("F1"),               // yCoord
                        gameEvent.Position.z.ToString("F1"),               // zCoord
                        gameEvent.PlayerJersey.ToString(),                 // playerJersey
                        CsvEscape(gameEvent.PlayerPosition),                // playerPosition
                        CsvEscape(gameEvent.PlayerName),                   // playerName
                        gameEvent.ForceMagnitude.ToString("F2"),            // forcemagnitude
                        (gameEvent.PlayerSpeed * 3.6f).ToString("F1"),     // PlayerSpeed (km/h)
                        gameEvent.Velocity.magnitude.ToString("F2"),        // PuckVelocity (magnitude in m/s)
                        CsvEscape(gameEvent.PlayerSteamId),                 // playerReferenceSteamID
                        gameEvent.TeamForwardsSteamID,                      // teamForwardsSteamID
                        gameEvent.TeamDefencemenSteamID,                    // teamDefencemenSteamID
                        gameEvent.TeamGoalieSteamID,                        // teamGoalieSteamID
                        gameEvent.OpposingTeamForwardsSteamID,              // opposingTeamForwardsSteamID
                        gameEvent.OpposingTeamDefencemenSteamID,            // opposingTeamDefencemenSteamID
                        gameEvent.OpposingTeamGoalieSteamID                 // opposingTeamGoalieSteamID
                    ));
                }

                File.WriteAllText(csvPath, csv.ToString(), Encoding.UTF8);
                Logging.Log($"Play-by-play data exported to {csvPath} with {_playByPlayEvents.Count} events", ModServerConfig);
            }
            catch (Exception ex) {
                Logging.LogError($"Can't write the play-by-play data in the stats folder. (Permission error ?)\n{ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Exports both JSON and CSV files (used for early game end via /endgame command or natural game end)
        /// </summary>
        /// <param name="forceExport">If true, bypasses the 8+ players and 300+ events requirement (for /endgame command)</param>
        private static void ExportGameStats(bool forceExport = false) {
#if DEBUG_MODE
            DebugTrace.Section("EXPORT");
            DebugTrace.Write("EXPORT", $"ExportGameStats called. forceExport={forceExport} pbpEvents={_playByPlayEvents.Count} goals={_goals.Count}");
#endif
            if (_playByPlayEvents.Count == 0) {
                Logging.Log("No play-by-play events to export", ModServerConfig);
#if DEBUG_MODE
                DebugTrace.Write("EXPORT", "ABORT ? _playByPlayEvents is empty, nothing to export.");
                DebugTrace.Flush();
#endif
                return;
            }
            
            // Calculate game-winning goal using _goals (authoritative — GameManager scores are
            // zeroed by the time the export runs at game end).
            string gwgSteamId = "";
            try {
                int blueScore = _goals.Count(g => g.Team == "Blue");
                int redScore  = _goals.Count(g => g.Team == "Red");

                // Clear any previously-marked GWG flag so we start fresh.
                foreach (GoalInfo g in _goals) g.GWG = false;

                string winningTeamStr = blueScore > redScore ? "Blue" : (redScore > blueScore ? "Red" : "");
                if (!string.IsNullOrEmpty(winningTeamStr)) {
                    int winnerGoals = 0, loserGoals = 0;
                    int loserFinalScore = winningTeamStr == "Blue" ? redScore : blueScore;
                    foreach (GoalInfo goal in _goals.OrderBy(g => g.GameTime)) {
                        if (goal.Team == winningTeamStr) winnerGoals++;
                        else                             loserGoals++;
                        // GWG = the goal that put the winner permanently ahead
                        if (goal.Team == winningTeamStr && winnerGoals > loserGoals && winnerGoals == loserFinalScore + 1) {
                            gwgSteamId = goal.Scorer;
                            goal.GWG = true;
                            break;
                        }
                    }
                }
            }
            catch { }
            
            // Check play-by-play data before creating JSON - always validate criteria
            // Count unique SteamIDs from play-by-play events
            int uniquePlayerCount = 0;
            int eventCount = _playByPlayEvents.Count;
            
            try {
                var uniqueSteamIds = _playByPlayEvents
                    .Where(e => !string.IsNullOrEmpty(e.PlayerSteamId))
                    .Select(e => e.PlayerSteamId)
                    .Distinct()
                    .Count();
                uniquePlayerCount = uniqueSteamIds;
            }
            catch (Exception ex) {
                Logging.LogError($"Error counting unique players for export: {ex}", ModServerConfig);
            }
            
            // Validate criteria (8+ players and 300+ events) unless limit is disabled
            bool meetsCriteria = !ModServerConfig.EnableExportLimit || (uniquePlayerCount >= 8 && eventCount >= 300);
            
            // Log validation results
            if (forceExport) {
                if (meetsCriteria) {
                    if (!ModServerConfig.EnableExportLimit) {
                        Logging.Log($"Export requested via /endgame - Export limit disabled, exporting regardless of criteria", ModServerConfig);
                    } else {
                        Logging.Log($"Export requested via /endgame - Criteria met: {uniquePlayerCount} unique players (8+), {eventCount} events (300+)", ModServerConfig);
                    }
                } else {
                    Logging.Log($"Export requested via /endgame - Criteria NOT met: {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+). Export skipped.", ModServerConfig);
                }
            } else {
                if (!meetsCriteria && ModServerConfig.EnableExportLimit) {
                    Logging.Log($"Export skipped (JSON and CSV): {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)", ModServerConfig);
                }
            }
            
#if DEBUG_MODE
            DebugTrace.Write("EXPORT", $"Criteria check: uniquePlayers={uniquePlayerCount} events={eventCount} meetsCriteria={meetsCriteria} EnableExportLimit={ModServerConfig.EnableExportLimit} forceExport={forceExport}");
#endif
            // Only create and export JSON and CSV if pbp check passes (8+ players and 300+ events)
            // forceExport flag is now only used for logging - criteria must still be met
            if (meetsCriteria) {
                // Generate JSON content (same logic as game end handler)
                List<Dictionary<string, object>> playersList = new List<Dictionary<string, object>>();
                
                // Calculate Time On Ice (TOI) from play-by-play events
                CalculateTimeOnIce();
                
                // Calculate Plus/Minus from goal events
                CalculatePlusMinus();
                
                // Calculate player-level faceoff stats from play-by-play events
                Dictionary<string, int> playerFaceoffWins = new Dictionary<string, int>();
                Dictionary<string, int> playerFaceoffLosses = new Dictionary<string, int>();
                
                var faceoffOutcomeEvents = _playByPlayEvents
                    .Where(e => e.EventType == PlayByPlayEventType.FaceoffOutcome && !string.IsNullOrEmpty(e.PlayerSteamId))
                    .ToList();
                
                foreach (var faceoffEvent in faceoffOutcomeEvents) {
                    string playerSteamId = faceoffEvent.PlayerSteamId;
                    
                    if (faceoffEvent.Outcome == "successful") {
                        if (!playerFaceoffWins.TryGetValue(playerSteamId, out int _))
                            playerFaceoffWins.Add(playerSteamId, 0);
                        playerFaceoffWins[playerSteamId]++;
                    }
                    else if (faceoffEvent.Outcome == "failed") {
                        if (!playerFaceoffLosses.TryGetValue(playerSteamId, out int _))
                            playerFaceoffLosses.Add(playerSteamId, 0);
                        playerFaceoffLosses[playerSteamId]++;
                    }
                }
                
                // Calculate player-level goals and assists from goal info
                Dictionary<string, int> playerGoals = new Dictionary<string, int>();
                Dictionary<string, int> playerAssists = new Dictionary<string, int>();
                
                foreach (GoalInfo goal in _goals) {
                    // Count goals
                    if (!string.IsNullOrEmpty(goal.Scorer)) {
                        if (!playerGoals.TryGetValue(goal.Scorer, out int _))
                            playerGoals.Add(goal.Scorer, 0);
                        playerGoals[goal.Scorer]++;
                    }
                    
                    // Count assists
                    if (!string.IsNullOrEmpty(goal.PrimaryAssist)) {
                        if (!playerAssists.TryGetValue(goal.PrimaryAssist, out int _))
                            playerAssists.Add(goal.PrimaryAssist, 0);
                        playerAssists[goal.PrimaryAssist]++;
                    }
                    if (!string.IsNullOrEmpty(goal.SecondaryAssist)) {
                        if (!playerAssists.TryGetValue(goal.SecondaryAssist, out int _))
                            playerAssists.Add(goal.SecondaryAssist, 0);
                        playerAssists[goal.SecondaryAssist]++;
                    }
                }
                
                // Collect all unique SteamIDs (same logic as game end handler)
                HashSet<string> allPlayerSteamIds = new HashSet<string>();
                foreach (string steamId in _sog.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _passes.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _puckTouches.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _turnovers.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _takeaways.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _exits.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _entries.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _hits.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _possessionTimeSeconds.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _timeOnIceSeconds.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (GoalInfo goal in _goals) {
                    if (!string.IsNullOrEmpty(goal.Scorer)) allPlayerSteamIds.Add(goal.Scorer);
                    if (!string.IsNullOrEmpty(goal.PrimaryAssist)) allPlayerSteamIds.Add(goal.PrimaryAssist);
                    if (!string.IsNullOrEmpty(goal.SecondaryAssist)) allPlayerSteamIds.Add(goal.SecondaryAssist);
                }
                foreach (string steamId in _plusMinus.Keys) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (var pbpEvent in _playByPlayEvents) {
                    if (!string.IsNullOrEmpty(pbpEvent.PlayerSteamId))
                        allPlayerSteamIds.Add(pbpEvent.PlayerSteamId);
                }
                
                // Build player stats (simplified - using same logic as game end handler)
                foreach (string playerSteamId in allPlayerSteamIds) {
                    Player player = PlayerManager.Instance.GetPlayerBySteamId(playerSteamId);
                    
                    string playerName = "";
                    string teamString = "spectator";
                    string position = "";
                    bool isGoalie = false;
                    
                    // Get time on ice to determine if player actually played
                    double timeOnIce = _timeOnIceSeconds.TryGetValue(playerSteamId, out double timeOnIceValue) ? timeOnIceValue : 0.0;
                    
                    // Get player name from current player object or last play-by-play event
                    if (player != null && player) {
                        playerName = player.Username.Value.Value;
                    } else {
                        var lastEvent = _playByPlayEvents
                            .LastOrDefault(e => e.PlayerSteamId == playerSteamId && !string.IsNullOrEmpty(e.PlayerName));
                        if (lastEvent != null) {
                            playerName = lastEvent.PlayerName;
                        } else {
                            playerName = playerSteamId;
                        }
                    }
                    
                    // Determine team from play-by-play events (most common team, only Red/Blue)
                    // Only use spectator if timeOnIce is 0 or no Red/Blue events found
                    var teamEvents = _playByPlayEvents
                        .Where(e => e.PlayerSteamId == playerSteamId && 
                               (e.PlayerTeam == (int)PlayerTeam.Blue || e.PlayerTeam == (int)PlayerTeam.Red))
                        .ToList();
                    
                    if (teamEvents.Count > 0 && timeOnIce > 0) {
                        var teamGroups = teamEvents
                            .GroupBy(e => e.PlayerTeam)
                            .Select(g => new { Team = g.Key, Count = g.Count() })
                            .OrderByDescending(g => g.Count)
                            .ToList();
                        
                        if (teamGroups.Count > 0) {
                            PlayerTeam mostCommonTeam = (PlayerTeam)teamGroups[0].Team;
                            if (mostCommonTeam == PlayerTeam.Blue) {
                                teamString = "Blue";
                            } else if (mostCommonTeam == PlayerTeam.Red) {
                                teamString = "Red";
                            }
                        }
                    }
                    // If timeOnIce is 0 or no Red/Blue events found, keep as "spectator" (default)
                    
                    // Determine position from play-by-play events (most common position)
                    var positionEvents = _playByPlayEvents
                        .Where(e => e.PlayerSteamId == playerSteamId && !string.IsNullOrEmpty(e.PlayerPosition))
                        .ToList();
                    
                    if (positionEvents.Count > 0) {
                        var positionGroups = positionEvents
                            .GroupBy(e => e.PlayerPosition)
                            .Select(g => new { Position = g.Key, Count = g.Count(), LastEvent = g.OrderByDescending(e => e.GameTime).First() })
                            .OrderByDescending(g => g.Count)
                            .ThenByDescending(g => g.LastEvent.GameTime)
                            .ToList();
                        
                        if (positionGroups.Count > 0) {
                            position = positionGroups[0].Position;
                            isGoalie = position == "G";
                        }
                    }
                    
                    // Fallback: if no position found in play-by-play but player exists, use current position
                    if (string.IsNullOrEmpty(position) && player != null && player) {
                        position = GetPlayerPosition(player);
                        isGoalie = PlayerFunc.IsGoalie(player);
                    }
                    
                    // If still no position, set to empty string (not "N/A")
                    if (string.IsNullOrEmpty(position)) {
                        position = "";
                    }
                    
                    Dictionary<string, object> playerStats = new Dictionary<string, object> {
                        { "steamId", playerSteamId },
                        { "name", playerName },
                        { "team", teamString },
                        { "position", position },
                        { "goals", playerGoals.TryGetValue(playerSteamId, out int goals) ? goals : 0 },
                        { "assists", playerAssists.TryGetValue(playerSteamId, out int assists) ? assists : 0 },
                        { "sog", _sog.TryGetValue(playerSteamId, out int sog) ? sog : 0 },
                        { "passes", _passes.TryGetValue(playerSteamId, out int passes) ? passes : 0 },
                        { "exits", _exits.TryGetValue(playerSteamId, out int exits) ? exits : 0 },
                        { "entries", _entries.TryGetValue(playerSteamId, out int entries) ? entries : 0 },
                        { "hits", _hits.TryGetValue(playerSteamId, out int hits) ? hits : 0 },
                        { "blocks", _blocks.TryGetValue(playerSteamId, out int blocks) ? blocks : 0 },
                        { "turnovers", _turnovers.TryGetValue(playerSteamId, out int turnovers) ? turnovers : 0 },
                        { "takeaways", _takeaways.TryGetValue(playerSteamId, out int takeaways) ? takeaways : 0 },
                        { "puckTouches", _puckTouches.TryGetValue(playerSteamId, out int puckTouches) ? puckTouches : 0 },
                        { "possessionTimeSeconds", Math.Round(_possessionTimeSeconds.TryGetValue(playerSteamId, out double possessionTime) ? possessionTime : 0.0, 1) },
                        { "timeOnIce", Math.Round(_timeOnIceSeconds.TryGetValue(playerSteamId, out double toi) ? toi : 0.0, 1) },
                        { "faceoffWins", playerFaceoffWins.TryGetValue(playerSteamId, out int fw) ? fw : 0 },
                        { "faceoffLosses", playerFaceoffLosses.TryGetValue(playerSteamId, out int fl) ? fl : 0 }
                    };
                    
                    // Add +/- only for skaters (not goalies)
                    if (!isGoalie) {
                        playerStats["plusMinus"] = _plusMinus.TryGetValue(playerSteamId, out int pm) ? pm : 0;
                    }
                    
                    if (isGoalie || _savePerc.ContainsKey(playerSteamId)) {
                        int shotsFaced = 0;
                        int saves = 0;
                        if (_savePerc.TryGetValue(playerSteamId, out var savePercValue)) {
                            shotsFaced = savePercValue.Shots;
                            saves = savePercValue.Saves;
                        }
                        
                        // goalsAllowed = goals scored while this specific goalie was in net (excludes empty net / other stints)
                        int goalsAllowedFromList = CountGoalsAllowedForGoalie(playerSteamId);
                        shotsFaced = saves + goalsAllowedFromList;
                        if (_savePerc.TryGetValue(playerSteamId, out var spAfterExport))
                            _savePerc[playerSteamId] = (saves, shotsFaced);
                        playerStats["shotsFaced"] = shotsFaced;
                        playerStats["saves"] = saves;
                        playerStats["goalsAllowed"] = goalsAllowedFromList;
                        playerStats["saveperc"] = shotsFaced > 0 ? (double)saves / shotsFaced : 0.0;
                        playerStats["bodySaves"] = _bodySaves.TryGetValue(playerSteamId, out int bodySaves) ? bodySaves : 0;
                        playerStats["stickSaves"] = _stickSaves.TryGetValue(playerSteamId, out int stickSaves) ? stickSaves : 0;
                    }
                    
                    playersList.Add(playerStats);
                }
                
                // Calculate team stats
                // Blocks are per-player only — aggregate from the player list
                int blueTeamBlocks = 0, redTeamBlocks = 0;
                foreach (var ps in playersList) {
                    var p = (Dictionary<string, object>)ps;
                    int b = p.TryGetValue("blocks", out object bVal) ? (int)bVal : 0;
                    string t = p.TryGetValue("team", out object tVal) ? (string)tVal : "";
                    if (t == "Blue") blueTeamBlocks += b;
                    else if (t == "Red") redTeamBlocks += b;
                }

                int blueTeamSogs = _teamShots.TryGetValue(PlayerTeam.Blue, out int bs) ? bs : 0;
                int redTeamSogs = _teamShots.TryGetValue(PlayerTeam.Red, out int rs) ? rs : 0;
                int blueTeamPasses = _teamPasses.TryGetValue(PlayerTeam.Blue, out int bp) ? bp : 0;
                int redTeamPasses = _teamPasses.TryGetValue(PlayerTeam.Red, out int rp) ? rp : 0;
                double blueTeamPossessionTime = _teamPossessionTime.TryGetValue(PlayerTeam.Blue, out double bpt) ? bpt : 0.0;
                double redTeamPossessionTime = _teamPossessionTime.TryGetValue(PlayerTeam.Red, out double rpt) ? rpt : 0.0;
                int blueTeamTakeaways = _teamTakeaways.TryGetValue(PlayerTeam.Blue, out int btk) ? btk : 0;
                int redTeamTakeaways = _teamTakeaways.TryGetValue(PlayerTeam.Red, out int rtk) ? rtk : 0;
                int blueTeamTurnovers = _teamTurnovers.TryGetValue(PlayerTeam.Blue, out int bto) ? bto : 0;
                int redTeamTurnovers = _teamTurnovers.TryGetValue(PlayerTeam.Red, out int rto) ? rto : 0;
                int blueTeamDZExits = _teamExits.TryGetValue(PlayerTeam.Blue, out int bte) ? bte : 0;
                int redTeamDZExits = _teamExits.TryGetValue(PlayerTeam.Red, out int rte) ? rte : 0;
                int blueTeamOZEntries = _teamEntries.TryGetValue(PlayerTeam.Blue, out int bten) ? bten : 0;
                int redTeamOZEntries = _teamEntries.TryGetValue(PlayerTeam.Red, out int rten) ? rten : 0;
                int blueFaceoffWins = _teamFaceoffWins.TryGetValue(PlayerTeam.Blue, out int bfw) ? bfw : 0;
                int redFaceoffWins = _teamFaceoffWins.TryGetValue(PlayerTeam.Red, out int rfw) ? rfw : 0;
                int blueFaceoffTotal = _teamFaceoffTotal.TryGetValue(PlayerTeam.Blue, out int bft) ? bft : 0;
                int redFaceoffTotal = _teamFaceoffTotal.TryGetValue(PlayerTeam.Red, out int rft) ? rft : 0;
                
                // Calculate actual game time from events (not hardcoded)
                // This reflects the true game duration, including early endings (forfeits)
                // Prefer GameEnd event's GameTime if available, otherwise use max from all events
                var gameEndEvent = _playByPlayEvents.FirstOrDefault(e => e.EventType == PlayByPlayEventType.GameEnd);
                float totalGameTimeSeconds;
                if (gameEndEvent != null) {
                    // Use GameEnd event's GameTime (should be 900.0 for regulation games)
                    totalGameTimeSeconds = gameEndEvent.GameTime;
                } else {
                    // Fallback: use max from all events if GameEnd doesn't exist
                    totalGameTimeSeconds = _playByPlayEvents.Count > 0 ? _playByPlayEvents.Max(e => e.GameTime) : 0f;
                }
                
                double totalGameTimeMinutes = totalGameTimeSeconds / 60.0;
                
                Dictionary<string, object> jsonDict = new Dictionary<string, object> {
                    { "players", playersList },
                    { "teamStats", new Dictionary<string, object> {
                        { "blueTeamSogs", blueTeamSogs },
                        { "blueTeamSogsAg", redTeamSogs },
                        { "blueTeamPasses", blueTeamPasses },
                        { "blueTeamPassesAg", redTeamPasses },
                        { "blueFaceoffsWon", blueFaceoffWins },
                        { "blueFaceoffsLost", blueFaceoffTotal > 0 ? blueFaceoffTotal - blueFaceoffWins : 0 },
                        { "blueTakeaways", blueTeamTakeaways },
                        { "blueTurnovers", blueTeamTurnovers },
                        { "blueBlocks", blueTeamBlocks },
                        { "blueDZExits", blueTeamDZExits },
                        { "blueDZExitsAllowed", redTeamDZExits },
                        { "blueOZEntries", blueTeamOZEntries },
                        { "blueOZEntriesAllowed", redTeamOZEntries },
                        { "blueTeamPossessionTime", blueTeamPossessionTime },
                        { "blueTeamPossessionTimeAg", redTeamPossessionTime },
                        { "redTeamSogs", redTeamSogs },
                        { "redTeamSogsAg", blueTeamSogs },
                        { "redTeamPasses", redTeamPasses },
                        { "redTeamPassesAg", blueTeamPasses },
                        { "redFaceoffsWon", redFaceoffWins },
                        { "redFaceoffsLost", redFaceoffTotal > 0 ? redFaceoffTotal - redFaceoffWins : 0 },
                        { "redTakeaways", redTeamTakeaways },
                        { "redTurnovers", redTeamTurnovers },
                        { "redBlocks", redTeamBlocks },
                        { "redDZExits", redTeamDZExits },
                        { "redDZExitsAllowed", blueTeamDZExits },
                        { "redOZEntries", redTeamOZEntries },
                        { "redOZEntriesAllowed", blueTeamOZEntries },
                        { "redTeamPossessionTime", redTeamPossessionTime },
                        { "redTeamPossessionTimeAg", blueTeamPossessionTime },
                        { "goals", _goals.OrderBy(g => g.GameTime).Select(g => new {
                            gameTime = g.GameTime,
                            period = g.Period,
                            team = g.Team,
                            scorer = g.Scorer,
                            primaryAssist = g.PrimaryAssist,
                            secondaryAssist = g.SecondaryAssist,
                            gwg = g.GWG
                        }).ToList() },
                        { "gwg", gwgSteamId },
                        { "stars", ComputeAndSetStars(playersList, gwgSteamId) },
                        { "gameTimeMinutes", totalGameTimeMinutes }
                    }}
                };
                
                string jsonContent = JsonConvert.SerializeObject(jsonDict, Formatting.Indented);
                Logging.Log("Stats:" + jsonContent, ModServerConfig);
#if DEBUG_MODE
                DebugTrace.Write("EXPORT", $"JSON serialized. contentLength={jsonContent.Length} chars. SaveEOGJSON={ModServerConfig.SaveEOGJSON}");
#endif
                // Export JSON file
                if (ModServerConfig.SaveEOGJSON) {
                    try {
                        string statsFolderPath = Path.Combine(Path.GetFullPath("."), "stats");
                        if (!Directory.Exists(statsFolderPath))
                            Directory.CreateDirectory(statsFolderPath);
                        
                        string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                        string sanitizedFileHeader = StripHtmlTags(ModServerConfig.FileHeaderName);
                        string jsonPath = Path.Combine(statsFolderPath, sanitizedFileHeader + "_" + gameReferenceId + "_stats.json");
                        
                        File.WriteAllText(jsonPath, jsonContent);
                        Logging.Log($"JSON exported: {uniquePlayerCount} unique players, {eventCount} events", ModServerConfig);
#if DEBUG_MODE
                        DebugTrace.Write("EXPORT", $"JSON written OK ? {jsonPath}  ({jsonContent.Length} bytes)");
#endif
                    }
                    catch (Exception ex) {
                        Logging.LogError($"Can't write the end of game stats in the stats folder. (Permission error ?)\n{ex}", ModServerConfig);
#if DEBUG_MODE
                        DebugTrace.Write("EXPORT", $"JSON write FAILED: {ex.Message}");
#endif
                    }
                }
#if DEBUG_MODE
                else {
                    DebugTrace.Write("EXPORT", "JSON export skipped ? SaveEOGJSON=false in server config.");
                }
#endif
                // Export CSV
                ExportPlayByPlayCSVInternal();
                Logging.Log($"CSV exported: {uniquePlayerCount} unique players, {eventCount} events", ModServerConfig);
#if DEBUG_MODE
                DebugTrace.Write("EXPORT", $"CSV export complete. Total pbpEvents={_playByPlayEvents.Count}");
                DebugTrace.Flush();
#endif
            }
        }

        /// <summary>
        /// Chat command handler for server chat commands.
        /// Postfix handles /statsversion and /endgame on Client_SendChatMessageRpc.
        /// Chat output Prefixes suppress any stray "Unknown command" broadcast (async fallback).
        /// </summary>
        [HarmonyPatch(typeof(ChatManager), "Client_SendChatMessageRpc")]
        public static class ChatManager_Client_SendChatMessageRpc_Patch {
            [HarmonyPrefix]
            public static void Prefix(string content, bool isQuickChat, bool isTeamChat, Unity.Netcode.RpcParams rpcParams) {
                if (!ServerFunc.IsDedicatedServer() || !_logic)
                    return;

                if (IsStatsModChatCommand(content))
                    MarkSuppressUnknownChatCommand();
            }

            [HarmonyPostfix]
            public static void Postfix(string content, bool isQuickChat, bool isTeamChat, Unity.Netcode.RpcParams rpcParams) {
                try {
                    if (!ServerFunc.IsDedicatedServer() || !_logic)
                        return;

                    TryHandleStatsModChatCommand(content, rpcParams);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in chat command handler: {ex}", ModServerConfig);
                }
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.Server_BroadcastChatMessage), typeof(string), typeof(string))]
        public static class ChatManager_SuppressUnknownCommandBroadcast_Patch {
            [HarmonyPrefix]
            public static bool Prefix(string content, string color) {
                return !ShouldSuppressUnknownChatOutput(content);
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.Server_BroadcastChatMessage), typeof(ChatMessage))]
        public static class ChatManager_SuppressUnknownCommandBroadcastChatMessage_Patch {
            [HarmonyPrefix]
            public static bool Prefix(ChatMessage chatMessage) {
                return !ShouldSuppressUnknownChatOutput(GetChatMessageText(chatMessage));
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.Server_SendChatMessage), typeof(string), typeof(string), typeof(ulong[]))]
        public static class ChatManager_SuppressUnknownCommandSend_Patch {
            [HarmonyPrefix]
            public static bool Prefix(string content, string color, ulong[] clientIds) {
                return !ShouldSuppressUnknownChatOutput(content);
            }
        }

        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.Server_SendChatMessage), typeof(ChatMessage), typeof(ulong[]))]
        public static class ChatManager_SuppressUnknownCommandSendChatMessage_Patch {
            [HarmonyPrefix]
            public static bool Prefix(ChatMessage chatMessage, ulong[] clientIds) {
                return !ShouldSuppressUnknownChatOutput(GetChatMessageText(chatMessage));
            }
        }

        private static void MarkSuppressUnknownChatCommand() {
            _suppressUnknownChatCommandUntil = Time.time + 2f;
        }

        private static bool ShouldSuppressUnknownChatOutput(string content) {
            if (Time.time > _suppressUnknownChatCommandUntil || !IsUnknownCommandBroadcast(content))
                return false;

            _suppressUnknownChatCommandUntil = 0f;
            return true;
        }

        private static string GetChatMessageText(object chatMessage) {
            if (chatMessage == null)
                return "";

            if (chatMessage is ChatMessage typed)
                return typed.Content.ToString();

            try {
                FieldInfo contentField = chatMessage.GetType().GetField("Content");
                return contentField?.GetValue(chatMessage)?.ToString() ?? "";
            }
            catch {
                return "";
            }
        }

        private static bool IsStatsModChatCommand(string msg) {
            if (string.IsNullOrEmpty(msg))
                return false;

            msg = msg.Trim();
            return msg.Equals("/statsversion", StringComparison.OrdinalIgnoreCase)
                || msg.StartsWith("/endgame", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnknownCommandBroadcast(string content) {
            return !string.IsNullOrEmpty(content)
                && content.Trim().Equals("Unknown command", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryHandleStatsModChatCommand(string content, Unity.Netcode.RpcParams rpcParams) {
            if (string.IsNullOrEmpty(content))
                return;

            string msg = content;

            if (msg.Trim().Equals("/statsversion", StringComparison.OrdinalIgnoreCase)) {
                if (NetworkBehaviourSingleton<ChatManager>.Instance != null) {
                    ulong senderClientId = rpcParams.Receive.SenderClientId;
                    Player senderPlayer = PlayerManager.Instance.GetPlayerByClientId(senderClientId);
                    string senderName = (senderPlayer != null && !string.IsNullOrEmpty(senderPlayer.Username.Value.Value))
                        ? senderPlayer.Username.Value.Value
                        : $"Client {senderClientId}";

                    string senderVersionDisplay = "unknown";
                    bool versionOk = false;
                    if (_clientReportedModVersions.TryGetValue(senderClientId, out string reportedVersion)) {
                        senderVersionDisplay = FormatReportedClientModVersion(reportedVersion);
                        versionOk = IsClientModVersionCompatible(reportedVersion, MOD_VERSION);
                    }

                    string status = versionOk ? "✅" : "❌";
                    NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage(
                        $"Stats mod server version: {MOD_VERSION}. {senderName} is using {senderVersionDisplay} {status}");
                }
                return;
            }

            if (msg.StartsWith("/endgame", StringComparison.OrdinalIgnoreCase)) {
                ulong senderClientId = rpcParams.Receive.SenderClientId;
                Player senderPlayer = PlayerManager.Instance.GetPlayerByClientId(senderClientId);
                string senderSteamId = senderPlayer?.SteamId.Value.Value ?? "";
                bool isAdmin = !string.IsNullOrEmpty(senderSteamId) &&
                               MonoBehaviourSingleton<AdminManager>.Instance != null &&
                               MonoBehaviourSingleton<AdminManager>.Instance.IsSteamIdAdmin(senderSteamId);
                if (!isAdmin) {
                    Logging.Log($"/endgame denied for client {senderClientId} (steamId={senderSteamId}) ? not an admin", ModServerConfig);
                    return;
                }

                if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Warmup) {
                    Logging.Log("Early game end export command received during warmup - no game to export", ModServerConfig);
                    if (NetworkBehaviourSingleton<ChatManager>.Instance != null) {
                        NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage("Cannot export stats during warmup - no game has started yet.");
                    }
                    return;
                }

                Logging.Log("Early game end export command received", ModServerConfig);
                int uniquePlayerCount = 0;
                int eventCount = _playByPlayEvents.Count;

                try {
                    uniquePlayerCount = _playByPlayEvents
                        .Where(e => !string.IsNullOrEmpty(e.PlayerSteamId))
                        .Select(e => e.PlayerSteamId)
                        .Distinct()
                        .Count();
                }
                catch (Exception ex) {
                    Logging.LogError($"Error counting unique players for /endgame export: {ex}", ModServerConfig);
                }

                bool meetsCriteria = !ModServerConfig.EnableExportLimit || (uniquePlayerCount >= 8 && eventCount >= 300);

                if (meetsCriteria) {
                    if (!ModServerConfig.EnableExportLimit) {
                        Logging.Log($"Export requested via /endgame - Export limit disabled, exporting regardless of criteria", ModServerConfig);
                    }
                    ExportGameStats(forceExport: true);
                    if (NetworkBehaviourSingleton<ChatManager>.Instance != null) {
                        string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                        string sanitizedFileHeader = StripHtmlTags(ModServerConfig.FileHeaderName);
                        string fullFileName = $"{sanitizedFileHeader}_{gameReferenceId}_stats";
                        NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage($"Game stats exported early - {fullFileName}");
                    }
                } else if (ModServerConfig.EnableExportLimit && NetworkBehaviourSingleton<ChatManager>.Instance != null) {
                    NetworkBehaviourSingleton<ChatManager>.Instance.Server_BroadcastChatMessage($"Cannot export stats - Game does not meet criteria: {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)");
                }
            }
        }
        // ==================== END DEBUG SECTION ====================

        /// <summary>
        /// Escapes CSV values
        /// </summary>
        private static string CsvEscape(string value) {
            if (string.IsNullOrEmpty(value))
                return "";

            // Use Excel formula format for SteamIDs (long numeric strings) to prevent truncation
            if (value.Length >= 15 && value.All(char.IsDigit)) {
                return "=\"" + value + "\"";
            }

            // Quote values that contain special characters
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n")) {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        /// <summary>
        /// Gets the event name string
        /// </summary>
        private static string GetEventName(PlayByPlayEventType eventType) {
            switch (eventType) {
                case PlayByPlayEventType.Touch:
                    return "touch";
                case PlayByPlayEventType.Takeaway:
                    return "takeaway";
                case PlayByPlayEventType.Turnover:
                    return "turnover";
                case PlayByPlayEventType.Pass:
                    return "pass";
                case PlayByPlayEventType.Shot:
                    return "shot";
                case PlayByPlayEventType.Save:
                    return "save";
                case PlayByPlayEventType.Goal:
                    return "goal";
                case PlayByPlayEventType.OwnGoal:
                    return "owngoal";
                case PlayByPlayEventType.Hit:
                    return "hit";
                case PlayByPlayEventType.Block:
                    return "block";
                case PlayByPlayEventType.PuckBattle:
                    return "puckbattle";
                case PlayByPlayEventType.Faceoff:
                    return "faceoff";
                case PlayByPlayEventType.FaceoffOutcome:
                    return "faceoffoutcome";
                case PlayByPlayEventType.DZExit:
                    return "dzexit";
                case PlayByPlayEventType.OZEntry:
                    return "ozentry";
                case PlayByPlayEventType.GameEnd:
                    return "gameend";
                default:
                    return "touch";
            }
        }

        /// <summary>
        /// Gets the event zone string
        /// </summary>
        private static string GetEventZoneString(EventZone zone) {
            switch (zone) {
                case EventZone.Neutral:
                    return "nz";
                case EventZone.Defensive:
                    return "dz";
                case EventZone.Offensive:
                    return "oz";
                default:
                    return "nz";
            }
        }
        #endregion

        /// <summary>
        /// Scans play-by-play events and adds zone exit/entry flags retroactively.
        /// Runs every 6 seconds to catch zone transitions.
        /// Only processes new events since last scan for efficiency.
        /// </summary>
        private static void AddZoneFlagsToPlayByPlayEvents() {
            try {
                if (_playByPlayEvents == null || _playByPlayEvents.Count < 2)
                    return;

                // Find the starting index - start from the event after the last processed one
                int startIndex = 1; // Always need previous event for comparison
                if (_lastProcessedZoneFlagEventId >= 0) {
                    // Find the index of the last processed event
                    for (int i = 0; i < _playByPlayEvents.Count; i++) {
                        if (_playByPlayEvents[i].EventId == _lastProcessedZoneFlagEventId) {
                            startIndex = i + 1; // Start from the event after the last processed one
                            break;
                        }
                    }
                }

                // If no new events, skip
                if (startIndex >= _playByPlayEvents.Count)
                    return;

                // Track which events we've already processed (to avoid duplicate flags)
                HashSet<int> processedEventIds = new HashSet<int>();

                // Iterate through new events only, checking for zone transitions
                for (int i = startIndex; i < _playByPlayEvents.Count; i++) {
                    var currentEvent = _playByPlayEvents[i];
                    
                    // Skip hit events - they don't change possession
                    if (currentEvent.EventType == PlayByPlayEventType.Hit)
                        continue;
                    
                    // Find the last non-hit, non-faceoff event from the same team (possession event)
                    // Skip failed touches from other teams - they don't change possession
                    PlayByPlayEvent previousEvent = null;
                    for (int j = i - 1; j >= 0; j--) {
                        var candidateEvent = _playByPlayEvents[j];
                        // Skip hits and faceoffs - they don't change possession
                        if (candidateEvent.EventType == PlayByPlayEventType.Hit || 
                            candidateEvent.EventType == PlayByPlayEventType.Faceoff)
                            continue;
                        // Skip failed touches from other teams - they don't change possession
                        bool isFailedTouchFromOtherTeam = (candidateEvent.EventType == PlayByPlayEventType.Touch && 
                                                          candidateEvent.Outcome == "failed" && 
                                                          candidateEvent.PlayerTeam != currentEvent.PlayerTeam);
                        if (isFailedTouchFromOtherTeam)
                            continue;
                        // Found a possession event from the same team
                        if (candidateEvent.PlayerTeam == currentEvent.PlayerTeam) {
                            previousEvent = candidateEvent;
                            break;
                        }
                        // If different team with successful event, stop looking (possession changed)
                        if (candidateEvent.PlayerTeam != currentEvent.PlayerTeam)
                            break;
                    }
                    
                    // Skip if no previous possession event found
                    if (previousEvent == null)
                        continue;

                    // Check if zone advanced forward (DZ->NZ, NZ->OZ, DZ->OZ)
                    EventZone prevZone = previousEvent.Zone;
                    EventZone currZone = currentEvent.Zone;
                    string zoneFlags = "";

                    if (prevZone == EventZone.Defensive && currZone == EventZone.Neutral) {
                        zoneFlags = "DZExit";
                    }
                    else if (prevZone == EventZone.Neutral && currZone == EventZone.Offensive) {
                        zoneFlags = "OZEntry";
                    }
                    else if (prevZone == EventZone.Defensive && currZone == EventZone.Offensive) {
                        zoneFlags = "DZExit,OZEntry";
                    }

                    if (string.IsNullOrEmpty(zoneFlags))
                        continue;

                    // Only detect zone transitions when comparing events from the same player
                    // This prevents false transitions when comparing different players on the same team
                    if (previousEvent.PlayerSteamId != currentEvent.PlayerSteamId) {
                        // Different players - only detect transition if it's a direct pass between them
                        // Otherwise, skip to avoid false positives
                        if (previousEvent.EventType != PlayByPlayEventType.Pass) {
                            continue;
                        }
                    }
                    
                    // Check if the team has already made this zone transition recently
                    // Allow a new entry/exit if the team has touched the puck outside the zone being entered
                    bool transitionAlreadyRecorded = false;
                    if (currentEvent.PlayerTeam != 0) {
                        // Find the last event from the same team that had the same zone flags
                        for (int k = i - 1; k >= 0; k--) {
                            var checkEvent = _playByPlayEvents[k];
                            // Only check events from the same team
                            if (checkEvent.PlayerTeam != currentEvent.PlayerTeam)
                                continue;
                            
                            // If we find a previous event with the same zone flags, check if team has touched outside the zone
                            if (!string.IsNullOrEmpty(checkEvent.Flags)) {
                                if (zoneFlags.Contains("DZExit") && checkEvent.Flags.Contains("DZExit")) {
                                    // Check if possession changed (an event from a different team occurred between the last DZExit and the current event)
                                    // Only ignore hits - all other events from opposing team indicate possession changed
                                    bool possessionChanged = false;
                                    for (int m = k + 1; m < i; m++) {
                                        var betweenEvent = _playByPlayEvents[m];
                                        
                                        // Check for any event from different team (possession changed)
                                        if (betweenEvent.PlayerTeam != currentEvent.PlayerTeam) {
                                            // Skip hits - they don't change possession
                                            if (betweenEvent.EventType != PlayByPlayEventType.Hit) {
                                                possessionChanged = true;
                                                break;
                                            }
                                        }
                                    }
                                    
                                    // Block new exit if possession hasn't changed (no event from different team, excluding hits)
                                    if (!possessionChanged) {
                                        transitionAlreadyRecorded = true;
                                        break;
                                    }
                                }
                                if (zoneFlags.Contains("OZEntry") && checkEvent.Flags.Contains("OZEntry")) {
                                    // Check if possession changed (an event from a different team occurred between the last OZEntry and the current event)
                                    // Only ignore hits - all other events from opposing team indicate possession changed
                                    bool possessionChanged = false;
                                    for (int m = k + 1; m < i; m++) {
                                        var betweenEvent = _playByPlayEvents[m];
                                        
                                        // Check for any event from different team (possession changed)
                                        if (betweenEvent.PlayerTeam != currentEvent.PlayerTeam) {
                                            // Skip hits - they don't change possession
                                            if (betweenEvent.EventType != PlayByPlayEventType.Hit) {
                                                possessionChanged = true;
                                                break;
                                            }
                                        }
                                    }
                                    
                                    // Block new entry if possession hasn't changed (no event from different team, excluding hits)
                                    if (!possessionChanged) {
                                        transitionAlreadyRecorded = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (transitionAlreadyRecorded)
                        continue;

                    // Determine which event should get the flag and check if it already has it
                    PlayByPlayEvent targetEvent = null;
                    bool shouldAddFlags = false;
                    
                    // If previous event was a Pass, flag goes on that Pass event
                    if (previousEvent.EventType == PlayByPlayEventType.Pass) {
                        targetEvent = previousEvent;
                        // Check if this Pass event already has the zone flags
                        if (string.IsNullOrEmpty(previousEvent.Flags) || 
                            !previousEvent.Flags.Contains(zoneFlags.Split(',')[0])) {
                            shouldAddFlags = true;
                        }
                    }
                    // Otherwise, flag goes on current Touch event
                    else if (currentEvent.EventType == PlayByPlayEventType.Touch) {
                        targetEvent = currentEvent;
                        // Check if this Touch event already has the zone flags
                        if (string.IsNullOrEmpty(currentEvent.Flags) || 
                            !currentEvent.Flags.Contains(zoneFlags.Split(',')[0])) {
                            shouldAddFlags = true;
                        }
                    }

                    // Only add flags and update stats if we haven't processed this event yet
                    if (targetEvent != null && shouldAddFlags && !processedEventIds.Contains(targetEvent.EventId)) {
                        // Add flags to the target event
                        if (string.IsNullOrEmpty(targetEvent.Flags)) {
                            targetEvent.Flags = zoneFlags;
                        } else {
                            targetEvent.Flags += "," + zoneFlags;
                        }
                        processedEventIds.Add(targetEvent.EventId);
                        
                        // Update stats for the player (only once per event)
                        if (!string.IsNullOrEmpty(targetEvent.PlayerSteamId)) {
                            if (zoneFlags.Contains("DZExit")) {
                                if (!_exits.TryGetValue(targetEvent.PlayerSteamId, out int _))
                                    _exits.Add(targetEvent.PlayerSteamId, 0);
                                _exits[targetEvent.PlayerSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.EXIT + targetEvent.PlayerSteamId, _exits[targetEvent.PlayerSteamId].ToString());
                                
                                // Track team exit stat
                                Player exitPlayer = PlayerManager.Instance.GetPlayerBySteamId(targetEvent.PlayerSteamId);
                                if (exitPlayer != null && exitPlayer) {
                                    PlayerTeam playerTeam = exitPlayer.Team;
                                    if (!_teamExits.TryGetValue(playerTeam, out int _))
                                        _teamExits.Add(playerTeam, 0);
                                    _teamExits[playerTeam] += 1;
                                    QueueStatUpdate(Codebase.Constants.TEAM_EXITS + playerTeam.ToString(), _teamExits[playerTeam].ToString());
                                }
                            }
                            if (zoneFlags.Contains("OZEntry")) {
                                if (!_entries.TryGetValue(targetEvent.PlayerSteamId, out int _))
                                    _entries.Add(targetEvent.PlayerSteamId, 0);
                                _entries[targetEvent.PlayerSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.ENTRY + targetEvent.PlayerSteamId, _entries[targetEvent.PlayerSteamId].ToString());
                                
                                // Track team entry stat
                                Player entryPlayer = PlayerManager.Instance.GetPlayerBySteamId(targetEvent.PlayerSteamId);
                                if (entryPlayer != null && entryPlayer) {
                                    PlayerTeam playerTeam = entryPlayer.Team;
                                    if (!_teamEntries.TryGetValue(playerTeam, out int _))
                                        _teamEntries.Add(playerTeam, 0);
                                    _teamEntries[playerTeam] += 1;
                                    QueueStatUpdate(Codebase.Constants.TEAM_ENTRIES + playerTeam.ToString(), _teamEntries[playerTeam].ToString());
                                }
                            }
                        }
                    }
                }
                
                // Update the last processed event ID to the most recent event
                if (_playByPlayEvents.Count > 0) {
                    _lastProcessedZoneFlagEventId = _playByPlayEvents[_playByPlayEvents.Count - 1].EventId;
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in AddZoneFlagsToPlayByPlayEvents: {ex}", ModServerConfig);
            }
        }

        /// <summary>
        /// Queues a stat update to be sent in the next batch.
        /// </summary>
        /// <param name="dataName">String, name of the data.</param>
        /// <param name="dataStr">String, value of the data.</param>
        private static void QueueStatUpdate(string dataName, string dataStr) {
            _pendingStatUpdates[dataName] = dataStr;
        }

        /// <summary>
        /// Sends all queued stat updates to all clients in batched format.
        /// </summary>
        /// <summary>
        /// Validates a string before sending to network to prevent segfaults.
        /// Returns true if safe to send, false otherwise.
        /// </summary>
        private static bool ValidateStringForNetwork(string data, string dataName, out string error) {
            error = null;
            
            if (data == null) {
                error = "Data is null";
                return false;
            }
            
            if (dataName == null) {
                error = "Data name is null";
                return false;
            }
            
            // Check dataName size first
            int dataNameByteSize = Encoding.UTF8.GetByteCount(dataName);
            if (dataNameByteSize > 1024) {
                error = $"Data name size ({dataNameByteSize} bytes) exceeds limit";
                return false;
            }
            
            // Check UTF-8 byte size - limit to 48KB to prevent MemCpy issues
            // Account for dataName size + sizeof(ulong) overhead (8 bytes) that NetworkCommunication.SendDataToAll adds
            const int MAX_SAFE_BYTES = 49152;
            int dataByteSize = Encoding.UTF8.GetByteCount(data);
            int totalByteSize = dataNameByteSize + sizeof(ulong) + dataByteSize;
            
            if (totalByteSize > MAX_SAFE_BYTES) {
                error = $"Total size ({totalByteSize} bytes = {dataNameByteSize} name + 8 overhead + {dataByteSize} data) exceeds safe limit ({MAX_SAFE_BYTES} bytes)";
                return false;
            }
            
            // Also check data size alone as a secondary safety check
            if (dataByteSize > 48000) {
                error = $"Data size ({dataByteSize} bytes) exceeds safe limit (48000 bytes)";
                return false;
            }
            
            // Check for extremely long strings (character count sanity check)
            if (data.Length > 100000) {
                error = $"Data length ({data.Length} characters) exceeds sanity limit";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Safely sends data to all clients with validation and error handling.
        /// Returns true if successful, false if failed.
        /// </summary>
        private static bool SafeSendDataToAll(string dataName, string dataValue, NetworkDelivery networkDelivery = NetworkDelivery.ReliableFragmentedSequenced) {
            if (!ValidateStringForNetwork(dataValue, dataName, out string validationError)) {
                Logging.LogError($"Validation failed for {dataName}: {validationError}", ModServerConfig);
                return false;
            }
            // Avoid calling into network layer when manager is null (e.g. shutdown) to prevent MemCpy segfault
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null) {
                Logging.LogError("SafeSendDataToAll: NetworkManager not ready, skipping send", ModServerConfig);
                return false;
            }
            try {
                NetworkCommunication.SendDataToAll(dataName, dataValue, Constants.FROM_SERVER_TO_CLIENT, ModServerConfig, networkDelivery);
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Error sending data {dataName}: {ex}", ModServerConfig);
                return false;
            }
        }
        
        private static void SendBatchedStatUpdates() {
            // Check circuit breaker - if batching is disabled, skip silently
            DateTime now = DateTime.UtcNow;
            if (_batchingDisabled) {
                // Check if we should re-enable batching
                if ((now - _lastBatchingFailureTime).TotalSeconds >= BATCHING_DISABLE_DURATION_SECONDS) {
                    _batchingDisabled = false;
                    _batchingFailureCount = 0;
                    Logging.Log("Batching re-enabled after cooldown period", ModServerConfig);
                }
                else {
                    // Silently skip batching - game continues without stat updates
                    return;
                }
            }
            
            if (_pendingStatUpdates.Count == 0) {
                _lastStatBatchSendTime = now;
                return;
            }
            // Don't touch network when manager isn't ready (avoids MemCpy segfault in SendDataToAll)
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null)
                return;
            
            // Wrap entire method in try-catch to prevent server crashes
            try {
            // Create a snapshot of pending updates to avoid thread safety issues during iteration
            // LockDictionary's enumerator doesn't maintain lock during iteration, so we need a snapshot
            Dictionary<string, string> updatesSnapshot = new Dictionary<string, string>();
            foreach (var kvp in _pendingStatUpdates) {
                updatesSnapshot[kvp.Key] = kvp.Value;
            }
            
            // Check if reset happened while taking snapshot (reset clears _pendingStatUpdates)
            // If so, discard the stale snapshot to prevent sending old data after RESET_ALL
            if (_pendingStatUpdates.Count == 0 && updatesSnapshot.Count > 0) {
                // Reset cleared pending updates - don't send stale batches
                return;
            }
            
            // Group updates by stat type for batching
            Dictionary<string, List<(string Key, string Value)>> batchedUpdates = new Dictionary<string, List<(string, string)>>();

            // Process updates from snapshot - safe to iterate
            foreach (var kvp in updatesSnapshot) {
                string dataName = kvp.Key;
                string dataValue = kvp.Value;
                
                // Skip null or empty values to prevent crashes
                if (string.IsNullOrEmpty(dataName) || dataValue == null)
                    continue;

                // Determine batch type based on data name prefix - use more efficient string operations
                string batchKey = null;
                string extractedId = null;
                
                if (dataName.StartsWith(Codebase.Constants.SOG)) {
                    batchKey = BATCH_SOG;
                    extractedId = dataName.Substring(Codebase.Constants.SOG.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.SAVEPERC)) {
                    batchKey = BATCH_SAVEPERC;
                    extractedId = dataName.Substring(Codebase.Constants.SAVEPERC.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.BLOCK)) {
                    batchKey = BATCH_BLOCK;
                    extractedId = dataName.Substring(Codebase.Constants.BLOCK.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.HIT)) {
                    batchKey = BATCH_HIT;
                    extractedId = dataName.Substring(Codebase.Constants.HIT.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TAKEAWAY)) {
                    batchKey = BATCH_TAKEAWAY;
                    extractedId = dataName.Substring(Codebase.Constants.TAKEAWAY.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TURNOVER)) {
                    batchKey = BATCH_TURNOVER;
                    extractedId = dataName.Substring(Codebase.Constants.TURNOVER.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.PASS)) {
                    batchKey = BATCH_PASS;
                    extractedId = dataName.Substring(Codebase.Constants.PASS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.PUCK_TOUCH)) {
                    batchKey = BATCH_PUCK_TOUCH;
                    extractedId = dataName.Substring(Codebase.Constants.PUCK_TOUCH.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.EXIT)) {
                    batchKey = BATCH_EXIT;
                    extractedId = dataName.Substring(Codebase.Constants.EXIT.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.ENTRY)) {
                    batchKey = BATCH_ENTRY;
                    extractedId = dataName.Substring(Codebase.Constants.ENTRY.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.POSSESSION_TIME)) {
                    batchKey = BATCH_POSSESSION_TIME;
                    extractedId = dataName.Substring(Codebase.Constants.POSSESSION_TIME.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.PUCK_BATTLE_WINS)) {
                    batchKey = BATCH_PUCK_BATTLE_WINS;
                    extractedId = dataName.Substring(Codebase.Constants.PUCK_BATTLE_WINS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.PUCK_BATTLE_LOSSES)) {
                    batchKey = BATCH_PUCK_BATTLE_LOSSES;
                    extractedId = dataName.Substring(Codebase.Constants.PUCK_BATTLE_LOSSES.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.SHOT_ATTEMPTS)) {
                    batchKey = BATCH_SHOT_ATTEMPTS;
                    extractedId = dataName.Substring(Codebase.Constants.SHOT_ATTEMPTS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_SHOTS)) {
                    batchKey = BATCH_TEAM_SHOTS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_SHOTS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_SHOT_ATTEMPTS)) {
                    batchKey = BATCH_TEAM_SHOT_ATTEMPTS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_SHOT_ATTEMPTS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.HOME_PLATE_SOGS)) {
                    batchKey = BATCH_HOME_PLATE_SOGS;
                    extractedId = dataName.Substring(Codebase.Constants.HOME_PLATE_SOGS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_HOME_PLATE_SOGS)) {
                    batchKey = BATCH_TEAM_HOME_PLATE_SOGS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_HOME_PLATE_SOGS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_PASSES)) {
                    batchKey = BATCH_TEAM_PASSES;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_PASSES.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_FACEOFF_WINS)) {
                    batchKey = BATCH_TEAM_FACEOFF_WINS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_FACEOFF_WINS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_FACEOFF_TOTAL)) {
                    batchKey = BATCH_TEAM_FACEOFF_TOTAL;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_FACEOFF_TOTAL.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_TAKEAWAYS)) {
                    batchKey = BATCH_TEAM_TAKEAWAYS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_TAKEAWAYS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_TURNOVERS)) {
                    batchKey = BATCH_TEAM_TURNOVERS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_TURNOVERS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_EXITS)) {
                    batchKey = BATCH_TEAM_EXITS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_EXITS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_ENTRIES)) {
                    batchKey = BATCH_TEAM_ENTRIES;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_ENTRIES.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_POSSESSION_TIME)) {
                    batchKey = BATCH_TEAM_POSSESSION_TIME;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_POSSESSION_TIME.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_PUCK_BATTLE_WINS)) {
                    batchKey = BATCH_TEAM_PUCK_BATTLE_WINS;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_PUCK_BATTLE_WINS.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.TEAM_PUCK_BATTLE_LOSSES)) {
                    batchKey = BATCH_TEAM_PUCK_BATTLE_LOSSES;
                    extractedId = dataName.Substring(Codebase.Constants.TEAM_PUCK_BATTLE_LOSSES.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.STICK_SAVES)) {
                    batchKey = BATCH_STICK_SAVES;
                    extractedId = dataName.Substring(Codebase.Constants.STICK_SAVES.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.BODY_SAVES)) {
                    batchKey = BATCH_BODY_SAVES;
                    extractedId = dataName.Substring(Codebase.Constants.BODY_SAVES.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.HOME_PLATE_SAVES)) {
                    batchKey = BATCH_HOME_PLATE_SAVES;
                    extractedId = dataName.Substring(Codebase.Constants.HOME_PLATE_SAVES.Length);
                }
                else if (dataName.StartsWith(Codebase.Constants.HOME_PLATE_SHOTS_FACED)) {
                    batchKey = BATCH_HOME_PLATE_SHOTS_FACED;
                    extractedId = dataName.Substring(Codebase.Constants.HOME_PLATE_SHOTS_FACED.Length);
                }
                else {
                    // For any other stat types, send immediately (like STAR, RESET_ALL, etc.)
                    SafeSendDataToAll(dataName, dataValue);
                    continue;
                }

                // Add to batch - validate data to prevent crashes
                if (batchKey != null && !string.IsNullOrEmpty(extractedId) && dataValue != null) {
                    if (!batchedUpdates.TryGetValue(batchKey, out var batchList)) {
                        batchList = new List<(string, string)>();
                        batchedUpdates[batchKey] = batchList;
                    }
                    batchList.Add((extractedId, dataValue));
                }
            }

            // Send all batched updates using StringBuilder for better performance
            foreach (var batch in batchedUpdates) {
                string batchDataName = batch.Key;
                var updates = batch.Value;
                
                if (updates.Count > 0) {
                    // Conservative limit: 48KB UTF-8 bytes to leave room for dataName and other overhead
                    // NetworkCommunication.SendDataToAll adds dataName + sizeof(ulong) (8 bytes) to the data size
                    // So we need to account for that in our batch size limit
                    int batchDataNameByteSize = Encoding.UTF8.GetByteCount(batchDataName);
                    const int OVERHEAD_BYTES = sizeof(ulong); // 8 bytes
                    const int MAX_SAFE_TOTAL_BYTES = 49152; // 48KB total limit
                    int MAX_BATCH_BYTES = MAX_SAFE_TOTAL_BYTES - batchDataNameByteSize - OVERHEAD_BYTES; // Reserve space for dataName and overhead
                    
                    // Safety check: ensure we don't go negative (shouldn't happen, but defensive)
                    if (MAX_BATCH_BYTES < 1024) {
                        Logging.LogError($"Batch data name too large ({batchDataNameByteSize} bytes) for {batchDataName}, using minimum batch size", ModServerConfig);
                        MAX_BATCH_BYTES = 1024; // Use minimum safe size
                    }
                    
                    // Build batches, splitting if they exceed the byte limit
                    List<string> batchChunks = new List<string>();
                    StringBuilder currentBatchBuilder = new StringBuilder();
                    int currentBatchByteSize = 0;
                    
                    foreach (var update in updates) {
                        // Validate data to prevent null reference or memory issues
                        string key = update.Key ?? "";
                        string value = update.Value ?? "";
                        
                        // Skip entries with invalid data
                        if (string.IsNullOrEmpty(key))
                            continue;
                        
                        // Calculate the size of this entry in UTF-8 bytes
                        string entry = key + ";" + value + ";";
                        int entryByteSize = Encoding.UTF8.GetByteCount(entry);
                        
                        // If adding this entry would exceed the limit, finalize current batch and start a new one
                        if (currentBatchByteSize > 0 && currentBatchByteSize + entryByteSize > MAX_BATCH_BYTES) {
                            // Remove trailing semicolon and add to chunks
                            if (currentBatchBuilder.Length > 0) {
                                currentBatchBuilder.Length--;
                                string batchData = currentBatchBuilder.ToString();
                                if (!string.IsNullOrEmpty(batchData)) {
                                    batchChunks.Add(batchData);
                                }
                            }
                            // Start new batch
                            currentBatchBuilder.Clear();
                            currentBatchByteSize = 0;
                        }
                        
                        // Add entry to current batch
                        currentBatchBuilder.Append(entry);
                        currentBatchByteSize += entryByteSize;
                    }
                    
                    // Add final batch if it has content
                    if (currentBatchBuilder.Length > 0) {
                        currentBatchBuilder.Length--; // Remove trailing semicolon
                        string batchData = currentBatchBuilder.ToString();
                        if (!string.IsNullOrEmpty(batchData)) {
                            batchChunks.Add(batchData);
                        }
                    }
                    
                    // Send all chunks
                    foreach (string batchData in batchChunks) {
                        if (!string.IsNullOrEmpty(batchData)) {
                            // Double-check total byte size before sending as a final safety measure
                            // Account for dataName + sizeof(ulong) overhead that NetworkCommunication adds
                            int batchDataByteSize = Encoding.UTF8.GetByteCount(batchData);
                            int totalSize = batchDataNameByteSize + OVERHEAD_BYTES + batchDataByteSize;
                            
                            if (totalSize > MAX_SAFE_TOTAL_BYTES) {
                                Logging.LogError($"Batch data still too large ({totalSize} total bytes = {batchDataNameByteSize} name + {OVERHEAD_BYTES} overhead + {batchDataByteSize} data) for {batchDataName} after splitting, skipping", ModServerConfig);
                                continue;
                            }
                            
                            if (batchDataByteSize > MAX_BATCH_BYTES) {
                                Logging.LogError($"Batch data still too large ({batchDataByteSize} bytes) for {batchDataName} after splitting, skipping", ModServerConfig);
                                continue;
                            }
                            
                            // Use safe send method
                            if (!SafeSendDataToAll(batchDataName, batchData)) {
                                // Track failure for circuit breaker
                                _batchingFailureCount++;
                                _lastBatchingFailureTime = now;
                                if (_batchingFailureCount >= MAX_BATCHING_FAILURES) {
                                    _batchingDisabled = true;
                                    Logging.LogError($"Batching disabled after {_batchingFailureCount} failures. Will re-enable after {BATCHING_DISABLE_DURATION_SECONDS} seconds.", ModServerConfig);
                                    // Clear pending updates to prevent accumulation
                                    _pendingStatUpdates.Clear();
                                    return;
                                }
                            }
                            else {
                                // Reset failure count on successful send
                                _batchingFailureCount = 0;
                            }
                        }
                    }
                    
                    if (batchChunks.Count > 1) {
                        Logging.Log($"Split {batchDataName} batch into {batchChunks.Count} chunks due to size limit", ModServerConfig);
                    }
                }
            }

            // Clear pending updates
            _pendingStatUpdates.Clear();
            _lastStatBatchSendTime = now;
            }
            catch (Exception ex) {
                // Catch any unexpected exceptions to prevent server crash
                // Log the error but allow the game to continue
                _batchingFailureCount++;
                _lastBatchingFailureTime = now;
                
                Logging.LogError($"Critical error in SendBatchedStatUpdates: {ex}. Batching will be disabled if this happens {MAX_BATCHING_FAILURES} times.", ModServerConfig);
                
                if (_batchingFailureCount >= MAX_BATCHING_FAILURES) {
                    _batchingDisabled = true;
                    Logging.LogError($"Batching permanently disabled after {_batchingFailureCount} critical failures. Will re-enable after {BATCHING_DISABLE_DURATION_SECONDS} seconds.", ModServerConfig);
                    // Clear pending updates to prevent accumulation
                    _pendingStatUpdates.Clear();
                }
                
                // Don't rethrow - allow game to continue without stat updates
            }
        }

        private static string FormatReportedClientModVersion(string reportedVersion) {
            if (string.IsNullOrEmpty(reportedVersion) || reportedVersion == "1")
                return "unknown";
            return reportedVersion;
        }

        /// <summary>
        /// True when the client mod is the same version or newer than the server (e.g. client 1.3 on server 1.1).
        /// </summary>
        private static bool IsClientModVersionCompatible(string clientVersion, string serverVersion) {
            if (string.IsNullOrEmpty(clientVersion) || clientVersion == "1")
                return false;
            if (clientVersion == serverVersion)
                return true;
            if (Version.TryParse(clientVersion, out Version client) && Version.TryParse(serverVersion, out Version server))
                return client >= server;
            return false;
        }

        /// <summary>
        /// True only when the client mod is older than the server requires — not when the client is ahead.
        /// </summary>
        private static bool IsClientModVersionOutdated(string clientVersion, string serverRequiredVersion) {
            if (string.IsNullOrEmpty(clientVersion) || clientVersion == "1")
                return true;
            if (clientVersion == serverRequiredVersion)
                return false;
            if (Version.TryParse(clientVersion, out Version client) && Version.TryParse(serverRequiredVersion, out Version required))
                return client < required;
            return clientVersion != serverRequiredVersion;
        }

        private static string GetGoalieSavePerc(int saves, int shots) {
            if (shots == 0)
                return "0%";

            double pct = ((double)saves / (double)shots) * 100.0;
            return pct.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static bool RulesetModEnabled() {
            return _rulesetModEnabled != null && (bool)_rulesetModEnabled;
        }

        private static string GetStarTagForChat(string playerSteamId) {
            if (_stars[1] == playerSteamId)
                return $"{MEDAL_GOLD} ";
            if (_stars[2] == playerSteamId)
                return $"{MEDAL_SILVER} ";
            if (_stars[3] == playerSteamId)
                return $"{MEDAL_BRONZE} ";
            return "";
        }

        private static string GetStarTagForScoreboard(string playerSteamId) {
            if (_stars[1] == playerSteamId)
                return $"<color=#FFD700FF><b>{STAR_GLYPH}</b></color> ";
            if (_stars[2] == playerSteamId)
                return $"<color=#C0C0C0FF><b>{STAR_GLYPH}</b></color> ";
            if (_stars[3] == playerSteamId)
                return $"<color=#CD7F32FF><b>{STAR_GLYPH}</b></color> ";
            return "";
        }

        private static void ApplyScoreboardStarTag(Label label, string playerSteamId) {
            if (label == null || string.IsNullOrEmpty(playerSteamId) || !_stars.Values.Contains(playerSteamId))
                return;
            label.enableRichText = true;
            string baseText = StripStarTags(label.text);
            label.text = GetStarTagForScoreboard(playerSteamId) + baseText;
        }

        /// <summary>
        /// Strips HTML/rich text tags (anything between &lt; and &gt; brackets) from a string
        /// </summary>
        private static string StripHtmlTags(string text) {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove all content between < and > brackets (including the brackets themselves)
            while (text.Contains("<") && text.Contains(">")) {
                int startIndex = text.IndexOf("<");
                int endIndex = text.IndexOf(">", startIndex);
                
                if (startIndex >= 0 && endIndex > startIndex) {
                    text = text.Remove(startIndex, endIndex - startIndex + 1);
                } else {
                    break; // Invalid tag structure, stop processing
                }
            }

            return text;
        }

        private static string StripStarTags(string text) {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.Replace($"{MEDAL_GOLD} ", "");
            text = text.Replace($"{MEDAL_SILVER} ", "");
            text = text.Replace($"{MEDAL_BRONZE} ", "");

            text = text.Replace($"<color=#FFD700FF><b>{STAR_GLYPH}</b></color> ", "");
            text = text.Replace($"<color=#C0C0C0FF><b>{STAR_GLYPH}</b></color> ", "");
            text = text.Replace($"<color=#CD7F32FF><b>{STAR_GLYPH}</b></color> ", "");
            text = text.Replace("? ", "");
            text = text.Replace("<color=#FFD700FF><b>?</b></color> ", "");
            text = text.Replace("<color=#C0C0C0FF><b>?</b></color> ", "");
            text = text.Replace("<color=#CD7F32FF><b>?</b></color> ", "");

            return text;
        }

        private static void Client_ResetSOG() {
            foreach (string key in new List<string>(_sog.Keys)) {
                _sog[key] = 0;
                
                // Update label if it exists (for scoreboard display)
                if (_sogLabels.TryGetValue(key, out Label label)) {
                    label.text = "0";

                    Player currentPlayer = PlayerManager.Instance.GetPlayerBySteamId(key);
                    if (currentPlayer != null && currentPlayer && PlayerFunc.IsGoalie(currentPlayer))
                        label.text = GetGoalieSavePerc(0, 0);
                }
            }
        }

        private static void Client_ResetSavePerc() {
            foreach (string key in new List<string>(_savePerc.Keys))
                _savePerc[key] = (0, 0);
        }

        private static void Client_ResetPasses() {
            foreach (string key in new List<string>(_passes.Keys))
                _passes[key] = 0;
        }

        private static void Client_ResetBlocks() {
            foreach (string key in new List<string>(_blocks.Keys))
                _blocks[key] = 0;
        }

        private static void Client_ResetHits() {
            foreach (string key in new List<string>(_hits.Keys))
                _hits[key] = 0;
        }

        private static void Client_ResetTakeaways() {
            foreach (string key in new List<string>(_takeaways.Keys))
                _takeaways[key] = 0;
        }

        private static void Client_ResetTurnovers() {
            foreach (string key in new List<string>(_turnovers.Keys))
                _turnovers[key] = 0;
        }

        private static void Client_ResetExits() {
            foreach (string key in new List<string>(_exits.Keys))
                _exits[key] = 0;
        }

        private static void Client_ResetEntries() {
            foreach (string key in new List<string>(_entries.Keys))
                _entries[key] = 0;
        }

        private static void Client_ResetShotAttempts() {
            foreach (string key in new List<string>(_shotAttempts.Keys))
                _shotAttempts[key] = 0;
        }


        private static void Client_ResetHomePlateSogs() {
            foreach (string key in new List<string>(_homePlateSogs.Keys))
                _homePlateSogs[key] = 0;
        }

        private static void Client_ResetPuckTouches() {
            foreach (string key in new List<string>(_puckTouches.Keys))
                _puckTouches[key] = 0;
        }

        private static void Client_ResetPossessionTime() {
            foreach (string key in new List<string>(_possessionTimeSeconds.Keys))
                _possessionTimeSeconds[key] = 0.0;
            _lastPossessionTouchTime.Clear();
            _lastPossessionUpdateTime.Clear();
            _lastPuckTouchTime.Clear();
            _lastPlayerZone.Clear();
        }

        private static void Client_ResetPuckBattles() {
            foreach (string key in new List<string>(_puckBattleWins.Keys))
                _puckBattleWins[key] = 0;
            foreach (string key in new List<string>(_puckBattleLosses.Keys))
                _puckBattleLosses[key] = 0;
        }

        private static void Client_ResetStickSaves() {
            foreach (string key in new List<string>(_stickSaves.Keys))
                _stickSaves[key] = 0;
        }

        private static void Client_ResetBodySaves() {
            foreach (string key in new List<string>(_bodySaves.Keys))
                _bodySaves[key] = 0;
        }

        private static void Client_ResetHomePlateSaves() {
            foreach (string key in new List<string>(_homePlateSaves.Keys))
                _homePlateSaves[key] = 0;
        }

        private static void Client_ResetHomePlateShots() {
            foreach (string key in new List<string>(_homePlateShots.Keys))
                _homePlateShots[key] = 0;
        }

        private static void Client_ResetTeamStats() {
            _teamShots.Clear();
            _teamShotAttempts.Clear();
            _teamHomePlateSogs.Clear();
            _teamPasses.Clear();
            _teamPossessionTime.Clear();
            _teamPuckBattleWins.Clear();
            _teamPuckBattleLosses.Clear();
            _teamFaceoffWins.Clear();
            _teamFaceoffTotal.Clear();
            _teamTakeaways.Clear();
            _teamTurnovers.Clear();
            _teamExits.Clear();
            _teamEntries.Clear();
            // Wipe client team stats file when stats reset (same logic as tooltip UI reset on RESET_ALL)
            if (_clientConfig.LogClientSideStats)
                WriteClientTeamStatsToFile();
        }

        /// <summary>
        /// Force-refresh tooltips and labels after a stats reset.
        /// Tears down team tooltips and rebuilds them so they read fresh zeros from cleared dicts.
        /// </summary>
        private static void Client_RefreshTooltipsAndLabelsAfterReset() {
            try {
                HideAllPlayerTooltips();
                int tornDown = _teamTooltips.Count;
                // 1. Tear down existing team tooltips and hit areas
                foreach (var kvp in new List<KeyValuePair<PlayerTeam, VisualElement>>(_teamTooltips)) {
                    kvp.Value?.parent?.Remove(kvp.Value);
                    _teamTooltips.Remove(kvp.Key);
                }
                foreach (var kvp in new List<KeyValuePair<PlayerTeam, VisualElement>>(_teamHitAreas)) {
                    kvp.Value?.parent?.Remove(kvp.Value);
                    _teamHitAreas.Remove(kvp.Key);
                }
                _teamTooltipsSetup = false;
                _teamTooltipSetupScheduled = false;

                // 2. Rebuild team tooltips - ScoreboardModifications will call SetupTeamTooltips, which creates fresh tooltips that read from (now cleared) dicts
                ScoreboardModifications(true);

                // 3. Reset player SOG/save labels on scoreboard
                foreach (var kvp in _sogLabels) {
                    if (kvp.Value != null) {
                        bool isGoalie = _playerTooltipIsGoalie.TryGetValue(kvp.Key, out bool g) && g;
                        kvp.Value.text = isGoalie ? GetGoalieSavePerc(0, 0) : "0";
                    }
                }

                // 4. Refresh all player tooltip stat labels (entries, passes, hits, etc.) from cleared dicts
                if (PlayerManager.Instance != null) {
                    foreach (var kvp in new List<KeyValuePair<string, VisualElement>>(_playerTooltips)) {
                        if (kvp.Value == null)
                            continue;
                        Player player = PlayerManager.Instance.GetPlayerBySteamId(kvp.Key);
                        if (player != null && player)
                            UpdateTooltipStats(kvp.Value, kvp.Key, player);
                    }
                }

                Logging.Log($"RESET_ALL refresh done: tore down {tornDown} team tooltips, reset {_sogLabels.Count} player labels", _clientConfig);
            } catch (Exception ex) {
                Logging.LogError($"Error in Client_RefreshTooltipsAndLabelsAfterReset: {ex}", _clientConfig);
            }
        }
        #endregion

        #region Classes
        internal abstract class Check {
            internal bool HasToCheck { get; set; } = false;
            internal int FramesChecked { get; set; } = 0;
        }
        internal class SaveCheck : Check {
            internal string ShooterSteamId { get; set; } = "";
            internal PlayerTeam ShooterTeam { get; set; } = PlayerTeam.Blue;
            internal bool HitStick { get; set; } = false;
        }

        internal class BlockCheck : Check {
            internal string BlockerSteamId { get; set; } = "";
            internal PlayerTeam ShooterTeam { get; set; } = PlayerTeam.Blue;
            internal bool ShotWasOnNet { get; set; } = false; // Track if the shot was confirmed to be on net
            internal float BlockGameTime { get; set; } = 0f; // Game time when the block check was set up (when block occurred)
        }
        
        internal class Possession {
            internal string SteamId { get; set; } = "";

            internal PlayerTeam Team { get; set; } = PlayerTeam.None;

            internal DateTime Date { get; set; } = DateTime.MinValue;
        }
        #endregion

        #region Play-by-Play Classes
        /// <summary>
        /// Represents a goal with all associated information
        /// </summary>
        internal class GoalInfo {
            public float GameTime { get; set; }
            public int Period { get; set; }
            public string Team { get; set; } = "";
            public string Scorer { get; set; } = "";
            public string PrimaryAssist { get; set; } = null;
            public string SecondaryAssist { get; set; } = null;
            public bool GWG { get; set; } = false;
            /// <summary>Defending goalie in net when scored; empty if empty net.</summary>
            public string DefendingGoalieSteamId { get; set; } = "";
            public bool IsEmptyNet { get; set; } = false;
        }

        /// <summary>
        /// Represents a play-by-play event for CSV export
        /// </summary>
        internal class PlayByPlayEvent {
            public int EventId { get; set; }
            public PlayByPlayEventType EventType { get; set; }
            public float GameTime { get; set; }
            public int Period { get; set; }
            public string PlayerSteamId { get; set; } = "";
            public string PlayerName { get; set; } = "";
            public int PlayerTeam { get; set; }
            public string PlayerPosition { get; set; } = "";
            public int PlayerJersey { get; set; }
            public float PlayerSpeed { get; set; }
            public EventZone Zone { get; set; }
            public Vector3 Position { get; set; }
            public Vector3 Velocity { get; set; }
            public float ForceMagnitude { get; set; }
            public string Outcome { get; set; } = "successful";
            public string Flags { get; set; } = ""; // "Carry" or "Pass" for exits/entries
            public string Team { get; set; } = ""; // Team of the player performing the event (regardless of possession)
            public string TeamInPossession { get; set; } = ""; // Team that has possession of the puck
            public string CurrentPlayInPossession { get; set; } = "";
            public string ScoreState { get; set; } = "";
            public string TeamForwardsSteamID { get; set; } = "";
            public string TeamDefencemenSteamID { get; set; } = "";
            public string TeamGoalieSteamID { get; set; } = "";
            public string OpposingTeamForwardsSteamID { get; set; } = "";
            public string OpposingTeamDefencemenSteamID { get; set; } = "";
            public string OpposingTeamGoalieSteamID { get; set; } = "";
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Play-by-play event types
        /// </summary>
        internal enum PlayByPlayEventType {
            Touch,
            Takeaway,
            Turnover,
            Pass,
            Reception,
            Shot,
            Save,
            Goal,
            OwnGoal,
            Hit,
            Block,
            PuckBattle,
            Faceoff,
            FaceoffOutcome,
            DZExit,
            OZEntry,
            GameEnd
        }

        /// <summary>
        /// Event zones on the rink
        /// </summary>
        internal enum EventZone {
            Defensive,   // DZ - zone with your net
            Neutral,     // NZ - center ice middle area
            Offensive    // OZ - zone with opponent's net
        }
        #endregion

#if PUCK_API_DUMP
        // =====================================================================
        // PUCK API DUMP  — runtime reflection of all loaded game assemblies.
        // Writes puck_api_dump.txt next to the server executable.
        // Disable by commenting out #define PUCK_API_DUMP at the top of the file.
        // =====================================================================
        private static void DumpGameAPI() {
            try {
                var sb = new StringBuilder();
                sb.AppendLine("=================================================================");
                sb.AppendLine($"  PUCK API DUMP  —  generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                sb.AppendLine("=================================================================");
                sb.AppendLine();

                // Collect every type from every loaded assembly
                var allTypes = new List<Type>();
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                    try {
                        foreach (Type t in asm.GetTypes())
                            allTypes.Add(t);
                    } catch (ReflectionTypeLoadException rtle) {
                        foreach (Type t in rtle.Types)
                            if (t != null) allTypes.Add(t);
                    } catch { }
                }

                // ---- 1. KEY GAME TYPES (classes we interact with directly) ----
                string[] keyNames = {
                    "GameManager","ServerManager","PlayerManager","PuckManager","UIManager",
                    "ChatManager","UIScoreboard","UIGameboard","UIGameState","UIChat",
                    "UIChatController","EventManager","ModManager","NetworkManager",
                    "Player","PlayerTeam","GameState","GamePhase","PlayByPlayEventType",
                    "ServerConfig","ClientConfig","IPuckPlugin","IGameMode",
                    "MonoBehaviourSingleton","NetworkBehaviourSingleton",
                    "SystemFunc","ServerFunc","Logging","NetworkCommunication",
                };

                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine("  SECTION 1 — KEY GAME TYPES");
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                foreach (string name in keyNames) {
                    var matches = allTypes.Where(t => t.Name == name || t.Name.StartsWith(name + "`")).ToList();
                    foreach (Type t in matches) {
                        sb.AppendLine();
                        sb.AppendLine($"  [{t.FullName}]  in {t.Assembly.GetName().Name}");
                        sb.AppendLine($"    Base: {t.BaseType?.FullName ?? "none"}");
                        string ifaces = string.Join(", ", t.GetInterfaces().Select(i => i.Name));
                        if (!string.IsNullOrEmpty(ifaces))
                            sb.AppendLine($"    Interfaces: {ifaces}");

                        // Fields
                        var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                      .OrderBy(f => f.Name).ToList();
                        if (fields.Count > 0) {
                            sb.AppendLine("    FIELDS:");
                            foreach (var f in fields)
                                sb.AppendLine($"      {(f.IsPublic ? "pub" : f.IsPrivate ? "prv" : "prt")} {(f.IsStatic ? "static " : "")}{f.FieldType.Name} {f.Name}");
                        }

                        // Properties
                        var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                     .OrderBy(p => p.Name).ToList();
                        if (props.Count > 0) {
                            sb.AppendLine("    PROPERTIES:");
                            foreach (var p in props)
                                sb.AppendLine($"      {p.PropertyType.Name} {p.Name}  get={p.CanRead} set={p.CanWrite}");
                        }

                        // Methods
                        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                       .Where(m => !m.IsSpecialName)
                                       .OrderBy(m => m.Name).ToList();
                        if (methods.Count > 0) {
                            sb.AppendLine("    METHODS:");
                            foreach (var m in methods) {
                                string parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                                sb.AppendLine($"      {(m.IsPublic ? "pub" : m.IsPrivate ? "prv" : "prt")} {(m.IsStatic ? "static " : "")}{m.ReturnType.Name} {m.Name}({parms})");
                            }
                        }
                    }
                }

                // ---- 2. ALL HARMONY-PATCHABLE TYPES (have methods we might target) ----
                sb.AppendLine();
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine("  SECTION 2 — ALL PUBLIC TYPES IN GAME ASSEMBLIES");
                sb.AppendLine("  (assemblies: Puck, Assembly-CSharp, Assembly-CSharp-firstpass)");
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                var gameAssemblies = new HashSet<string> { "Puck", "Assembly-CSharp", "Assembly-CSharp-firstpass" };
                var gameTypes = allTypes
                    .Where(t => gameAssemblies.Contains(t.Assembly.GetName().Name) && t.IsPublic)
                    .OrderBy(t => t.FullName)
                    .ToList();

                foreach (Type t in gameTypes) {
                    sb.AppendLine();
                    sb.AppendLine($"  [{t.FullName}]");
                    if (t.BaseType != null && t.BaseType != typeof(object))
                        sb.AppendLine($"    : {t.BaseType.FullName}");

                    var pubMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                      .Where(m => !m.IsSpecialName).OrderBy(m => m.Name).ToList();
                    foreach (var m in pubMethods) {
                        string parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        sb.AppendLine($"    {(m.IsStatic ? "static " : "")}{m.ReturnType.Name} {m.Name}({parms})");
                    }

                    var pubFields = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                     .OrderBy(f => f.Name).ToList();
                    foreach (var f in pubFields)
                        sb.AppendLine($"    field: {(f.IsStatic ? "static " : "")}{f.FieldType.Name} {f.Name}");
                }

                // ---- 3. ENUMS ----
                sb.AppendLine();
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine("  SECTION 3 — ENUMS IN GAME ASSEMBLIES");
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                var enumTypes = allTypes
                    .Where(t => gameAssemblies.Contains(t.Assembly.GetName().Name) && t.IsEnum)
                    .OrderBy(t => t.FullName).ToList();
                foreach (Type t in enumTypes) {
                    sb.AppendLine($"  enum {t.FullName}: {string.Join(", ", Enum.GetNames(t))}");
                }

                // ---- 4. EVENT NAMES (EventManager listeners) ----
                sb.AppendLine();
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine("  SECTION 4 — EVENTMANAGER EVENT NAMES (via reflection)");
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                try {
                    Type emType = allTypes.FirstOrDefault(t => t.Name == "EventManager");
                    if (emType != null) {
                        // Try to find a dictionary of registered events
                        var dictField = emType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            .FirstOrDefault(f => f.FieldType.IsGenericType &&
                                f.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>));
                        if (dictField != null) {
                            var dict = dictField.GetValue(null);
                            if (dict != null) {
                                var keys = (dict as System.Collections.IDictionary)?.Keys;
                                if (keys != null)
                                    foreach (var k in keys)
                                        sb.AppendLine($"  event: {k}");
                            }
                        } else {
                            sb.AppendLine("  (EventManager fields not accessible via reflection)");
                        }
                    }
                } catch (Exception ex) {
                    sb.AppendLine($"  ERROR: {ex.Message}");
                }

                // ---- 5. MOD CONFIG FIELDS ----
                sb.AppendLine();
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine("  SECTION 5 — MOD CONFIG FIELDS");
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                try {
                    Type modCfgType = allTypes.FirstOrDefault(t => t.Name == "ModConfig" || t.Name == "IModConfig");
                    if (modCfgType != null) {
                        foreach (var p in modCfgType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                            sb.AppendLine($"  {p.PropertyType.Name} {p.Name}");
                        foreach (var f in modCfgType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                            sb.AppendLine($"  field: {f.FieldType.Name} {f.Name}");
                    } else {
                        sb.AppendLine("  (ModConfig type not found)");
                    }
                } catch (Exception ex) {
                    sb.AppendLine($"  ERROR: {ex.Message}");
                }

                // Write output
                string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "puck_api_dump.txt");
                File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
                Logging.Log($"API dump written to: {outPath}", ModServerConfig);
            } catch (Exception ex) {
                Logging.LogError($"DumpGameAPI failed: {ex}", ModServerConfig);
            }
        }
#endif

    }
}
