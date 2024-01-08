using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SharpTimer
{
    [MinimumApiVersion(141)]
    public partial class SharpTimer : BasePlugin
    {
        public override void Load(bool hotReload)
        {
            SharpTimerDebug("Loading Plugin...");

            gameDir = Server.GameDirectory;
            SharpTimerDebug($"Set gameDir to {gameDir}");

            string recordsFileName = "SharpTimer/player_records.json";
            playerRecordsPath = Path.Join(gameDir + "/csgo/cfg", recordsFileName);
            SharpTimerDebug($"Set playerRecordsPath to {playerRecordsPath}");

            string mysqlConfigFileName = "SharpTimer/mysqlConfig.json";
            mySQLpath = Path.Join(gameDir + "/csgo/cfg", mysqlConfigFileName);
            SharpTimerDebug($"Set mySQLpath to {mySQLpath}");

            currentMapName = Server.MapName;

            RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);

            RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                var player = @event.Userid;

                if (player.IsBot || !player.IsValid)
                {
                    return HookResult.Continue;
                }
                else
                {
                    connectedPlayers[player.Slot] = player;

                    SharpTimerDebug($"Added player {player.PlayerName} with UserID {player.UserId} to connectedPlayers");
                    playerTimers[player.Slot] = new PlayerTimerInfo();

                    if (connectMsgEnabled == true) Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{player.PlayerName} {ChatColors.White}connected!");

                    if (cmdJoinMsgEnabled == true)
                    {
                        PrintAllEnabledCommands(player);
                    }

                    playerTimers[player.Slot].MovementService = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices!.Handle);
                    playerTimers[player.Slot].StageTimes = new Dictionary<int, int>();
                    playerTimers[player.Slot].StageVelos = new Dictionary<int, string>();
                    playerTimers[player.Slot].CurrentMapStage = 0;
                    playerTimers[player.Slot].CurrentMapCheckpoint = 0;

                    _ = IsPlayerATester(player.SteamID.ToString(), player.Slot);

                    if (removeLegsEnabled == true) player.PlayerPawn.Value.Render = Color.FromArgb(254, 254, 254, 254);

                    //PlayerSettings
                    if (useMySQL == true)
                    {
                        //_ = GetPlayerSettingFromDatabase(player, "Azerty");
                        //_ = GetPlayerSettingFromDatabase(player, "HideTimerHud");
                        //_ = GetPlayerSettingFromDatabase(player, "TimesConnected");
                        //_ = GetPlayerSettingFromDatabase(player, "SoundsEnabled");
                        //_ = SavePlayerStatToDatabase(player.SteamID.ToString(), "MouseSens", player.GetConVarValue("sensitivity").ToString());
                    }

                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                LoadMapData();
                SharpTimerDebug($"Loading MapData on RoundStart...");

                SharpTimerDebug("Re-Executing custom_exec with 2sec delay...");
                var custom_exec_delay = AddTimer(2.0f, () =>
                {
                    SharpTimerDebug("Re-Executing SharpTimer/custom_exec");
                    Server.ExecuteCommand("execifexists SharpTimer/custom_exec.cfg");
                });
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerSpawned>((@event, info) =>
            {
                if (@event.Userid == null) return HookResult.Continue;

                var player = @event.Userid;

                if (player.IsBot || !player.IsValid || player == null)
                {
                    return HookResult.Continue;
                }
                else
                {
                    if (removeCollisionEnabled == true && player.PlayerPawn != null)
                    {
                        RemovePlayerCollision(player);
                    }
                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                var player = @event.Userid;

                if (player.IsBot || !player.IsValid)
                {
                    return HookResult.Continue;
                }
                else
                {
                    if (connectedPlayers.TryGetValue(player.Slot, out var connectedPlayer))
                    {
                        connectedPlayers.Remove(player.Slot);
                        playerTimers.Remove(player.Slot);
                        playerCheckpoints.Remove(player.Slot);
                        SharpTimerDebug($"Removed player {connectedPlayer.PlayerName} with UserID {connectedPlayer.UserId} from connectedPlayers");

                        if (connectMsgEnabled == true) Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{connectedPlayer.PlayerName} {ChatColors.White}disconnected!");
                    }

                    return HookResult.Continue;
                }
            });

            RegisterListener<Listeners.OnTick>(() =>
            {
                foreach (var playerEntry in connectedPlayers)
                {
                    var player = playerEntry.Value;
                    if (player == null) continue;
                    TimerOnTick(player, player.Slot);
                }
            });

            HookEntityOutput("trigger_multiple", "OnStartTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                try
                {
                    if (activator == null || output == null || value == null || caller == null)
                    {
                        SharpTimerDebug("Null reference detected in trigger_multiple OnStartTouch hook.");
                        return HookResult.Continue;
                    }

                    if (activator.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                    var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value.Handle);

                    if (player == null)
                    {
                        SharpTimerDebug("Player is null in trigger_multiple OnStartTouch hook.");
                        return HookResult.Continue;
                    }

                    if (!IsAllowedPlayer(player) || caller.Entity.Name == null) return HookResult.Continue;

                    if (useStageTriggers == true && stageTriggers.ContainsKey(caller.Handle) && playerTimers[player.Slot].IsTimerBlocked == false && playerTimers[player.Slot].IsTimerRunning == true && IsAllowedPlayer(player))
                    {
                        if (stageTriggers[caller.Handle] == 1)
                        {
                            playerTimers[player.Slot].CurrentMapStage = 1;
                        }
                        else
                        {
                            HandlePlayerStageTimes(player, caller.Handle);
                        }
                    }

                    if (useCheckpointTriggers == true && cpTriggers.ContainsKey(caller.Handle) && playerTimers[player.Slot].IsTimerBlocked == false && playerTimers[player.Slot].IsTimerRunning == true && IsAllowedPlayer(player))
                    {
                        HandlePlayerCheckpointTimes(player, caller.Handle);
                    }

                    if (IsValidEndTriggerName(caller.Entity.Name.ToString()) && IsAllowedPlayer(player) && playerTimers[player.Slot].IsTimerRunning && !playerTimers[player.Slot].IsTimerBlocked)
                    {
                        OnTimerStop(player);
                        SharpTimerDebug($"Player {player.PlayerName} entered EndZone");
                        return HookResult.Continue;
                    }

                    if (IsValidStartTriggerName(caller.Entity.Name.ToString()) && IsAllowedPlayer(player))
                    {
                        playerTimers[player.Slot].IsTimerRunning = false;
                        playerTimers[player.Slot].TimerTicks = 0;
                        playerTimers[player.Slot].IsBonusTimerRunning = false;
                        playerTimers[player.Slot].BonusTimerTicks = 0;
                        playerCheckpoints.Remove(player.Slot);
                        if (stageTriggerCount != 0 && useStageTriggers == true)
                        {
                            playerTimers[player.Slot].StageTimes.Clear();
                            playerTimers[player.Slot].StageVelos.Clear();
                            playerTimers[player.Slot].CurrentMapStage = stageTriggers.GetValueOrDefault(caller.Handle, 0);
                        }
                        else if (cpTriggerCount != 0 && useStageTriggers == false)
                        {
                            playerTimers[player.Slot].StageTimes.Clear();
                            playerTimers[player.Slot].StageVelos.Clear();
                            playerTimers[player.Slot].CurrentMapCheckpoint = 0;
                        }

                        if (maxStartingSpeedEnabled == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed)
                        {
                            AdjustPlayerVelocity(player, maxStartingSpeed);
                        }

                        SharpTimerDebug($"Player {player.PlayerName} entered StartZone");

                        return HookResult.Continue;
                    }

                    var (validEndBonus, endBonusX) = IsValidEndBonusTriggerName(caller.Entity.Name.ToString(), player.Slot);

                    if (validEndBonus && IsAllowedPlayer(player) && playerTimers[player.Slot].IsBonusTimerRunning && !playerTimers[player.Slot].IsTimerBlocked)
                    {
                        OnBonusTimerStop(player, endBonusX);
                        SharpTimerDebug($"Player {player.PlayerName} entered Bonus{endBonusX} EndZone");
                        return HookResult.Continue;
                    }

                    var (validStartBonus, startBonusX) = IsValidStartBonusTriggerName(caller.Entity.Name.ToString());

                    if (validStartBonus && IsAllowedPlayer(player))
                    {
                        playerTimers[player.Slot].IsTimerRunning = false;
                        playerTimers[player.Slot].TimerTicks = 0;
                        playerTimers[player.Slot].IsBonusTimerRunning = false;
                        playerTimers[player.Slot].BonusTimerTicks = 0;
                        playerCheckpoints.Remove(player.Slot);

                        if (maxStartingSpeedEnabled == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed)
                        {
                            AdjustPlayerVelocity(player, maxStartingSpeed);
                        }
                        SharpTimerDebug($"Player {player.PlayerName} entered Bonus{startBonusX} StartZone");
                        return HookResult.Continue;
                    }
                    return HookResult.Continue;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Exception in trigger_multiple OnStartTouch hook: {ex.Message}");
                    return HookResult.Continue;
                }
            });

            HookEntityOutput("trigger_multiple", "OnEndTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                try
                {
                    if (activator == null || output == null || value == null || caller == null)
                    {
                        SharpTimerDebug("Null reference detected in trigger_multiple OnEndTouch hook.");
                        return HookResult.Continue;
                    }

                    if (activator.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                    var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value.Handle);

                    if (player == null)
                    {
                        SharpTimerDebug("Player is null in trigger_multiple OnEndTouch hook.");
                        return HookResult.Continue;
                    }

                    if (!IsAllowedPlayer(player) || caller.Entity.Name == null) return HookResult.Continue;

                    if (IsValidStartTriggerName(caller.Entity.Name.ToString()) && IsAllowedPlayer(player) && !playerTimers[player.Slot].IsTimerBlocked)
                    {
                        OnTimerStart(player);

                        if (maxStartingSpeedEnabled == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed)
                        {
                            AdjustPlayerVelocity(player, maxStartingSpeed);
                        }

                        SharpTimerDebug($"Player {player.PlayerName} left StartZone");

                        return HookResult.Continue;
                    }

                    var (validStartBonus, StartBonusX) = IsValidStartBonusTriggerName(caller.Entity.Name.ToString());

                    if (validStartBonus == true && IsAllowedPlayer(player) && !playerTimers[player.Slot].IsTimerBlocked)
                    {
                        OnTimerStart(player, StartBonusX);

                        if (maxStartingSpeedEnabled == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed)
                        {
                            AdjustPlayerVelocity(player, maxStartingSpeed);
                        }

                        SharpTimerDebug($"Player {player.PlayerName} left BonusStartZone {StartBonusX}");

                        return HookResult.Continue;
                    }
                    return HookResult.Continue;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Exception in trigger_multiple OnEndTouch hook: {ex.Message}");
                    return HookResult.Continue;
                }
            });

            HookEntityOutput("trigger_teleport", "OnEndTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                try
                {
                    if (activator == null || output == null || value == null || caller == null)
                    {
                        SharpTimerDebug("Null reference detected in trigger_teleport hook.");
                        return HookResult.Continue;
                    }

                    if (activator.DesignerName != "player" || resetTriggerTeleportSpeedEnabled == false)
                    {
                        return HookResult.Continue;
                    }

                    var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value.Handle);

                    if (player == null)
                    {
                        SharpTimerDebug("Player is null in trigger_teleport hook.");
                        return HookResult.Continue;
                    }

                    if (!IsAllowedPlayer(player)) return HookResult.Continue;

                    if (IsAllowedPlayer(player) && resetTriggerTeleportSpeedEnabled == true && currentMapOverrideDisableTelehop == false) AdjustPlayerVelocity(player, 0);

                    return HookResult.Continue;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Exception in trigger_teleport hook: {ex.Message}");
                    return HookResult.Continue;
                }
            });

            HookEntityOutput("trigger_push", "OnStartTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                try
                {
                    if (activator == null || output == null || value == null || caller == null)
                    {
                        SharpTimerDebug("Null reference detected in trigger_push hook.");
                        return HookResult.Continue;
                    }

                    if (activator.DesignerName != "player" || triggerPushFixEnabled == false)
                    {
                        return HookResult.Continue;
                    }

                    var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value.Handle);

                    if (player == null)
                    {
                        SharpTimerDebug("Player is null in trigger_push hook.");
                        return HookResult.Continue;
                    }

                    if (!IsAllowedPlayer(player)) return HookResult.Continue;

                    if (triggerPushData.TryGetValue(caller.Handle, out TriggerPushData TriggerPushData) && triggerPushFixEnabled == true)
                    {
                        player.PlayerPawn.Value.AbsVelocity.X += TriggerPushData.PushDirEntitySpace.X * TriggerPushData.PushSpeed;
                        player.PlayerPawn.Value.AbsVelocity.Y += TriggerPushData.PushDirEntitySpace.Y * TriggerPushData.PushSpeed;
                        player.PlayerPawn.Value.AbsVelocity.Z += TriggerPushData.PushDirEntitySpace.Z * TriggerPushData.PushSpeed;
                        SharpTimerDebug($"trigger_push OnStartTouch Player velocity adjusted for {player.PlayerName} by {TriggerPushData.PushSpeed}");
                    }

                    return HookResult.Continue;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Exception in trigger_push hook: {ex.Message}");
                    return HookResult.Continue;
                }
            });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && disableDamage == true)
            {
                VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(this.OnTakeDamage, HookMode.Pre);
            }
            else
            {
                SharpTimerDebug($"Platform is Windows. Blocking TakeDamage hook");
            }

            SharpTimerDebug("Plugin Loaded");
        }

        public override void Unload(bool hotReload)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && disableDamage == true)
            {
                VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(this.OnTakeDamage, HookMode.Pre);
            }
        }

        private HookResult OnTakeDamage(dynamic h) {
            if (disableDamage == false || h == null) return HookResult.Continue;

            var damageInfoParam = h.GetParam<CTakeDamageInfo>(1);

            if (damageInfoParam == null) return HookResult.Continue;

            if (disableDamage == true) damageInfoParam.Damage = 0;

            return HookResult.Continue;
        }
    }
}
