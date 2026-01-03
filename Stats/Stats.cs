using Codebase;
using HarmonyLib;
using Newtonsoft.Json;
using oomtm450PuckMod_Stats.Configs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace oomtm450PuckMod_Stats {
    public class Stats : IPuckMod {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private static readonly string MOD_VERSION = "0.6.0";

        /// <summary>
        /// List of string, last released versions of the mod.
        /// </summary>
        private static readonly ReadOnlyCollection<string> OLD_MOD_VERSIONS = new ReadOnlyCollection<string>(new List<string> {
            "0.1.0",
            "0.1.1",
            "0.1.2",
            "0.2.0",
            "0.2.1",
            "0.2.2",
            "0.3.0",
            "0.4.0",
            "0.4.1",
            "0.5.0",
        });

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

        private const string SOG_HEADER_LABEL_NAME = "SOGHeaderLabel";

        private const string SOG_LABEL = "SOGLabel";

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
        /// ServerConfig, config set and sent by the server.
        /// </summary>
        internal static ServerConfig ServerConfig { get; set; } = new ServerConfig();

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
        /// LockDictionary of ulong and DateTime, last time a mod out of date message was sent to a client (ulong clientId).
        /// </summary>
        private static readonly LockDictionary<ulong, DateTime> _sentOutOfDateMessage = new LockDictionary<ulong, DateTime>();


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

        private static readonly LockList<string> _blueGoals = new LockList<string>();

        private static readonly LockList<string> _redGoals = new LockList<string>();

        private static readonly LockList<string> _blueAssists = new LockList<string>();

        private static readonly LockList<string> _redAssists = new LockList<string>();

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
        /// Bool, true if the client asked to be kicked because of versionning problems.
        /// </summary>
        private static bool _askForKick = false;

        /// <summary>
        /// Bool, true if the client needs to notify the user that the server is running an out of date version of the mod.
        /// </summary>
        private static bool _addServerModVersionOutOfDateMessage = false;

        /// <summary>
        /// Int, number of time client asked the server for startup data.
        /// </summary>
        private static int _askServerForStartupDataCount = 0;

        private static readonly List<string> _hasUpdatedUIScoreboard = new List<string>();
        private static bool _teamTooltipsSetup = false;

        private static readonly LockDictionary<string, Label> _sogLabels = new LockDictionary<string, Label>();

        private static readonly LockDictionary<string, VisualElement> _playerTooltips = new LockDictionary<string, VisualElement>();
        private static readonly LockDictionary<PlayerTeam, VisualElement> _teamTooltips = new LockDictionary<PlayerTeam, VisualElement>();
        private static readonly LockDictionary<string, Label> _playerTooltipNameLabels = new LockDictionary<string, Label>();
        private static readonly LockDictionary<string, VisualElement> _playerTooltipContainers = new LockDictionary<string, VisualElement>();
        private static readonly LockDictionary<string, bool> _playerTooltipIsGoalie = new LockDictionary<string, bool>();
        
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

        #region Harmony Patches
        /// <summary>
        /// Class that patches the Server_SpawnPuck event from PuckManager.
        /// </summary>
        [HarmonyPatch(typeof(PuckManager), nameof(PuckManager.Server_SpawnPuck))]
        public class PuckManager_Server_SpawnPuck_Patch {
            [HarmonyPostfix]
            public static void Postfix(ref Puck __result, Vector3 position, Quaternion rotation, Vector3 velocity, bool isReplay) {
                try {
                    // If this is not the server or this is a replay or game is not started, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || isReplay || (GameManager.Instance.Phase != GamePhase.Playing && GameManager.Instance.Phase != GamePhase.FaceOff))
                        return;

                    __result.gameObject.AddComponent<PuckRaycast>();
                    _puckRaycast = __result.gameObject.GetComponent<PuckRaycast>();
                    
                    // Record Faceoff event when puck spawns (only once per period/game start)
                    if (!isReplay && GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Playing && !_faceoffRecordedForCurrentPeriod) {
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
                    Logging.LogError($"Error in PuckManager_Server_SpawnPuck_Patch Postfix().\n{ex}", ServerConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the Server_GoalScored event from GameManager.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Server_GoalScored))]
        public class GameManager_Server_GoalScored_Patch {
            [HarmonyPrefix]
            public static bool Prefix(PlayerTeam team, ref Player lastPlayer, ref Player goalPlayer, ref Player assistPlayer, ref Player secondAssistPlayer, Puck puck) {
                try {
                    // If this is not the server or game is not started, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || RulesetModEnabled() || !_logic)
                        return true;

                    // Reset own goal flag
                    _isCurrentGoalOwnGoal = false;
                    
                    if (goalPlayer != null) {
                        // Normal goal - offensive player got credit
                        Player lastTouchPlayerTipIncluded = PlayerManager.Instance.GetPlayers().Where(x => x.SteamId.Value.ToString() == _lastPlayerOnPuckTipIncludedSteamId[team].SteamId).FirstOrDefault();
                        if (lastTouchPlayerTipIncluded != null && lastTouchPlayerTipIncluded.SteamId.Value.ToString() != goalPlayer.SteamId.Value.ToString()) {
                            secondAssistPlayer = assistPlayer;
                            assistPlayer = goalPlayer;
                            goalPlayer = PlayerManager.Instance.GetPlayers().Where(x => x.SteamId.Value.ToString() == _lastPlayerOnPuckTipIncludedSteamId[team].SteamId).FirstOrDefault();

                            while (assistPlayer != null && assistPlayer.SteamId.Value.ToString() == goalPlayer.SteamId.Value.ToString()) {
                                assistPlayer = secondAssistPlayer;
                                secondAssistPlayer = null;
                            }

                            if (secondAssistPlayer != null && (secondAssistPlayer.SteamId.Value.ToString() == assistPlayer.SteamId.Value.ToString() || secondAssistPlayer.SteamId.Value.ToString() == goalPlayer.SteamId.Value.ToString()))
                                secondAssistPlayer = null;
                        }
                        SendSavePercDuringGoal(team, SendSOGDuringGoal(goalPlayer));
                        return true;
                    }

                    // Own goal - no offensive player got credit (goalPlayer was null)
                    _isCurrentGoalOwnGoal = true;
                    // For own goals, the player who last touched the puck is from the defending team (the team that got scored on)
                    PlayerTeam defendingTeam = TeamFunc.GetOtherTeam(team);
                    Player lastTouchPlayer = PlayerManager.Instance.GetPlayers().Where(x => x.SteamId.Value.ToString() == _lastPlayerOnPuckTipIncludedSteamId[defendingTeam].SteamId).FirstOrDefault();
                    
                    if (lastTouchPlayer != null) {
                        UIChat.Instance.Server_SendSystemChatMessage($"OWN GOAL BY {lastTouchPlayer.Username.Value}");
                        lastPlayer = lastTouchPlayer;
                        // Don't set goalPlayer for own goals - it should remain null
                        // Don't call SendSOGDuringGoal for own goals - they shouldn't count as shots on goal
                    }

                    // Still need to update save percentage for the goalie (they didn't save it)
                    SendSavePercDuringGoal(team, false);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in GameManager_Server_GoalScored_Patch Prefix().\n{ex}", ServerConfig);
                }

                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(PlayerTeam team, Player lastPlayer, Player goalPlayer, Player assistPlayer, Player secondAssistPlayer, Puck puck) {
                try {
                    // If this is not the server, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer())
                        return;

                    // Check if this is an own goal (only when no offensive player got credit, as determined in Prefix)
                    bool isOwnGoal = _isCurrentGoalOwnGoal;

                    // Only process goal stats if we have a goalPlayer (NOT for own goals - goalPlayer should be null)
                    if (!isOwnGoal && goalPlayer != null && goalPlayer) {
                        if (team == PlayerTeam.Blue) {
                            _blueGoals.Add(goalPlayer.SteamId.Value.ToString());
                            if (assistPlayer != null)
                                _blueAssists.Add(assistPlayer.SteamId.Value.ToString());
                            if (secondAssistPlayer != null)
                                _blueAssists.Add(secondAssistPlayer.SteamId.Value.ToString());
                        }
                        else {
                            _redGoals.Add(goalPlayer.SteamId.Value.ToString());
                            if (assistPlayer != null)
                                _redAssists.Add(assistPlayer.SteamId.Value.ToString());
                            if (secondAssistPlayer != null)
                                _redAssists.Add(secondAssistPlayer.SteamId.Value.ToString());
                        }
                    }

                    // Reset possession/event logic on goals
                    _currentTeamInPossession = PlayerTeam.None;
                    _currentPlayInPossession = 0;
                    _lastEvent = null;

                    // Record play-by-play event as part of unified tracking
                    if (puck != null) {
                        Vector3 goalPos = puck.transform.position;
                        // Don't track velocity for goal events
                        if (isOwnGoal) {
                            // For own goals, use lastPlayer (last touch) for player info, but set goalscorer (PlayerSteamId) to empty
                            if (lastPlayer != null && lastPlayer) {
                                RecordPlayByPlayEventInternal(PlayByPlayEventType.OwnGoal, lastPlayer, goalPos, Vector3.zero, "successful");
                                // Set goalscorer to empty for own goals (playerReferenceSteamID should be null)
                                if (_playByPlayEvents.Count > 0) {
                                    var lastEvent = _playByPlayEvents[_playByPlayEvents.Count - 1];
                                    if (lastEvent.EventType == PlayByPlayEventType.OwnGoal) {
                                        lastEvent.PlayerSteamId = ""; // Goalscorer is null for own goals
                                    }
                                }
                            }
                        }
                        else {
                            // Normal goal - use goalPlayer
                            if (goalPlayer != null && goalPlayer) {
                                RecordPlayByPlayEventInternal(PlayByPlayEventType.Goal, goalPlayer, goalPos, Vector3.zero, "successful");
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in GameManager_Server_GoalScored_Patch Postfix().\n{ex}", ServerConfig);
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

                    _sogLabels.Remove(player.SteamId.Value.ToString());
                    string steamId = player.SteamId.Value.ToString();
                    if (_playerTooltips.TryGetValue(steamId, out VisualElement tooltip)) {
                        tooltip.parent?.Remove(tooltip);
                        _playerTooltips.Remove(steamId);
                    }
                    _playerTooltipNameLabels.Remove(steamId);
                    _playerTooltipContainers.Remove(steamId);
                    _playerTooltipIsGoalie.Remove(steamId);
                    _hasUpdatedUIScoreboard.Remove(player.SteamId.Value.ToString());
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in UIScoreboard_RemovePlayer_Patch Postfix().\n{ex}", _clientConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the Server_ResetGameState event from GameManager.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Server_ResetGameState))]
        public class GameManager_Server_ResetGameState_Patch {
            [HarmonyPostfix]
            public static void Postfix(bool resetPhase) {
                try {
                    // If this is not the server, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer())
                        return;

                    // Reset s%.
                    List<Player> players = PlayerManager.Instance.GetPlayers();
                    foreach (string key in new List<string>(_savePerc.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _savePerc[key] = (0, 0);
                        else
                            _savePerc.Remove(key);
                    }

                    // SOG reset is now handled like passes - reset here for server-side, and via RESET_ALL for client-side

                    // Reset stick saves.
                    foreach (string key in new List<string>(_stickSaves.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _stickSaves[key] = 0;
                        else
                            _stickSaves.Remove(key);
                    }

                    // Reset body saves.
                    foreach (string key in new List<string>(_bodySaves.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _bodySaves[key] = 0;
                        else
                            _bodySaves.Remove(key);
                    }

                    // Reset blocked shots.
                    foreach (string key in new List<string>(_blocks.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _blocks[key] = 0;
                        else
                            _blocks.Remove(key);
                    }

                    // Reset hits.
                    foreach (string key in new List<string>(_hits.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _hits[key] = 0;
                        else
                            _hits.Remove(key);
                    }

                    // Reset takeaways.
                    foreach (string key in new List<string>(_takeaways.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _takeaways[key] = 0;
                        else
                            _takeaways.Remove(key);
                    }

                    // Reset turnovers.
                    foreach (string key in new List<string>(_turnovers.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _turnovers[key] = 0;
                        else
                            _turnovers.Remove(key);
                    }

                    // Reset puck touches.
                    foreach (string key in new List<string>(_puckTouches.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _puckTouches[key] = 0;
                        else
                            _puckTouches.Remove(key);
                    }

                    // Reset shot attempts and home plate shots.
                    foreach (string key in new List<string>(_shotAttempts.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _shotAttempts[key] = 0;
                        else
                            _shotAttempts.Remove(key);
                    }

                    // Reset exits and entries.
                    foreach (string key in new List<string>(_exits.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _exits[key] = 0;
                        else
                            _exits.Remove(key);
                    }
                    foreach (string key in new List<string>(_entries.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _entries[key] = 0;
                        else
                            _entries.Remove(key);
                    }

                    // Reset passes.
                    foreach (string key in new List<string>(_passes.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _passes[key] = 0;
                        else
                            _passes.Remove(key);
                    }

                    // Reset SOG.
                    foreach (string key in new List<string>(_sog.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _sog[key] = 0;
                        else
                            _sog.Remove(key);
                    }

                    // Reset possession time.
                    foreach (string key in new List<string>(_possessionTimeSeconds.Keys)) {
                        if (players.FirstOrDefault(x => x.SteamId.Value.ToString() == key) != null)
                            _possessionTimeSeconds[key] = 0.0;
                        else
                            _possessionTimeSeconds.Remove(key);
                    }
                    _lastPossessionTouchTime.Clear();
                    _lastPossessionUpdateTime.Clear();
                    _lastPuckTouchTime.Clear();
                    _lastPlayerZone.Clear();
                    
                    // Reset team stats
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
                    
                    // Clear recent turnovers tracking
                    _recentTurnovers.Clear();
                    
                    // Clear time on ice tracking
                    _timeOnIceSeconds.Clear();
                    
                    // Reset continuous team possession tracking
                    _currentTeamPossession = PlayerTeam.None;
                    _teamPossessionStartTime = DateTime.UtcNow;
                    _teamLastEventTime.Clear();

                    // Reset goal and assists trackers.
                    _blueGoals.Clear();
                    _blueAssists.Clear();
                    _redGoals.Clear();
                    _redAssists.Clear();

                    // Reset last possession.
                    _lastPossession = new Possession();
                    _wasTippedLastFrame = false;

                    // Reset play-by-play events and tracking
                    _playByPlayEvents.Clear();
                    _nextPlayByPlayEventId = 0;
                    _lastProcessedZoneFlagEventId = -1; // Reset zone flag scanner
                    _currentGameReferenceId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    _currentTeamInPossession = PlayerTeam.None;
                    _currentPlayInPossession = 0;
                    _gameStartTime = 0f; // Reset so it initializes on next game start
                    _currentPeriod = 1;
                    _lastTrackedPeriod = 0; // Reset period tracking
                    _faceoffRecordedForCurrentPeriod = false;
                    _lastEvent = null;
                    
                    // Reset game time tracking for fractional precision
                    _lastWholeSecondGameTime = 0f;
                    _lastUnityTimeForGameTime = 0f;
                    _lastCountdownValue = -1;
                    
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
                    
                    // Reset shot attempt cooldown
                    _lastShotAttemptGameTime.Clear();
                    
                    // Reset zone exit/entry flags
                    foreach (PlayerTeam team in new List<PlayerTeam>(_teamHasExitedDZ.Keys)) {
                        _teamHasExitedDZ[team] = false;
                    }
                    foreach (PlayerTeam team in new List<PlayerTeam>(_teamHasEnteredOZ.Keys)) {
                        _teamHasEnteredOZ[team] = false;
                    }

                    // Clear pending stat updates (but don't reset tooltips here - they reset when new game begins)
                    _pendingStatUpdates.Clear();

                    _sentOutOfDateMessage.Clear();
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in GameManager_Server_ResetGameState_Patch Postfix().\n{ex}", ServerConfig);
                }
            }
        }

        /// <summary>
        /// Class that patches the Update event from ServerManager.
        /// </summary>
        [HarmonyPatch(typeof(ServerManager), "Update")]
        public class ServerManager_Update_Patch {
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
                            Logging.LogError($"Unexpected error calling SendBatchedStatUpdates (this should not happen): {ex}", ServerConfig);
                        }
                    }
                    
                    // Check if it's time to scan play-by-play events and add zone flags (every 6 seconds)
                    if ((now - _lastZoneFlagScanTime).TotalSeconds >= 6.0) {
                        AddZoneFlagsToPlayByPlayEvents();
                        _lastZoneFlagScanTime = now;
                    }

                    bool sendSavePercDuringGoalNextFrame = _sendSavePercDuringGoalNextFrame;
                    if (sendSavePercDuringGoalNextFrame) {
                        _sendSavePercDuringGoalNextFrame = false;
                        SendSavePercDuringGoal(_sendSavePercDuringGoalNextFrame_Player.Team.Value, SendSOGDuringGoal(_sendSavePercDuringGoalNextFrame_Player));
                    }

                    // If game is not started, do not use the rest of the patch.
                    if (PlayerManager.Instance == null || PuckManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing || _paused)
                        return;

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
                        
                        // Reset shot recorded flag when raycast goes back to false (new shot cycle)
                        // If shot was marked "on net" but no save/goal was recorded, mark it as "missed"
                        if (!currentRaycastState && previousRaycastState) {
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
                            
                            _shotRecordedForRaycast[defendingTeam] = false;
                            _raycastTrueFrames[defendingTeam] = 0;
                        }
                        
                        // Update previous state
                        _previousRaycastState[defendingTeam] = currentRaycastState;
                    }

                    // Save logic.
                    if (!sendSavePercDuringGoalNextFrame) {
                        foreach (PlayerTeam key in new List<PlayerTeam>(_checkIfPuckWasSaved.Keys)) {
                            SaveCheck saveCheck = _checkIfPuckWasSaved[key];
                            if (!saveCheck.HasToCheck) {
                                _checkIfPuckWasSaved[key] = new SaveCheck();
                                continue;
                            }

                            //Logging.Log($"kvp.Check {saveCheck.FramesChecked} for team net {key} by {saveCheck.ShooterSteamId}.", ServerConfig, true);

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
                                    string _goaliePlayerSteamId = goalie.SteamId.Value.ToString();
                                    if (!_savePerc.TryGetValue(_goaliePlayerSteamId, out var savePercValue)) {
                                        _savePerc.Add(_goaliePlayerSteamId, (0, 0));
                                        savePercValue = (0, 0);
                                    }

                                    (int saves, int sog) = _savePerc[_goaliePlayerSteamId] = (++savePercValue.Saves, ++savePercValue.Shots);

                                    QueueStatUpdate(Codebase.Constants.SAVEPERC + _goaliePlayerSteamId, _savePerc[_goaliePlayerSteamId].ToString());
                                    LogSavePerc(_goaliePlayerSteamId, saves, sog);
                                    
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
                                                    string shotFlag = DetermineShotFlag(releaseInfo.PuckPosition, shooter.Team.Value);
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
                                            string shotFlag = (shotPosition != Vector3.zero) ? DetermineShotFlag(shotPosition, shooter.Team.Value) : "";
                                            
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
                                                    string _goaliePlayerSteamId = goalie.SteamId.Value.ToString();
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
                                if (++saveCheck.FramesChecked > ServerManager.Instance.ServerConfigurationManager.ServerConfiguration.serverTickRate)
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

                            //Logging.Log($"kvp.Check {blockCheck.FramesChecked} for team {key} blocked by {blockCheck.BlockerSteamId}.", ServerConfig, true);

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
                                if (++blockCheck.FramesChecked > ServerManager.Instance.ServerConfigurationManager.ServerConfiguration.serverTickRate)
                                    _checkIfPuckWasBlocked[key] = new BlockCheck();
                            }
                        }
                    }

                    Puck puck = PuckManager.Instance.GetPuck();
                    if (puck) {
                        _puckZCoordinateDifference = (puck.Rigidbody.transform.position.z - _puckLastCoordinate.z) / 240 * ServerManager.Instance.ServerConfigurationManager.ServerConfiguration.serverTickRate;
                        _puckLastCoordinate = new Vector3(puck.Rigidbody.transform.position.x, puck.Rigidbody.transform.position.y, puck.Rigidbody.transform.position.z);
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in ServerManager_Update_Patch Postfix().\n{ex}", ServerConfig);
                }

                return;
            }
        }

        /// <summary>
        /// Class that patches the UpdatePlayer event from UIScoreboard.
        /// </summary>
        [HarmonyPatch(typeof(UIScoreboard), nameof(UIScoreboard.UpdatePlayer))]
        public class UIScoreboard_UpdatePlayer_Patch {
            [HarmonyPostfix]
            public static void Postfix(UIScoreboard __instance, Player player) {
                try {
                    // If this is the server, do not use the patch.
                    if (ServerFunc.IsDedicatedServer())
                        return;

                    if (!_hasRegisteredWithNamedMessageHandler || !_serverHasResponded) {
                        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(Constants.FROM_SERVER_TO_CLIENT, ReceiveData);
                        _hasRegisteredWithNamedMessageHandler = true;

                        DateTime now = DateTime.UtcNow;
                        if (_lastDateTimeAskStartupData + TimeSpan.FromSeconds(1) < now && _askServerForStartupDataCount++ < 10) {
                            _lastDateTimeAskStartupData = now;
                            NetworkCommunication.SendData(Constants.ASK_SERVER_FOR_STARTUP_DATA, "1", NetworkManager.ServerClientId, Constants.FROM_CLIENT_TO_SERVER, _clientConfig);
                        }
                    }
                    else if (_askForKick) {
                        _askForKick = false;
                        NetworkCommunication.SendData(Constants.MOD_NAME + "_kick", "1", NetworkManager.ServerClientId, Constants.FROM_CLIENT_TO_SERVER, _clientConfig);
                    }
                    else if (_addServerModVersionOutOfDateMessage) {
                        _addServerModVersionOutOfDateMessage = false;
                        UIChat.Instance.AddChatMessage($"Server's {Constants.WORKSHOP_MOD_NAME} mod is out of date. Some functionalities might not work properly.");
                    }

                    ScoreboardModifications(true);

                    string playerSteamId = player.SteamId.Value.ToString();
                    if (!string.IsNullOrEmpty(playerSteamId) && _stars.Values.Contains(playerSteamId)) {
                        Dictionary<Player, VisualElement> playerVisualElementMap =
                            SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), __instance, "playerVisualElementMap");

                        if (playerVisualElementMap.ContainsKey(player)) {
                            VisualElement visualElement = playerVisualElementMap[player];
                            Label label = visualElement.Query<Label>("UsernameLabel");
                            // Strip any existing star tags before adding the correct one
                            string baseText = StripStarTags(label.text);
                            label.text = GetStarTag(playerSteamId) + baseText;
                        }
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in UIScoreboard_UpdateServer_Patch Postfix().\n{ex}", _clientConfig);
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
                if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Playing || !_logic)
                    return;

                try {
                    Player player = null;
                    Stick stick = SystemFunc.GetStick(collision.gameObject);
                    if (!stick) {
                        PlayerBodyV2 playerBody = SystemFunc.GetPlayerBodyV2(collision.gameObject);
                        if (!playerBody || !playerBody.Player)
                            return;

                        player = playerBody.Player;
                    }
                    else {
                        if (!stick.Player)
                            return;

                        player = stick.Player;
                    }

                    string currentPlayerSteamId = player.SteamId.Value.ToString();

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
                    else if (lastTimeCollisionExitWatch.ElapsedMilliseconds > ServerConfig.MaxPossessionMilliseconds || (!string.IsNullOrEmpty(lastPlayerOnPuckTipIncludedSteamId) && lastPlayerOnPuckTipIncludedSteamId != currentPlayerSteamId)) {
                        watch.Restart();

                        if (!string.IsNullOrEmpty(lastPlayerOnPuckTipIncludedSteamId) && lastPlayerOnPuckTipIncludedSteamId != currentPlayerSteamId) {
                            if (_playersCurrentPuckTouch.TryGetValue(lastPlayerOnPuckTipIncludedSteamId, out Stopwatch lastPlayerWatch))
                                lastPlayerWatch.Reset();
                        }
                    }

                    PlayerTeam otherTeam = TeamFunc.GetOtherTeam(player.Team.Value);

                    if (_puckRaycast.PuckIsGoingToNet[player.Team.Value]) {
                        if (PlayerFunc.IsGoalie(player) && Math.Abs(player.PlayerBody.Rigidbody.transform.position.z) > 13.5) {
                            PlayerTeam shooterTeam = otherTeam;
                            string shooterSteamId = _lastPlayerOnPuckTipIncludedSteamId[shooterTeam].SteamId;
                            if (!string.IsNullOrEmpty(shooterSteamId)) {
                                // Check if a SaveCheck already exists - if so, preserve stick hit status (OR logic)
                                // This prevents body saves from being counted multiple times when puck hits multiple body parts
                                bool hitStick = stick != null;
                                if (_checkIfPuckWasSaved.TryGetValue(player.Team.Value, out SaveCheck existingCheck) && existingCheck.HasToCheck) {
                                    // Preserve stick hit if any collision was with stick
                                    hitStick = existingCheck.HitStick || hitStick;
                                }
                                
                                _checkIfPuckWasSaved[player.Team.Value] = new SaveCheck {
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
                                _checkIfPuckWasBlocked[player.Team.Value] = new BlockCheck {
                                    HasToCheck = true,
                                    BlockerSteamId = player.SteamId.Value.ToString(),
                                    ShooterTeam = shooterTeam,
                                    BlockGameTime = GetCurrentGameTime(), // Store the game time when block check is set up
                                };
                            }
                        }
                    }
                    else {
                        if (_lastTeamOnPuckTipIncluded == otherTeam && PlayerFunc.IsGoalie(player) && Math.Abs(player.PlayerBody.Rigidbody.transform.position.z) > 13.5) {
                            if ((player.Team.Value == PlayerTeam.Blue && _puckZCoordinateDifference > ServerConfig.GoalieSaveCreaseSystemZDelta) || (player.Team.Value == PlayerTeam.Red && _puckZCoordinateDifference < -ServerConfig.GoalieSaveCreaseSystemZDelta)) {
                                (double startX, double endX) = (0, 0);
                                (double startZ, double endZ) = (0, 0);
                                if (player.Team.Value == PlayerTeam.Blue) {
                                    (startX, endX) = ZoneFunc.ICE_X_POSITIONS[IceElement.BlueTeam_BluePaint];
                                    (startZ, endZ) = ZoneFunc.ICE_Z_POSITIONS[IceElement.BlueTeam_BluePaint];
                                }
                                else {
                                    (startX, endX) = ZoneFunc.ICE_X_POSITIONS[IceElement.RedTeam_BluePaint];
                                    (startZ, endZ) = ZoneFunc.ICE_Z_POSITIONS[IceElement.RedTeam_BluePaint];
                                }

                                bool goalieIsInHisCrease = true;
                                if (player.PlayerBody.Rigidbody.transform.position.x - ServerConfig.GoalieRadius < startX ||
                                    player.PlayerBody.Rigidbody.transform.position.x + ServerConfig.GoalieRadius > endX ||
                                    player.PlayerBody.Rigidbody.transform.position.z - ServerConfig.GoalieRadius < startZ ||
                                    player.PlayerBody.Rigidbody.transform.position.z + ServerConfig.GoalieRadius > endZ) {
                                    goalieIsInHisCrease = false;
                                }

                                if (goalieIsInHisCrease) {
                                    PlayerTeam shooterTeam = TeamFunc.GetOtherTeam(player.Team.Value);
                                    string shooterSteamId = _lastPlayerOnPuckTipIncludedSteamId[shooterTeam].SteamId;
                                    if (!string.IsNullOrEmpty(shooterSteamId)) {
                                        // Check if a SaveCheck already exists - if so, preserve stick hit status (OR logic)
                                        // This prevents body saves from being counted multiple times when puck hits multiple body parts
                                        bool hitStick = stick != null;
                                        if (_checkIfPuckWasSaved.TryGetValue(player.Team.Value, out SaveCheck existingCheck) && existingCheck.HasToCheck) {
                                            // Preserve stick hit if any collision was with stick
                                            hitStick = existingCheck.HitStick || hitStick;
                                        }
                                        
                                        _checkIfPuckWasSaved[player.Team.Value] = new SaveCheck {
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
                    Logging.LogError($"Error in Puck_OnCollisionEnter_Patch Postfix().\n{ex}", ServerConfig);
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
                    if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Playing || !_logic)
                        return;

                    Player player;

                    Stick stick = SystemFunc.GetStick(collision.gameObject);
                    if (!stick) {
                        PlayerBodyV2 playerBody = SystemFunc.GetPlayerBodyV2(collision.gameObject);
                        if (!playerBody || !playerBody.Player)
                            return;

                        player = playerBody.Player;
                    }
                    else {
                        if (!stick.Player)
                            return;

                        player = stick.Player;
                    }

                    string playerSteamId = player.SteamId.Value.ToString();

                    // Possession time is now only tracked in OnCollisionEnter (between consecutive touches)
                    // No need to track in OnCollisionStay

                    if (!_lastTimeOnCollisionStayOrExitWasCalled.TryGetValue(playerSteamId, out Stopwatch lastTimeCollisionWatch)) {
                        lastTimeCollisionWatch = new Stopwatch();
                        lastTimeCollisionWatch.Start();
                        _lastTimeOnCollisionStayOrExitWasCalled.Add(playerSteamId, lastTimeCollisionWatch);
                    }
                    lastTimeCollisionWatch.Restart();

                    string lastPlayerOnPuckTipIncluded = _lastPlayerOnPuckTipIncludedSteamId[player.Team.Value].SteamId;

                    // Note: Pass/Reception events are now created in real-time in ProcessPuckTouch
                    // when a new player on the same team touches the puck, converting the previous
                    // player's last Touch to a Pass and recording the current touch as a Reception
                    
                    if (playerSteamId != lastPlayerOnPuckTipIncluded) {
                        _lastPlayerOnPuckTipIncludedSteamId[player.Team.Value] = (playerSteamId, DateTime.UtcNow);
                    }

                    _lastTeamOnPuckTipIncluded = player.Team.Value;

                    // Puck battle tracking - detect when puck is tipped (multiple sticks contacting simultaneously)
                    bool isTipped = PuckFunc.PuckIsTipped(playerSteamId, ServerConfig.MaxTippedMilliseconds, _playersCurrentPuckTouch, _lastTimeOnCollisionStayOrExitWasCalled);
                    
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
                                if (kvp.Value.IsRunning && kvp.Value.ElapsedMilliseconds < ServerConfig.MaxTippedMilliseconds * 2) {
                                    // Also check if they haven't exited recently
                                    if (_lastTimeOnCollisionStayOrExitWasCalled.TryGetValue(touchingSteamId, out Stopwatch exitWatch)) {
                                        if (exitWatch.IsRunning && exitWatch.ElapsedMilliseconds < ServerConfig.MaxTippedMilliseconds * 2) {
                                            playersTouchingPuck.Add(touchingSteamId);
                                            teamsTouchingPuck.Add(touchingPlayer.Team.Value);
                                        }
                                    } else {
                                        // If no exit watch, assume they're touching
                                        playersTouchingPuck.Add(touchingSteamId);
                                        teamsTouchingPuck.Add(touchingPlayer.Team.Value);
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
                                            string battleFlag = battlePlayer.Team.Value == defendingTeam ? "defending" : "contesting";
                                            
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
                        _lastTeamOnPuck = player.Team.Value;
                        _lastPlayerOnPuckSteamId[player.Team.Value] = (playerSteamId, DateTime.UtcNow);
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
                        _lastEvent.PlayerTeam != (int)player.Team.Value &&
                        _lastPossession.Team != PlayerTeam.None && 
                        _lastPossession.Team != player.Team.Value) {
                        // Last event was a failed touch from opposing team - don't update possession
                        // Keep the previous team's possession until a successful touch occurs
                        shouldUpdatePossession = false;
                    }
                    
                    if (shouldUpdatePossession) {
                        string currentPossessionSteamId = PlayerFunc.GetPlayerSteamIdInPossession(ServerConfig.MinPossessionMilliseconds, ServerConfig.MaxPossessionMilliseconds,
                        ServerConfig.MaxTippedMilliseconds, _playersLastTimePuckPossession, _playersCurrentPuckTouch, true);
                        if (!string.IsNullOrEmpty(currentPossessionSteamId)) {
                            // Track possession start time for new possession
                            if (!_possessionStartTime.ContainsKey(currentPossessionSteamId)) {
                                _possessionStartTime[currentPossessionSteamId] = DateTime.UtcNow;
                            }

                            _lastPossession = new Possession {
                                SteamId = currentPossessionSteamId,
                                Team = player.Team.Value,
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
                    Logging.LogError($"Error in Puck_OnCollisionStay_Patch Postfix().\n{ex}", ServerConfig);
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
                    if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Playing || !_logic)
                        return;

                    Stick stick = SystemFunc.GetStick(collision.gameObject);
                    if (!stick)
                        return;

                    string playerSteamId = stick.Player.SteamId.Value.ToString();
                    Player player = stick.Player;

                    // Record shot attempts when puck leaves a player's stick (if it meets shot criteria)
                    // Only reset _lastShotWasCounted for non-goalies releasing the puck (new shot attempts)
                    // This prevents resetting the flag when goalie touches puck after a save, which would cause duplicate save counting
                    if (!__instance.IsTouchingStick && !PlayerFunc.IsGoalie(player)) {
                        _lastShotWasCounted[stick.Player.Team.Value] = false;
                        _lastBlockWasCounted[stick.Player.Team.Value] = false;
                        
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
                            bool isBlueTeam = (player.Team.Value == PlayerTeam.Blue);
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
                                    // Standard threshold: 18-degree threshold: cos(18°) ≈ 0.951
                                    // Tighter threshold for distance shots: 12-degree threshold: cos(12°) ≈ 0.978
                                    const float ANGLE_DISTANCE_THRESHOLD = 50.0f; // Distance threshold for tighter angle requirement
                                    const float MIN_ALIGNMENT_DOT_STANDARD = 0.951f; // cos(18°) - for shots within 50 units
                                    const float MIN_ALIGNMENT_DOT_DISTANCE = 0.978f; // cos(12°) - for shots beyond 50 units
                                    
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
                            if (_lastPlayerOnPuckTipIncludedSteamId.TryGetValue(player.Team.Value, out var lastTouchInfo)) {
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
                                string shotFlag = DetermineShotFlag(puckPos, player.Team.Value);
                                // Record shot attempt with "attempt" outcome (will be updated to "on net" or "missed" based on raycast)
                                RecordPlayByPlayEventInternal(PlayByPlayEventType.Shot, player, puckPos, puckVel, "attempt", shotFlag);
                                
                                // Track shot attempt stats for syncing
                                if (!_shotAttempts.TryGetValue(playerSteamId, out int _))
                                    _shotAttempts.Add(playerSteamId, 0);
                                _shotAttempts[playerSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.SHOT_ATTEMPTS + playerSteamId, _shotAttempts[playerSteamId].ToString());
                                
                                // Track team shot attempts
                                if (!_teamShotAttempts.TryGetValue(player.Team.Value, out int _))
                                    _teamShotAttempts.Add(player.Team.Value, 0);
                                _teamShotAttempts[player.Team.Value] += 1;
                                QueueStatUpdate(Codebase.Constants.TEAM_SHOT_ATTEMPTS + player.Team.Value.ToString(), _teamShotAttempts[player.Team.Value].ToString());
                                
                                // Update last shot attempt game time for cooldown (per player)
                                _lastShotAttemptGameTime[playerSteamId] = currentGameTime;
                            }
                            
                            // Store release info - will be used if raycast confirms (for on-net shots)
                            // Only store if we actually recorded a shot attempt, to prevent duplicates
                            if (puckSpeed >= MIN_SHOT_VELOCITY && movingTowardsNet && movingTowardsOpponentNet && velocityAlignedWithNet && playerReleasingPossession && (lastShotGameTime < 0f || secondsSinceLastShot >= SHOT_ATTEMPT_COOLDOWN_SECONDS)) {
                                _pendingShotReleases[player.Team.Value] = (playerSteamId, shooterPos, puckPos, puckVel, DateTime.UtcNow);
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

                    _lastPlayerOnPuckTipIncludedSteamId[stick.Player.Team.Value] = (playerSteamId, DateTime.UtcNow);
                    _lastTeamOnPuckTipIncluded = stick.Player.Team.Value;

                    if (!PuckFunc.PuckIsTipped(playerSteamId, ServerConfig.MaxTippedMilliseconds, _playersCurrentPuckTouch, _lastTimeOnCollisionStayOrExitWasCalled)) {
                        _lastTeamOnPuck = stick.Player.Team.Value;
                        _lastPlayerOnPuckSteamId[stick.Player.Team.Value] = (playerSteamId, DateTime.UtcNow);
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in Puck_OnCollisionExit_Patch Postfix().\n{ex}", ServerConfig);
                }
            }
        }

        #region PlayerBodyV2_OnCollision
        /// <summary>
        /// Class that patches the OnCollisionEnter event from PlayerBodyV2.
        /// </summary>
        [HarmonyPatch(typeof(PlayerBodyV2), "OnCollisionEnter")]
        public class PlayerBodyV2_OnCollisionEnter_Patch {
            [HarmonyPostfix]
            public static void Postfix(PlayerBodyV2 __instance, Collision collision) {
                // If this is not the server or game is not started, do not use the patch.
                if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Playing || !_logic)
                    return;

                try {
                    if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
                        return;

                    PlayerBodyV2 collisionPlayerBody = SystemFunc.GetPlayerBodyV2(collision.gameObject);

                    if (!collisionPlayerBody || !collisionPlayerBody.Player || !collisionPlayerBody.Player.IsCharacterFullySpawned)
                        return;

                    if (!__instance || !__instance.Player || !__instance.Player.IsCharacterFullySpawned)
                        return;

                    //float force = Utils.GetCollisionForce(collision);

                    // If the player has been hit by the same team, return;
                    if (collisionPlayerBody.Player.Team.Value == __instance.Player.Team.Value)
                        return;

                    string collisionPlayerBodySteamId = collisionPlayerBody.Player.SteamId.Value.ToString();
                    if (!_playerIsDown.TryGetValue(collisionPlayerBodySteamId, out bool collisionPlayerBodyIsDown))
                        collisionPlayerBodyIsDown = false;

                    string instancePlayerSteamId = __instance.Player.SteamId.Value.ToString();

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

                        ProcessHit(__instance.Player.SteamId.Value.ToString(), collisionPlayerBody.Player.SteamId.Value.ToString());
                    }

                    if (__instance.Player.PlayerBody.HasFallen || __instance.Player.PlayerBody.HasSlipped) {
                        if (_playerIsDown.TryGetValue(instancePlayerSteamId, out bool _))
                            _playerIsDown[instancePlayerSteamId] = true;
                        else
                            _playerIsDown.Add(instancePlayerSteamId, true);
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(PlayerBodyV2_OnCollisionEnter_Patch)} Postfix().\n{ex}", ServerConfig);
                }

                return;
            }
        }
        #endregion

        /// <summary>
        /// Class that patches the OnStandUp event from PlayerBodyV2.
        /// </summary>
        [HarmonyPatch(typeof(PlayerBodyV2), nameof(PlayerBodyV2.OnStandUp))]
        public class PlayerBodyV2_OnStandUp_Patch {
            [HarmonyPostfix]
            public static void Postfix(PlayerBodyV2 __instance) {
                // If this is not the server or game is not started, do not use the patch.
                if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance.Phase != GamePhase.Playing || !_logic)
                    return;

                try {
                    string playerSteamId = __instance.Player.SteamId.Value.ToString();
                    if (_playerIsDown.TryGetValue(playerSteamId, out bool _))
                        _playerIsDown[playerSteamId] = false;
                    else
                        _playerIsDown.Add(playerSteamId, false);
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(PlayerBodyV2_OnStandUp_Patch)} Postfix().\n{ex}", ServerConfig);
                }

                return;
            }
        }

        /// <summary>
        /// Class that patches the Server_SetPhase event from GameManager.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Server_SetPhase))]
        public class GameManager_Server_SetPhase_Patch {
            [HarmonyPrefix]
            public static bool Prefix(GameManager __instance, GamePhase phase, ref int time) {
                try {
                    // If this is not the server, do not use the patch.
                    if (!ServerFunc.IsDedicatedServer() || !_logic)
                        return true;

                    if (phase == GamePhase.FaceOff || phase == GamePhase.Warmup || phase == GamePhase.GameOver) {
                        ResetPuckWasSavedOrBlockedChecks();

                        _puckLastCoordinate = Vector3.zero;
                        _puckZCoordinateDifference = 0;

                        // Initialize play-by-play tracking
                        if (phase == GamePhase.FaceOff) {
                            // Only reset if this is the start of a new game (not a period transition)
                            bool isNewGame = (_gameStartTime == 0f || _playByPlayEvents.Count == 0);
                            if (isNewGame) {
                                _gameStartTime = Time.time;
                                _currentPeriod = 1;
                                _lastTrackedPeriod = 0; // Initialize period tracking
                                _nextPlayByPlayEventId = 0;
                                _playByPlayEvents.Clear();
                                _lastProcessedZoneFlagEventId = -1; // Reset zone flag scanner
                                _currentGameReferenceId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                                _currentTeamInPossession = PlayerTeam.None;
                                _currentPlayInPossession = 0;
                                
                                // Reset game time tracking for fractional precision
                                _lastWholeSecondGameTime = 0f;
                                _lastUnityTimeForGameTime = 0f;
                                _lastCountdownValue = -1;
                                
                                // Reset tooltips when new game begins (same timing as goals/assists reset)
                                // Clear pending stat updates before sending reset
                                _pendingStatUpdates.Clear();
                                SafeSendDataToAll(RESET_ALL, "1");
                            }
                            // Don't update period here - FaceOff happens after every goal, not just period transitions
                            // Period is read directly from GameState when needed
                            
                            // Reset faceoff flag so each faceoff (including after goals) gets recorded
                            _faceoffRecordedForCurrentPeriod = false;
                            
                            _lastRecordedPhase = phase;
                        }
                        else if (phase == GamePhase.Warmup) {
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
                        else if (phase == GamePhase.Playing) {
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
                                Logging.Log($"Period transition detected: {_lastTrackedPeriod} -> {currentPeriod}. Resolved pending faceoff.", ServerConfig);
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
                            // Record GameEnd event when game concludes
                            // Check if we already have a GameEnd event
                            bool hasGameEndEvent = _playByPlayEvents.Any(e => 
                                e.EventType == PlayByPlayEventType.GameEnd);
                            
                            if (!hasGameEndEvent) {
                                int finalPeriod = GetCurrentPeriod();
                                
                                // If game ended in regulation (3 periods), use exactly 900.0
                                // Otherwise, use the maximum gameTime from all events
                                float gameEndGameTime;
                                if (finalPeriod == 3) {
                                    // Game ended in regulation - use exactly 900.0 seconds
                                    gameEndGameTime = 900.0f;
                                }
                                else {
                                    // Game went to overtime or ended early - use actual max gameTime
                                    gameEndGameTime = _playByPlayEvents.Count > 0 
                                        ? _playByPlayEvents.Max(e => e.GameTime) 
                                        : GetCurrentGameTime();
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
                                Logging.Log($"Recorded GameEnd event at gameTime {gameEndGameTime:F3} (period {finalPeriod}).", ServerConfig);
                            }
                            
                            string gwgSteamId = "";
                            PlayerTeam winningTeam = PlayerTeam.None;
                            try {
                                if (__instance.GameState.Value.BlueScore > __instance.GameState.Value.RedScore) {
                                    winningTeam = PlayerTeam.Blue;
                                    gwgSteamId = _blueGoals[__instance.GameState.Value.RedScore];
                                }
                                else {
                                    winningTeam = PlayerTeam.Red;
                                    gwgSteamId = _redGoals[__instance.GameState.Value.BlueScore];
                                }

                                LogGWG(gwgSteamId);
                            }
                            catch (IndexOutOfRangeException) { } // Shootout goal or something, so no GWG.

                            Dictionary<string, double> starPoints = new Dictionary<string, double>();
                            foreach (Player player in PlayerManager.Instance.GetPlayers()) {
                                if (player == null || !player)
                                    continue;

                                string steamId = player.SteamId.Value.ToString();
                                starPoints.Add(steamId, 0);

                                double gwgModifier = gwgSteamId == player.SteamId.Value.ToString() ? 0.5d : 0;
                                double teamModifier = winningTeam == player.Team.Value ? 1.1d : 1d;

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

                            UIChat.Instance.Server_SendSystemChatMessage("STARS OF THE MATCH");
                            foreach (KeyValuePair<int, string> star in _stars.OrderByDescending(x => x.Key)) {
                                if (!string.IsNullOrEmpty(star.Value)) {
                                    Player player = PlayerManager.Instance.GetPlayerBySteamId(star.Value);
                                    if (player != null && player)
                                        UIChat.Instance.Server_SendSystemChatMessage($"The {(star.Key == 1 ? "first" : (star.Key == 2 ? "second" : "third"))} star is... #{player.Number.Value} {player.Username.Value} !");

                                    SafeSendDataToAll(STAR, $"{star.Value};{star.Key}");
                                    LogStar(star.Value, star.Key);
                                }
                            }

                            // Check play-by-play data before creating JSON - only export if game has sufficient players and events
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
                                Logging.LogError($"Error counting unique players for export: {ex}", ServerConfig);
                            }
                            
                            // Only create and export JSON and CSV if pbp check passes (8+ players and 300+ events)
                            if (uniquePlayerCount >= 8 && eventCount >= 300) {
                                // Use shared export function (exports both JSON and CSV)
                                ExportGameStats();
                            }
                            else {
                                Logging.Log($"Export skipped (JSON and CSV): {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)", ServerConfig);
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in GameManager_Server_SetPhase_Patch Prefix().\n{ex}", ServerConfig);
                }

                return true;
            }
        }

        /// <summary>
        /// Class that patches WrapPlayerUsername event from UIChat.
        /// </summary>
        [HarmonyPatch(typeof(UIChat), nameof(UIChat.WrapPlayerUsername))]
        public static class UIChat_WrapPlayerUsername_Patch {
            public static void Postfix(Player player, ref string __result) {
                if (player == null || !player)
                    return;

                string steamId = player.SteamId.Value.ToString();
                if (string.IsNullOrEmpty(steamId))
                    return;

                 __result = GetStarTag(steamId) + __result;
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

                Logging.Log($"Enabling...", ServerConfig, true);

                _harmony.PatchAll();

                Logging.Log($"Enabled.", ServerConfig, true);

                NetworkCommunication.AddToNotLogList(DATA_NAMES_TO_IGNORE);

                if (ServerFunc.IsDedicatedServer()) {
                    Server_RegisterNamedMessageHandler();

                    Logging.Log("Setting server sided config.", ServerConfig, true);
                    ServerConfig = ServerConfig.ReadConfig();
                    
                    // Always use server name (part before | if present) and remove spaces
                    try {
                        if (ServerManager.Instance != null && 
                            ServerManager.Instance.ServerConfigurationManager != null && 
                            ServerManager.Instance.ServerConfigurationManager.ServerConfiguration != null) {
                            string serverName = ServerManager.Instance.ServerConfigurationManager.ServerConfiguration.name;
                            if (!string.IsNullOrEmpty(serverName)) {
                                // Get only the part before | if it exists
                                int pipeIndex = serverName.IndexOf('|');
                                if (pipeIndex >= 0) {
                                    serverName = serverName.Substring(0, pipeIndex).Trim();
                                }
                                // Remove all spaces
                                serverName = serverName.Replace(" ", "");
                                // Sanitize for filename use - remove invalid characters
                                char[] invalidChars = Path.GetInvalidFileNameChars();
                                foreach (char c in invalidChars) {
                                    serverName = serverName.Replace(c.ToString(), "");
                                }
                                serverName = serverName.Trim();
                                // If after sanitization it's empty, use default
                                if (string.IsNullOrEmpty(serverName)) {
                                    serverName = "puck";
                                }
                                ServerConfig.FileHeaderName = serverName;
                                Logging.Log($"FileHeaderName set from server name: {ServerConfig.FileHeaderName}", ServerConfig, true);
                                
                                // Write updated config back to file
                                try {
                                    string rootPath = Path.GetFullPath(".");
                                    string configPath = Path.Combine(rootPath, Constants.MOD_NAME + "_serverconfig.json");
                                    File.WriteAllText(configPath, ServerConfig.ToString());
                                    Logging.Log($"Updated server config file with FileHeaderName: {ServerConfig.FileHeaderName}", ServerConfig, true);
                                }
                                catch (Exception writeEx) {
                                    Logging.LogError($"Can't write the server config file after updating FileHeaderName. (Permission error ?)\n{writeEx}", ServerConfig);
                                }
                            }
                        }
                    }
                    catch (Exception ex) {
                        Logging.LogError($"Error setting FileHeaderName from server name: {ex}", ServerConfig);
                    }
                }
                else {
                    Logging.Log("Setting client sided config.", ServerConfig, true);
                    _clientConfig = ClientConfig.ReadConfig();
                }

                Logging.Log("Subscribing to events.", ServerConfig, true);

                if (ServerFunc.IsDedicatedServer()) {
                    EventManager.Instance.AddEventListener("Event_OnClientConnected", Event_OnClientConnected);
                    EventManager.Instance.AddEventListener("Event_OnClientDisconnected", Event_OnClientDisconnected);
                    EventManager.Instance.AddEventListener("Event_OnPlayerRoleChanged", Event_OnPlayerRoleChanged);
                    EventManager.Instance.AddEventListener(Codebase.Constants.STATS_MOD_NAME, Event_OnStatsTrigger);
                    EventManager.Instance.AddEventListener(Codebase.Constants.RULESET_MOD_NAME, Event_OnRulesetTrigger);
                }
                else {
                    EventManager.Instance.AddEventListener("Event_Client_OnClientStopped", Event_Client_OnClientStopped);
                }

                _harmonyPatched = true;
                _logic = true;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to enable.\n{ex}", ServerConfig);
                return false;
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

                Logging.Log($"Disabling...", ServerConfig, true);

                Logging.Log("Unsubscribing from events.", ServerConfig, true);
                NetworkCommunication.RemoveFromNotLogList(DATA_NAMES_TO_IGNORE);
                if (ServerFunc.IsDedicatedServer()) {
                    EventManager.Instance.RemoveEventListener("Event_OnClientConnected", Event_OnClientConnected);
                    EventManager.Instance.RemoveEventListener("Event_OnClientDisconnected", Event_OnClientDisconnected);
                    EventManager.Instance.RemoveEventListener("Event_OnPlayerRoleChanged", Event_OnPlayerRoleChanged);
                    EventManager.Instance.RemoveEventListener(Codebase.Constants.STATS_MOD_NAME, Event_OnStatsTrigger);
                    EventManager.Instance.RemoveEventListener(Codebase.Constants.RULESET_MOD_NAME, Event_OnRulesetTrigger);
                    NetworkManager.Singleton?.CustomMessagingManager?.UnregisterNamedMessageHandler(Constants.FROM_CLIENT_TO_SERVER);
                }
                else {
                    EventManager.Instance.RemoveEventListener("Event_Client_OnClientStopped", Event_Client_OnClientStopped);
                    Event_Client_OnClientStopped(new Dictionary<string, object>());
                    NetworkManager.Singleton?.CustomMessagingManager?.UnregisterNamedMessageHandler(Constants.FROM_SERVER_TO_CLIENT);
                }

                _hasRegisteredWithNamedMessageHandler = false;
                _rulesetModEnabled = null;
                _serverHasResponded = false;
                _askServerForStartupDataCount = 0;

                ScoreboardModifications(false);

                _harmony.UnpatchSelf();

                Logging.Log($"Disabled.", ServerConfig, true);

                _harmonyPatched = false;
                _logic = true;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to disable.\n{ex}", ServerConfig);
                return false;
            }
        }
        #endregion

        #region Events
        public static void Event_OnStatsTrigger(Dictionary<string, object> message) {
            try {
                foreach (KeyValuePair<string, object> kvp in message) {
                    string value = (string)kvp.Value;

                    switch (kvp.Key) {
                        case Codebase.Constants.SOG:
                            _sendSavePercDuringGoalNextFrame_Player = PlayerManager.Instance.GetPlayerBySteamId(value);
                            if (_sendSavePercDuringGoalNextFrame_Player == null || !_sendSavePercDuringGoalNextFrame_Player)
                                Logging.LogError($"{nameof(_sendSavePercDuringGoalNextFrame_Player)} is null.", ServerConfig);
                            else
                                _sendSavePercDuringGoalNextFrame = true;
                            break;

                        case Codebase.Constants.LOGIC:
                            _logic = bool.Parse(value);
                            break;
                    }
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnStatsTrigger)}.\n{ex}", ServerConfig);
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
                Logging.LogError($"Error in {nameof(Event_OnRulesetTrigger)}.\n{ex}", ServerConfig);
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

            //Logging.Log("Event_OnClientConnected", ServerConfig);

            try {
                Server_RegisterNamedMessageHandler();

                ulong clientId = (ulong)message["clientId"];
                string clientSteamId = PlayerManager.Instance.GetPlayerByClientId(clientId).SteamId.Value.ToString();
                try {
                    _players_ClientId_SteamId.Add(clientId, "");
                }
                catch {
                    _players_ClientId_SteamId.Remove(clientId);
                    _players_ClientId_SteamId.Add(clientId, "");
                }

                // Send version check to client (optional - for compatibility checking)
                NetworkCommunication.SendData(Constants.MOD_NAME + "_" + nameof(MOD_VERSION), MOD_VERSION, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);

                CheckForRulesetMod();
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnClientConnected)}.\n{ex}", ServerConfig);
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

            //Logging.Log("Event_OnClientDisconnected", ServerConfig);

            try {
                ulong clientId = (ulong)message["clientId"];
                string clientSteamId;
                try {
                    clientSteamId = _players_ClientId_SteamId[clientId];
                }
                catch {
                    Logging.LogError($"Client Id {clientId} steam Id not found in {nameof(_players_ClientId_SteamId)}.", ServerConfig);
                    return;
                }

                _sentOutOfDateMessage.Remove(clientId);

                _playerIsDown.Remove(clientSteamId);
                _playersCurrentPuckTouch.Remove(clientSteamId);
                _playersLastTimePuckPossession.Remove(clientSteamId);
                _lastTimeOnCollisionStayOrExitWasCalled.Remove(clientSteamId);

                _players_ClientId_SteamId.Remove(clientId);
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnClientDisconnected)}.\n{ex}", ServerConfig);
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

            //Logging.Log("Event_Client_OnClientStopped", _clientConfig);

            try {
                ServerConfig = new ServerConfig();

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
                _blueGoals.Clear();
                _blueAssists.Clear();
                _redGoals.Clear();
                _redAssists.Clear();
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
                if (string.IsNullOrEmpty(kvp.Value))
                    players_ClientId_SteamId_ToChange.Add(kvp.Key, PlayerManager.Instance.GetPlayerByClientId(kvp.Key).SteamId.Value.ToString());
            }

            foreach (var kvp in players_ClientId_SteamId_ToChange) {
                if (!string.IsNullOrEmpty(kvp.Value)) {
                    _players_ClientId_SteamId[kvp.Key] = kvp.Value;
                    Logging.Log($"Added clientId {kvp.Key} linked to Steam Id {kvp.Value}.", ServerConfig);
                }
            }

            Player player = (Player)message["player"];

            string playerSteamId = player.SteamId.Value.ToString();

            if (string.IsNullOrEmpty(playerSteamId))
                return;

            PlayerRole newRole = (PlayerRole)message["newRole"];

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
                PlayerTeam playerTeam = takeawayPlayer.Team.Value;
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
                EventZone currentZone = DetermineEventZone(puckPos, player.Team.Value);
                
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
                            _lastEvent.PlayerTeam == (int)player.Team.Value) {
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
                                    PlayerTeam playerTeam = passer.Team.Value;
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
            if (_currentPlayInPossession != 1 || _currentTeamInPossession != player.Team.Value)
                return;
            
            // Prevent re-entrancy - if we're already validating, don't validate again
            // This prevents infinite recursion when recording turnover/takeaway events
            if (_isValidatingTurnoversTakeaways)
                return;
            
            _isValidatingTurnoversTakeaways = true;
            try {
                // Detect possession change by looking at play-by-play events
                // Find the last event from a different team before the current team's possession started
                PlayerTeam previousTeam = PlayerTeam.None;
                if (_playByPlayEvents.Count > 1) {
                    // Look backwards from the current event to find when team changed
                    for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        if (_playByPlayEvents[i].PlayerTeam != (int)player.Team.Value) {
                            previousTeam = (PlayerTeam)_playByPlayEvents[i].PlayerTeam;
                            break;
                        }
                    }
                }
                
                // If no previous team found, try using _lastPossession as fallback
                if (previousTeam == PlayerTeam.None && _lastPossession.Team != PlayerTeam.None && _lastPossession.Team != player.Team.Value) {
                    previousTeam = _lastPossession.Team;
                }
                
                // Check if possession changed (different team than current)
                if (previousTeam == PlayerTeam.None || previousTeam == player.Team.Value)
                    return; // No possession change or same team
                
                string currentPlayerSteamId = player.SteamId.Value.ToString();
                
                // Find the first successful touch from the new team (the takeaway player)
                // Look for the first event from the new team in the current possession
                string takeawaySteamId = currentPlayerSteamId; // Default to current player
                PlayByPlayEvent firstNewTeamEvent = null; // Track the first successful event from new team
                if (_playByPlayEvents.Count > 0) {
                    // Search backwards to find where team changed from previousTeam to new team
                    // The first new team event is the one right after the last previous team event
                    for (int i = _playByPlayEvents.Count - 1; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        if (_playByPlayEvents[i].PlayerTeam == (int)player.Team.Value) {
                            string outcome = _playByPlayEvents[i].Outcome ?? "";
                            if (outcome == "successful" || outcome == "neutral" || outcome == "") {
                                // This is a new team event - track it (we'll keep the first one we find going backwards)
                                takeawaySteamId = _playByPlayEvents[i].PlayerSteamId;
                                firstNewTeamEvent = _playByPlayEvents[i];
                            }
                        } else if (_playByPlayEvents[i].PlayerTeam == (int)previousTeam) {
                            // We've hit the previous team's events - the last new team event we found is the first one
                            break;
                        }
                    }
                }
                
                // Check if previous team had 2+ events in possession chain
                // Find the last successful play from the previous team (before the new team's possession started)
                PlayByPlayEvent lastSuccessfulPlay = null;
                if (_playByPlayEvents.Count > 1) {
                    for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        if (_playByPlayEvents[i].PlayerTeam == (int)previousTeam) {
                            string outcome = _playByPlayEvents[i].Outcome ?? "";
                            if (outcome == "successful" || outcome == "neutral" || outcome == "") {
                                lastSuccessfulPlay = _playByPlayEvents[i];
                                break;
                            }
                            // Continue searching backwards even if this event is failed - we want the last successful one
                        }
                        // Don't break on different team - continue searching backwards to find previous team's events
                    }
                }
                
                // Check if last successful play was recent (within 4 seconds)
                bool recentPossessionChange = false;
                if (lastSuccessfulPlay != null && _playByPlayEvents.Count > 0) {
                    float newTeamTouchTime = _playByPlayEvents[_playByPlayEvents.Count - 1].GameTime;
                    float timeSinceLastSuccessfulPlay = newTeamTouchTime - lastSuccessfulPlay.GameTime;
                    recentPossessionChange = timeSinceLastSuccessfulPlay <= 4.0f;
                } else {
                    recentPossessionChange = (DateTime.UtcNow - _lastPossession.Date).TotalMilliseconds < 1000;
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
                
                // Fallback: count consecutive successful events from previous team
                // Search backwards through events, counting consecutive successful events from previous team
                if (previousPlayCount == 0 && _playByPlayEvents.Count > 1) {
                    int count = 0;
                    for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                        if (_playByPlayEvents[i].PlayerTeam == (int)previousTeam) {
                            string outcome = _playByPlayEvents[i].Outcome ?? "";
                            if (outcome == "successful" || outcome == "neutral" || outcome == "") {
                                count++;
                            } else {
                                // Stop counting when we hit a failed event (breaks consecutive chain)
                                break;
                            }
                        }
                        // Continue searching backwards even if we hit different team's events
                        // (we want to find all previous team's consecutive successful events)
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
                    
                    // Get the turnover player from the last successful play from the previous team
                    string turnoverSteamId = "";
                    if (lastSuccessfulPlay != null) {
                        turnoverSteamId = lastSuccessfulPlay.PlayerSteamId;
                    } else {
                        // Fallback: find last successful event from previous team
                        for (int i = _playByPlayEvents.Count - 2; i >= 0 && i >= _playByPlayEvents.Count - 22; i--) {
                            if (_playByPlayEvents[i].PlayerTeam == (int)previousTeam) {
                                string outcome = _playByPlayEvents[i].Outcome ?? "";
                                if (outcome == "successful" || outcome == "neutral" || outcome == "") {
                                    turnoverSteamId = _playByPlayEvents[i].PlayerSteamId;
                                    break;
                                }
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
                        string takeawayTeamInPossession = (takeawayPlayer.Team.Value == PlayerTeam.Blue ? "Blue" : "Red");
                        string turnoverTeamInPossession = (turnoverPlayer.Team.Value == PlayerTeam.Blue ? "Blue" : "Red");
                        
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
                PlayerTeam playerTeam = turnoverPlayer.Team.Value;
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
                    PlayerTeam playerTeam = player.Team.Value;
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
                        PlayerTeam takeawayTeam = takeawayPlayer.Team.Value;
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
            
            // Helper function to extract SteamIDs from roster field (format: `="steamId1";="steamId2"`)
            HashSet<string> ExtractSteamIdsFromRosterField(string rosterField) {
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
            
            // Process each event chronologically
            for (int i = 0; i < sortedEvents.Count; i++) {
                var currentEvent = sortedEvents[i];
                float currentGameTime = currentEvent.GameTime;
                
                // Skip events with invalid game time
                if (currentGameTime <= 0f)
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
                        playerOnIceStartTime[steamId] = currentGameTime;
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
                PlayerTeam winnerTeam = winnerPlayer.Team.Value;
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
                PlayerTeam loserTeam = loserPlayer.Team.Value;
                if (!_teamPuckBattleLosses.TryGetValue(loserTeam, out int _))
                    _teamPuckBattleLosses.Add(loserTeam, 0);
                _teamPuckBattleLosses[loserTeam] += 1;
                QueueStatUpdate(Codebase.Constants.TEAM_PUCK_BATTLE_LOSSES + loserTeam.ToString(), _teamPuckBattleLosses[loserTeam].ToString());
            }
        }

        private static void Server_RegisterNamedMessageHandler() {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null && !_hasRegisteredWithNamedMessageHandler) {
                Logging.Log($"RegisterNamedMessageHandler {Constants.FROM_CLIENT_TO_SERVER}.", ServerConfig);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(Constants.FROM_CLIENT_TO_SERVER, ReceiveData);

                _hasRegisteredWithNamedMessageHandler = true;
            }
        }

        private static void CheckForRulesetMod() {
            if (ModManagerV2.Instance == null || ModManagerV2.Instance.EnabledModIds == null || (_rulesetModEnabled != null && (bool)_rulesetModEnabled))
                return;

            _rulesetModEnabled = ModManagerV2.Instance.EnabledModIds.Contains(3501446576) ||
                                 ModManagerV2.Instance.EnabledModIds.Contains(3500559233);
            Logging.Log($"Ruleset mod is enabled : {_rulesetModEnabled}.", ServerConfig, true);
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
                    (dataName, dataStr) = NetworkCommunication.GetData(clientId, reader, ServerConfig);

                switch (dataName) {
                    case Constants.MOD_NAME + "_" + nameof(MOD_VERSION): // CLIENT-SIDE : Mod version check, kick if client and server versions are not the same.
                        _serverHasResponded = true;
                        if (MOD_VERSION == dataStr) {
                            // Server has the mod - set up tooltips if scoreboard is ready
                            if (UIScoreboard.Instance != null) {
                                VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), UIScoreboard.Instance, "container");
                                if (scoreboardContainer != null && !_hasUpdatedUIScoreboard.Contains("header")) {
                                    ScoreboardModifications(true);
                                } else if (scoreboardContainer != null && _hasUpdatedUIScoreboard.Contains("header") && _teamTooltips.Count == 0) {
                                    // Scoreboard is set up but team tooltips aren't - set them up now
                                    SetupTeamTooltips(scoreboardContainer);
                                }
                                
                                // Ensure tooltips are created for all existing players now that server has responded
                                // Call ScoreboardModifications again to create tooltips for players that don't have them
                                if (_hasUpdatedUIScoreboard.Contains("header") && scoreboardContainer != null) {
                                    // Schedule tooltip creation for existing players
                                    scoreboardContainer.schedule.Execute(() => {
                                        // This will iterate through all players and create tooltips for those that don't have them
                                        ScoreboardModifications(true);
                                    }).ExecuteLater(100); // Small delay to ensure scoreboard is ready
                                }
                            }
                            break;
                        }
                        else if (OLD_MOD_VERSIONS.Contains(dataStr)) {
                            _addServerModVersionOutOfDateMessage = true;
                            break;
                        }

                        _askForKick = true;
                        break;

                    case Constants.MOD_NAME + "_kick": // SERVER-SIDE : Kick the client that asked to be kicked.
                        if (dataStr != "1")
                            break;

                        //NetworkManager.Singleton.DisconnectClient(clientId,
                        //$"Mod is out of date. Please unsubscribe from {Constants.WORKSHOP_MOD_NAME} in the workshop and restart your game to update.");

                        if (!_sentOutOfDateMessage.TryGetValue(clientId, out DateTime lastCheckTime)) {
                            lastCheckTime = DateTime.MinValue;
                            _sentOutOfDateMessage.Add(clientId, lastCheckTime);
                        }

                        DateTime utcNow = DateTime.UtcNow;
                        if (lastCheckTime + TimeSpan.FromSeconds(900) < utcNow) {
                            if (string.IsNullOrEmpty(PlayerManager.Instance.GetPlayerByClientId(clientId).Username.Value.ToString()))
                                break;

                            Logging.Log($"Warning client {clientId} mod out of date.", ServerConfig);
                            UIChat.Instance.Server_SendSystemChatMessage($"{PlayerManager.Instance.GetPlayerByClientId(clientId).Username.Value} : {Constants.WORKSHOP_MOD_NAME} Mod is out of date. Please unsubscribe from {Constants.WORKSHOP_MOD_NAME} in the workshop and restart your game to update.");
                            _sentOutOfDateMessage[clientId] = utcNow;
                        }
                        break;

                    case Constants.ASK_SERVER_FOR_STARTUP_DATA: // SERVER-SIDE : Send the necessary data to client.
                        if (dataStr != "1")
                            break;

                        NetworkCommunication.SendData(Constants.MOD_NAME + "_" + nameof(MOD_VERSION), MOD_VERSION, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);

                        if (_sog.Count != 0) {
                            string batchSOG = "";
                            foreach (string key in new List<string>(_sog.Keys))
                                batchSOG += key + ';' + _sog[key].ToString() + ';';
                            batchSOG = batchSOG.Remove(batchSOG.Length - 1);
                            NetworkCommunication.SendData(BATCH_SOG, batchSOG, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_savePerc.Count != 0) {
                            string batchSavePerc = "";
                            foreach (string key in new List<string>(_savePerc.Keys))
                                batchSavePerc += key + ';' + _savePerc[key].ToString() + ';';
                            batchSavePerc = batchSavePerc.Remove(batchSavePerc.Length - 1);
                            NetworkCommunication.SendData(BATCH_SAVEPERC, batchSavePerc, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_hits.Count != 0) {
                            string batchHits = "";
                            foreach (string key in new List<string>(_hits.Keys))
                                batchHits += key + ';' + _hits[key].ToString() + ';';
                            batchHits = batchHits.Remove(batchHits.Length - 1);
                            NetworkCommunication.SendData(BATCH_HIT, batchHits, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_turnovers.Count != 0) {
                            string batchTurnovers = "";
                            foreach (string key in new List<string>(_turnovers.Keys))
                                batchTurnovers += key + ';' + _turnovers[key].ToString() + ';';
                            batchTurnovers = batchTurnovers.Remove(batchTurnovers.Length - 1);
                            NetworkCommunication.SendData(BATCH_TURNOVER, batchTurnovers, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_takeaways.Count != 0) {
                            string batchTakeaways = "";
                            foreach (string key in new List<string>(_takeaways.Keys))
                                batchTakeaways += key + ';' + _takeaways[key].ToString() + ';';
                            batchTakeaways = batchTakeaways.Remove(batchTakeaways.Length - 1);
                            NetworkCommunication.SendData(BATCH_TAKEAWAY, batchTakeaways, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_passes.Count != 0) {
                            string batchPasses = "";
                            foreach (string key in new List<string>(_passes.Keys))
                                batchPasses += key + ';' + _passes[key].ToString() + ';';
                            batchPasses = batchPasses.Remove(batchPasses.Length - 1);
                            NetworkCommunication.SendData(BATCH_PASS, batchPasses, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_puckTouches.Count != 0) {
                            string batchPuckTouches = "";
                            foreach (string key in new List<string>(_puckTouches.Keys))
                                batchPuckTouches += key + ';' + _puckTouches[key].ToString() + ';';
                            batchPuckTouches = batchPuckTouches.Remove(batchPuckTouches.Length - 1);
                            NetworkCommunication.SendData(BATCH_PUCK_TOUCH, batchPuckTouches, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_exits.Count != 0) {
                            string batchExits = "";
                            foreach (string key in new List<string>(_exits.Keys))
                                batchExits += key + ';' + _exits[key].ToString() + ';';
                            batchExits = batchExits.Remove(batchExits.Length - 1);
                            NetworkCommunication.SendData(BATCH_EXIT, batchExits, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_entries.Count != 0) {
                            string batchEntries = "";
                            foreach (string key in new List<string>(_entries.Keys))
                                batchEntries += key + ';' + _entries[key].ToString() + ';';
                            batchEntries = batchEntries.Remove(batchEntries.Length - 1);
                            NetworkCommunication.SendData(BATCH_ENTRY, batchEntries, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_possessionTimeSeconds.Count != 0) {
                            string batchPossessionTime = "";
                            foreach (string key in new List<string>(_possessionTimeSeconds.Keys))
                                batchPossessionTime += key + ';' + _possessionTimeSeconds[key].ToString("F2") + ';';
                            batchPossessionTime = batchPossessionTime.Remove(batchPossessionTime.Length - 1);
                            NetworkCommunication.SendData(BATCH_POSSESSION_TIME, batchPossessionTime, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_puckBattleWins.Count != 0) {
                            string batchPuckBattleWins = "";
                            foreach (string key in new List<string>(_puckBattleWins.Keys))
                                batchPuckBattleWins += key + ';' + _puckBattleWins[key].ToString() + ';';
                            batchPuckBattleWins = batchPuckBattleWins.Remove(batchPuckBattleWins.Length - 1);
                            NetworkCommunication.SendData(BATCH_PUCK_BATTLE_WINS, batchPuckBattleWins, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_puckBattleLosses.Count != 0) {
                            string batchPuckBattleLosses = "";
                            foreach (string key in new List<string>(_puckBattleLosses.Keys))
                                batchPuckBattleLosses += key + ';' + _puckBattleLosses[key].ToString() + ';';
                            batchPuckBattleLosses = batchPuckBattleLosses.Remove(batchPuckBattleLosses.Length - 1);
                            NetworkCommunication.SendData(BATCH_PUCK_BATTLE_LOSSES, batchPuckBattleLosses, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_shotAttempts.Count != 0) {
                            string batchShotAttempts = "";
                            foreach (string key in new List<string>(_shotAttempts.Keys))
                                batchShotAttempts += key + ';' + _shotAttempts[key].ToString() + ';';
                            batchShotAttempts = batchShotAttempts.Remove(batchShotAttempts.Length - 1);
                            NetworkCommunication.SendData(BATCH_SHOT_ATTEMPTS, batchShotAttempts, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_homePlateSogs.Count != 0) {
                            string batchHomePlateSogs = "";
                            foreach (string key in new List<string>(_homePlateSogs.Keys))
                                batchHomePlateSogs += key + ';' + _homePlateSogs[key].ToString() + ';';
                            batchHomePlateSogs = batchHomePlateSogs.Remove(batchHomePlateSogs.Length - 1);
                            NetworkCommunication.SendData(BATCH_HOME_PLATE_SOGS, batchHomePlateSogs, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        // Send team stats
                        if (_teamShots.Count != 0) {
                            string batchTeamShots = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamShots.Keys))
                                batchTeamShots += team.ToString() + ';' + _teamShots[team].ToString() + ';';
                            batchTeamShots = batchTeamShots.Remove(batchTeamShots.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_SHOTS, batchTeamShots, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamShotAttempts.Count != 0) {
                            string batchTeamShotAttempts = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamShotAttempts.Keys))
                                batchTeamShotAttempts += team.ToString() + ';' + _teamShotAttempts[team].ToString() + ';';
                            batchTeamShotAttempts = batchTeamShotAttempts.Remove(batchTeamShotAttempts.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_SHOT_ATTEMPTS, batchTeamShotAttempts, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamHomePlateSogs.Count != 0) {
                            string batchTeamHomePlateSogs = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamHomePlateSogs.Keys))
                                batchTeamHomePlateSogs += team.ToString() + ';' + _teamHomePlateSogs[team].ToString() + ';';
                            batchTeamHomePlateSogs = batchTeamHomePlateSogs.Remove(batchTeamHomePlateSogs.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_HOME_PLATE_SOGS, batchTeamHomePlateSogs, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamFaceoffWins.Count != 0) {
                            string batchTeamFaceoffWins = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamFaceoffWins.Keys))
                                batchTeamFaceoffWins += team.ToString() + ';' + _teamFaceoffWins[team].ToString() + ';';
                            batchTeamFaceoffWins = batchTeamFaceoffWins.Remove(batchTeamFaceoffWins.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_FACEOFF_WINS, batchTeamFaceoffWins, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamFaceoffTotal.Count != 0) {
                            string batchTeamFaceoffTotal = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamFaceoffTotal.Keys))
                                batchTeamFaceoffTotal += team.ToString() + ';' + _teamFaceoffTotal[team].ToString() + ';';
                            batchTeamFaceoffTotal = batchTeamFaceoffTotal.Remove(batchTeamFaceoffTotal.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_FACEOFF_TOTAL, batchTeamFaceoffTotal, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamTakeaways.Count != 0) {
                            string batchTeamTakeaways = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamTakeaways.Keys))
                                batchTeamTakeaways += team.ToString() + ';' + _teamTakeaways[team].ToString() + ';';
                            batchTeamTakeaways = batchTeamTakeaways.Remove(batchTeamTakeaways.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_TAKEAWAYS, batchTeamTakeaways, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamTurnovers.Count != 0) {
                            string batchTeamTurnovers = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamTurnovers.Keys))
                                batchTeamTurnovers += team.ToString() + ';' + _teamTurnovers[team].ToString() + ';';
                            batchTeamTurnovers = batchTeamTurnovers.Remove(batchTeamTurnovers.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_TURNOVERS, batchTeamTurnovers, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamExits.Count != 0) {
                            string batchTeamExits = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamExits.Keys))
                                batchTeamExits += team.ToString() + ';' + _teamExits[team].ToString() + ';';
                            batchTeamExits = batchTeamExits.Remove(batchTeamExits.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_EXITS, batchTeamExits, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamEntries.Count != 0) {
                            string batchTeamEntries = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamEntries.Keys))
                                batchTeamEntries += team.ToString() + ';' + _teamEntries[team].ToString() + ';';
                            batchTeamEntries = batchTeamEntries.Remove(batchTeamEntries.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_ENTRIES, batchTeamEntries, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamPasses.Count != 0) {
                            string batchTeamPasses = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPasses.Keys))
                                batchTeamPasses += team.ToString() + ';' + _teamPasses[team].ToString() + ';';
                            batchTeamPasses = batchTeamPasses.Remove(batchTeamPasses.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_PASSES, batchTeamPasses, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamPossessionTime.Count != 0) {
                            string batchTeamPossessionTime = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPossessionTime.Keys))
                                batchTeamPossessionTime += team.ToString() + ';' + _teamPossessionTime[team].ToString("F2") + ';';
                            batchTeamPossessionTime = batchTeamPossessionTime.Remove(batchTeamPossessionTime.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_POSSESSION_TIME, batchTeamPossessionTime, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamPuckBattleWins.Count != 0) {
                            string batchTeamPuckBattleWins = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPuckBattleWins.Keys))
                                batchTeamPuckBattleWins += team.ToString() + ';' + _teamPuckBattleWins[team].ToString() + ';';
                            batchTeamPuckBattleWins = batchTeamPuckBattleWins.Remove(batchTeamPuckBattleWins.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_PUCK_BATTLE_WINS, batchTeamPuckBattleWins, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        if (_teamPuckBattleLosses.Count != 0) {
                            string batchTeamPuckBattleLosses = "";
                            foreach (PlayerTeam team in new List<PlayerTeam>(_teamPuckBattleLosses.Keys))
                                batchTeamPuckBattleLosses += team.ToString() + ';' + _teamPuckBattleLosses[team].ToString() + ';';
                            batchTeamPuckBattleLosses = batchTeamPuckBattleLosses.Remove(batchTeamPuckBattleLosses.Length - 1);
                            NetworkCommunication.SendData(BATCH_TEAM_PUCK_BATTLE_LOSSES, batchTeamPuckBattleLosses, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        // Send stick saves
                        if (_stickSaves.Count != 0) {
                            string batchStickSaves = "";
                            foreach (string key in new List<string>(_stickSaves.Keys))
                                batchStickSaves += key + ';' + _stickSaves[key].ToString() + ';';
                            batchStickSaves = batchStickSaves.Remove(batchStickSaves.Length - 1);
                            NetworkCommunication.SendData(BATCH_STICK_SAVES, batchStickSaves, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        // Send body saves
                        if (_bodySaves.Count != 0) {
                            string batchBodySaves = "";
                            foreach (string key in new List<string>(_bodySaves.Keys))
                                batchBodySaves += key + ';' + _bodySaves[key].ToString() + ';';
                            batchBodySaves = batchBodySaves.Remove(batchBodySaves.Length - 1);
                            NetworkCommunication.SendData(BATCH_BODY_SAVES, batchBodySaves, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        // Send home plate saves
                        if (_homePlateSaves.Count != 0) {
                            string batchHomePlateSaves = "";
                            foreach (string key in new List<string>(_homePlateSaves.Keys))
                                batchHomePlateSaves += key + ';' + _homePlateSaves[key].ToString() + ';';
                            batchHomePlateSaves = batchHomePlateSaves.Remove(batchHomePlateSaves.Length - 1);
                            NetworkCommunication.SendData(BATCH_HOME_PLATE_SAVES, batchHomePlateSaves, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        // Send home plate shots faced
                        if (_homePlateShots.Count != 0) {
                            string batchHomePlateShots = "";
                            foreach (string key in new List<string>(_homePlateShots.Keys))
                                batchHomePlateShots += key + ';' + _homePlateShots[key].ToString() + ';';
                            batchHomePlateShots = batchHomePlateShots.Remove(batchHomePlateShots.Length - 1);
                            NetworkCommunication.SendData(BATCH_HOME_PLATE_SHOTS_FACED, batchHomePlateShots, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        }

                        foreach (int key in new List<int>(_stars.Keys))
                            NetworkCommunication.SendData(STAR, $"{_stars[key]};{key}", clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
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
                        if (dataName.StartsWith(Codebase.Constants.SOG)) {
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
                Logging.LogError($"Error in ReceiveData.\n{ex}", ServerConfig);
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

            // Write to client-side file.
            if (_clientConfig.LogClientSideStats) {
                StringBuilder csvContent = new StringBuilder();
                foreach (var kvp in _sog) {
                    Player player = PlayerManager.Instance.GetPlayerBySteamId(kvp.Key);
                    if (!player || PlayerFunc.IsGoalie(player))
                        continue;

                    csvContent.AppendLine($"{player.Username.Value};{player.Number.Value};{player.Team.Value};{kvp.Key};{kvp.Value}");
                }

                File.WriteAllText(Path.Combine(Path.GetFullPath("."), Constants.MOD_NAME + "_shots.csv"), csvContent.ToString());
            }
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

            // Write to client-side file.
            if (_clientConfig.LogClientSideStats) {
                StringBuilder csvContent = new StringBuilder();
                foreach (var kvp in _savePerc) {
                    Player player = PlayerManager.Instance.GetPlayerBySteamId(kvp.Key);
                    if (!player || !PlayerFunc.IsGoalie(player))
                        continue;
                    csvContent.AppendLine($"{player.Username.Value};{player.Number.Value};{player.Team.Value};{kvp.Key};{kvp.Value.Saves};{kvp.Value.Shots}");
                }

                File.WriteAllText(Path.Combine(Path.GetFullPath("."), Constants.MOD_NAME + "_saves.csv"), csvContent.ToString());
            }
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
        }

        private static void ReceiveData_TeamShotAttempts(PlayerTeam team, string dataStr) {
            int attempts = int.Parse(dataStr);
            if (_teamShotAttempts.TryGetValue(team, out int _))
                _teamShotAttempts[team] = attempts;
            else
                _teamShotAttempts.Add(team, attempts);
        }


        private static void ReceiveData_TeamHomePlateSogs(PlayerTeam team, string dataStr) {
            int homePlateSog = int.Parse(dataStr);
            if (_teamHomePlateSogs.TryGetValue(team, out int _))
                _teamHomePlateSogs[team] = homePlateSog;
            else
                _teamHomePlateSogs.Add(team, homePlateSog);
        }

        private static void ReceiveData_TeamPasses(PlayerTeam team, string dataStr) {
            int passes = int.Parse(dataStr);
            if (_teamPasses.TryGetValue(team, out int _)) {
                _teamPasses[team] = passes;
            } else {
                _teamPasses.Add(team, passes);
            }
        }

        private static void ReceiveData_TeamFaceoffWins(PlayerTeam team, string dataStr) {
            int wins = int.Parse(dataStr);
            if (_teamFaceoffWins.TryGetValue(team, out int _)) {
                _teamFaceoffWins[team] = wins;
            } else {
                _teamFaceoffWins.Add(team, wins);
            }
        }

        private static void ReceiveData_TeamFaceoffTotal(PlayerTeam team, string dataStr) {
            int total = int.Parse(dataStr);
            if (_teamFaceoffTotal.TryGetValue(team, out int _)) {
                _teamFaceoffTotal[team] = total;
            } else {
                _teamFaceoffTotal.Add(team, total);
            }
        }

        private static void ReceiveData_TeamTakeaways(PlayerTeam team, string dataStr) {
            int takeaways = int.Parse(dataStr);
            if (_teamTakeaways.TryGetValue(team, out int _)) {
                _teamTakeaways[team] = takeaways;
            } else {
                _teamTakeaways.Add(team, takeaways);
            }
        }

        private static void ReceiveData_TeamTurnovers(PlayerTeam team, string dataStr) {
            int turnovers = int.Parse(dataStr);
            if (_teamTurnovers.TryGetValue(team, out int _)) {
                _teamTurnovers[team] = turnovers;
            } else {
                _teamTurnovers.Add(team, turnovers);
            }
        }

        private static void ReceiveData_TeamExits(PlayerTeam team, string dataStr) {
            int exits = int.Parse(dataStr);
            if (_teamExits.TryGetValue(team, out int _)) {
                _teamExits[team] = exits;
            } else {
                _teamExits.Add(team, exits);
            }
        }

        private static void ReceiveData_TeamEntries(PlayerTeam team, string dataStr) {
            int entries = int.Parse(dataStr);
            if (_teamEntries.TryGetValue(team, out int _)) {
                _teamEntries[team] = entries;
            } else {
                _teamEntries.Add(team, entries);
            }
        }

        private static void ReceiveData_TeamPossessionTime(PlayerTeam team, string dataStr) {
            double possessionTime = double.Parse(dataStr);
            if (_teamPossessionTime.TryGetValue(team, out double _)) {
                _teamPossessionTime[team] = possessionTime;
            } else {
                _teamPossessionTime.Add(team, possessionTime);
            }
        }

        private static void ReceiveData_TeamPuckBattleWins(PlayerTeam team, string dataStr) {
            int wins = int.Parse(dataStr);
            if (_teamPuckBattleWins.TryGetValue(team, out int _)) {
                _teamPuckBattleWins[team] = wins;
            } else {
                _teamPuckBattleWins.Add(team, wins);
            }
        }

        private static void ReceiveData_TeamPuckBattleLosses(PlayerTeam team, string dataStr) {
            int losses = int.Parse(dataStr);
            if (_teamPuckBattleLosses.TryGetValue(team, out int _)) {
                _teamPuckBattleLosses[team] = losses;
            } else {
                _teamPuckBattleLosses.Add(team, losses);
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

        /// <summary>
        /// Method used to modify the scoreboard to add additional stats.
        /// </summary>
        /// <param name="enable">Bool, true if new stats scoreboard has to added to the scoreboard. False if they need to be removed.</param>
        private static void ScoreboardModifications(bool enable) {
            if (UIScoreboard.Instance == null)
                return;

            VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), UIScoreboard.Instance, "container");

            if (enable) {
                // Always check if header exists in UI first (regardless of our tracking)
                // This ensures we handle conflicts with other mods properly
                bool headerExistsInUI = false;
                foreach (VisualElement ve in scoreboardContainer.Children()) {
                    if (ve is TemplateContainer && ve.childCount == 1) {
                        VisualElement templateContainer = ve.Children().First();
                        VisualElement existingHeader = templateContainer.Children().FirstOrDefault(x => x.name == SOG_HEADER_LABEL_NAME);
                        if (existingHeader != null) {
                            headerExistsInUI = true;
                            break;
                        }
                    }
                }

                // Only add header if it doesn't exist in UI AND we haven't added it ourselves
                if (!headerExistsInUI && !_hasUpdatedUIScoreboard.Contains("header")) {
                    foreach (VisualElement ve in scoreboardContainer.Children()) {
                        if (ve is TemplateContainer && ve.childCount == 1) {
                            VisualElement templateContainer = ve.Children().First();

                            // Double-check and remove any existing header (safety check)
                            VisualElement existingHeader = templateContainer.Children().FirstOrDefault(x => x.name == SOG_HEADER_LABEL_NAME);
                            if (existingHeader != null) {
                                templateContainer.Remove(existingHeader);
                                // If another mod already added the header, labels may have been shifted
                                // Reset them to original position first, then we'll apply our shift
                                foreach (VisualElement child in templateContainer.Children()) {
                                    if (child.name == "GoalsLabel" || child.name == "AssistsLabel" || child.name == "PointsLabel")
                                        child.transform.position = new Vector3(child.transform.position.x + 100, child.transform.position.y, child.transform.position.z);
                                }
                            }

                            // Add S/Sv% header (matching other mod's format)
                            Label sogHeader = new Label("S/Sv%") {
                                name = SOG_HEADER_LABEL_NAME,
                            };
                            templateContainer.Add(sogHeader);
                            sogHeader.transform.position = new Vector3(sogHeader.transform.position.x - 260, sogHeader.transform.position.y + 15, sogHeader.transform.position.z);

                            // Shift Goals/Assists/Points labels to the left to make room (original behavior)
                            foreach (VisualElement child in templateContainer.Children()) {
                                if (child.name == "GoalsLabel" || child.name == "AssistsLabel" || child.name == "PointsLabel")
                                    child.transform.position = new Vector3(child.transform.position.x - 100, child.transform.position.y, child.transform.position.z);
                            }
                        }
                    }

                    _hasUpdatedUIScoreboard.Add("header");
                }
                
                // Setup team tooltips for team scores - only if server has the mod
                if (_serverHasResponded && enable) {
                    SetupTeamTooltips(scoreboardContainer);
                }
            }
            else if (_hasUpdatedUIScoreboard.Contains("header") && !enable) {
                foreach (VisualElement ve in scoreboardContainer.Children()) {
                    if (ve is TemplateContainer && ve.childCount == 1) {
                        VisualElement templateContainer = ve.Children().First();

                        templateContainer.Remove(templateContainer.Children().First(x => x.name == SOG_HEADER_LABEL_NAME));

                        // Restore Goals/Assists/Points labels to original position
                        foreach (VisualElement child in templateContainer.Children()) {
                            if (child.name == "GoalsLabel" || child.name == "AssistsLabel" || child.name == "PointsLabel")
                                child.transform.position = new Vector3(child.transform.position.x + 100, child.transform.position.y, child.transform.position.z);
                        }
                    }
                }
            }

            foreach (var kvp in SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), UIScoreboard.Instance, "playerVisualElementMap")) {
                string playerSteamId = kvp.Key.SteamId.Value.ToString();

                if (string.IsNullOrEmpty(playerSteamId))
                    continue;

                if (enable) {
                    // Check if SOG label exists in UI first (regardless of our tracking)
                    bool sogLabelExistsInUI = false;
                    if (kvp.Value.childCount > 0) {
                        VisualElement playerContainer = kvp.Value.Children().First();
                        VisualElement existingSogLabel = playerContainer.Children().FirstOrDefault(x => x.name == SOG_LABEL);
                        if (existingSogLabel != null) {
                            sogLabelExistsInUI = true;
                        }
                    }

                    // Only add label if it doesn't exist in UI AND we haven't added it ourselves
                    if (!sogLabelExistsInUI && !_hasUpdatedUIScoreboard.Contains(playerSteamId)) {
                        if (kvp.Value.childCount > 0) {
                            VisualElement playerContainer = kvp.Value.Children().First();

                            Player currentPlayer = kvp.Key;
                            // Use GetPlayerPosition to match play-by-play detection method
                            bool isGoalie = GetPlayerPosition(currentPlayer) == "G";

                            // Double-check and remove any existing SOG label (safety check)
                            VisualElement existingSogLabel = playerContainer.Children().FirstOrDefault(x => x.name == SOG_LABEL);
                            if (existingSogLabel != null) {
                                playerContainer.Remove(existingSogLabel);
                                // If another mod already added the label, Goals/Assists/Points labels may have been shifted
                                // Reset them to original position first, then we'll apply our shift
                                foreach (VisualElement child in playerContainer.Children()) {
                                    if (child.name == "GoalsLabel" || child.name == "AssistsLabel" || child.name == "PointsLabel")
                                        child.transform.position = new Vector3(child.transform.position.x + 100, child.transform.position.y, child.transform.position.z);
                                }
                            }

                            // Add SOG label (original position)
                            Label sogLabel = new Label("0") {
                                name = SOG_LABEL
                            };
                            sogLabel.style.flexGrow = 1;
                            sogLabel.style.unityTextAlign = TextAnchor.UpperRight;
                            playerContainer.Add(sogLabel);
                            sogLabel.transform.position = new Vector3(sogLabel.transform.position.x - 225, sogLabel.transform.position.y, sogLabel.transform.position.z);
                            _sogLabels.Add(playerSteamId, sogLabel);

                            // Shift Goals/Assists/Points labels to the left to make room (original behavior)
                            foreach (VisualElement child in playerContainer.Children()) {
                                if (child.name == "GoalsLabel" || child.name == "AssistsLabel" || child.name == "PointsLabel")
                                    child.transform.position = new Vector3(child.transform.position.x - 100, child.transform.position.y, child.transform.position.z);
                            }
                            
                            // Store the font size from the SOG label for tooltip labels
                            float defaultFontSize = sogLabel.resolvedStyle.fontSize > 0 ? sogLabel.resolvedStyle.fontSize : 13;

                        // Add tooltip to player name label - search more thoroughly
                        Label nameLabel = null;
                        
                        // First, try direct children
                        foreach (VisualElement child in playerContainer.Children()) {
                            if (child is Label label) {
                                string childName = label.name?.ToLower() ?? "";
                                if (childName.Contains("username") || childName.Contains("name") || childName == "usernamelabel" || childName == "namelabel") {
                                    nameLabel = label;
                                    break;
                                }
                            }
                        }
                        
                        // If not found, try query
                        if (nameLabel == null) {
                            try {
                                var queryResult = playerContainer.Query<Label>("UsernameLabel");
                                if (queryResult != null) {
                                    nameLabel = queryResult.First();
                                }
                            } catch { }
                        }
                        
                        // If still not found, try searching all labels
                        if (nameLabel == null) {
                            var allLabels = playerContainer.Query<Label>().ToList();
                            foreach (var label in allLabels) {
                                if (label.text != null && label.text.Contains(currentPlayer.Username.Value.ToString())) {
                                    nameLabel = label;
                                    break;
                                }
                            }
                        }
                        
                        if (nameLabel != null && _serverHasResponded) {
                            // Enable picking mode so the label can receive mouse events
                            nameLabel.pickingMode = PickingMode.Position;
                            // Also enable picking on the container
                            playerContainer.pickingMode = PickingMode.Position;
                            // Get font size from name label or SOG label
                            float tooltipFontSize = nameLabel.resolvedStyle.fontSize > 0 ? nameLabel.resolvedStyle.fontSize : (sogLabel.resolvedStyle.fontSize > 0 ? sogLabel.resolvedStyle.fontSize : 13f);
                            SetupPlayerTooltip(nameLabel, playerContainer, playerSteamId, currentPlayer, nameLabel, tooltipFontSize);
                        }

                        _hasUpdatedUIScoreboard.Add(playerSteamId);

                        if (!_sog.TryGetValue(playerSteamId, out int _))
                            _sog.Add(playerSteamId, 0);
                        }
                    }
                    // Check if player's position changed - recreate tooltip if needed
                    else if (_hasUpdatedUIScoreboard.Contains(playerSteamId) && enable && kvp.Value.childCount > 0) {
                        Player currentPlayer = kvp.Key;
                        // Use GetPlayerPosition to match play-by-play detection method
                        bool isGoalie = GetPlayerPosition(currentPlayer) == "G";
                        
                        // Check if position changed
                        if (_playerTooltipIsGoalie.TryGetValue(playerSteamId, out bool wasGoalie)) {
                            if (wasGoalie != isGoalie) {
                                // Position changed - recreate tooltip
                                VisualElement playerContainer = kvp.Value.Children().First();
                                Label nameLabel = null;
                                
                                // Find name label
                                foreach (VisualElement child in playerContainer.Children()) {
                                    if (child is Label label) {
                                        string childName = label.name?.ToLower() ?? "";
                                        if (childName.Contains("username") || childName.Contains("name") || childName == "usernamelabel" || childName == "namelabel") {
                                            nameLabel = label;
                                            break;
                                        }
                                    }
                                }
                                
                                if (nameLabel == null) {
                                    try {
                                        var queryResult = playerContainer.Query<Label>("UsernameLabel");
                                        if (queryResult != null) {
                                            nameLabel = queryResult.First();
                                        }
                                    } catch { }
                                }
                                
                                if (nameLabel == null) {
                                    var allLabels = playerContainer.Query<Label>().ToList();
                                    foreach (var label in allLabels) {
                                        if (label.text != null && label.text.Contains(currentPlayer.Username.Value.ToString())) {
                                            nameLabel = label;
                                            break;
                                        }
                                    }
                                }
                                
                                if (nameLabel != null && _serverHasResponded) {
                                    Label sogLabel = null;
                                    if (_sogLabels.TryGetValue(playerSteamId, out Label existingSogLabel)) {
                                        sogLabel = existingSogLabel;
                                    }
                                    float tooltipFontSize = nameLabel.resolvedStyle.fontSize > 0 ? nameLabel.resolvedStyle.fontSize : (sogLabel != null && sogLabel.resolvedStyle.fontSize > 0 ? sogLabel.resolvedStyle.fontSize : 13f);
                                    SetupPlayerTooltip(nameLabel, playerContainer, playerSteamId, currentPlayer, nameLabel, tooltipFontSize);
                                }
                            }
                        }

                        if (!_savePerc.TryGetValue(playerSteamId, out (int, int) _))
                            _savePerc.Add(playerSteamId, (0, 0));

                        // Initialize stats for all players (including goalies)
                        if (!_hits.TryGetValue(playerSteamId, out int _))
                            _hits.Add(playerSteamId, 0);
                        if (!_turnovers.TryGetValue(playerSteamId, out int _))
                            _turnovers.Add(playerSteamId, 0);
                        if (!_takeaways.TryGetValue(playerSteamId, out int _))
                            _takeaways.Add(playerSteamId, 0);
                        if (!_passes.TryGetValue(playerSteamId, out int _))
                            _passes.Add(playerSteamId, 0);
                    }
                    else if (_hasUpdatedUIScoreboard.Contains(playerSteamId) && !enable) {
                        VisualElement playerContainer = kvp.Value.Children().First();
                        playerContainer.Remove(playerContainer.Children().First(x => x.name == SOG_LABEL));

                        // Restore Goals/Assists/Points labels to original position
                        foreach (VisualElement child in playerContainer.Children()) {
                            if (child.name == "GoalsLabel" || child.name == "AssistsLabel" || child.name == "PointsLabel")
                                child.transform.position = new Vector3(child.transform.position.x + 100, child.transform.position.y, child.transform.position.z);
                        }
                        
                        // Remove tooltip
                        if (_playerTooltips.TryGetValue(playerSteamId, out VisualElement tooltip)) {
                            tooltip.parent?.Remove(tooltip);
                            _playerTooltips.Remove(playerSteamId);
                        }
                        _playerTooltipNameLabels.Remove(playerSteamId);
                        _playerTooltipContainers.Remove(playerSteamId);
                        _playerTooltipIsGoalie.Remove(playerSteamId);
                    }
                    else {
                        Logging.Log($"Not adding player {kvp.Key.Username.Value}, childCount {kvp.Value.childCount}.", _clientConfig, true);
                        foreach (var test in kvp.Value.Children())
                            Logging.Log($"{test.name}", _clientConfig, true);
                    }
                }
            }

            // Reorder players by position if enabled
            if (enable) {
                ReorderScoreboardPlayers(scoreboardContainer);
            }

            if (!enable) {
                _sog.Clear();
                _savePerc.Clear();
                _sogLabels.Clear();
                _playerTooltips.Clear();
                _playerTooltipNameLabels.Clear();
                _playerTooltipContainers.Clear();
                _playerTooltipIsGoalie.Clear();
                _hasUpdatedUIScoreboard.Clear();
                _teamTooltipsSetup = false;
            }
            else if (enable && _serverHasResponded) {
                // Retry tooltip setup for players that have UI elements but no tooltips
                foreach (var kvp in SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), UIScoreboard.Instance, "playerVisualElementMap")) {
                    string playerSteamId = kvp.Key.SteamId.Value.ToString();
                    
                    if (string.IsNullOrEmpty(playerSteamId))
                        continue;
                    
                    // If player has UI elements set up but no tooltip, try to set it up
                    if (_hasUpdatedUIScoreboard.Contains(playerSteamId) && !_playerTooltips.ContainsKey(playerSteamId) && kvp.Value.childCount > 0) {
                        VisualElement playerContainer = kvp.Value.Children().First();
                        Player currentPlayer = kvp.Key;
                        
                        // Try to find the name label again
                        Label nameLabel = null;
                        
                        // First, try direct children
                        foreach (VisualElement child in playerContainer.Children()) {
                            if (child is Label label) {
                                string childName = label.name?.ToLower() ?? "";
                                if (childName.Contains("username") || childName.Contains("name") || childName == "usernamelabel" || childName == "namelabel") {
                                    nameLabel = label;
                                    break;
                                }
                            }
                        }
                        
                        // If not found, try query
                        if (nameLabel == null) {
                            try {
                                var queryResult = playerContainer.Query<Label>("UsernameLabel");
                                if (queryResult != null) {
                                    nameLabel = queryResult.First();
                                }
                            } catch { }
                        }
                        
                        // If still not found, try searching all labels
                        if (nameLabel == null) {
                            var allLabels = playerContainer.Query<Label>().ToList();
                            foreach (var label in allLabels) {
                                if (label.text != null && label.text.Contains(currentPlayer.Username.Value.ToString())) {
                                    nameLabel = label;
                                    break;
                                }
                            }
                        }
                        
                        // If we found the name label, set up the tooltip
                        if (nameLabel != null && _sogLabels.TryGetValue(playerSteamId, out Label sogLabel)) {
                            nameLabel.pickingMode = PickingMode.Position;
                            playerContainer.pickingMode = PickingMode.Position;
                            float tooltipFontSize = nameLabel.resolvedStyle.fontSize > 0 ? nameLabel.resolvedStyle.fontSize : (sogLabel.resolvedStyle.fontSize > 0 ? sogLabel.resolvedStyle.fontSize : 13f);
                            SetupPlayerTooltip(nameLabel, playerContainer, playerSteamId, currentPlayer, nameLabel, tooltipFontSize);
                        }
                    }
                }
            }
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
        private static Label CreateTooltipLabel(string text, string name, string playerSteamId, Label referenceLabel, float defaultFontSize = 13f) {
            Label label = new Label(text) { name = name + "_" + playerSteamId };
            label.text = text;
            if (referenceLabel != null) {
                label.style.fontSize = referenceLabel.resolvedStyle.fontSize;
                label.style.color = referenceLabel.resolvedStyle.color;
                label.style.unityTextAlign = referenceLabel.resolvedStyle.unityTextAlign;
                label.style.whiteSpace = referenceLabel.resolvedStyle.whiteSpace;
                try {
                    var fontAssetField = typeof(Label).GetField("fontAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fontAssetField != null) {
                        var fontAsset = fontAssetField.GetValue(referenceLabel) as UnityEngine.Font;
                        if (fontAsset != null) {
                            fontAssetField.SetValue(label, fontAsset);
                        }
                    }
                } catch { }
            } else {
                label.style.fontSize = defaultFontSize;
                label.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
                label.style.unityTextAlign = TextAnchor.UpperLeft;
            }
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
                trimmedSteamId == "76561198155889632" ||
                trimmedSteamId == "76561198980346669") {
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
            // Remove existing tooltip if it exists
            if (_playerTooltips.TryGetValue(playerSteamId, out VisualElement existingTooltip)) {
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
            

            // Create stat labels with proper styling - copy ALL styles from reference label
            Label titleLabel = new Label(player.Username.Value.ToString());
            titleLabel.text = player.Username.Value.ToString();
            
            // Copy ALL style properties from the reference label to ensure identical rendering
            if (referenceLabel != null) {
                // Copy all text-related styles
                titleLabel.style.fontSize = referenceLabel.resolvedStyle.fontSize + 3;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.color = referenceLabel.resolvedStyle.color;
                titleLabel.style.unityTextAlign = referenceLabel.resolvedStyle.unityTextAlign;
                titleLabel.style.whiteSpace = referenceLabel.resolvedStyle.whiteSpace;
                // Try to copy font asset if accessible using reflection
                try {
                    var fontAssetField = typeof(Label).GetField("fontAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fontAssetField != null) {
                        var fontAsset = fontAssetField.GetValue(referenceLabel) as UnityEngine.Font;
                        if (fontAsset != null) {
                            fontAssetField.SetValue(titleLabel, fontAsset);
                        }
                    }
                } catch {
                }
            } else {
                titleLabel.style.fontSize = defaultFontSize + 3;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
                titleLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            }
            
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
                titleLabel.text = player.Username.Value.ToString();
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
                VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), UIScoreboard.Instance, "container");
                if (scoreboardContainer != null) {
                    scoreboardContainer.Add(tooltip);
                    scoreboardContainer.style.overflow = Overflow.Visible;
                } else {
                    // Fallback to root
                    var root = UIScoreboard.Instance?.GetComponent<UnityEngine.UIElements.UIDocument>()?.rootVisualElement;
                    if (root != null) {
                        root.Add(tooltip);
                        root.style.overflow = Overflow.Visible;
                    } else {
                        Logging.LogError("Could not find container or root for tooltip", _clientConfig);
                    }
                }
            } catch (Exception ex) {
                Logging.LogError($"Error adding tooltip: {ex}", _clientConfig);
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
            Action showTooltip = () => {
                try {
                    // Only show tooltip if server has the stats mod
                    // If tooltip exists, show it (tooltip creation already requires _serverHasResponded to be true)
                    // This handles cases where tooltip was created but _serverHasResponded got reset
                    if (!_serverHasResponded && !_playerTooltips.ContainsKey(playerSteamId)) {
                        return; // Don't show if server hasn't responded AND tooltip doesn't exist
                    }
                    
                    // Check if player's position has changed - recreate tooltip if needed
                    if (player != null && player) {
                        bool currentIsGoalie = GetPlayerPosition(player) == "G";
                        if (_playerTooltipIsGoalie.TryGetValue(playerSteamId, out bool wasGoalie) && wasGoalie != currentIsGoalie) {
                            // Position changed - recreate tooltip with correct structure
                            if (_playerTooltipNameLabels.TryGetValue(playerSteamId, out Label storedNameLabel) && 
                                _playerTooltipContainers.TryGetValue(playerSteamId, out VisualElement storedContainer) &&
                                storedNameLabel != null && storedContainer != null) {
                                float tooltipFontSize = storedNameLabel.resolvedStyle.fontSize > 0 ? storedNameLabel.resolvedStyle.fontSize : 13f;
                                SetupPlayerTooltip(storedNameLabel, storedContainer, playerSteamId, player, storedNameLabel, tooltipFontSize);
                                // Get the newly created tooltip
                                if (_playerTooltips.TryGetValue(playerSteamId, out VisualElement newTooltip)) {
                                    tooltip = newTooltip;
                                }
                            }
                        }
                    }
                    
                    UpdateTooltipStats(tooltip, playerSteamId, player);
                    // Reapply background color when showing tooltip
                    tooltip.style.backgroundColor = GetColorFromSteamId(playerSteamId);
                    tooltip.style.display = DisplayStyle.Flex;
                    tooltip.BringToFront();
                    
                    // Update tooltip stats periodically while visible (every 0.5 seconds)
                    void UpdatePeriodically() {
                        if (tooltip.style.display == DisplayStyle.Flex) {
                            UpdateTooltipStats(tooltip, playerSteamId, player);
                            tooltip.schedule.Execute(UpdatePeriodically).ExecuteLater(500);
                        }
                    }
                    tooltip.schedule.Execute(UpdatePeriodically).ExecuteLater(500);
                    
                    // Use a scheduled callback to update position after layout is calculated
                    tooltip.schedule.Execute(() => {
                        // Only update and log if tooltip is still visible
                        if (tooltip.style.display == DisplayStyle.Flex) {
                            UpdateTooltipPosition(tooltip, nameLabel);
                            // Force layout recalculation
                            tooltip.MarkDirtyRepaint();
                            // Ensure all labels are visible and have proper text rendering
                            foreach (Label label in tooltip.Children().OfType<Label>()) {
                                // Force re-set text to ensure it's actually set
                                string labelText = label.text;
                                label.text = ""; // Clear first
                                label.text = labelText; // Re-set
                                
                                label.style.color = new StyleColor(new Color(1f, 1f, 1f, 1f));
                                label.style.opacity = 1f;
                                label.style.visibility = Visibility.Visible;
                                label.style.display = DisplayStyle.Flex;
                                
                                // Check if font size is valid
                                float fontSize = label.resolvedStyle.fontSize;
                                if (fontSize <= 0) {
                                    label.style.fontSize = 13; // Set default if invalid
                                }
                                
                                // Force text to render
                                label.MarkDirtyRepaint();
                                
                                // Log the actual text content to verify it's set
                            }
                            // Log tooltip dimensions after layout (only if visible)
                            float resolvedWidth = tooltip.resolvedStyle.width;
                            float resolvedHeight = tooltip.resolvedStyle.height;
                            if (resolvedWidth > 0 && resolvedHeight > 0) {
                                float layoutHeight = tooltip.layout.height;
                                // Log each label's height, text content, and font size
                                var labelInfo = tooltip.Children().OfType<Label>().Select(l => {
                                    float fontSize = l.resolvedStyle.fontSize;
                                    float labelHeight = l.layout.height;
                                    return $"'{l.text}' (fontSize: {fontSize}, height: {labelHeight}, color: {l.resolvedStyle.color}, opacity: {l.resolvedStyle.opacity})";
                                }).ToList();
                            }
                        }
                    });
                } catch (Exception ex) {
                    Logging.LogError($"Error showing tooltip: {ex}", _clientConfig);
                }
            };
            
            Action hideTooltip = () => {
                tooltip.style.display = DisplayStyle.None;
            };
            
            // Register on label
            nameLabel.RegisterCallback<MouseEnterEvent>(evt => showTooltip());
            nameLabel.RegisterCallback<PointerEnterEvent>(evt => showTooltip());
            nameLabel.RegisterCallback<MouseLeaveEvent>(evt => hideTooltip());
            nameLabel.RegisterCallback<PointerLeaveEvent>(evt => hideTooltip());
            
            // Also register on the entire player container row
            playerContainer.RegisterCallback<MouseEnterEvent>(evt => {
                if (evt.target == nameLabel || evt.target == playerContainer) {
                    showTooltip();
                }
            });
            playerContainer.RegisterCallback<PointerEnterEvent>(evt => {
                if (evt.target == nameLabel || evt.target == playerContainer) {
                    showTooltip();
                }
            });
            playerContainer.RegisterCallback<MouseLeaveEvent>(evt => hideTooltip());
            playerContainer.RegisterCallback<PointerLeaveEvent>(evt => hideTooltip());

            // Update position on mouse move (throttled to reduce excessive logging)
            nameLabel.RegisterCallback<MouseMoveEvent>(evt => {
                if (tooltip.style.display == DisplayStyle.Flex) {
                    float currentTime = UnityEngine.Time.time;
                    if (!_tooltipMoveTimes.ContainsKey(tooltip) || currentTime - _tooltipMoveTimes[tooltip] > 0.1f) {
                        _tooltipMoveTimes[tooltip] = currentTime;
                        UpdateTooltipPosition(tooltip, nameLabel);
                    }
                }
            });
            
            playerContainer.RegisterCallback<MouseMoveEvent>(evt => {
                if (tooltip.style.display == DisplayStyle.Flex) {
                    float currentTime = UnityEngine.Time.time;
                    if (!_tooltipMoveTimes.ContainsKey(tooltip) || currentTime - _tooltipMoveTimes[tooltip] > 0.1f) {
                        _tooltipMoveTimes[tooltip] = currentTime;
                        UpdateTooltipPosition(tooltip, nameLabel);
                    }
                }
            });
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
                Logging.LogError($"Error updating faceoff stats from pbp: {ex}", ServerConfig);
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
                Logging.LogError($"Error updating body/stick saves from pbp: {ex}", ServerConfig);
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
                if (GameManager.Instance != null && GameManager.Instance.GameState != null) {
                    if (GameManager.Instance.GameState.Value.BlueScore > GameManager.Instance.GameState.Value.RedScore) {
                        winningTeam = PlayerTeam.Blue;
                        if (_blueGoals.Count > GameManager.Instance.GameState.Value.RedScore) {
                            gwgSteamId = _blueGoals[GameManager.Instance.GameState.Value.RedScore];
                        }
                    }
                    else if (GameManager.Instance.GameState.Value.RedScore > GameManager.Instance.GameState.Value.BlueScore) {
                        winningTeam = PlayerTeam.Red;
                        if (_redGoals.Count > GameManager.Instance.GameState.Value.BlueScore) {
                            gwgSteamId = _redGoals[GameManager.Instance.GameState.Value.BlueScore];
                        }
                    }
                }
            } catch { }
            
            double gwgModifier = gwgSteamId == playerSteamId ? 0.5d : 0;
            double teamModifier = winningTeam == player.Team.Value ? 1.1d : 1d;
            
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
        private static void SetupTeamTooltips(VisualElement scoreboardContainer) {
            try {
                // Use a scheduled callback to ensure UI is fully loaded
                scoreboardContainer.schedule.Execute(() => {
                    try {
                        // Guard: Only setup once, similar to how player tooltips work
                        if (_teamTooltipsSetup) {
                            return;
                        }
                        
                        // Find team score labels - search for labels that might be team scores
                        var allLabels = scoreboardContainer.Query<Label>().ToList();
                        
                        Label blueScoreLabel = null;
                        Label redScoreLabel = null;
                
                // Try to find team score labels by name - check for exact matches first
                foreach (var label in allLabels) {
                    string labelName = label.name?.ToLower() ?? "";
                    string labelText = label.text ?? "";
                    
                    // Exact name match (most reliable)
                    if (labelName == "bluescore" && blueScoreLabel == null) {
                        blueScoreLabel = label;
                    } else if (labelName == "redscore" && redScoreLabel == null) {
                        redScoreLabel = label;
                    }
                    // Fallback: contains "blue" or "red" and "score"
                    else if (labelName.Contains("blue") && labelName.Contains("score") && blueScoreLabel == null) {
                        blueScoreLabel = label;
                    } else if (labelName.Contains("red") && labelName.Contains("score") && redScoreLabel == null) {
                        redScoreLabel = label;
                    }
                }
                
                // If not found by name, try to find by position (team scores are usually at the top, numeric)
                if (blueScoreLabel == null || redScoreLabel == null) {
                    var topLabels = allLabels.Where(l => {
                        if (l.text == null || l.text.Length == 0) return false;
                        
                        // Exclude labels we know are NOT team scores
                        string labelName = l.name?.ToLower() ?? "";
                        if (labelName.Contains("ping") || labelName.Contains("sog") || labelName.Contains("tooltip") || 
                            labelName.Contains("players") || labelName.Contains("position") || labelName.Contains("username") ||
                            labelName.Contains("goals") || labelName.Contains("assists") || labelName.Contains("points")) {
                            return false;
                        }
                        
                        var layout = l.layout;
                        bool isNumeric = l.text.All(c => char.IsDigit(c) || c == '-');
                        // Look for numeric labels at the very top (y < 50) - team scores are usually there
                        return isNumeric && layout.y >= 0 && layout.y < 50;
                    }).OrderBy(l => l.layout.x).ToList(); // Order by X position (left to right)
                    
                    if (topLabels.Count >= 2) {
                        // Leftmost is blue, rightmost is red
                        if (blueScoreLabel == null) {
                            blueScoreLabel = topLabels[0]; // Leftmost
                        }
                        if (redScoreLabel == null) {
                            redScoreLabel = topLabels[topLabels.Count - 1]; // Rightmost
                        }
                    } else if (topLabels.Count == 1) {
                        // Only one found - use X position relative to parent center to determine team
                        var layout = topLabels[0].layout;
                        var parentLayout = topLabels[0].parent?.layout ?? new Rect();
                        float centerX = parentLayout.width > 0 ? parentLayout.width / 2 : 0;
                        
                        if (blueScoreLabel == null && layout.x < centerX) {
                            blueScoreLabel = topLabels[0];
                        } else if (redScoreLabel == null && layout.x >= centerX) {
                            redScoreLabel = topLabels[0];
                        }
                    }
                }
                
                // If still not found, try searching all UI documents (team scores might be in main game UI, not scoreboard)
                if (blueScoreLabel == null || redScoreLabel == null) {
                    try {
                        // Search all UIDocument components in the scene
                        var allUIDocuments = UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsSortMode.None);
                        
                        foreach (var uiDoc in allUIDocuments) {
                            if (uiDoc?.rootVisualElement == null) continue;
                            
                            var rootLabels = uiDoc.rootVisualElement.Query<Label>().ToList();
                            
                            // First, try to find by exact name match (case-insensitive)
                            foreach (var label in rootLabels) {
                                string labelName = label.name?.ToLower() ?? "";
                                if (labelName == "bluescore" && blueScoreLabel == null) {
                                    blueScoreLabel = label;
                                } else if (labelName == "redscore" && redScoreLabel == null) {
                                    redScoreLabel = label;
                                }
                            }
                            
                            // If still not found, look for team score labels by position
                            if (blueScoreLabel == null || redScoreLabel == null) {
                                var potentialScores = rootLabels.Where(l => {
                                    if (l.text == null || l.text.Length == 0) return false;
                                    string labelName = l.name?.ToLower() ?? "";
                                    // Exclude known non-score labels
                                    if (labelName.Contains("ping") || labelName.Contains("sog") || labelName.Contains("tooltip") || 
                                        labelName.Contains("players") || labelName.Contains("position") || labelName.Contains("username") ||
                                        labelName.Contains("goals") || labelName.Contains("assists") || labelName.Contains("points") ||
                                        labelName.Contains("title") || labelName.Contains("name")) {
                                        return false;
                                    }
                                    
                                    bool isNumeric = l.text.All(c => char.IsDigit(c) || c == '-');
                                    var layout = l.layout;
                                    // Team scores are usually at the top and reasonably sized
                                    return isNumeric && layout.y >= 0 && layout.y < 200 && layout.height > 10;
                                }).OrderBy(l => l.layout.x).ToList(); // Order by X position (left to right)
                                
                                if (potentialScores.Count >= 2) {
                                    // Leftmost label is blue, rightmost is red
                                    if (blueScoreLabel == null) {
                                        blueScoreLabel = potentialScores[0]; // Leftmost
                                    }
                                    if (redScoreLabel == null) {
                                        redScoreLabel = potentialScores[potentialScores.Count - 1]; // Rightmost
                                    }
                                } else if (potentialScores.Count == 1) {
                                    // Only one found - use X position to determine team
                                    var layout = potentialScores[0].layout;
                                    var parentLayout = potentialScores[0].parent?.layout ?? new Rect();
                                    float centerX = parentLayout.width > 0 ? parentLayout.width / 2 : 0;
                                    
                                    if (blueScoreLabel == null && layout.x < centerX) {
                                        blueScoreLabel = potentialScores[0];
                                    } else if (redScoreLabel == null && layout.x >= centerX) {
                                        redScoreLabel = potentialScores[0];
                                    }
                                }
                            }
                            
                            // If we found both, stop searching
                            if (blueScoreLabel != null && redScoreLabel != null) break;
                        }
                    } catch (Exception ex) {
                        Logging.LogError($"Error searching all UI documents: {ex}", _clientConfig);
                    }
                }
                
                        if (blueScoreLabel != null) {
                            blueScoreLabel.pickingMode = PickingMode.Position;
                            SetupTeamTooltip(blueScoreLabel, PlayerTeam.Blue, blueScoreLabel);
                        }
                        
                        if (redScoreLabel != null) {
                            redScoreLabel.pickingMode = PickingMode.Position;
                            SetupTeamTooltip(redScoreLabel, PlayerTeam.Red, redScoreLabel);
                        }
                        
                        // Mark tooltips as set up to prevent repeated calls
                        _teamTooltipsSetup = true;
                    } catch (Exception ex) {
                        Logging.LogError($"Error in scheduled SetupTeamTooltips: {ex}", _clientConfig);
                    }
                }).ExecuteLater(500); // Wait 500ms for UI to fully load
            } catch (Exception ex) {
                Logging.LogError($"Error setting up team tooltips: {ex}", _clientConfig);
            }
        }

        /// <summary>
        /// Sets up a tooltip that appears when hovering over a team score.
        /// </summary>
        private static void SetupTeamTooltip(Label scoreLabel, PlayerTeam team, Label referenceLabel) {
            string teamName = team == PlayerTeam.Blue ? "Blue Team" : "Red Team";
            string teamKey = team.ToString();
            
            // Remove existing tooltip if it exists (same pattern as player tooltips)
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
            
            // Add tooltip to the scoreLabel's parent container (where the team scores are displayed)
            // Using BringToFront() to ensure it appears above scoreboard elements
            VisualElement parentContainer = scoreLabel.parent;
            if (parentContainer != null) {
                parentContainer.Add(tooltip);
                parentContainer.style.overflow = Overflow.Visible;
            } else {
                // Fallback to scoreboard container if parent is not available
                VisualElement scoreboardContainer = SystemFunc.GetPrivateField<VisualElement>(typeof(UIScoreboard), UIScoreboard.Instance, "container");
                if (scoreboardContainer != null) {
                    scoreboardContainer.Add(tooltip);
                    scoreboardContainer.style.overflow = Overflow.Visible;
                }
            }
            
            _teamTooltips.Add(team, tooltip);
            
            // Mouse event handlers
            Action showTooltip = () => {
                // Only show tooltip if server has the stats mod
                if (!_serverHasResponded) {
                    return;
                }
                UpdateTeamTooltipStats(tooltip, team);
                tooltip.style.display = DisplayStyle.Flex;
                // Ensure tooltip is brought to front to appear above scoreboard elements
                tooltip.BringToFront();
                UpdateTeamTooltipPosition(tooltip, scoreLabel, team);
                
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
            
            scoreLabel.RegisterCallback<MouseEnterEvent>(evt => showTooltip());
            scoreLabel.RegisterCallback<PointerEnterEvent>(evt => showTooltip());
            scoreLabel.RegisterCallback<MouseLeaveEvent>(evt => hideTooltip());
            scoreLabel.RegisterCallback<PointerLeaveEvent>(evt => hideTooltip());
            
            scoreLabel.RegisterCallback<MouseMoveEvent>(evt => {
                if (tooltip.style.display == DisplayStyle.Flex) {
                    float currentTime = UnityEngine.Time.time;
                    if (!_tooltipMoveTimes.ContainsKey(tooltip) || currentTime - _tooltipMoveTimes[tooltip] > 0.1f) {
                        _tooltipMoveTimes[tooltip] = currentTime;
                        UpdateTeamTooltipPosition(tooltip, scoreLabel, team);
                    }
                }
            });
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
        /// Updates the team tooltip position relative to the score label.
        /// </summary>
        private static void UpdateTeamTooltipPosition(VisualElement tooltip, Label scoreLabel, PlayerTeam team) {
            try {
                VisualElement parent = tooltip.parent;
                if (parent == null) {
                    return;
                }
                
                // Get the label's position in world space, then convert to parent's local space
                Rect labelLayout = scoreLabel.layout;
                Rect parentLayout = parent.layout;
                
                // Get tooltip dimensions
                float tooltipWidth = tooltip.resolvedStyle.width > 0 ? tooltip.resolvedStyle.width : 280;
                float tooltipHeight = tooltip.resolvedStyle.height > 0 ? tooltip.resolvedStyle.height : 200;
                
                float xPos;
                float yPos = labelLayout.y;
                
                // Blue team: position to the left of the label by default
                // Red team: position to the right of the label by default
                if (team == PlayerTeam.Blue) {
                    // Position to the left of the label
                    xPos = labelLayout.x - tooltipWidth - 10;
                    
                    // Check if tooltip would go off the left edge
                    if (xPos < 0) {
                        xPos = labelLayout.x + labelLayout.width + 10; // Fallback to right side
                    }
                } else {
                    // Position to the right of the label (Red team)
                    xPos = labelLayout.x + labelLayout.width + 10;
                    
                    // Check if tooltip would go off the right edge
                    if (xPos + tooltipWidth > parentLayout.width) {
                        xPos = labelLayout.x - tooltipWidth - 10; // Fallback to left side
                    }
                }
                
                // Check if tooltip would go off the left edge (for both teams after fallback)
                if (xPos < 0) {
                    xPos = labelLayout.x + (labelLayout.width / 2) - (tooltipWidth / 2); // Center as last resort
                }
                
                // Check if tooltip would go off the bottom edge
                if (yPos + tooltipHeight > parentLayout.height) {
                    yPos = parentLayout.height - tooltipHeight - 10;
                }
                
                // Ensure minimum margins
                if (xPos < 10) xPos = 10;
                if (yPos < 10) yPos = 10;
                
                tooltip.style.left = xPos;
                tooltip.style.top = yPos;
                
                // Ensure tooltip maintains highest priority when repositioned
                tooltip.BringToFront();
            } catch (Exception ex) {
                Logging.LogError($"Error updating team tooltip position: {ex}", _clientConfig);
            }
        }

        /// <summary>
        /// Updates the tooltip position relative to the name label.
        /// </summary>
        private static void UpdateTooltipPosition(VisualElement tooltip, Label nameLabel) {
            try {
                // Get the parent container (scoreboard container)
                VisualElement parent = tooltip.parent;
                if (parent == null) {
                    Logging.LogError("Tooltip has no parent container", _clientConfig);
                    return;
                }

                // Get label position relative to parent
                Rect labelLayout = nameLabel.layout;
                Rect parentLayout = parent.layout;
                Rect tooltipLayout = tooltip.layout;
                
                if (labelLayout.width > 0 && labelLayout.height > 0) {
                    // Position tooltip to the right of the label
                    float xPos = labelLayout.xMax + 10;
                    float yPos = labelLayout.yMin;
                    
                    // Check if tooltip would go off the right edge of parent
                    float tooltipWidth = tooltipLayout.width > 0 ? tooltipLayout.width : 280;
                    if (xPos + tooltipWidth > parentLayout.width) {
                        // Position to the left of the label instead
                        xPos = labelLayout.xMin - tooltipWidth - 10;
                    }
                    
                    // Check if tooltip would go off the bottom edge
                    float tooltipHeight = tooltipLayout.height > 0 ? tooltipLayout.height : 150;
                    if (yPos + tooltipHeight > parentLayout.height) {
                        // Adjust upward
                        yPos = parentLayout.height - tooltipHeight - 5;
                    }
                    
                    // Ensure tooltip doesn't go off the top edge
                    if (yPos < 0) {
                        yPos = 5;
                    }
                    
                    // Ensure tooltip doesn't go off the left edge
                    if (xPos < 0) {
                        xPos = 5;
                    }
                    
                    tooltip.style.left = xPos;
                    tooltip.style.top = yPos;
                    
                    Logging.Log($"Tooltip positioned at ({xPos}, {yPos}) relative to parent. Label layout: ({labelLayout.x}, {labelLayout.y}) size {labelLayout.width}x{labelLayout.height}, Parent size: {parentLayout.width}x{parentLayout.height}", _clientConfig, true);
                } else {
                    Logging.LogError($"Label layout is invalid: {labelLayout}", _clientConfig);
                }
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

            if (!_lastShotWasCounted[player.Team.Value]) {
                string playerSteamId = player.SteamId.Value.ToString();

                if (string.IsNullOrEmpty(playerSteamId))
                    return true;

                if (!_sog.TryGetValue(playerSteamId, out int _))
                    _sog.Add(playerSteamId, 0);

                _sog[playerSteamId] += 1;
                int sog = _sog[playerSteamId];
                QueueStatUpdate(Codebase.Constants.SOG + playerSteamId, sog.ToString());
                LogSOG(playerSteamId, sog);
                
                                // Track team stat
                                if (!_teamShots.TryGetValue(player.Team.Value, out int _))
                                    _teamShots.Add(player.Team.Value, 0);
                _teamShots[player.Team.Value] += 1;
                QueueStatUpdate(Codebase.Constants.TEAM_SHOTS + player.Team.Value.ToString(), _teamShots[player.Team.Value].ToString());

                _lastShotWasCounted[player.Team.Value] = true;

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
                    PlayerTeam attackingTeam = player.Team.Value;
                    
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
                    string shotFlag = (shotPosition != Vector3.zero) ? DetermineShotFlag(shotPosition, player.Team.Value) : "";
                    
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
                        if (!_teamShotAttempts.TryGetValue(player.Team.Value, out int _))
                            _teamShotAttempts.Add(player.Team.Value, 0);
                        _teamShotAttempts[player.Team.Value] += 1;
                        QueueStatUpdate(Codebase.Constants.TEAM_SHOT_ATTEMPTS + player.Team.Value.ToString(), _teamShotAttempts[player.Team.Value].ToString());
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
                        if (!_teamHomePlateSogs.TryGetValue(player.Team.Value, out int _))
                            _teamHomePlateSogs.Add(player.Team.Value, 0);
                        _teamHomePlateSogs[player.Team.Value] += 1;
                        QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + player.Team.Value.ToString(), _teamHomePlateSogs[player.Team.Value].ToString());
                        homePlateTracked = true;
                    }
                    
                    // Also check pending release info if touch event didn't have home plate flag
                    if (!homePlateTracked) {
                        if (_pendingShotReleases.TryGetValue(attackingTeam, out var releaseInfo) && !string.IsNullOrEmpty(releaseInfo.ShooterSteamId) && releaseInfo.ShooterSteamId == playerSteamId) {
                            string releaseShotFlag = DetermineShotFlag(releaseInfo.PuckPosition, player.Team.Value);
                            if (releaseShotFlag == "HomePlate") {
                                if (!_homePlateSogs.TryGetValue(playerSteamId, out int _))
                                    _homePlateSogs.Add(playerSteamId, 0);
                                _homePlateSogs[playerSteamId] += 1;
                                QueueStatUpdate(Codebase.Constants.HOME_PLATE_SOGS + playerSteamId, _homePlateSogs[playerSteamId].ToString());
                                
                                // Track team home plate SOGs
                                if (!_teamHomePlateSogs.TryGetValue(player.Team.Value, out int _))
                                    _teamHomePlateSogs.Add(player.Team.Value, 0);
                                _teamHomePlateSogs[player.Team.Value] += 1;
                                QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + player.Team.Value.ToString(), _teamHomePlateSogs[player.Team.Value].ToString());
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
                            if (!_teamHomePlateSogs.TryGetValue(player.Team.Value, out int _))
                                _teamHomePlateSogs.Add(player.Team.Value, 0);
                            _teamHomePlateSogs[player.Team.Value] += 1;
                            QueueStatUpdate(Codebase.Constants.TEAM_HOME_PLATE_SOGS + player.Team.Value.ToString(), _teamHomePlateSogs[player.Team.Value].ToString());
                        }
                    }
                }

                // Track home plate shots for goalie when goal is scored on home plate shot
                Player goalie = PlayerFunc.GetOtherTeamGoalie(player.Team.Value);
                if (goalie != null) {
                    string goalieSteamId = goalie.SteamId.Value.ToString();
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
                        PlayerTeam attackingTeam = player.Team.Value;
                        if (_pendingShotReleases.TryGetValue(attackingTeam, out var releaseInfo) && 
                            !string.IsNullOrEmpty(releaseInfo.ShooterSteamId) && 
                            releaseInfo.ShooterSteamId == playerSteamId) {
                            string shotFlag = DetermineShotFlag(releaseInfo.PuckPosition, player.Team.Value);
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

                // Record Goal event (separate from shot)
                Puck goalPuck2 = PuckManager.Instance?.GetPuck();
                if (player != null && player && goalPuck2 != null) {
                    Vector3 puckPos = goalPuck2.transform.position;
                    Vector3 puckVel = goalPuck2.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                    RecordPlayByPlayEventInternal(PlayByPlayEventType.Goal, player, puckPos, puckVel, "successful");
                }

                return false;
            }
            else {
                // Shot was already counted, but still record Goal event
                Puck goalPuck3 = PuckManager.Instance?.GetPuck();
                if (player != null && player && goalPuck3 != null) {
                    Vector3 puckPos = goalPuck3.transform.position;
                    Vector3 puckVel = goalPuck3.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                    RecordPlayByPlayEventInternal(PlayByPlayEventType.Goal, player, puckPos, puckVel, "successful");
                }
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
        /// Function that sends and sets the s% for a goalie when a goal is scored.
        /// </summary>
        /// <param name="team">PlayerTeam, team that scored the goal.</param>
        /// <param name="saveWasCounted">Bool, true if a save was already counted for that shot.</param>
        private static void SendSavePercDuringGoal(PlayerTeam team, bool saveWasCounted) {
            // Get other team goalie.
            Player goalie = PlayerFunc.GetOtherTeamGoalie(team);
            if (goalie == null)
                return;

            string _goaliePlayerSteamId = goalie.SteamId.Value.ToString();
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
                    
                    if (!hasFollowUpTouch) {
                        // No follow-up touch by attacking team - the save didn't actually block the shot
                        // Mark the save as "failed" since the puck went in despite the save attempt
                        if (lastSaveEvent.Outcome == "successful") {
                            lastSaveEvent.Outcome = "failed";
                        }
                        
                        // Update body/stick saves dictionaries from pbp scanning (server-side only)
                        UpdateBodyStickSavesFromPbp();
                        
                        // Check if it was a home plate save
                        bool wasHomePlateSave = lastSaveEvent.Flags != null && lastSaveEvent.Flags.Contains("HomePlate");
                        
                        // Decrement home plate saves if it was a home plate save
                        if (wasHomePlateSave) {
                            if (_homePlateSaves.TryGetValue(_goaliePlayerSteamId, out int hpSaveValue) && hpSaveValue > 0) {
                                int hpSaves = _homePlateSaves[_goaliePlayerSteamId] = --hpSaveValue;
                                QueueStatUpdate(Codebase.Constants.HOME_PLATE_SAVES + _goaliePlayerSteamId, hpSaves.ToString());
                            }
                        }
                    }
                    // If hasFollowUpTouch is true, the save remains "successful" (rebound goal scenario)
                }
            }

            (int saves, int sog) = _savePerc[_goaliePlayerSteamId] = saveWasCounted ? (--_savePercValue.Saves, _savePercValue.Shots) : (_savePercValue.Saves, ++_savePercValue.Shots);
            QueueStatUpdate(Codebase.Constants.SAVEPERC + _goaliePlayerSteamId, _savePerc[_goaliePlayerSteamId].ToString());
            LogSavePerc(_goaliePlayerSteamId, saves, sog);
        }

        /// <summary>
        /// Method that logs the save percentage of a goalie.
        /// </summary>
        /// <param name="goaliePlayerSteamId">String, steam Id of the goalie.</param>
        /// <param name="saves">Int, number of saves.</param>
        /// <param name="sog">Int, number of shots on goal on the goalie.</param>
        private static void LogSavePerc(string goaliePlayerSteamId, int saves, int sog) {
            Logging.Log($"playerSteamId:{goaliePlayerSteamId},saveperc:{GetGoalieSavePerc(saves, sog)},saves:{saves},sog:{sog}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the stick saves of a goalie.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="stickSaves">Int, number of stick saves.</param>
        private static void LogStickSave(string playerSteamId, int stickSaves) {
            Logging.Log($"playerSteamId:{playerSteamId},sticksv:{stickSaves}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the shots on goal of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="sog">Int, number of shots on goal.</param>
        private static void LogSOG(string playerSteamId, int sog) {
            Logging.Log($"playerSteamId:{playerSteamId},sog:{sog}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the blocked shots of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="block">Int, number of blocked shots.</param>
        private static void LogBlock(string playerSteamId, int block) {
            Logging.Log($"playerSteamId:{playerSteamId},block:{block}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the hits of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="hit">Int, number of hits.</param>
        private static void LogHit(string playerSteamId, int hit) {
            Logging.Log($"playerSteamId:{playerSteamId},hit:{hit}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the takeaways of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="takeaway">Int, number of takeaways.</param>
        private static void LogTakeaways(string playerSteamId, int takeaway) {
            Logging.Log($"playerSteamId:{playerSteamId},takeaway:{takeaway}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the turnovers of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="turnover">Int, number of turnovers.</param>
        private static void LogTurnovers(string playerSteamId, int turnover) {
            Logging.Log($"playerSteamId:{playerSteamId},turnover:{turnover}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the passes of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="pass">Int, number of passes.</param>
        private static void LogPass(string playerSteamId, int pass) {
            Logging.Log($"playerSteamId:{playerSteamId},pass:{pass}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the puck touches of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="puckTouch">Int, number of puck touches.</param>
        private static void LogPuckTouch(string playerSteamId, int puckTouch) {
            Logging.Log($"playerSteamId:{playerSteamId},pucktouch:{puckTouch}", ServerConfig);
        }

        /// <summary>
        /// Method that logs the game winning goal of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        private static void LogGWG(string playerSteamId) {
            Logging.Log($"playerSteamId:{playerSteamId},gwg:1", ServerConfig);
        }

        /// <summary>
        /// Method that logs the match star of a player.
        /// </summary>
        /// <param name="playerSteamId">String, steam Id of the player.</param>
        /// <param name="starIndex">Int, star number of the player (1 is first star, etc.).</param>
        private static void LogStar(string playerSteamId, int starIndex) {
            Logging.Log($"playerSteamId:{playerSteamId},star:{starIndex}", ServerConfig);
        }

        #region Play-by-Play Methods
        /// <summary>
        /// Records a play-by-play event as part of unified stat tracking system
        /// This is called from the same places where stats are updated, ensuring chronological logging
        /// </summary>
        private static void RecordPlayByPlayEventInternal(PlayByPlayEventType eventType, Player player, Vector3 position, Vector3 velocity, string outcome = "successful", string flags = "", float? playerSpeedOverride = null, bool skipPossessionReset = false, float? gameTimeOverride = null, string teamInPossessionOverride = null) {
            // Use same game active check as existing stats mod - only track during Playing phase (FaceOff is just a 3 second delay)
            if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing || !_logic)
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
                string playerSteamId = player.SteamId.Value.ToString();
                if (string.IsNullOrEmpty(playerSteamId))
                    return;

                EventZone zone = DetermineEventZone(position, player.Team.Value);
                float gameTime = gameTimeOverride ?? GetCurrentGameTime();
                int period = GetCurrentPeriod();
                PlayerTeam eventTeam = player.Team.Value;

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
                    PlayerName = player.Username.Value.ToString(),
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
                
                // If this is a shot event, check if we need to cancel any recent turnovers
                // This handles cases where shot tracking has a delay and the shot event is created after the turnover validation
                if (eventType == PlayByPlayEventType.Shot && gameTime > 0f) {
                    CancelTurnoverIfShotOccurred(playerSteamId, gameTime);
                }
                
                // Check for and process turnovers/takeaways when team hits their 2nd possession chain event (_currentPlayInPossession == 1)
                // This combines detection and validation - checks if possession changed and processes immediately
                // Re-entrancy guard in ValidatePendingTurnoversTakeaways prevents infinite recursion
                if (_currentPlayInPossession == 1 && _currentTeamInPossession == eventTeam) {
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
                                PlayerTeam playerTeam = player.Team.Value;
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
                                PlayerTeam playerTeam = player.Team.Value;
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
                Logging.LogError($"Error recording play-by-play event: {ex}", ServerConfig);
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
                Logging.LogError($"Error recording faceoff event: {ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Records placeholder FaceoffOutcome events immediately after faceoff (will be updated when outcome is determined)
        /// </summary>
        private static PlayByPlayEvent RecordFaceoffOutcomePlaceholder(PlayerTeam team, System.Collections.Generic.IEnumerable<Player> allPlayers, float gameTime, int period, Vector3 centerIce) {
            // Find center for this team
            Player center = allPlayers.FirstOrDefault(p => 
                p != null && p && 
                p.Team.Value == team && 
                GetPlayerPosition(p) == "C");

            PlayByPlayEvent outcomeEvent;
            if (center != null) {
                outcomeEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = PlayByPlayEventType.FaceoffOutcome,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = center.SteamId.Value.ToString(),
                    PlayerName = center.Username.Value.ToString(),
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
                Logging.LogError($"Error resolving pending faceoff on tracking stop: {ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Records a FaceoffOutcome event for the center of each team
        /// </summary>
        /// <param name="winningTeam">The team that won (if won=true) or lost (if won=false)</param>
        /// <param name="won">True if this team won (3+ chain), false if they lost possession</param>
        private static void RecordFaceoffOutcome(PlayerTeam winningTeam, bool won) {
            if (!ServerFunc.IsDedicatedServer() || _paused || GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing || !_logic)
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
                Logging.LogError($"Error recording faceoff outcome: {ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Helper method to record faceoff outcome for a single team
        /// </summary>
        private static void RecordFaceoffOutcomeForTeam(PlayerTeam team, bool won, System.Collections.Generic.IEnumerable<Player> allPlayers, float gameTime, int period, Vector3 centerIce) {
            // Find center for this team
            Player center = allPlayers.FirstOrDefault(p => 
                p != null && p && 
                p.Team.Value == team && 
                GetPlayerPosition(p) == "C");

            if (center != null) {
                var outcomeEvent = new PlayByPlayEvent {
                    EventId = _nextPlayByPlayEventId++,
                    EventType = PlayByPlayEventType.FaceoffOutcome,
                    GameTime = gameTime,
                    Period = period,
                    PlayerSteamId = center.SteamId.Value.ToString(),
                    PlayerName = center.Username.Value.ToString(),
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
                // Use exact same method as Ruleset mod: GameManager.Instance.GameState.Value.Time
                var gameState = GameManager.Instance.GameState.Value;
                int timeRemaining = gameState.Time;
                
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
                if (GameManager.Instance.Phase == GamePhase.Playing) {
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
                Logging.LogError($"Error in GetCurrentGameTime: {ex}", ServerConfig);
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
                // Use same pattern as Ruleset mod: direct property access on GameState.Value
                var gameState = GameManager.Instance.GameState.Value;
                
                // Try direct property access (like BlueScore/RedScore/Time)
                int period = gameState.Period;
                
                if (period > 0) {
                    return period;
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in GetCurrentPeriod: {ex}", ServerConfig);
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
                    return "G";

                var positionField = typeof(Player).GetField("PlayerPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (positionField != null) {
                    var positionValue = positionField.GetValue(player);
                    if (positionValue != null) {
                        string positionStr = positionValue.ToString();
                        // Remove enum suffix if present
                        if (positionStr.Contains("(")) {
                            positionStr = positionStr.Substring(0, positionStr.IndexOf("(")).Trim();
                        }
                        return positionStr;
                    }
                }
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

        /// <summary>
        /// Reorders players in the scoreboard by position: C, LW, RW, LD, RD, G, N/A
        /// Red team first, then Blue team
        /// </summary>
        private static void ReorderScoreboardPlayers(VisualElement scoreboardContainer) {
            if (scoreboardContainer == null || UIScoreboard.Instance == null)
                return;

            try {
                var playerVisualElementMap = SystemFunc.GetPrivateField<Dictionary<Player, VisualElement>>(typeof(UIScoreboard), UIScoreboard.Instance, "playerVisualElementMap");
                if (playerVisualElementMap == null || playerVisualElementMap.Count == 0)
                    return;

                // Get all players with their visual elements and sort by position
                var playersWithElements = playerVisualElementMap.Select(kvp => new {
                    Player = kvp.Key,
                    VisualElement = kvp.Value,
                    Position = GetPlayerPosition(kvp.Key),
                    Team = kvp.Key.Team.Value,
                    OrderPriority = GetPositionOrderPriority(GetPlayerPosition(kvp.Key), kvp.Key.Team.Value)
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

                var allPlayers = PlayerManager.Instance.GetPlayers();
                if (allPlayers == null)
                    return;

                PlayerTeam playerTeam = (PlayerTeam)gameEvent.PlayerTeam;
                PlayerTeam opposingTeam = playerTeam == PlayerTeam.Blue ? PlayerTeam.Red : PlayerTeam.Blue;

                var teamPlayers = allPlayers.Where(p => p != null && p && p.Team.Value == playerTeam).ToList();
                var opposingTeamPlayers = allPlayers.Where(p => p != null && p && p.Team.Value == opposingTeam).ToList();

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
                Logging.LogError($"Error capturing team roster data: {ex}", ServerConfig);
            }
        }

        // ==================== EARLY DATA EXPORT - FOR USE IN CASE OF FF ====================
        // This method allows exporting both JSON and CSV early via /endgame command (for forfeits)
        /// <summary>
        /// Exports play-by-play data to CSV (internal method, can be called from GameOver)
        /// </summary>
        private static void ExportPlayByPlayCSVInternal() {
            if (_playByPlayEvents.Count == 0) {
                Logging.Log("No play-by-play events to export", ServerConfig);
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
                Logging.Log($"Converted {convertedAttempts} pending shot 'attempt' outcomes to 'missed' before CSV export", ServerConfig);
            }

            try {
                string statsFolderPath = Path.Combine(Path.GetFullPath("."), "stats");
                if (!Directory.Exists(statsFolderPath))
                    Directory.CreateDirectory(statsFolderPath);

                // Use the game reference ID from game start, or generate one if not set
                string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string csvPath = Path.Combine(statsFolderPath, $"{ServerConfig.FileHeaderName}_playbyplay_{gameReferenceId}.csv");

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
                Logging.Log($"Play-by-play data exported to {csvPath} with {_playByPlayEvents.Count} events", ServerConfig);
            }
            catch (Exception ex) {
                Logging.LogError($"Can't write the play-by-play data in the stats folder. (Permission error ?)\n{ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Exports both JSON and CSV files (used for early game end via /endgame command or natural game end)
        /// </summary>
        private static void ExportGameStats() {
            if (_playByPlayEvents.Count == 0) {
                Logging.Log("No play-by-play events to export", ServerConfig);
                return;
            }
            
            // Calculate game-winning goal scorer (same logic as game end handler)
            string gwgSteamId = "";
            if (GameManager.Instance != null && GameManager.Instance.GameState != null) {
                try {
                    if (GameManager.Instance.GameState.Value.BlueScore > GameManager.Instance.GameState.Value.RedScore) {
                        // Blue won - GWG is the goal that put them ahead by 1 (at index of opponent's score)
                        gwgSteamId = _blueGoals[GameManager.Instance.GameState.Value.RedScore];
                    }
                    else if (GameManager.Instance.GameState.Value.RedScore > GameManager.Instance.GameState.Value.BlueScore) {
                        // Red won - GWG is the goal that put them ahead by 1 (at index of opponent's score)
                        gwgSteamId = _redGoals[GameManager.Instance.GameState.Value.BlueScore];
                    }
                }
                catch (IndexOutOfRangeException) {
                    // Shootout goal or something, so no GWG - leave empty
                }
                catch {
                    // If calculation fails for any other reason, leave empty
                }
            }
            
            // Check play-by-play data before creating JSON - only export if game has sufficient players and events
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
                Logging.LogError($"Error counting unique players for export: {ex}", ServerConfig);
            }
            
            // Only create and export JSON and CSV if pbp check passes (8+ players and 300+ events)
            if (uniquePlayerCount >= 8 && eventCount >= 300) {
                // Generate JSON content (same logic as game end handler)
                List<Dictionary<string, object>> playersList = new List<Dictionary<string, object>>();
                
                // Calculate Time On Ice (TOI) from play-by-play events
                CalculateTimeOnIce();
                
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
                
                // Calculate player-level goals and assists
                Dictionary<string, int> playerGoals = new Dictionary<string, int>();
                Dictionary<string, int> playerAssists = new Dictionary<string, int>();
                
                foreach (string steamId in _blueGoals) {
                    if (!playerGoals.TryGetValue(steamId, out int _))
                        playerGoals.Add(steamId, 0);
                    playerGoals[steamId]++;
                }
                foreach (string steamId in _redGoals) {
                    if (!playerGoals.TryGetValue(steamId, out int _))
                        playerGoals.Add(steamId, 0);
                    playerGoals[steamId]++;
                }
                
                foreach (string steamId in _blueAssists) {
                    if (!playerAssists.TryGetValue(steamId, out int _))
                        playerAssists.Add(steamId, 0);
                    playerAssists[steamId]++;
                }
                foreach (string steamId in _redAssists) {
                    if (!playerAssists.TryGetValue(steamId, out int _))
                        playerAssists.Add(steamId, 0);
                    playerAssists[steamId]++;
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
                foreach (string steamId in _blueGoals) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _redGoals) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _blueAssists) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (string steamId in _redAssists) { if (!string.IsNullOrEmpty(steamId)) allPlayerSteamIds.Add(steamId); }
                foreach (var pbpEvent in _playByPlayEvents) {
                    if (!string.IsNullOrEmpty(pbpEvent.PlayerSteamId))
                        allPlayerSteamIds.Add(pbpEvent.PlayerSteamId);
                }
                
                // Build player stats (simplified - using same logic as game end handler)
                foreach (string playerSteamId in allPlayerSteamIds) {
                    Player player = PlayerManager.Instance.GetPlayerBySteamId(playerSteamId);
                    
                    string playerName = "";
                    string teamString = "spectator";
                    string position = "N/A";
                    bool isGoalie = false;
                    PlayerTeam playerTeam = PlayerTeam.Blue;
                    
                    if (player != null && player) {
                        playerName = player.Username.Value.ToString();
                        isGoalie = PlayerFunc.IsGoalie(player);
                        position = GetPlayerPosition(player);
                        playerTeam = player.Team.Value;
                        
                        if (playerTeam == PlayerTeam.Blue) {
                            teamString = "Blue";
                        } else if (playerTeam == PlayerTeam.Red) {
                            teamString = "Red";
                        }
                    } else {
                        var lastEvent = _playByPlayEvents
                            .LastOrDefault(e => e.PlayerSteamId == playerSteamId && !string.IsNullOrEmpty(e.PlayerName));
                        
                        if (lastEvent != null) {
                            playerName = lastEvent.PlayerName;
                            playerTeam = (PlayerTeam)lastEvent.PlayerTeam;
                            
                            if (playerTeam == PlayerTeam.Blue) {
                                teamString = "Blue";
                            } else if (playerTeam == PlayerTeam.Red) {
                                teamString = "Red";
                            }
                            
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
                        } else {
                            playerName = playerSteamId;
                        }
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
                        { "turnovers", _turnovers.TryGetValue(playerSteamId, out int turnovers) ? turnovers : 0 },
                        { "takeaways", _takeaways.TryGetValue(playerSteamId, out int takeaways) ? takeaways : 0 },
                        { "puckTouches", _puckTouches.TryGetValue(playerSteamId, out int puckTouches) ? puckTouches : 0 },
                        { "possessionTimeSeconds", _possessionTimeSeconds.TryGetValue(playerSteamId, out double possessionTime) ? possessionTime : 0.0 },
                        { "timeOnIce", _timeOnIceSeconds.TryGetValue(playerSteamId, out double toi) ? toi : 0.0 },
                        { "faceoffWins", playerFaceoffWins.TryGetValue(playerSteamId, out int fw) ? fw : 0 },
                        { "faceoffLosses", playerFaceoffLosses.TryGetValue(playerSteamId, out int fl) ? fl : 0 }
                    };
                    
                    if (isGoalie || _savePerc.ContainsKey(playerSteamId)) {
                        int shotsFaced = 0;
                        int saves = 0;
                        if (_savePerc.TryGetValue(playerSteamId, out var savePercValue)) {
                            shotsFaced = savePercValue.Shots;
                            saves = savePercValue.Saves;
                        }
                        
                        playerStats["shotsFaced"] = shotsFaced;
                        playerStats["saves"] = saves;
                        playerStats["saveperc"] = shotsFaced > 0 ? (double)saves / shotsFaced : 0.0;
                        playerStats["bodySaves"] = _bodySaves.TryGetValue(playerSteamId, out int bodySaves) ? bodySaves : 0;
                        playerStats["stickSaves"] = _stickSaves.TryGetValue(playerSteamId, out int stickSaves) ? stickSaves : 0;
                    }
                    
                    playersList.Add(playerStats);
                }
                
                // Calculate team stats
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
                float totalGameTimeSeconds = _playByPlayEvents.Count > 0 ? _playByPlayEvents.Max(e => e.GameTime) : 0f;
                
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
                        { "redDZExits", redTeamDZExits },
                        { "redDZExitsAllowed", blueTeamDZExits },
                        { "redOZEntries", redTeamOZEntries },
                        { "redOZEntriesAllowed", blueTeamOZEntries },
                        { "redTeamPossessionTime", redTeamPossessionTime },
                        { "redTeamPossessionTimeAg", blueTeamPossessionTime },
                        { "bluegoals", _blueGoals },
                        { "redgoals", _redGoals },
                        { "blueassists", _blueAssists },
                        { "redassists", _redAssists },
                        { "gwg", gwgSteamId },
                        { "stars", _stars },
                        { "gameTimeMinutes", totalGameTimeMinutes }
                    }}
                };
                
                string jsonContent = JsonConvert.SerializeObject(jsonDict, Formatting.Indented);
                Logging.Log("Stats:" + jsonContent, ServerConfig);
                
                // Export JSON file
                if (ServerConfig.SaveEOGJSON) {
                    try {
                        string statsFolderPath = Path.Combine(Path.GetFullPath("."), "stats");
                        if (!Directory.Exists(statsFolderPath))
                            Directory.CreateDirectory(statsFolderPath);
                        
                        // Use the game reference ID from game start, or generate one if not set (same as CSV)
                        string gameReferenceId = !string.IsNullOrEmpty(_currentGameReferenceId) ? _currentGameReferenceId : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                        string jsonPath = Path.Combine(statsFolderPath, ServerConfig.FileHeaderName + "_stats_" + gameReferenceId + ".json");
                        
                        File.WriteAllText(jsonPath, jsonContent);
                        Logging.Log($"JSON exported: {uniquePlayerCount} unique players, {eventCount} events", ServerConfig);
                    }
                    catch (Exception ex) {
                        Logging.LogError($"Can't write the end of game stats in the stats folder. (Permission error ?)\n{ex}", ServerConfig);
                    }
                }
                
                // Export CSV
                ExportPlayByPlayCSVInternal();
                Logging.Log($"CSV exported: {uniquePlayerCount} unique players, {eventCount} events", ServerConfig);
            }
            else {
                Logging.Log($"Export skipped (JSON and CSV): {uniquePlayerCount} unique players (need 8+), {eventCount} events (need 300+)", ServerConfig);
            }
        }

        /// <summary>
        /// Chat command handler for early game end export
        /// Handles /endgame command to export both JSON and CSV files early (for forfeits)
        /// </summary>
        [HarmonyPatch]
        public static class UIChatController_Event_Server_OnChatCommand_Patch {
            public static System.Reflection.MethodBase TargetMethod() {
                // Search in all loaded assemblies for UIChatController
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                    try {
                        var chatControllerType = assembly.GetTypes()
                            .FirstOrDefault(t => t.Name == "UIChatController");
                        if (chatControllerType != null) {
                            var method = chatControllerType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                                .FirstOrDefault(m => m.Name == "Event_Server_OnChatCommand");
                            if (method != null)
                                return method;
                        }
                    }
                    catch {
                        // Skip assemblies that can't be inspected
                        continue;
                    }
                }
                return null;
            }

            public static void Postfix(object __instance, object[] __args) {
                try {
                    if (!ServerFunc.IsDedicatedServer() || !_logic)
                        return;

                    // Extract chat message from arguments
                    string msg = null;
                    if (__args != null && __args.Length > 0) {
                        var firstArg = __args[0];
                        if (firstArg is System.Collections.IDictionary dict) {
                            string[] possibleKeys = { "message", "text", "content", "msg", "command" };
                            foreach (var key in possibleKeys) {
                                if (dict.Contains(key) && dict[key] is string strValue) {
                                    msg = strValue;
                                    break;
                                }
                            }
                        }
                        else if (firstArg is string strArg) {
                            msg = strArg;
                        }
                    }

                    if (string.IsNullOrEmpty(msg))
                        return;

                    // Handle /endgame command to export both JSON and CSV early (for forfeits)
                    if (msg.StartsWith("/endgame", StringComparison.OrdinalIgnoreCase)) {
                        Logging.Log("Early game end export command received", ServerConfig);
                        ExportGameStats();
                        if (UIChat.Instance != null) {
                            UIChat.Instance.Server_SendSystemChatMessage($"Game stats exported early (JSON and CSV) with {_playByPlayEvents.Count} events");
                        }
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in chat command handler: {ex}", ServerConfig);
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
                                    PlayerTeam playerTeam = exitPlayer.Team.Value;
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
                                    PlayerTeam playerTeam = entryPlayer.Team.Value;
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
                Logging.LogError($"Error in AddZoneFlagsToPlayByPlayEvents: {ex}", ServerConfig);
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
            
            // Check UTF-8 byte size - limit to 48KB to prevent MemCpy issues
            const int MAX_SAFE_BYTES = 49152;
            int byteSize = Encoding.UTF8.GetByteCount(data);
            if (byteSize > MAX_SAFE_BYTES) {
                error = $"Data size ({byteSize} bytes) exceeds safe limit ({MAX_SAFE_BYTES} bytes)";
                return false;
            }
            
            // Check for extremely long strings (character count sanity check)
            if (data.Length > 100000) {
                error = $"Data length ({data.Length} characters) exceeds sanity limit";
                return false;
            }
            
            // Check dataName size
            int dataNameByteSize = Encoding.UTF8.GetByteCount(dataName);
            if (dataNameByteSize > 1024) {
                error = $"Data name size ({dataNameByteSize} bytes) exceeds limit";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Safely sends data to all clients with validation and error handling.
        /// Returns true if successful, false if failed.
        /// </summary>
        private static bool SafeSendDataToAll(string dataName, string dataValue) {
            if (!ValidateStringForNetwork(dataValue, dataName, out string validationError)) {
                Logging.LogError($"Validation failed for {dataName}: {validationError}", ServerConfig);
                return false;
            }
            
            try {
                NetworkCommunication.SendDataToAll(dataName, dataValue, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Error sending data {dataName}: {ex}", ServerConfig);
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
                    Logging.Log("Batching re-enabled after cooldown period", ServerConfig);
                }
                else {
                    // Silently skip batching - game continues without stat updates
                    return;
                }
            }
            
            if (_pendingStatUpdates.Count == 0)
                return;
            
            // Wrap entire method in try-catch to prevent server crashes
            try {
            // Create a snapshot of pending updates to avoid thread safety issues during iteration
            // LockDictionary's enumerator doesn't maintain lock during iteration, so we need a snapshot
            Dictionary<string, string> updatesSnapshot = new Dictionary<string, string>();
            foreach (var kvp in _pendingStatUpdates) {
                updatesSnapshot[kvp.Key] = kvp.Value;
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
                    // NetworkCommunication.SendDataToAll converts to UTF-8, so we must check byte size, not character count
                    const int MAX_BATCH_BYTES = 49152; // 48KB limit to prevent memory issues in MemCpy
                    
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
                            // Double-check byte size before sending as a final safety measure
                            int finalByteSize = Encoding.UTF8.GetByteCount(batchData);
                            if (finalByteSize > MAX_BATCH_BYTES) {
                                Logging.LogError($"Batch data still too large ({finalByteSize} bytes) for {batchDataName} after splitting, skipping", ServerConfig);
                                continue;
                            }
                            
                            // Use safe send method
                            if (!SafeSendDataToAll(batchDataName, batchData)) {
                                // Track failure for circuit breaker
                                _batchingFailureCount++;
                                _lastBatchingFailureTime = now;
                                if (_batchingFailureCount >= MAX_BATCHING_FAILURES) {
                                    _batchingDisabled = true;
                                    Logging.LogError($"Batching disabled after {_batchingFailureCount} failures. Will re-enable after {BATCHING_DISABLE_DURATION_SECONDS} seconds.", ServerConfig);
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
                        Logging.Log($"Split {batchDataName} batch into {batchChunks.Count} chunks due to size limit", ServerConfig);
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
                
                Logging.LogError($"Critical error in SendBatchedStatUpdates: {ex}. Batching will be disabled if this happens {MAX_BATCHING_FAILURES} times.", ServerConfig);
                
                if (_batchingFailureCount >= MAX_BATCHING_FAILURES) {
                    _batchingDisabled = true;
                    Logging.LogError($"Batching permanently disabled after {_batchingFailureCount} critical failures. Will re-enable after {BATCHING_DISABLE_DURATION_SECONDS} seconds.", ServerConfig);
                    // Clear pending updates to prevent accumulation
                    _pendingStatUpdates.Clear();
                }
                
                // Don't rethrow - allow game to continue without stat updates
            }
        }

        private static string GetGoalieSavePerc(int saves, int shots) {
            if (shots == 0)
                return "0.000";

            return (((double)saves) / ((double)shots)).ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static bool RulesetModEnabled() {
            return _rulesetModEnabled != null && (bool)_rulesetModEnabled;
        }

        private static string GetStarTag(string playerSteamId) {
            string star = "";
            if (_stars[1] == playerSteamId)
                star = "<color=#FFD700FF><b>★</b></color> ";
            else if (_stars[2] == playerSteamId)
                star = "<color=#C0C0C0FF><b>★</b></color> ";
            else if (_stars[3] == playerSteamId)
                star = "<color=#CD7F32FF><b>★</b></color> ";

            return star;
        }

        private static string StripStarTags(string text) {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove all possible star tag patterns
            text = text.Replace("<color=#FFD700FF><b>★</b></color> ", "");
            text = text.Replace("<color=#C0C0C0FF><b>★</b></color> ", "");
            text = text.Replace("<color=#CD7F32FF><b>★</b></color> ", "");

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
                        label.text = "0.000";
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
    }
}
