using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
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

            defaultServerHostname = ConVar.Find("hostname").StringValue;
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
                var player = @event.Userid;

                if (player.IsBot || !player.IsValid)
                {
                    return HookResult.Continue;
                }
                else
                {
                    OnPlayerConnect(player);
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
                    specTargets[player.Pawn.Value.EntityHandle.Index] = new CCSPlayerController(player.Handle);
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
                    OnPlayerDisconnect(player);
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
                        if (enableReplays) OnRecordingStop(player);
                        SharpTimerDebug($"Player {player.PlayerName} entered EndZone");
                        return HookResult.Continue;
                    }

                    if (IsValidStartTriggerName(caller.Entity.Name.ToString()) && IsAllowedPlayer(player))
                    {
                        if (!playerTimers[player.Slot].IsTimerBlocked) playerCheckpoints.Remove(player.Slot);
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
                        if (!playerTimers[player.Slot].IsTimerBlocked) playerCheckpoints.Remove(player.Slot);
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

                    if (!IsAllowedPlayer(player) || caller.Entity.Name == null) return HookResult.Continue;

                    if (IsAllowedPlayer(player) && resetTriggerTeleportSpeedEnabled && currentMapOverrideDisableTelehop != null)
                    {
                        string triggerName = caller.Entity.Name.ToString();
                        if (!currentMapOverrideDisableTelehop.Contains(triggerName))
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
                SharpTimerDebug($"TakeDamage hook...");
                VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook((h =>
                {
                    if (disableDamage == false || h == null) return HookResult.Continue;

                    var damageInfoParam = h.GetParam<CTakeDamageInfo>(1);

                    if (damageInfoParam == null) return HookResult.Continue;

                    if (disableDamage == true) damageInfoParam.Damage = 0;

                    return HookResult.Continue;
                }), HookMode.Pre);
            }
            else
            {
                SharpTimerDebug($"Platform is Windows. Blocking TakeDamage hook");
            }

            SharpTimerDebug("Plugin Loaded");
        }
    }
}
