using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public override string ModuleName => "SharpTimer";
        public override string ModuleVersion => $"0.2.0 - {new DateTime(Builtin.CompileTime, DateTimeKind.Utc)}";
        public override string ModuleAuthor => "DEAFPS https://github.com/DEAFPS/";
        public override string ModuleDescription => "A simple CSS Timer Plugin";

        private Dictionary<int, PlayerTimerInfo> playerTimers = new Dictionary<int, PlayerTimerInfo>();
        private Dictionary<int, PlayerReplays> playerReplays = new Dictionary<int, PlayerReplays>();
        private Dictionary<int, List<PlayerCheckpoint>> playerCheckpoints = new Dictionary<int, List<PlayerCheckpoint>>();
        private Dictionary<int, CCSPlayerController> connectedPlayers = new Dictionary<int, CCSPlayerController>();
        private Dictionary<int, CCSPlayerController> connectedReplayBots = new Dictionary<int, CCSPlayerController>();
        private Dictionary<uint, CCSPlayerController> specTargets = new Dictionary<uint, CCSPlayerController>();
        Dictionary<nint, TriggerPushData> triggerPushData = new Dictionary<nint, TriggerPushData>();
        private EntityCache? entityCache;
        public Dictionary<string, PlayerRecord>? SortedCachedRecords = new Dictionary<string, PlayerRecord>();
        private static readonly HttpClient httpClient = new HttpClient();

        public string msgPrefix = $"[SharpTimer] ";
        public string primaryHUDcolor = "green";
        public string secondaryHUDcolor = "orange";
        public string tertiaryHUDcolor = "white";
        public string primaryChatColor = "";
        public string startBeamColor = "";
        public string endBeamColor = "";
        public bool beamColorOverride = false;
        public string currentMapStartTrigger = "trigger_startzone";
        public Vector? currentMapStartTriggerMaxs = null;
        public Vector? currentMapStartTriggerMins = null;
        public string currentMapEndTrigger = "trigger_endzone";
        public Vector currentMapStartC1 = new Vector(0, 0, 0);
        public Vector currentMapStartC2 = new Vector(0, 0, 0);
        public Vector currentMapEndC1 = new Vector(0, 0, 0);
        public Vector currentMapEndC2 = new Vector(0, 0, 0);
        public Vector? currentRespawnPos = null;
        public QAngle? currentRespawnAng = null;
        public Vector? currentEndPos = null;
        public bool currentMapOverrideDisableTelehop = false;
        public string[]? currentMapOverrideMaxSpeedLimit = null;
        public bool currentMapOverrideStageRequirement = false;
        public bool currentMapOverrideTriggerPushFix = false;
        private Dictionary<int, Vector?> bonusRespawnPoses = new Dictionary<int, Vector?>();
        private Dictionary<int, QAngle?> bonusRespawnAngs = new Dictionary<int, QAngle?>();
        private Dictionary<nint, int> stageTriggers = new Dictionary<nint, int>();
        private Dictionary<nint, int> cpTriggers = new Dictionary<nint, int>();
        private Dictionary<int, Vector?> stageTriggerPoses = new Dictionary<int, Vector?>();
        private Dictionary<int, QAngle?> stageTriggerAngs = new Dictionary<int, QAngle?>();
        private int stageTriggerCount;
        private int cpTriggerCount;
        private bool useStageTriggers = false;
        private bool useCheckpointTriggers = false;
        public string? currentMapType = null;
        public int? currentMapTier = null;

        public bool enableDebug = true;
        public bool killServerCommands = true;
        public bool useMySQL = false;
        public bool enableReplays = false;
        public bool enableSRreplayBot = false;
        public bool startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile = false;
        public bool globalRanksEnabled = false;
        public bool globalRanksFreePointsEnabled = true;
        public int maxGlobalFreePoints = 20;
        public float? globalPointsMultiplier = 1.0f;
        public int minGlobalPointsForRank = 1000;
        public bool displayChatTags = true;
        public bool displayScoreboardTags = true;
        public string customVIPTag = "VIP";

        public bool useTriggers = true;
        public bool respawnEnabled = true;
        public bool keysOverlayEnabled = true;
        public bool hudOverlayEnabled = true;
        public bool topEnabled = true;
        public bool rankEnabled = true;
        public bool pbComEnabled = true;
        public bool alternativeSpeedometer = false;
        public bool removeLegsEnabled = true;
        public bool jumpStatsEnabled = false;
        public bool hideAllPlayers = false;
        public bool removeCollisionEnabled = true;
        public bool disableDamage = true;
        public bool cpEnabled = false;
        public bool use2DSpeed = false;
        public bool removeCpRestrictEnabled = false;
        public bool cpOnlyWhenTimerStopped = false;
        public bool connectMsgEnabled = true;
        public bool cmdJoinMsgEnabled = true;
        public bool autosetHostname = false;
        public bool srEnabled = true;
        public int adTimer = 120;
        public int rankHUDTimer = 170;
        public bool resetTriggerTeleportSpeedEnabled = false;
        public bool maxStartingSpeedEnabled = true;
        public int maxStartingSpeed = 320;
        public bool isADTimerRunning = false;
        public bool isRankHUDTimerRunning = false;
        public bool removeCrouchFatigueEnabled = true;
        public bool goToEnabled = false;
        public bool fovChangerEnabled = true;
        public bool triggerPushFixEnabled = false;
        public int cmdCooldown = 64;
        public float fakeTriggerHeight = 50;
        public int altVeloMaxSpeed = 3000;
        public bool forcePlayerSpeedEnabled = false;
        public float forcedPlayerSpeed = 250;

         public bool execCustomMapCFG  = false;

        public string beepSound = "sounds/ui/csgo_ui_button_rollover_large.vsnd";
        public string respawnSound = "sounds/ui/menu_accept.vsnd";
        public string cpSound = "sounds/ui/counter_beep.vsnd";
        public string cpSoundAir = "sounds/ui/weapon_cant_buy.vsnd";
        public string tpSound = "sounds/ui/buttonclick.vsnd";
        public string? gameDir;
        public string? mySQLpath;
        public string? playerRecordsPath;
        public string? currentMapName;
        public string? defaultServerHostname = ConVar.Find("hostname").StringValue;

        public string? remoteBhopDataSource = "https://raw.githubusercontent.com/DEAFPS/SharpTimer/main/remote_data/bhop_.json";
        public string? remoteKZDataSource = "https://raw.githubusercontent.com/DEAFPS/SharpTimer/main/remote_data/kz_.json";
        public string? remoteSurfDataSource = "https://raw.githubusercontent.com/DEAFPS/SharpTimer/main/remote_data/surf_.json";
        public string? testerPersonalGifsSource = "https://raw.githubusercontent.com/DEAFPS/SharpTimer/main/remote_data/tester_bling.json";

        public static string god3Icon = "<img src='https://i.imgur.com/SEnzkzv.gif' class=''>";
        public static string god2Icon = "<img src='https://i.imgur.com/SEnzkzv.gif' class=''>";
        public static string god1Icon = "<img src='https://i.imgur.com/SEnzkzv.gif' class=''>";
        public static string royalty3Icon = "<img src='https://i.imgur.com/JlOXD4R.png' class=''>";
        public static string royalty2Icon = "<img src='https://i.imgur.com/KvRyMSa.png' class=''>";
        public static string royalty1Icon = "<img src='https://i.imgur.com/uMXGmlf.png' class=''>";
        public static string legend3Icon = "<img src='https://i.imgur.com/HRArP8P.png' class=''>";
        public static string legend2Icon = "<img src='https://i.imgur.com/Q9VqY5U.png' class=''>";
        public static string legend1Icon = "<img src='https://i.imgur.com/v5hCxhS.png' class=''>";
        public static string master3Icon = "<img src='https://i.imgur.com/tld9l3l.png' class=''>";
        public static string master2Icon = "<img src='https://i.imgur.com/8QKSYcu.png' class=''>";
        public static string master1Icon = "<img src='https://i.imgur.com/qpkfFNr.png' class=''>";
        public static string diamond3Icon = "<img src='https://i.imgur.com/Nq5K2MM.png' class=''>";
        public static string diamond2Icon = "<img src='https://i.imgur.com/u2rYsVi.png' class=''>";
        public static string diamond1Icon = "<img src='https://i.imgur.com/VYn6sRF.png' class=''>";
        public static string platinum3Icon = "<img src='https://i.imgur.com/izGtCGl.png' class=''>";
        public static string platinum2Icon = "<img src='https://i.imgur.com/iwj1YfK.png' class=''>";
        public static string platinum1Icon = "<img src='https://i.imgur.com/5ny9N9j.png' class=''>";
        public static string gold3Icon = "<img src='https://i.imgur.com/XM2ReIY.png' class=''>";
        public static string gold2Icon = "<img src='https://i.imgur.com/MCdGy7k.png' class=''>";
        public static string gold1Icon = "<img src='https://i.imgur.com/eo04Y0x.png' class=''>";
        public static string silver3Icon = "<img src='https://i.imgur.com/DE6Ptj9.png' class=''>";
        public static string silver2Icon = "<img src='https://i.imgur.com/OciFq7d.png' class=''>";
        public static string silver1Icon = "<img src='https://i.imgur.com/lKJIPSL.png' class=''>";
        public static string unrankedIcon = "<img src='https://i.imgur.com/2OIdZ5s.png' class=''>";


        public struct WeaponSpeedStats
        {
            public double Running { get; }
            public double Walking { get; }

            public WeaponSpeedStats(double running, double walking)
            {
                Running = running;
                Walking = walking;
            }

            public double GetSpeed(bool isWalking)
            {
                return isWalking ? Walking : Running;
            }
        }

        Dictionary<string, WeaponSpeedStats> weaponSpeedLookup = new Dictionary<string, WeaponSpeedStats>
        {
            {"weapon_glock", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_usp_silencer", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_hkp2000", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_elite", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_p250", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_fiveseven", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_cz75a", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_deagle", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_revolver", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_nova", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_xm1014", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_sawedoff", new WeaponSpeedStats(210.00, 109.20)},
            {"weapon_mag7", new WeaponSpeedStats(225.00, 117.00)},
            {"weapon_m249", new WeaponSpeedStats(195.00, 101.40)},
            {"weapon_negev", new WeaponSpeedStats(150.00, 78.00)},
            {"weapon_mac10", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_mp7", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_mp9", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_mp5sd", new WeaponSpeedStats(235.00, 122.20)},
            {"weapon_ump45", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_p90", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_bizon", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_galilar", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_famas", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_ak47", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_m4a4", new WeaponSpeedStats(225.00, 117.00)},
            {"weapon_m4a1_silencer", new WeaponSpeedStats(225.00, 117.00)},
            {"weapon_ssg08", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_sg556", new WeaponSpeedStats(210.00, 109.20)},
            {"weapon_aug", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_awp", new WeaponSpeedStats(200.00, 104.00)},
            {"weapon_g3sg1", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_scar20", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_molotov", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_incgrenade", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_decoy", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_flashbang", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_hegrenade", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_smokegrenade", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_taser", new WeaponSpeedStats(245.00, 127.40)},
            {"item_healthshot", new WeaponSpeedStats(250.00, 130.00)},
            {"weapon_knife_t", new WeaponSpeedStats(250.00, 130.00)},
            {"weapon_knife", new WeaponSpeedStats(250.00, 130.00)},
            {"weapon_c4", new WeaponSpeedStats(250.00, 130.00)},
            {"no_knife", new WeaponSpeedStats(260.00, 130.56)} //no knife
        };

        private readonly WIN_LINUX<int> OnCollisionRulesChangedOffset = new WIN_LINUX<int>(175, 174);
    }
}