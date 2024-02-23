using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    [MinimumApiVersion(165)]
    public partial class SharpTimer : BasePlugin
    {
        public override void Load(bool hotReload)
        {
            SharpTimerDebug("Loading Plugin...");

            defaultServerHostname = ConVar.Find("hostname")?.StringValue ?? throw new InvalidOperationException($"The 'hostname' ConVar is not set.");
            Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");

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
                if (@event.Userid.IsValid)
                {
                    var player = @event.Userid;

                    if (!player.IsValid || player.IsBot)
                    {
                        return HookResult.Continue;
                    }
                    else
                    {
                        OnPlayerConnect(player);
                        return HookResult.Continue;
                    }
                }
                else
                {
                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventPlayerTeam>((@event, info) =>
            {
                if (@event.Userid.IsValid)
                {
                    var bot = @event.Userid;

                    if (bot.IsValid && bot.IsBot)
                    {
                        if (startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile)
                        {
                            AddTimer(4.0f, () =>
                            {
                                Server.ExecuteCommand($"kickid {bot.Slot}");
                                SharpTimerDebug($"Kicking unused bot on spawn...");
                            });
                            return HookResult.Continue;
                        }
                    }
                    return HookResult.Continue;
                }
                else
                {
                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                LoadMapData();
                SharpTimerDebug($"Loading MapData on RoundStart...");
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerSpawned>((@event, info) =>
            {
                if (@event.Userid.IsValid)
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
                        specTargets[player.Pawn.Value.EntityHandle.Index] = new CCSPlayerController(player.Handle);
                        return HookResult.Continue;
                    }
                }
                else
                {
                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                if (@event.Userid.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsBot || !player.IsValid)
                    {
                        return HookResult.Continue;
                    }
                    else
                    {
                        OnPlayerDisconnect(player);
                        return HookResult.Continue;
                    }
                }
                else
                {
                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventPlayerJump>((@event, info) =>
            {
                if (@event.Userid.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsBot || !player.IsValid)
                    {
                        return HookResult.Continue;
                    }
                    else
                    {
                        if (jumpStatsEnabled == true) OnJumpStatJumped(player);
                        return HookResult.Continue;
                    }
                }
                else
                {
                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventPlayerSound>((@event, info) =>
            {
                if (@event.Userid.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsBot || !player.IsValid)
                    {
                        return HookResult.Continue;
                    }
                    else
                    {
                        if (jumpStatsEnabled == true && @event.Step == true) OnJumpStatSound(player);
                        return HookResult.Continue;
                    }
                }
                else
                {
                    return HookResult.Continue;
                }
            });

            RegisterListener<Listeners.OnTick>(TimerOnTick);

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
                            return HookResult.Continue;
                        }
                        else
                        {
                            _ = HandlePlayerStageTimes(player, caller.Handle);
                            return HookResult.Continue;
                        }
                    }

                    if (useCheckpointTriggers == true && cpTriggers.ContainsKey(caller.Handle) && playerTimers[player.Slot].IsTimerBlocked == false && playerTimers[player.Slot].IsTimerRunning == true && IsAllowedPlayer(player))
                    {
                        _ = HandlePlayerCheckpointTimes(player, caller.Handle);
                        return HookResult.Continue;
                    }

                    if (IsValidEndTriggerName(caller.Entity.Name.ToString()) && IsAllowedPlayer(player) && playerTimers[player.Slot].IsTimerRunning && !playerTimers[player.Slot].IsTimerBlocked)
                    {
                        OnTimerStop(player);
                        if (enableReplays) OnRecordingStop(player);
                        SharpTimerDebug($"Player {player.PlayerName} entered EndZone");
                        return HookResult.Continue;
                    }

                    if (IsValidStartTriggerName(caller.Entity.Name.ToString()) && IsAllowedPlayer(player))
                    {
                        if (!playerTimers[player.Slot].IsTimerBlocked)
                        {
                            playerCheckpoints.Remove(player.Slot);
                        }
                        playerTimers[player.Slot].TimerTicks = 0;
                        playerTimers[player.Slot].BonusTimerTicks = 0;
                        playerTimers[player.Slot].IsTimerRunning = false;
                        playerTimers[player.Slot].IsBonusTimerRunning = false;
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

                        if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length()) > maxStartingSpeed) ||
                            (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed))
                        {
                            Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                            adjustVelocity(player, maxStartingSpeed, false);
                        }

                        SharpTimerDebug($"Player {player.PlayerName} entered StartZone");

                        return HookResult.Continue;
                    }

                    var (validEndBonus, endBonusX) = IsValidEndBonusTriggerName(caller.Entity.Name.ToString(), player.Slot);

                    if (validEndBonus && IsAllowedPlayer(player) && playerTimers[player.Slot].IsBonusTimerRunning && !playerTimers[player.Slot].IsTimerBlocked)
                    {
                        OnBonusTimerStop(player, endBonusX);
                        if (enableReplays) OnRecordingStop(player);
                        SharpTimerDebug($"Player {player.PlayerName} entered Bonus{endBonusX} EndZone");
                        return HookResult.Continue;
                    }

                    var (validStartBonus, startBonusX) = IsValidStartBonusTriggerName(caller.Entity.Name.ToString());

                    if (validStartBonus && IsAllowedPlayer(player))
                    {
                        if (!playerTimers[player.Slot].IsTimerBlocked)
                        {
                            playerCheckpoints.Remove(player.Slot);
                        }
                        playerTimers[player.Slot].TimerTicks = 0;
                        playerTimers[player.Slot].BonusTimerTicks = 0;
                        playerTimers[player.Slot].IsTimerRunning = false;
                        playerTimers[player.Slot].IsBonusTimerRunning = false;

                        if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length()) > maxStartingSpeed) ||
                            (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed))
                        {
                            Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                            adjustVelocity(player, maxStartingSpeed, false);
                        }
                        SharpTimerDebug($"Player {player.PlayerName} entered Bonus{startBonusX} StartZone");
                        return HookResult.Continue;
                    }

                    if (IsValidStopTriggerName(caller.Entity.Name.ToString()))
                    {
                        playerTimers[player.Slot].TimerTicks = 0;
                        playerTimers[player.Slot].BonusTimerTicks = 0;
                        playerTimers[player.Slot].IsTimerRunning = false;
                        playerTimers[player.Slot].IsBonusTimerRunning = false;
                        player.PrintToChat(msgPrefix + $"Timer cancelled due to illegal skip attempt");
                    }

                    if (IsValidStopTriggerName(caller.Entity.Name.ToString()))
                    {
                        playerTimers[player.Slot].TimerTicks = 0;
                        playerTimers[player.Slot].BonusTimerTicks = 0;
                        playerTimers[player.Slot].IsTimerRunning = false;
                        playerTimers[player.Slot].IsBonusTimerRunning = false;
                        RespawnPlayer(player);
                        player.PrintToChat(msgPrefix + $"You got reset due to illegal skip attempt");
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
                        if (enableReplays) OnRecordingStart(player);

                        if (((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length()) > maxStartingSpeed) ||
                            (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed)) &&
                            !currentMapOverrideMaxSpeedLimit.Contains(caller.Entity.Name.ToString()) && currentMapOverrideMaxSpeedLimit != null)
                        {
                            Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                            adjustVelocity(player, maxStartingSpeed, false);
                        }

                        SharpTimerDebug($"Player {player.PlayerName} left StartZone");

                        return HookResult.Continue;
                    }

                    var (validStartBonus, StartBonusX) = IsValidStartBonusTriggerName(caller.Entity.Name.ToString());

                    if (validStartBonus == true && IsAllowedPlayer(player) && !playerTimers[player.Slot].IsTimerBlocked)
                    {
                        OnTimerStart(player, StartBonusX);
                        if (enableReplays) OnRecordingStart(player, StartBonusX);

                        if (((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length()) > maxStartingSpeed) ||
                            (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed)) &&
                            !currentMapOverrideMaxSpeedLimit.Contains(caller.Entity.Name.ToString()) && currentMapOverrideMaxSpeedLimit != null)
                        {
                            Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                            adjustVelocity(player, maxStartingSpeed, false);
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

                    if (IsAllowedPlayer(player) && resetTriggerTeleportSpeedEnabled && currentMapOverrideDisableTelehop != null)
                    {
                        /* string triggerName = caller.Entity.Name.ToString();
                        if (!currentMapOverrideDisableTelehop.Contains(triggerName))
                        {
                            Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                            adjustVelocity(player, 0, false);
                        } */
                        if (!currentMapOverrideDisableTelehop)
                        {
                            Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                            adjustVelocity(player, 0, false);
                        }
                    }

                    return HookResult.Continue;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Exception in trigger_teleport hook: {ex.Message}");
                    return HookResult.Continue;
                }
            });

            HookEntityOutput("trigger_teleport", "OnStartTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                try
                {
                    if (activator == null || output == null || value == null || caller == null)
                    {
                        SharpTimerDebug("Null reference detected in trigger_teleport hook.");
                        return HookResult.Continue;
                    }

                    if (activator.DesignerName != "player")
                    {
                        SharpTimerDebug("activator.DesignerName != player in trigger_teleport hook.");
                        return HookResult.Continue;
                    }

                    var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value.Handle);

                    if (player == null)
                    {

                        return HookResult.Continue;
                    }

                    if (!IsAllowedPlayer(player))
                    {
                        SharpTimerDebug("Player not allowed in trigger_teleport hook.");
                        return HookResult.Continue;
                    }

                    if (jumpStatsEnabled) InvalidateJS(player.Slot);

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

                    if (triggerPushData.TryGetValue(caller.Handle, out TriggerPushData TriggerPushData) && triggerPushFixEnabled == true && currentMapOverrideTriggerPushFix == false)
                    {
                        if (player.PlayerPawn.Value.AbsVelocity.Length() > TriggerPushData.PushSpeed)
                        {
                            player.PlayerPawn.Value.AbsVelocity.X += TriggerPushData.PushDirEntitySpace.X * TriggerPushData.PushSpeed;
                            player.PlayerPawn.Value.AbsVelocity.Y += TriggerPushData.PushDirEntitySpace.Y * TriggerPushData.PushSpeed;
                            player.PlayerPawn.Value.AbsVelocity.Z += TriggerPushData.PushDirEntitySpace.Z * TriggerPushData.PushSpeed;
                            SharpTimerDebug($"trigger_push OnStartTouch Player velocity adjusted for {player.PlayerName} by {TriggerPushData.PushSpeed}");
                        }
                    }

                    return HookResult.Continue;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Exception in trigger_push hook: {ex.Message}");
                    return HookResult.Continue;
                }
            });

            Server.NextFrame(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && disableDamage == true)
                {
                    VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(this.OnTakeDamage, HookMode.Pre);
                }
                else if (disableDamage == true)
                {
                    SharpTimerDebug($"disableDamage: Platform is Windows. Using different method...");
                    RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
                }
            });

            AddCommandListener("say", OnPlayerChatAll);
            AddCommandListener("say_team", OnPlayerChatTeam);

            SharpTimerConPrint("Plugin Loaded");
        }

        public override void Unload(bool hotReload)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && disableDamage == true)
            {
                VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
            }

            RemoveCommandListener("say", OnPlayerChatAll, HookMode.Pre);
            RemoveCommandListener("say_team", OnPlayerChatTeam, HookMode.Pre);

            SharpTimerConPrint("Plugin Unloaded");
        }

        HookResult OnTakeDamage(DynamicHook h)
        {
            if (disableDamage == false || h == null) return HookResult.Continue;

            var damageInfoParam = h.GetParam<CTakeDamageInfo>(1);

            if (damageInfoParam == null) return HookResult.Continue;

            if (disableDamage == true) damageInfoParam.Damage = 0;

            return HookResult.Continue;
        }

        HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            if (disableDamage == true)
            {
                var player = @event.Userid;
                Vector playerSpeed = player.PlayerPawn.Value.AbsVelocity ?? new Vector(0, 0, 0);

                if (!player.IsValid)
                    return HookResult.Continue;

                player.PlayerPawn.Value.Health = 696969;
                player.PlayerPawn.Value.ArmorValue = 696969;

                if (!player.PawnHasHelmet) player.GiveNamedItem("item_assaultsuit");

                Server.NextFrame(() =>
                {
                    if (IsAllowedPlayer(player)) AdjustPlayerVelocity(player, playerSpeed.Length(), true);
                });
            }

            return HookResult.Continue;
        }
    }
}
