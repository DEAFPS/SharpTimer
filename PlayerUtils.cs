using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text;
using System.Text.Json;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private bool IsAllowedPlayer(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.Pawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive || player.IsBot)
            {
                return false;
            }

            CsTeam teamNum = (CsTeam)player.TeamNum;
            bool isTeamValid = teamNum == CsTeam.CounterTerrorist || teamNum == CsTeam.Terrorist;

            bool isTeamSpectatorOrNone = teamNum != CsTeam.Spectator && teamNum != CsTeam.None;
            bool isConnected = connectedPlayers.ContainsKey(player.Slot) && playerTimers.ContainsKey(player.Slot);

            return isTeamValid && isTeamSpectatorOrNone && isConnected;
        }

        private bool IsAllowedSpectator(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.IsBot)
            {
                return false;
            }

            CsTeam teamNum = (CsTeam)player.TeamNum;
            bool isTeamValid = teamNum == CsTeam.Spectator;
            bool isConnected = connectedPlayers.ContainsKey(player.Slot) &&
                                playerTimers.ContainsKey(player.Slot) &&
                                specTargets.ContainsKey(player.Pawn.Value.ObserverServices.ObserverTarget.Index);

            return isTeamValid && isConnected;
        }

        async Task IsPlayerATester(string steamId64, int playerSlot)
        {
            try
            {
                string response = await httpClient.GetStringAsync(testerPersonalGifsSource);

                using (JsonDocument jsonDocument = JsonDocument.Parse(response))
                {
                    playerTimers[playerSlot].IsTester = jsonDocument.RootElement.TryGetProperty(steamId64, out JsonElement steamData);
                    //playerTimers[playerSlot].IsTester = false;

                    if (playerTimers[playerSlot].IsTester)
                    {
                        if (steamData.TryGetProperty("SmolGif", out JsonElement smolGifElement))
                        {
                            playerTimers[playerSlot].TesterSparkleGif = smolGifElement.GetString() ?? "";
                        }

                        if (steamData.TryGetProperty("BigGif", out JsonElement bigGifElement))
                        {
                            playerTimers[playerSlot].TesterPausedGif = bigGifElement.GetString() ?? "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in IsPlayerATester: {ex.Message}");
            }
        }

        private void OnPlayerConnect(CCSPlayerController? player)
        {
            connectedPlayers[player.Slot] = new CCSPlayerController(player.Handle);
            playerTimers[player.Slot] = new PlayerTimerInfo();
            if (enableReplays) playerReplays[player.Slot] = new PlayerReplays();
            if (connectMsgEnabled == true) Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{player.PlayerName} {ChatColors.White}connected!");
            if (cmdJoinMsgEnabled == true) PrintAllEnabledCommands(player);
            playerTimers[player.Slot].MovementService = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices!.Handle);
            playerTimers[player.Slot].StageTimes = new Dictionary<int, int>();
            playerTimers[player.Slot].StageVelos = new Dictionary<int, string>();
            playerTimers[player.Slot].CurrentMapStage = 0;
            playerTimers[player.Slot].CurrentMapCheckpoint = 0;
            playerTimers[player.Slot].IsRecordingReplay = false;
            playerTimers[player.Slot].SetRespawnPos = null;
            playerTimers[player.Slot].SetRespawnAng = null;

            _ = IsPlayerATester(player.SteamID.ToString(), player.Slot);

            if (removeLegsEnabled == true) player.PlayerPawn.Value.Render = Color.FromArgb(254, 254, 254, 254);

            //PlayerSettings
            if (useMySQL) _ = GetPlayerStats(player, player.SteamID.ToString(), player.PlayerName, player.Slot);

            SharpTimerDebug($"Added player {player.PlayerName} with UserID {player.UserId} to connectedPlayers");
            SharpTimerDebug($"Total players connected: {connectedPlayers.Count}");
            SharpTimerDebug($"Total playerTimers: {playerTimers.Count}");
            SharpTimerDebug($"Total playerCheckpoints: {playerCheckpoints.Count}");
        }

        private void OnPlayerDisconnect(CCSPlayerController? player)
        {
            if (connectedPlayers.TryGetValue(player.Slot, out var connectedPlayer))
            {
                connectedPlayers.Remove(player.Slot);

                //schizo removing data from memory
                playerTimers[player.Slot] = new PlayerTimerInfo();
                playerTimers.Remove(player.Slot);

                //schizo removing data from memory
                playerCheckpoints[player.Slot] = new List<PlayerCheckpoint>();
                playerCheckpoints.Remove(player.Slot);

                specTargets.Remove(player.Pawn.Value.EntityHandle.Index);

                if (enableReplays)
                {
                    //schizo removing data from memory
                    playerReplays[player.Slot] = new PlayerReplays();
                    playerReplays.Remove(player.Slot);
                }

                SharpTimerDebug($"Removed player {connectedPlayer.PlayerName} with UserID {connectedPlayer.UserId} from connectedPlayers.");
                SharpTimerDebug($"Removed specTarget index {player.Pawn.Value.EntityHandle.Index} from specTargets.");
                SharpTimerDebug($"Total players connected: {connectedPlayers.Count}");
                SharpTimerDebug($"Total playerTimers: {playerTimers.Count}");
                SharpTimerDebug($"Total specTargets: {specTargets.Count}");

                if (connectMsgEnabled == true) Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{connectedPlayer.PlayerName} {ChatColors.White}disconnected!");
            }
        }

        public void TimerOnTick()
        {
            var updates = new Dictionary<int, PlayerTimerInfo>();
            foreach (CCSPlayerController player in connectedPlayers.Values)
            {
                if (player == null || !player.IsValid) continue;

                if ((CsTeam)player.TeamNum == CsTeam.Spectator)
                {
                    SpectatorOnTick(player);
                    continue;
                }

                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo playerTimer) && IsAllowedPlayer(player))
                {
                    if (!IsAllowedPlayer(player))
                    {
                        playerTimer.IsTimerRunning = false;
                        playerTimer.TimerTicks = 0;
                        playerCheckpoints.Remove(player.Slot);
                        playerTimer.TicksSinceLastCmd++;
                        continue;
                    }

                    //SharpTimerDebug($"Player Pawn Value.EntHandle.Index {player.Pawn.Value.EntityHandle.Index}");
                    //SharpTimerDebug($"Player Pawn Index {player.Pawn.Index}");
                    //SharpTimerDebug($"Player Pawn Value.Index {player.Pawn.Value.Index}");

                    bool isTimerRunning = playerTimer.IsTimerRunning;
                    int timerTicks = playerTimer.TimerTicks;
                    PlayerButtons? playerButtons = player.Buttons;

                    string formattedPlayerVel = Math.Round(use2DSpeed ? player.PlayerPawn.Value.AbsVelocity.Length2D()
                                                                        : player.PlayerPawn.Value.AbsVelocity.Length())
                                                                        .ToString("0000");
                    string formattedPlayerPre = Math.Round(ParseVector(playerTimer.PreSpeed ?? "0 0 0").Length2D()).ToString("000");
                    string playerTime = FormatTime(timerTicks);
                    string playerBonusTime = FormatTime(playerTimer.BonusTimerTicks);
                    string timerLine = playerTimer.IsBonusTimerRunning
                                        ? $" <font color='gray' class='fontSize-s'>Bonus: {playerTimer.BonusStage}</font> <font class='fontSize-l' color='{primaryHUDcolor}'>{playerBonusTime}</font> <br>"
                                        : isTimerRunning
                                            ? $" <font color='gray' class='fontSize-s'>{GetPlayerPlacement(player)}</font> <font class='fontSize-l' color='{primaryHUDcolor}'>{playerTime}</font>{((playerTimer.CurrentMapStage != 0 && useStageTriggers == true) ? $"<font color='gray' class='fontSize-s'> {playerTimer.CurrentMapStage}/{stageTriggerCount}</font>" : "")} <br>"
                                            : playerTimer.IsReplaying
                                                ? $" <font class='' color='red'>◉ REPLAY {FormatTime(playerReplays[player.Slot].CurrentPlaybackFrame)}</font> <br>"
                                                : "";

                    //string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")}<font class='fontSize-s' color='{tertiaryHUDcolor}'>Speed:</font> <font class='fontSize-l' color='{secondaryHUDcolor}'>{formattedPlayerVel}</font> <font class='fontSize-s' color='gray'>({formattedPlayerPre})</font>{(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")} <br>";
                    string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")}<font class='fontSize-s' color='{tertiaryHUDcolor}'>Speed:</font> {(playerTimer.IsReplaying ? "<font class=''" : "<font class='fontSize-l'")} color='{secondaryHUDcolor}'>{formattedPlayerVel}</font> <font class='fontSize-s' color='gray'>({formattedPlayerPre})</font>{(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")} <br>";
                    string infoLine = !playerTimer.IsReplaying
                                        ? $"{playerTimer.RankHUDString}" +
                                          $"{(currentMapTier != null ? $" | Tier: {currentMapTier}" : "")}" +
                                          $"{(currentMapType != null ? $" | {currentMapType}" : "")}" +
                                          $"{((currentMapType == null && currentMapTier == null) ? $" {currentMapName} " : "")} </font> "
                                        : $" <font class='fontSize-s' color='gray'>{playerTimers[player.Slot].ReplayHUDString}</font>";

                    string keysLineNoHtml = $"{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Duck) != 0 ? "C" : "_")}";

                    string hudContent = timerLine +
                                        veloLine +
                                        infoLine +
                                        ((playerTimer.IsTester && !isTimerRunning && !playerTimer.IsBonusTimerRunning && !playerTimer.IsReplaying) ? playerTimer.TesterPausedGif : "") +
                                        ((playerTimer.IsVip && !playerTimer.IsTester && !isTimerRunning && !playerTimer.IsBonusTimerRunning && !playerTimer.IsReplaying) ? $"<br><img src='https://i.imgur.com/{playerTimer.VipPausedGif}.gif'><br>" : "") +
                                        ((playerTimer.IsReplaying && playerTimer.VipReplayGif != "x") ? $"<br><img src='https://i.imgur.com/{playerTimer.VipReplayGif}.gif'><br>" : "");

                    updates[player.Slot] = playerTimer;

                    var @event = new EventShowSurvivalRespawnStatus(false)
                    {
                        LocToken = hudContent,
                        Duration = 999,
                        Userid = player
                    };

                    if (playerTimer.HideTimerHud != true && hudOverlayEnabled == true)
                    {
                        @event.FireEvent(false);
                    }

                    if (playerTimer.HideKeys != true && playerTimer.IsReplaying != true && keysOverlayEnabled == true)
                    {
                        player.PrintToCenter(keysLineNoHtml);
                    }

                    if (isTimerRunning)
                    {
                        playerTimer.TimerTicks++;
                    }
                    else if (playerTimer.IsBonusTimerRunning)
                    {
                        playerTimer.BonusTimerTicks++;
                    }

                    if (!useTriggers)
                    {
                        CheckPlayerCoords(player);
                    }

                    if (forcePlayerSpeedEnabled == true) ForcePlayerSpeed(player, player.Pawn.Value.WeaponServices.ActiveWeapon.Value.DesignerName);

                    if (playerTimer.RankHUDString == null && playerTimer.IsRankPbCached == false)
                    {
                        SharpTimerDebug($"{player.PlayerName} has rank and pb null... calling handler");
                        _ = RankCommandHandler(player, player.SteamID.ToString(), player.Slot, player.PlayerName, true);
                        playerTimer.IsRankPbCached = true;
                    }

                    if (playerTimer.IsSpecTargetCached == false || specTargets.ContainsKey(player.Pawn.Value.EntityHandle.Index) == false)
                    {
                        specTargets[player.Pawn.Value.EntityHandle.Index] = new CCSPlayerController(player.Handle);
                        playerTimer.IsSpecTargetCached = true;
                        SharpTimerDebug($"{player.PlayerName} was not in specTargets, adding...");
                    }

                    if (removeCollisionEnabled == true)
                    {
                        if (player.PlayerPawn.Value.Collision.CollisionGroup != (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING || player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup != (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING)
                        {
                            SharpTimerDebug($"{player.PlayerName} has wrong collision group... RemovePlayerCollision");
                            RemovePlayerCollision(player);
                        }
                    }

                    if (playerTimer.MovementService != null && removeCrouchFatigueEnabled == true)
                    {
                        if (playerTimer.MovementService.DuckSpeed != 7.0f)
                        {
                            playerTimer.MovementService.DuckSpeed = 7.0f;
                        }
                    }

                    if (((PlayerFlags)player.Pawn.Value.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND)
                    {
                        playerTimer.TicksInAir++;
                        if (playerTimer.TicksInAir == 1)
                        {
                            playerTimer.PreSpeed = $"{player.PlayerPawn.Value.AbsVelocity.X.ToString()} {player.PlayerPawn.Value.AbsVelocity.Y.ToString()} {player.PlayerPawn.Value.AbsVelocity.Z.ToString()}";
                        }
                    }
                    else
                    {
                        playerTimer.TicksInAir = 0;
                    }

                    if (enableReplays && !playerTimer.IsReplaying && timerTicks > 0 && playerTimer.IsRecordingReplay && !playerTimer.IsTimerBlocked) ReplayUpdate(player, timerTicks);
                    if (enableReplays && playerTimer.IsReplaying && !playerTimer.IsRecordingReplay && playerTimer.IsTimerBlocked)
                    {
                        ReplayPlay(player);
                    }
                    else
                    {
                        if (!playerTimer.IsTimerBlocked && player.PlayerPawn.Value.MoveType != MoveType_t.MOVETYPE_WALK) player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                    }

                    if (playerTimer.TicksSinceLastCmd < cmdCooldown) playerTimer.TicksSinceLastCmd++;

                    playerButtons = null;
                    formattedPlayerVel = null;
                    formattedPlayerPre = null;
                    playerTime = null;
                    playerBonusTime = null;
                    keysLineNoHtml = null;
                    hudContent = null;
                    @event = null;
                }
            }

            foreach (var update in updates)
            {
                playerTimers[update.Key] = update.Value;
            }
        }

        public void SpectatorOnTick(CCSPlayerController player)
        {
            if (!IsAllowedSpectator(player)) return;

            try
            {
                var target = specTargets[player.Pawn.Value.ObserverServices.ObserverTarget.Index];
                if (playerTimers.TryGetValue(target.Slot, out PlayerTimerInfo playerTimer) && IsAllowedPlayer(target))
                {
                    bool isTimerRunning = playerTimer.IsTimerRunning;
                    int timerTicks = playerTimer.TimerTicks;
                    PlayerButtons? playerButtons = target.Buttons;

                    string formattedPlayerVel = Math.Round(use2DSpeed ? target.PlayerPawn.Value.AbsVelocity.Length2D()
                                                                        : target.PlayerPawn.Value.AbsVelocity.Length())
                                                                        .ToString("0000");
                    string formattedPlayerPre = Math.Round(ParseVector(playerTimer.PreSpeed ?? "0 0 0").Length2D()).ToString("000");
                    string playerTime = FormatTime(timerTicks);
                    string playerBonusTime = FormatTime(playerTimer.BonusTimerTicks);
                    string timerLine = playerTimer.IsBonusTimerRunning
                                        ? $" <font color='gray' class='fontSize-s'>Bonus: {playerTimer.BonusStage}</font> <font class='fontSize-l' color='{primaryHUDcolor}'>{playerBonusTime}</font> <br>"
                                        : isTimerRunning
                                            ? $" <font color='gray' class='fontSize-s'>{GetPlayerPlacement(target)}</font> <font class='fontSize-l' color='{primaryHUDcolor}'>{playerTime}</font>{((playerTimer.CurrentMapStage != 0 && useStageTriggers == true) ? $"<font color='gray' class='fontSize-s'> {playerTimer.CurrentMapStage}/{stageTriggerCount}</font>" : "")} <br>"
                                            : playerTimer.IsReplaying
                                                ? $" <font class='' color='red'>◉ REPLAY {FormatTime(playerReplays[target.Slot].CurrentPlaybackFrame)}</font> <br>"
                                                : "";

                    //string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")}<font class='fontSize-s' color='{tertiaryHUDcolor}'>Speed:</font> <font class='' color='{secondaryHUDcolor}'>{formattedPlayerVel}</font> <font class='fontSize-s' color='gray'>({formattedPlayerPre})</font>{(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")} <br>";
                    string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")}<font class='fontSize-s' color='{tertiaryHUDcolor}'>Speed:</font> <font class='' color='{secondaryHUDcolor}'>{formattedPlayerVel}</font> <font class='fontSize-s' color='gray'>({formattedPlayerPre})</font>{(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")} <br>";
                    string infoLine = !playerTimer.IsReplaying
                                        ? $"{playerTimer.RankHUDString}" +
                                          $"{(currentMapTier != null ? $" | Tier: {currentMapTier}" : "")}" +
                                          $"{(currentMapType != null ? $" | {currentMapType}" : "")}" +
                                          $"{((currentMapType == null && currentMapTier == null) ? $" {currentMapName} " : "")} </font> <br>"
                                        : $" <font class='fontSize-s' color='gray'>{playerTimers[target.Slot].ReplayHUDString}</font> <br>";

                    string keysLine = $"<font class='fontSize-l' color='{secondaryHUDcolor}'>{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Duck) != 0 ? "C" : "_")}</font>";

                    string hudContent = timerLine +
                                        veloLine +
                                        infoLine +
                                        (keysOverlayEnabled == true ? keysLine : "") +
                                        ((playerTimer.IsTester && !isTimerRunning && !playerTimer.IsBonusTimerRunning && !playerTimer.IsReplaying) ? playerTimer.TesterPausedGif : "") +
                                        ((playerTimer.IsVip && !playerTimer.IsTester && !isTimerRunning && !playerTimer.IsBonusTimerRunning && !playerTimer.IsReplaying) ? $"<br><img src='https://i.imgur.com/{playerTimer.VipPausedGif}.gif'><br>" : "");

                    if (playerTimer.HideTimerHud != true && hudOverlayEnabled == true)
                    {
                        var @event = new EventShowSurvivalRespawnStatus(false)
                        {
                            LocToken = hudContent,
                            Duration = 999,
                            Userid = player
                        };
                        @event.FireEvent(false);
                        @event = null;
                    }

                    /* if (playerTimer.HideKeys != true && playerTimer.IsReplaying != true)
                    {
                        player.PrintToCenter(keysLine);
                    } */

                    playerButtons = null;
                    formattedPlayerVel = null;
                    formattedPlayerPre = null;
                    playerTime = null;
                    playerBonusTime = null;
                    keysLine = null;
                    hudContent = null;
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in SpectatorOnTick: {ex.Message}");
            }
        }

        public void PrintAllEnabledCommands(CCSPlayerController player)
        {
            SharpTimerDebug($"Printing Commands for {player.PlayerName}");
            player.PrintToChat($"{msgPrefix}Available Commands:");

            if (respawnEnabled)                             player.PrintToChat($"{msgPrefix}!r (css_r) - Respawns you");
            if (respawnEnabled && bonusRespawnPoses.Any())  player.PrintToChat($"{msgPrefix}!rb <#> / !b <#> (css_rb / css_b) - Respawns you to a bonus");
            if (topEnabled)                                 player.PrintToChat($"{msgPrefix}!top (css_top) - Lists top 10 records on this map");
            if (topEnabled && bonusRespawnPoses.Any())      player.PrintToChat($"{msgPrefix}!topbonus <#> (css_topbonus) - Lists top 10 records of a bonus");
            if (rankEnabled)                                player.PrintToChat($"{msgPrefix}!rank (css_rank) - Shows your current rank and pb");
            if (globalRankeEnabled)                         player.PrintToChat($"{msgPrefix}!points (css_points) - Prints top 10 points");
            if (goToEnabled)                                player.PrintToChat($"{msgPrefix}!goto <name> (css_goto) - Teleports you to a player");
            if (stageTriggerPoses.Any())                    player.PrintToChat($"{msgPrefix}!stage <#> (css_goto) - Teleports you to a stage");

            if (cpEnabled)
            {
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!saveloc (css_saveloc) - Saves a Loc" : "!cp (css_cp) - Sets a Checkpoint")}");
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!loadloc (css_loadloc) - Teleports you to the last Loc" : "!tp (css_tp) - Teleports you to the last Checkpoint")}");
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!prevloc (css_prevloc) - Teleports you one Loc back" : "!prevcp (css_prevcp) - Teleports you one Checkpoint back")}");
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!nextloc (css_nextloc) - Teleports you one Loc forward" : "!nextcp (css_nextcp) - Teleports you one Checkpoint forward")}");
            }
        }

        private void CheckPlayerCoords(CCSPlayerController? player)
        {
            if (player == null || !IsAllowedPlayer(player))
            {
                return;
            }

            Vector incorrectVector = new Vector(0, 0, 0);
            Vector? playerPos = player.Pawn?.Value.CBodyComponent?.SceneNode.AbsOrigin;

            if (playerPos == null || currentMapStartC1 == incorrectVector || currentMapStartC2 == incorrectVector ||
                currentMapEndC1 == incorrectVector || currentMapEndC2 == incorrectVector)
            {
                return;
            }

            bool isInsideStartBox = IsVectorInsideBox(playerPos, currentMapStartC1, currentMapStartC2);
            bool isInsideEndBox = IsVectorInsideBox(playerPos, currentMapEndC1, currentMapEndC2);

            if (!isInsideStartBox && isInsideEndBox)
            {
                OnTimerStop(player);
            }
            else if (isInsideStartBox)
            {
                OnTimerStart(player);

                if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length()) > maxStartingSpeed) ||
                    (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()) > maxStartingSpeed))
                {
                    Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                    adjustVelocity(player, maxStartingSpeed, true);
                }
            }
        }

        public void ForcePlayerSpeed(CCSPlayerController player, string activeWeapon)
        {

            activeWeapon ??= "no_knife";
            if (!weaponSpeedLookup.TryGetValue(activeWeapon, out WeaponSpeedStats weaponStats) || !player.IsValid) return;

            player.PlayerPawn.Value.VelocityModifier = (float)(forcedPlayerSpeed / weaponStats.GetSpeed(player.PlayerPawn.Value.IsWalking));
        }

        private void AdjustPlayerVelocity(CCSPlayerController? player, float velocity, bool forceNoDebug = false)
        {
            if (!IsAllowedPlayer(player)) return;

            var currentX = player.PlayerPawn.Value.AbsVelocity.X;
            var currentY = player.PlayerPawn.Value.AbsVelocity.Y;
            var currentZ = player.PlayerPawn.Value.AbsVelocity.Z;

            var currentSpeed3D = Math.Sqrt(currentX * currentX + currentY * currentY + currentZ * currentZ);

            var normalizedX = currentX / currentSpeed3D;
            var normalizedY = currentY / currentSpeed3D;
            var normalizedZ = currentZ / currentSpeed3D;

            var adjustedX = normalizedX * velocity; // Adjusted speed limit
            var adjustedY = normalizedY * velocity; // Adjusted speed limit
            var adjustedZ = normalizedZ * velocity; // Adjusted speed limit

            player.PlayerPawn.Value.AbsVelocity.X = (float)adjustedX;
            player.PlayerPawn.Value.AbsVelocity.Y = (float)adjustedY;
            player.PlayerPawn.Value.AbsVelocity.Z = (float)adjustedZ;

            if (!forceNoDebug) SharpTimerDebug($"Adjusted Velo for {player.PlayerName} to {player.PlayerPawn.Value.AbsVelocity}");
        }

        private void AdjustPlayerVelocity2D(CCSPlayerController? player, float velocity, bool forceNoDebug = false)
        {
            if (!IsAllowedPlayer(player)) return;

            var currentX = player.PlayerPawn.Value.AbsVelocity.X;
            var currentY = player.PlayerPawn.Value.AbsVelocity.Y;
            var currentSpeed2D = Math.Sqrt(currentX * currentX + currentY * currentY);
            var normalizedX = currentX / currentSpeed2D;
            var normalizedY = currentY / currentSpeed2D;
            var adjustedX = normalizedX * velocity; // Adjusted speed limit
            var adjustedY = normalizedY * velocity; // Adjusted speed limit
            player.PlayerPawn.Value.AbsVelocity.X = (float)adjustedX;
            player.PlayerPawn.Value.AbsVelocity.Y = (float)adjustedY;
            if (!forceNoDebug) SharpTimerDebug($"Adjusted Velo for {player.PlayerName} to {player.PlayerPawn.Value.AbsVelocity}");
        }

        private void RemovePlayerCollision(CCSPlayerController? player)
        {
            if (removeCollisionEnabled == false || player == null) return;

            player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            VirtualFunctionVoid<nint> collisionRulesChanged = new VirtualFunctionVoid<nint>(player.PlayerPawn.Value.Handle, OnCollisionRulesChangedOffset.Get());
            collisionRulesChanged.Invoke(player.PlayerPawn.Value.Handle);
            SharpTimerDebug($"Removed Collison for {player.PlayerName}");
        }

        private void HandlePlayerStageTimes(CCSPlayerController player, nint triggerHandle)
        {
            if (!IsAllowedPlayer(player) || playerTimers[player.Slot].CurrentMapStage == stageTriggers[triggerHandle])
            {
                return;
            }

            SharpTimerDebug($"Player {player.PlayerName} has a stage trigger with handle {triggerHandle}");
            var (previousStageTime, previousStageSpeed) = GetStageTime(player.SteamID.ToString(), stageTriggers[triggerHandle]);

            string currentStageSpeed = Math.Round(use2DSpeed ? player.PlayerPawn.Value.AbsVelocity.Length2D()
                                                                : player.PlayerPawn.Value.AbsVelocity.Length())
                                                                .ToString("0000");

            if (previousStageTime != 0)
            {
                player.PrintToChat(msgPrefix + $" Entering Stage: {stageTriggers[triggerHandle]}");
                player.PrintToChat(msgPrefix + $" Time: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimers[player.Slot].TimerTicks)}{ChatColors.White}] [{FormatTimeDifference(playerTimers[player.Slot].TimerTicks, previousStageTime)}{ChatColors.White}]");
                player.PrintToChat(msgPrefix + $" Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}] [{FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]");
            }

            if (playerTimers[player.Slot].StageVelos != null && playerTimers[player.Slot].StageTimes != null && playerTimers[player.Slot].IsTimerRunning == true)
            {
                playerTimers[player.Slot].StageTimes[stageTriggers[triggerHandle]] = playerTimers[player.Slot].TimerTicks;
                playerTimers[player.Slot].StageVelos[stageTriggers[triggerHandle]] = $"{currentStageSpeed}";
                SharpTimerDebug($"Player {player.PlayerName} Entering stage {stageTriggers[triggerHandle]} Time {playerTimers[player.Slot].StageTimes[stageTriggers[triggerHandle]]}");
            }

            playerTimers[player.Slot].CurrentMapStage = stageTriggers[triggerHandle];
        }

        private void HandlePlayerCheckpointTimes(CCSPlayerController player, nint triggerHandle)
        {
            if (!IsAllowedPlayer(player) || playerTimers[player.Slot].CurrentMapCheckpoint == cpTriggers[triggerHandle])
            {
                return;
            }

            if (useStageTriggers == true) //use stagetime instead
            {
                playerTimers[player.Slot].CurrentMapCheckpoint = cpTriggers[triggerHandle];
                return;
            }

            SharpTimerDebug($"Player {player.PlayerName} has a checkpoint trigger with handle {triggerHandle}");
            var (previousStageTime, previousStageSpeed) = GetStageTime(player.SteamID.ToString(), cpTriggers[triggerHandle]);

            string currentStageSpeed = Math.Round(use2DSpeed ? player.PlayerPawn.Value.AbsVelocity.Length2D()
                                                                : player.PlayerPawn.Value.AbsVelocity.Length())
                                                                .ToString("0000");

            if (previousStageTime != 0)
            {
                player.PrintToChat(msgPrefix + $" Checkpoint: {cpTriggers[triggerHandle]}");
                player.PrintToChat(msgPrefix + $" Time: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimers[player.Slot].TimerTicks)}{ChatColors.White}] [{FormatTimeDifference(playerTimers[player.Slot].TimerTicks, previousStageTime)}{ChatColors.White}]");
                player.PrintToChat(msgPrefix + $" Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}] [{FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]");
            }

            if (playerTimers[player.Slot].StageVelos != null && playerTimers[player.Slot].StageTimes != null && playerTimers[player.Slot].IsTimerRunning == true)
            {
                playerTimers[player.Slot].StageTimes[cpTriggers[triggerHandle]] = playerTimers[player.Slot].TimerTicks;
                playerTimers[player.Slot].StageVelos[cpTriggers[triggerHandle]] = $"{currentStageSpeed}";
                SharpTimerDebug($"Player {player.PlayerName} Entering checkpoint {cpTriggers[triggerHandle]} Time {playerTimers[player.Slot].StageTimes[cpTriggers[triggerHandle]]}");
            }

            playerTimers[player.Slot].CurrentMapCheckpoint = cpTriggers[triggerHandle];
        }

        public (int time, string speed) GetStageTime(string steamId, int stageIndex)
        {
            string fileName = $"{currentMapName.ToLower()}_stage_times.json";
            string playerStageRecordsPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerStageData", fileName);

            try
            {
                using (JsonDocument jsonDocument = LoadJson(playerStageRecordsPath))
                {
                    if (jsonDocument != null)
                    {
                        string jsonContent = jsonDocument.RootElement.GetRawText();

                        Dictionary<string, PlayerStageData> playerData;
                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            playerData = JsonSerializer.Deserialize<Dictionary<string, PlayerStageData>>(jsonContent);

                            if (playerData.TryGetValue(steamId, out var playerStageData))
                            {
                                if (playerStageData.StageTimes != null && playerStageData.StageTimes.TryGetValue(stageIndex, out var time) &&
                                    playerStageData.StageVelos != null && playerStageData.StageVelos.TryGetValue(stageIndex, out var speed))
                                {
                                    return (time, speed);
                                }
                            }
                        }
                    }
                    else
                    {
                        SharpTimerDebug($"Error in GetStageTime jsonDoc was null");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetStageTime: {ex.Message}");
            }

            return (0, string.Empty);
        }

        public void DumpPlayerStageTimesToJson(CCSPlayerController? player)
        {
            if (!IsAllowedPlayer(player)) return;

            string fileName = $"{currentMapName.ToLower()}_stage_times.json";
            string playerStageRecordsPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerStageData", fileName);

            try
            {
                using (JsonDocument jsonDocument = LoadJson(playerStageRecordsPath))
                {
                    if (jsonDocument != null)
                    {
                        string jsonContent = jsonDocument.RootElement.GetRawText();

                        Dictionary<string, PlayerStageData> playerData;
                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            playerData = JsonSerializer.Deserialize<Dictionary<string, PlayerStageData>>(jsonContent);
                        }
                        else
                        {
                            playerData = new Dictionary<string, PlayerStageData>();
                        }

                        string playerId = player.SteamID.ToString();

                        if (!playerData.ContainsKey(playerId))
                        {
                            playerData[playerId] = new PlayerStageData();
                        }

                        playerData[playerId].StageTimes = playerTimers[player.Slot].StageTimes;
                        playerData[playerId].StageVelos = playerTimers[player.Slot].StageVelos;

                        string updatedJson = JsonSerializer.Serialize(playerData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(playerStageRecordsPath, updatedJson);
                    }
                    else
                    {
                        Dictionary<string, PlayerStageData> playerData = new Dictionary<string, PlayerStageData>();

                        string playerId = player.SteamID.ToString();

                        playerData[playerId] = new PlayerStageData
                        {
                            StageTimes = playerTimers[player.Slot].StageTimes,
                            StageVelos = playerTimers[player.Slot].StageVelos
                        };

                        string updatedJson = JsonSerializer.Serialize(playerData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(playerStageRecordsPath, updatedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in DumpPlayerStageTimesToJson: {ex.Message}");
            }
        }

        private int GetPreviousPlayerRecord(CCSPlayerController? player, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return 0;

            string mapRecordsPath = Path.Combine(playerRecordsPath, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");
            string steamId = player.SteamID.ToString();

            try
            {
                using (JsonDocument jsonDocument = LoadJson(mapRecordsPath))
                {
                    if (jsonDocument != null)
                    {
                        string json = jsonDocument.RootElement.GetRawText();
                        Dictionary<string, PlayerRecord> records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(json) ?? new Dictionary<string, PlayerRecord>();

                        if (records.ContainsKey(steamId))
                        {
                            return records[steamId].TimerTicks;
                        }
                    }
                    else
                    {
                        SharpTimerDebug($"Error in GetPreviousPlayerRecord: json was null");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetPreviousPlayerRecord: {ex.Message}");
            }

            return 0;
        }

        public string GetPlayerPlacement(CCSPlayerController? player)
        {
            if (!IsAllowedPlayer(player) || !playerTimers[player.Slot].IsTimerRunning) return "";


            int currentPlayerTime = playerTimers[player.Slot].TimerTicks;

            int placement = 1;

            foreach (var kvp in SortedCachedRecords.Take(100))
            {
                int recordTimerTicks = kvp.Value.TimerTicks;

                if (currentPlayerTime > recordTimerTicks)
                {
                    placement++;
                }
                else
                {
                    break;
                }
            }
            if (placement > 100)
            {
                return "#100" + "+";
            }
            else
            {
                return "#" + placement;
            }
        }

        public async Task<string> GetPlayerPlacementWithTotal(CCSPlayerController? player, string steamId, string playerName, bool getRankImg = false)
        {
            if (!IsAllowedPlayer(player))
            {
                return "";
            }

            int savedPlayerTime;
            if (useMySQL == true)
            {
                savedPlayerTime = await GetPreviousPlayerRecordFromDatabase(player, steamId, currentMapName, playerName);
            }
            else
            {
                savedPlayerTime = GetPreviousPlayerRecord(player);
            }

            if (savedPlayerTime == 0 && getRankImg == false)
            {
                return "Unranked";
            }
            else if (savedPlayerTime == 0)
            {
                return "<img src='https://files.catbox.moe/h3zqzd.png' class=''>";
            }

            Dictionary<string, PlayerRecord> sortedRecords;
            if (useMySQL == true)
            {
                sortedRecords = await GetSortedRecordsFromDatabase();
            }
            else
            {
                sortedRecords = GetSortedRecords();
            }

            int placement = 1;

            foreach (var kvp in sortedRecords)
            {
                int recordTimerTicks = kvp.Value.TimerTicks; // Get the timer ticks from the dictionary value

                if (savedPlayerTime > recordTimerTicks)
                {
                    placement++;
                }
                else
                {
                    break;
                }
            }

            int totalPlayers = sortedRecords.Count;

            string rank;
            double percentage = (double)placement / totalPlayers * 100;

            if (getRankImg)
            {
                if (totalPlayers < 100)
                {
                    if (placement <= 1)
                        rank = god3Icon; // God 3
                    else if (placement <= 2)
                        rank = god2Icon; // God 2
                    else if (placement <= 3)
                        rank = god1Icon; // God 1
                    else if (placement <= 10)
                        rank = royalty3Icon; // Royal 3
                    else if (placement <= 15)
                        rank = royalty2Icon; // Royal 2
                    else if (placement <= 20)
                        rank = royalty1Icon; // Royal 1
                    else if (placement <= 25)
                        rank = legend3Icon; // Legend 3
                    else if (placement <= 30)
                        rank = legend2Icon; // Legend 2
                    else if (placement <= 35)
                        rank = legend1Icon; // Legend 1
                    else if (placement <= 40)
                        rank = master3Icon; // Master 3
                    else if (placement <= 45)
                        rank = master2Icon; // Master 2
                    else if (placement <= 50)
                        rank = master1Icon; // Master 1
                    else if (placement <= 55)
                        rank = diamond3Icon; // Diamond 3
                    else if (placement <= 60)
                        rank = diamond2Icon; // Diamond 2
                    else if (placement <= 65)
                        rank = diamond1Icon; // Diamond 1
                    else if (placement <= 70)
                        rank = platinum3Icon; // Platinum 3
                    else if (placement <= 75)
                        rank = platinum2Icon; // Platinum 2
                    else if (placement <= 80)
                        rank = platinum1Icon; // Platinum 1
                    else if (placement <= 85)
                        rank = gold3Icon; // Gold 3
                    else if (placement <= 90)
                        rank = gold2Icon; // Gold 2
                    else if (placement <= 95)
                        rank = gold1Icon; // Gold 1
                    else
                        rank = silver1Icon; // Silver 1
                }
                else
                {
                    if (placement <= 1)
                        rank = god3Icon; // God 3
                    else if (placement <= 2)
                        rank = god2Icon; // God 2
                    else if (placement <= 3)
                        rank = god1Icon; // God 1
                    else if (percentage <= 1)
                        rank = royalty3Icon; // Royal 3
                    else if (percentage <= 5.0)
                        rank = royalty2Icon; // Royalty 2
                    else if (percentage <= 10.0)
                        rank = royalty1Icon; // Royalty 1
                    else if (percentage <= 15.0)
                        rank = legend3Icon; // Legend 3
                    else if (percentage <= 20.0)
                        rank = legend2Icon; // Legend 2
                    else if (percentage <= 25.0)
                        rank = legend1Icon; // Legend 1
                    else if (percentage <= 30.0)
                        rank = master3Icon; // Master 3
                    else if (percentage <= 35.0)
                        rank = master2Icon; // Master 2
                    else if (percentage <= 40.0)
                        rank = master1Icon; // Master 1
                    else if (percentage <= 45.0)
                        rank = diamond3Icon; // Diamond 3
                    else if (percentage <= 50.0)
                        rank = diamond2Icon; // Diamond 2
                    else if (percentage <= 55.0)
                        rank = diamond1Icon; // Diamond 1
                    else if (percentage <= 60.0)
                        rank = platinum3Icon; // Platinum 3
                    else if (percentage <= 65.0)
                        rank = platinum2Icon; // Platinum 2
                    else if (percentage <= 70.0)
                        rank = platinum1Icon; // Platinum 1
                    else if (percentage <= 75.0)
                        rank = gold3Icon; // Gold 3
                    else if (percentage <= 80.0)
                        rank = gold2Icon; // Gold 2
                    else if (percentage <= 85.0)
                        rank = gold1Icon; // Gold 1
                    else if (percentage <= 90.0)
                        rank = silver3Icon; // Silver 3
                    else if (percentage <= 95.0)
                        rank = silver2Icon; // Silver 2
                    else
                        rank = silver1Icon; // Silver 1
                }
            }
            else
            {
                if (totalPlayers < 100)
                {
                    if (placement <= 1)
                        rank = $"God III ({placement}/{totalPlayers})";
                    else if (placement <= 2)
                        rank = $"God II ({placement}/{totalPlayers})";
                    else if (placement <= 3)
                        rank = $"God I ({placement}/{totalPlayers})";
                    else if (placement <= 10)
                        rank = $"Royalty III ({placement}/{totalPlayers})";
                    else if (placement <= 15)
                        rank = $"Royalty II ({placement}/{totalPlayers})";
                    else if (placement <= 20)
                        rank = $"Royalty I ({placement}/{totalPlayers})";
                    else if (placement <= 25)
                        rank = $"Legend III ({placement}/{totalPlayers})";
                    else if (placement <= 30)
                        rank = $"Legend II ({placement}/{totalPlayers})";
                    else if (placement <= 35)
                        rank = $"Legend I ({placement}/{totalPlayers})";
                    else if (placement <= 40)
                        rank = $"Master III ({placement}/{totalPlayers})";
                    else if (placement <= 45)
                        rank = $"Master II ({placement}/{totalPlayers})";
                    else if (placement <= 50)
                        rank = $"Master I ({placement}/{totalPlayers})";
                    else if (placement <= 55)
                        rank = $"Diamond III ({placement}/{totalPlayers})";
                    else if (placement <= 60)
                        rank = $"Diamond II ({placement}/{totalPlayers})";
                    else if (placement <= 65)
                        rank = $"Diamond I ({placement}/{totalPlayers})";
                    else if (placement <= 70)
                        rank = $"Platinum III ({placement}/{totalPlayers})";
                    else if (placement <= 75)
                        rank = $"Platinum II ({placement}/{totalPlayers})";
                    else if (placement <= 80)
                        rank = $"Platinum I ({placement}/{totalPlayers})";
                    else if (placement <= 85)
                        rank = $"Gold III ({placement}/{totalPlayers})";
                    else if (placement <= 90)
                        rank = $"Gold II ({placement}/{totalPlayers})";
                    else if (placement <= 95)
                        rank = $"Gold I ({placement}/{totalPlayers})";
                    else
                        rank = $"Silver I ({placement}/{totalPlayers})";
                }
                else
                {
                    if (placement <= 1)
                        rank = $"God III ({placement}/{totalPlayers})";
                    else if (placement <= 2)
                        rank = $"God II ({placement}/{totalPlayers})";
                    else if (placement <= 3)
                        rank = $"God I ({placement}/{totalPlayers})";
                    else if (percentage <= 1)
                        rank = $"Royalty III ({placement}/{totalPlayers})";
                    else if (percentage <= 5.0)
                        rank = $"Royalty II ({placement}/{totalPlayers})";
                    else if (percentage <= 10.0)
                        rank = $"Royalty I ({placement}/{totalPlayers})";
                    else if (percentage <= 15.0)
                        rank = $"Legend III ({placement}/{totalPlayers})";
                    else if (percentage <= 20.0)
                        rank = $"Legend II ({placement}/{totalPlayers})";
                    else if (percentage <= 25.0)
                        rank = $"Legend I ({placement}/{totalPlayers})";
                    else if (percentage <= 30.0)
                        rank = $"Master III ({placement}/{totalPlayers})";
                    else if (percentage <= 35.0)
                        rank = $"Master II ({placement}/{totalPlayers})";
                    else if (percentage <= 40.0)
                        rank = $"Master I ({placement}/{totalPlayers})";
                    else if (percentage <= 45.0)
                        rank = $"Diamond III ({placement}/{totalPlayers})";
                    else if (percentage <= 50.0)
                        rank = $"Diamond II ({placement}/{totalPlayers})";
                    else if (percentage <= 55.0)
                        rank = $"Diamond I ({placement}/{totalPlayers})";
                    else if (percentage <= 60.0)
                        rank = $"Platinum III ({placement}/{totalPlayers})";
                    else if (percentage <= 65.0)
                        rank = $"Platinum II ({placement}/{totalPlayers})";
                    else if (percentage <= 70.0)
                        rank = $"Platinum I ({placement}/{totalPlayers})";
                    else if (percentage <= 75.0)
                        rank = $"Gold III ({placement}/{totalPlayers})";
                    else if (percentage <= 80.0)
                        rank = $"Gold II ({placement}/{totalPlayers})";
                    else if (percentage <= 85.0)
                        rank = $"Gold I ({placement}/{totalPlayers})";
                    else if (percentage <= 90.0)
                        rank = $"Silver III ({placement}/{totalPlayers})";
                    else if (percentage <= 95.0)
                        rank = $"Silver II ({placement}/{totalPlayers})";
                    else
                        rank = $"Silver I ({placement}/{totalPlayers})";
                }
            }


            return rank;
        }

        public void OnTimerStart(CCSPlayerController? player, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;

            if (bonusX != 0)
            {
                if (useTriggers) SharpTimerDebug($"Starting Bonus Timer for {player.PlayerName}");

                // Remove checkpoints for the current player
                playerCheckpoints.Remove(player.Slot);

                playerTimers[player.Slot].IsTimerRunning = false;
                playerTimers[player.Slot].TimerTicks = 0;

                playerTimers[player.Slot].IsBonusTimerRunning = true;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerTimers[player.Slot].BonusStage = bonusX;
            }
            else
            {
                if (useTriggers) SharpTimerDebug($"Starting Timer for {player.PlayerName}");

                // Remove checkpoints for the current player
                playerCheckpoints.Remove(player.Slot);

                playerTimers[player.Slot].IsTimerRunning = true;
                playerTimers[player.Slot].TimerTicks = 0;

                playerTimers[player.Slot].IsBonusTimerRunning = false;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerTimers[player.Slot].BonusStage = bonusX;
            }

            playerTimers[player.Slot].IsRecordingReplay = true;

        }

        public async void OnTimerStop(CCSPlayerController? player)
        {
            if (!IsAllowedPlayer(player) || playerTimers[player.Slot].IsTimerRunning == false) return;

            if (useStageTriggers == true && useCheckpointTriggers == true)
            {
                if (playerTimers[player.Slot].CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == true)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Error Saving Time: Player current stage does not match final one ({stageTriggerCount})");
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].IsRecordingReplay = false;
                    return;
                }

                if (playerTimers[player.Slot].CurrentMapCheckpoint != cpTriggerCount)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Error Saving Time: Player current checkpoint does not match final one ({cpTriggerCount})");
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == true && useCheckpointTriggers == false)
            {
                if (playerTimers[player.Slot].CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == true)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Error Saving Time: Player current stage does not match final one ({stageTriggerCount})");
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == false && useCheckpointTriggers == true)
            {
                if (playerTimers[player.Slot].CurrentMapCheckpoint != cpTriggerCount)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Error Saving Time: Player current checkpoint does not match final one ({cpTriggerCount})");
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].IsRecordingReplay = false;
                    return;
                }
            }

            if (useTriggers) SharpTimerDebug($"Stopping Timer for {player.PlayerName}");

            int currentTicks = playerTimers[player.Slot].TimerTicks;
            int previousRecordTicks = GetPreviousPlayerRecord(player);

            SavePlayerTime(player, currentTicks);
            if (useMySQL == true) _ = SavePlayerTimeToDatabase(player, currentTicks, player.SteamID.ToString(), player.PlayerName, player.Slot);
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].IsRecordingReplay = false;

            if (useMySQL == false) _ = RankCommandHandler(player, player.SteamID.ToString(), player.Slot, player.PlayerName, true);
        }

        public void OnBonusTimerStop(CCSPlayerController? player, int bonusX)
        {
            if (!IsAllowedPlayer(player) || playerTimers[player.Slot].IsBonusTimerRunning == false) return;

            if (useTriggers) SharpTimerDebug($"Stopping Bonus Timer for {player.PlayerName}");

            int currentTicks = playerTimers[player.Slot].BonusTimerTicks;
            int previousRecordTicks = GetPreviousPlayerRecord(player, bonusX);

            SavePlayerTime(player, currentTicks, bonusX);
            if (useMySQL == true) _ = SavePlayerTimeToDatabase(player, currentTicks, player.SteamID.ToString(), player.PlayerName, player.Slot, bonusX);
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].IsRecordingReplay = false;
        }

        public void SavePlayerTime(CCSPlayerController? player, int timerTicks, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;
            if ((bonusX == 0 && playerTimers[player.Slot].IsTimerRunning == false) || (bonusX != 0 && playerTimers[player.Slot].IsBonusTimerRunning == false)) return;

            SharpTimerDebug($"Saving player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} of {timerTicks} ticks for {player.PlayerName} to json");
            string mapRecordsPath = Path.Combine(playerRecordsPath, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");

            try
            {
                using (JsonDocument jsonDocument = LoadJson(mapRecordsPath))
                {
                    Dictionary<string, PlayerRecord> records;

                    if (jsonDocument != null)
                    {
                        string json = jsonDocument.RootElement.GetRawText();
                        records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(json) ?? new Dictionary<string, PlayerRecord>();
                    }
                    else
                    {
                        records = new Dictionary<string, PlayerRecord>();
                    }

                    string steamId = player.SteamID.ToString();
                    string playerName = player.PlayerName;

                    if (!records.ContainsKey(steamId) || records[steamId].TimerTicks > timerTicks)
                    {
                        if (!useMySQL) _ = PrintMapTimeToChat(player, records[steamId].TimerTicks, timerTicks, bonusX);
                        records[steamId] = new PlayerRecord
                        {
                            PlayerName = playerName,
                            TimerTicks = timerTicks
                        };

                        string updatedJson = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(mapRecordsPath, updatedJson);
                        if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0) DumpPlayerStageTimesToJson(player);
                        if (enableReplays == true && useMySQL == false) DumpReplayToJson(player, bonusX);
                    }
                    else
                    {
                        if (!useMySQL) _ = PrintMapTimeToChat(player, records[steamId].TimerTicks, timerTicks, bonusX);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in SavePlayerTime: {ex.Message}");
            }
        }

        public async Task PrintMapTimeToChat(CCSPlayerController player, int oldticks, int newticks, int bonusX = 0)
        {
            string ranking = await GetPlayerPlacementWithTotal(player, player.SteamID.ToString(), player.PlayerName);

            string timeDifference = "";
            if (oldticks != 0) timeDifference = $"[{FormatTimeDifference(newticks, oldticks)}{ChatColors.White}] ";

            Server.NextFrame(() =>
            {
                Server.PrintToChatAll(msgPrefix + $"{primaryChatColor}{player.PlayerName} {ChatColors.White} finished the {(bonusX != 0 ? $" Bonus {bonusX}" : "map")} " +
                                                    $"in: {primaryChatColor}[{FormatTime(newticks)}]{ChatColors.White}! {timeDifference}" +
                                                    $"{(bonusX != 0 ? $"" : $"{((oldticks > newticks) && oldticks != 0 ? $"[{primaryChatColor}{ranking}{ChatColors.White}]" : "")}")}");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {beepSound}");
            });
        }

        public static void SendCommandToEveryone(string command)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                if (player is { PawnIsAlive: true, IsValid: true })
                {
                    player.ExecuteClientCommand(command);
                }
            });
        }
    }
}