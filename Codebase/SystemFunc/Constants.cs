namespace Codebase {
    internal static class Constants {
        /// <summary>
        /// Const string, prefix of all the mod's names.
        /// </summary>
        private const string MODS_PREFIX = "oomtm450_";

        /// <summary>
        /// Const string, name of the Stats mod.
        /// </summary>
        internal const string STATS_MOD_NAME = MODS_PREFIX + "stats";

        /// <summary>
        /// Const string, name of the Ruleset mod.
        /// </summary>
        internal const string RULESET_MOD_NAME = MODS_PREFIX + "ruleset";

        /// <summary>
        /// Const string, name of the Sounds mod.
        /// </summary>
        internal const string SOUNDS_MOD_NAME = MODS_PREFIX + "sounds";

        /// <summary>
        /// Const string, name of the Sounds mod.
        /// </summary>
        internal const string SOUNDSPACK_MOD_NAME = MODS_PREFIX + "soundspack";

        /// <summary>
        /// Const string, used for the communication from the server for the Sounds mod.
        /// </summary>
        internal const string SOUNDS_FROM_SERVER_TO_CLIENT = SOUNDS_MOD_NAME + "_server";

        /// <summary>
        /// Const string, used for the communication from the client for the Sounds mod.
        /// </summary>
        internal const string SOUNDS_FROM_CLIENT_TO_SERVER = SOUNDS_MOD_NAME + "_client";

        /// <summary>
        /// Const float, radius of the puck.
        /// </summary>
        internal const float PUCK_RADIUS = 0.13f;

        /// <summary>
        /// Const float, radius of a player.
        /// </summary>
        internal const float PLAYER_RADIUS = 0.262f;

        /// <summary>
        /// Const float, height of the net's crossbar.
        /// </summary>
        internal const float CROSSBAR_HEIGHT = 1.8f;

        /// <summary>
        /// Const string, data name for SOG.
        /// </summary>
        public const string SOG = STATS_MOD_NAME + "SOG";

        /// <summary>
        /// Const string, data name for shot attempts.
        /// </summary>
        public const string SHOT_ATTEMPTS = STATS_MOD_NAME + "SHOT_ATTEMPTS";

        /// <summary>
        /// Const string, data name for home plate shots.
        /// </summary>
        public const string HOME_PLATE_SHOTS = STATS_MOD_NAME + "HOME_PLATE_SHOTS";

        /// <summary>
        /// Const string, data name for the save percentage.
        /// </summary>
        public const string SAVEPERC = STATS_MOD_NAME + "SAVEPERC";

        /// <summary>
        /// Const string, data name for blocked shots.
        /// </summary>
        public const string BLOCK = STATS_MOD_NAME + "BLOCK";

        /// <summary>
        /// Const string, data name for passes.
        /// </summary>
        public const string PASS = STATS_MOD_NAME + "PASS";

        /// <summary>
        /// Const string, data name for hits.
        /// </summary>
        public const string HIT = STATS_MOD_NAME + "HIT";

        /// <summary>
        /// Const string, data name for takeways.
        /// </summary>
        public const string TAKEAWAY = STATS_MOD_NAME + "TAKEAWAY";

        /// <summary>
        /// Const string, data name for turnovers.
        /// </summary>
        public const string TURNOVER = STATS_MOD_NAME + "TURNOVER";

        /// <summary>
        /// Const string, data name for puck touches.
        /// </summary>
        public const string PUCK_TOUCH = STATS_MOD_NAME + "PUCK_TOUCH";

        /// <summary>
        /// Const string, data name for zone exits.
        /// </summary>
        public const string EXIT = STATS_MOD_NAME + "EXIT";

        /// <summary>
        /// Const string, data name for zone entries.
        /// </summary>
        public const string ENTRY = STATS_MOD_NAME + "ENTRY";

        /// <summary>
        /// Const string, data name for possession time.
        /// </summary>
        public const string POSSESSION_TIME = STATS_MOD_NAME + "POSSESSION_TIME";

        /// <summary>
        /// Const string, data name for puck battle wins.
        /// </summary>
        public const string PUCK_BATTLE_WINS = STATS_MOD_NAME + "PUCK_BATTLE_WINS";

        /// <summary>
        /// Const string, data name for puck battle losses.
        /// </summary>
        public const string PUCK_BATTLE_LOSSES = STATS_MOD_NAME + "PUCK_BATTLE_LOSSES";

        /// <summary>
        /// Const string, data name for team shots.
        /// </summary>
        public const string TEAM_SHOTS = STATS_MOD_NAME + "TEAM_SHOTS";

        /// <summary>
        /// Const string, data name for team shot attempts.
        /// </summary>
        public const string TEAM_SHOT_ATTEMPTS = STATS_MOD_NAME + "TEAM_SHOT_ATTEMPTS";

        /// <summary>
        /// Const string, data name for team home plate shots.
        /// </summary>
        public const string TEAM_HOME_PLATE_SHOTS = STATS_MOD_NAME + "TEAM_HOME_PLATE_SHOTS";

        /// <summary>
        /// Const string, data name for home plate shots on goal (SOGs only).
        /// </summary>
        public const string HOME_PLATE_SOGS = STATS_MOD_NAME + "HOME_PLATE_SOGS";

        /// <summary>
        /// Const string, data name for team home plate shots on goal (SOGs only).
        /// </summary>
        public const string TEAM_HOME_PLATE_SOGS = STATS_MOD_NAME + "TEAM_HOME_PLATE_SOGS";

        /// <summary>
        /// Const string, data name for team passes.
        /// </summary>
        public const string TEAM_PASSES = STATS_MOD_NAME + "TEAM_PASSES";

        /// <summary>
        /// Const string, data name for team possession time.
        /// </summary>
        public const string TEAM_POSSESSION_TIME = STATS_MOD_NAME + "TEAM_POSSESSION_TIME";

        /// <summary>
        /// Const string, data name for team puck battle wins.
        /// </summary>
        public const string TEAM_PUCK_BATTLE_WINS = STATS_MOD_NAME + "TEAM_PUCK_BATTLE_WINS";

        /// <summary>
        /// Const string, data name for team puck battle losses.
        /// </summary>
        public const string TEAM_PUCK_BATTLE_LOSSES = STATS_MOD_NAME + "TEAM_PUCK_BATTLE_LOSSES";

        /// <summary>
        /// Const string, data name for team faceoff wins.
        /// </summary>
        public const string TEAM_FACEOFF_WINS = STATS_MOD_NAME + "TEAM_FACEOFF_WINS";

        /// <summary>
        /// Const string, data name for team faceoff total.
        /// </summary>
        public const string TEAM_FACEOFF_TOTAL = STATS_MOD_NAME + "TEAM_FACEOFF_TOTAL";

        /// <summary>
        /// Const string, data name for team takeaways.
        /// </summary>
        public const string TEAM_TAKEAWAYS = STATS_MOD_NAME + "TEAM_TAKEAWAYS";

        /// <summary>
        /// Const string, data name for team turnovers.
        /// </summary>
        public const string TEAM_TURNOVERS = STATS_MOD_NAME + "TEAM_TURNOVERS";

        /// <summary>
        /// Const string, data name for team zone exits.
        /// </summary>
        public const string TEAM_EXITS = STATS_MOD_NAME + "TEAM_EXITS";

        /// <summary>
        /// Const string, data name for team zone entries.
        /// </summary>
        public const string TEAM_ENTRIES = STATS_MOD_NAME + "TEAM_ENTRIES";

        /// <summary>
        /// Const string, data name for stick saves (goalie stat).
        /// </summary>
        public const string STICK_SAVES = STATS_MOD_NAME + "STICK_SAVES";

        /// <summary>
        /// Const string, data name for body saves (goalie stat).
        /// </summary>
        public const string BODY_SAVES = STATS_MOD_NAME + "BODY_SAVES";

        /// <summary>
        /// Const string, data name for home plate saves (goalie stat).
        /// </summary>
        public const string HOME_PLATE_SAVES = STATS_MOD_NAME + "HOME_PLATE_SAVES";

        /// <summary>
        /// Const string, data name for home plate shots faced (goalie stat).
        /// </summary>
        public const string HOME_PLATE_SHOTS_FACED = STATS_MOD_NAME + "HOME_PLATE_SHOTS_FACED";

        /// <summary>
        /// Const string, data name for pausing mods.
        /// </summary>
        public const string PAUSE = "pause";

        /// <summary>
        /// Const string, data name for enabling or disabling the logic in mods.
        /// </summary>
        public const string LOGIC = "logic";

        /// <summary>
        /// Const string, data name for telling mods that Ruleset changed phase manually.
        /// </summary>
        public const string CHANGED_PHASE = "chphase";
    }
}
