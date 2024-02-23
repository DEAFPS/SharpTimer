using CounterStrikeSharp.API.Core;

namespace SharpTimer.Data
{
    public class PlayerTimerInfo
    {
        //timer
        public bool IsTimerRunning { get; set; }
        public bool IsTimerBlocked { get; set; }
        public int TimerTicks { get; set; }
        public bool IsBonusTimerRunning { get; set; }
        public int BonusTimerTicks { get; set; }
        public int BonusStage { get; set; }

        //replay
        public bool IsReplaying { get; set; }
        public bool IsRecordingReplay { get; set; }

        //hud
        public string? ReplayHUDString { get; set; }
        public string? RankHUDIcon { get; set; }
        public string? CachedRank { get; set; }
        public bool IsRankPbCached { get; set; }
        public bool IsSpecTargetCached { get; set; }
        public string? PreSpeed { get; set; }
        public string? CachedPB { get; set; }
        public string? CachedMapPlacement { get; set; }

        //logic
        public int? TicksInAir { get; set; }
        public int? TicksOnGround { get; set; }
        public int CheckpointIndex { get; set; }
        public Dictionary<int, int>? StageTimes { get; set; }
        public Dictionary<int, string>? StageVelos { get; set; }
        public int CurrentMapStage { get; set; }
        public int CurrentMapCheckpoint { get; set; }
        public CCSPlayer_MovementServices? MovementService { get; set; }

        //player settings/stats
        public bool Azerty { get; set; }
        public bool HideTimerHud { get; set; }
        public bool HideKeys { get; set; }
        public bool SoundsEnabled { get; set; }
        public bool HideJumpStats { get; set; }
        public int TimesConnected { get; set; }
        public int TicksSinceLastCmd { get; set; }
        public int TicksSinceLastRankUpdate { get; set; }

        //super special stuff for testers
        public bool IsTester { get; set; }
        public string? TesterSparkleGif { get; set; }
        public string? TesterPausedGif { get; set; }

        //vip stuff 
        public bool IsVip { get; set; }
        public string? VipReplayGif { get; set; }
        public string? VipPausedGif { get; set; }

        //admin stuff
        public bool IsNoclipEnabled { get; set; }
        public bool IsAddingStartZone { get; set; }
        public string? StartZoneC1 { get; set; }
        public string? StartZoneC2 { get; set; }
        public bool IsAddingEndZone { get; set; }
        public string? EndZoneC1 { get; set; }
        public string? EndZoneC2 { get; set; }
        public string? RespawnPos { get; set; }
        public Dictionary<int, CBeam>? ZoneToolWire { get; set; }

        //set respawn
        public string? SetRespawnPos { get; set; }
        public string? SetRespawnAng { get; set; }
    }
}
