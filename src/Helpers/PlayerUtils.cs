using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text.Json;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private bool IsAllowedPlayer(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.Pawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive)
            {
                return false;
            }

            int playerSlot = player.Slot;

            CsTeam teamNum = (CsTeam)player.TeamNum;
            bool isTeamValid = teamNum == CsTeam.CounterTerrorist || teamNum == CsTeam.Terrorist;

            bool isTeamSpectatorOrNone = teamNum != CsTeam.Spectator && teamNum != CsTeam.None;
            bool isConnected = connectedPlayers.ContainsKey(playerSlot) && playerTimers.ContainsKey(playerSlot);
            bool isConnectedJS = !jumpStatsEnabled || playerJumpStats.ContainsKey(playerSlot);

            return isTeamValid && isTeamSpectatorOrNone && isConnected && isConnectedJS;
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
                    if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                    {
                        playerTimer.IsTester = jsonDocument.RootElement.TryGetProperty(steamId64, out JsonElement steamData);

                        if (playerTimer.IsTester)
                        {
                            if (steamData.TryGetProperty("SmolGif", out JsonElement smolGifElement))
                            {
                                playerTimer.TesterSparkleGif = smolGifElement.GetString() ?? "";
                            }

                            if (steamData.TryGetProperty("BigGif", out JsonElement bigGifElement))
                            {
                                playerTimer.TesterPausedGif = bigGifElement.GetString() ?? "";
                            }
                        }
                    }
                    else
                    {
                        SharpTimerError($"Error in IsPlayerATester: player not on server anymore");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in IsPlayerATester: {ex.Message}");
            }
        }

        private void OnPlayerConnect(CCSPlayerController? player, bool isForBot = false)
        {
            try
            {
                if (player == null)
                {
                    SharpTimerError("Player object is null.");
                    return;
                }

                if (player.PlayerPawn == null)
                {
                    SharpTimerError("PlayerPawn is null.");
                    return;
                }

                if (player.PlayerPawn.Value.MovementServices == null)
                {
                    SharpTimerError("MovementServices is null.");
                    return;
                }

                int playerSlot = player.Slot;

                try
                {
                    connectedPlayers[playerSlot] = new CCSPlayerController(player.Handle);
                    playerTimers[playerSlot] = new PlayerTimerInfo();
                    if (enableReplays) playerReplays[playerSlot] = new PlayerReplays();
                    playerTimers[playerSlot].MovementService = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices!.Handle);
                    playerTimers[playerSlot].StageTimes = new Dictionary<int, int>();
                    playerTimers[playerSlot].StageVelos = new Dictionary<int, string>();
                    if (AdminManager.PlayerHasPermissions(player, "@css/root")) playerTimers[playerSlot].ZoneToolWire = new Dictionary<int, CBeam>();
                    if (jumpStatsEnabled) playerJumpStats[playerSlot] = new PlayerJumpStats();
                    playerTimers[playerSlot].CurrentMapStage = 0;
                    playerTimers[playerSlot].CurrentMapCheckpoint = 0;
                    playerTimers[playerSlot].IsRecordingReplay = false;
                    playerTimers[playerSlot].SetRespawnPos = null;
                    playerTimers[playerSlot].SetRespawnAng = null;

                    if (isForBot == false) _ = IsPlayerATester(player.SteamID.ToString(), playerSlot);

                    //PlayerSettings
                    if (useMySQL == true && isForBot == false) _ = GetPlayerStats(player, player.SteamID.ToString(), player.PlayerName, playerSlot, true);

                    if (connectMsgEnabled == true && useMySQL == false) Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{player.PlayerName} {ChatColors.White}connected!");
                    if (cmdJoinMsgEnabled == true && isForBot == false) PrintAllEnabledCommands(player);

                    SharpTimerDebug($"Added player {player.PlayerName} with UserID {player.UserId} to connectedPlayers");
                    SharpTimerDebug($"Total players connected: {connectedPlayers.Count}");
                    SharpTimerDebug($"Total playerTimers: {playerTimers.Count}");
                    SharpTimerDebug($"Total playerReplays: {playerReplays.Count}");

                    if (isForBot == true || hideAllPlayers == true) player.PlayerPawn.Value.Render = Color.FromArgb(0, 0, 0, 0);

                    if (removeLegsEnabled == true) player.PlayerPawn.Value.Render = Color.FromArgb(254, 254, 254, 254);
                }
                finally
                {
                    if (connectedPlayers[playerSlot] == null)
                    {
                        connectedPlayers.Remove(playerSlot);
                    }

                    if (playerTimers[playerSlot] == null)
                    {
                        playerTimers.Remove(playerSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in OnPlayerConnect: {ex.Message}");
            }
        }

        private void OnPlayerDisconnect(CCSPlayerController? player, bool isForBot = false)
        {
            if (player == null) return;

            try
            {
                if (isForBot == true && connectedReplayBots.TryGetValue(player.Slot, out var connectedReplayBot))
                {
                    connectedReplayBots.Remove(player.Slot);
                    SharpTimerDebug($"Removed bot {connectedReplayBot.PlayerName} with UserID {connectedReplayBot.UserId} from connectedReplayBots.");
                }

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

                    if (connectMsgEnabled == true && isForBot == false)
                    {
                        Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{connectedPlayer.PlayerName} {ChatColors.White}disconnected!");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in OnPlayerDisconnect (probably replay bot related lolxd): {ex.Message}");
            }
        }

        private HookResult OnPlayerChatTeam(CCSPlayerController? player, CommandInfo message)
        {
            if (displayChatTags == false) return HookResult.Continue;
            if (player == null || !player.IsValid || player.IsBot || string.IsNullOrEmpty(message.GetArg(1))) return HookResult.Handled;

            if (message.GetArg(1).StartsWith("!") || message.GetArg(1).StartsWith("/") || message.GetArg(1).StartsWith("."))
            {
                return HookResult.Continue;
            }
            else
            {
                char rankColor = GetRankColorForChat(player);

                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? value))
                {
                    Server.PrintToChatAll($" {primaryChatColor}● {(value.IsVip ? $"{ChatColors.Magenta}[{customVIPTag}] " : "")}{rankColor}[{value.CachedRank}]{ChatColors.Default} {player.PlayerName}: {message.GetArg(1)}");
                }
                return HookResult.Handled;
            }
        }

        private HookResult OnPlayerChatAll(CCSPlayerController? player, CommandInfo message)
        {
            if (displayChatTags == false) return HookResult.Continue;
            if (player == null || !player.IsValid || player.IsBot || string.IsNullOrEmpty(message.GetArg(1))) return HookResult.Handled;

            if (message.GetArg(1).StartsWith("!") || message.GetArg(1).StartsWith("/") || message.GetArg(1).StartsWith("."))
            {
                return HookResult.Continue;
            }
            else
            {
                char rankColor = GetRankColorForChat(player);

                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? value))
                {
                    Server.PrintToChatAll($" {ChatColors.Grey}[ALL] {primaryChatColor}● {(value.IsVip ? $"{ChatColors.Magenta}[{customVIPTag}] " : "")}{rankColor}[{value.CachedRank}]{ChatColors.Default} {player.PlayerName}: {message.GetArg(1)}");
                }
                return HookResult.Handled;
            }
        }

        public void TimerOnTick()
        {
            try
            {
                var updates = new Dictionary<int, PlayerTimerInfo>();

                foreach (CCSPlayerController player in connectedPlayers.Values)
                {
                    if (player == null || !player.IsValid || player.Slot == null) continue;

                    if ((CsTeam)player.TeamNum == CsTeam.Spectator)
                    {
                        SpectatorOnTick(player);
                        continue;
                    }

                    if (playerTimers[player.Slot].IsAddingStartZone || playerTimers[player.Slot].IsAddingEndZone)
                    {
                        OnTickZoneTool(player);
                        continue;
                    }

                    if (playerTimers[player.Slot].TicksSinceLastRankUpdate > 511 && connectedReplayBots.ContainsKey(player.Slot))
                    {
                        if (player.PlayerPawn.Value.Render != Color.FromArgb(0, 0, 0, 0))
                            player.PlayerPawn.Value.Render = Color.FromArgb(0, 0, 0, 0);

                        if (player.PlayerName != "SERVER RECORD REPLAY")
                            ChangePlayerName(player, "SERVER RECORD REPLAY");

                        if (playerTimers[player.Slot].IsReplaying == false)
                            _ = ReplaySRHandler(player);

                        playerTimers[player.Slot].TicksSinceLastRankUpdate = 0;
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

                        bool isTimerRunning = playerTimer.IsTimerRunning;
                        int timerTicks = playerTimer.TimerTicks;
                        PlayerButtons? playerButtons = player.Buttons;
                        Vector playerSpeed = player.PlayerPawn.Value.AbsVelocity;

                        string formattedPlayerVel = Math.Round(use2DSpeed ? playerSpeed.Length2D()
                                                                            : playerSpeed.Length())
                                                                            .ToString("0000");
                        string formattedPlayerPre = Math.Round(ParseVector(playerTimer.PreSpeed ?? "0 0 0").Length2D()).ToString("000");
                        string playerTime = FormatTime(timerTicks);
                        string playerBonusTime = FormatTime(playerTimer.BonusTimerTicks);
                        string timerLine = playerTimer.IsBonusTimerRunning
                                            ? $" <font color='gray' class='fontSize-s horizontal-center'>Bonus: {playerTimer.BonusStage}</font> <i><font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerBonusTime}</font></i> <br>"
                                            : isTimerRunning
                                                ? $" <font color='gray' class='fontSize-s horizontal-center'>{GetPlayerPlacement(player)}</font> <i><font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerTime}</font></i>{((playerTimer.CurrentMapStage != 0 && useStageTriggers == true) ? $"<font color='gray' class='fontSize-s horizontal-center'> {playerTimer.CurrentMapStage}/{stageTriggerCount}</font>" : "")} <br>"
                                                : playerTimer.IsReplaying
                                                    ? $" <font class='horizontal-center' color='red'>◉ REPLAY {FormatTime(playerReplays[player.Slot].CurrentPlaybackFrame)}</font> <br>"
                                                    : "";

                        string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")}<font class='fontSize-s horizontal-center' color='{tertiaryHUDcolor}'>Speed:</font> <i>{(playerTimer.IsReplaying ? "<font class=''" : "<font class='fontSize-l horizontal-center'")} color='{secondaryHUDcolor}'>{formattedPlayerVel}</font></i> <font class='fontSize-s horizontal-center' color='gray'>({formattedPlayerPre})</font>{(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")} <br>";
                        string infoLine = !playerTimer.IsReplaying
                                            ? $"<font class='fontSize-s horizontal-center' color='gray'>{playerTimer.CachedPB} " + $"({playerTimer.CachedMapPlacement}) | </font>" + $"{playerTimer.RankHUDIcon} <font class='fontSize-s horizontal-center' color='gray'>" +
                                              $"{(currentMapTier != null ? $" | Tier: {currentMapTier}" : "")}" +
                                              $"{(currentMapType != null ? $" | {currentMapType}" : "")}" +
                                              $"{((currentMapType == null && currentMapTier == null) ? $" | {currentMapName} " : "")}  "
                                            : $" <font class='fontSize-s horizontal-center' color='gray'>{playerTimers[player.Slot].ReplayHUDString}</font>";

                        string keysLineNoHtml = $"{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Jump) != 0 || playerTimer.MovementService.OldJumpPressed ? "J" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Duck) != 0 ? "C" : "_")}";

                        if (playerTimer.MovementService.OldJumpPressed == true) playerTimer.MovementService.OldJumpPressed = false;

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

                        if (useTriggers == false && playerTimer.IsTimerBlocked == false)
                        {
                            CheckPlayerCoords(player, playerSpeed);
                        }

                        if (triggerPushFixEnabled == true)
                        {
                            CheckPlayerTriggerPushCoords(player, playerSpeed);
                        }

                        if (jumpStatsEnabled == true) OnJumpStatTick(player, playerSpeed, player.Pawn?.Value.CBodyComponent?.SceneNode.AbsOrigin, playerButtons);

                        if (forcePlayerSpeedEnabled == true) ForcePlayerSpeed(player, player.Pawn.Value.WeaponServices.ActiveWeapon.Value.DesignerName);

                        if (playerTimer.IsRankPbCached == false)
                        {
                            SharpTimerDebug($"{player.PlayerName} has rank and pb null... calling handler");
                            _ = RankCommandHandler(player, player.SteamID.ToString(), player.Slot, player.PlayerName, true);


                            playerTimer.IsRankPbCached = true;
                        }

                        if (hideAllPlayers == true)
                        {
                            foreach (var gun in player.PlayerPawn.Value.WeaponServices.MyWeapons)
                            {
                                if (gun.Value.Render != Color.FromArgb(0, 255, 255, 255) ||
                                    gun.Value.ShadowStrength != 0.0f)
                                {
                                    gun.Value.Render = Color.FromArgb(0, 255, 255, 255);
                                    gun.Value.ShadowStrength = 0.0f;
                                }
                            }
                        }

                        if (displayScoreboardTags == true &&
                            playerTimer.TicksSinceLastRankUpdate > 511 &&
                            playerTimer.CachedRank != null &&
                            (player.Clan != null || !player.Clan.Contains($"[{playerTimer.CachedRank}]")))
                        {
                            AddScoreboardTagToPlayer(player, playerTimer.CachedRank);
                            playerTimer.TicksSinceLastRankUpdate = 0;
                            SharpTimerDebug($"Setting Scoreboard Tag for {player.PlayerName} from TimerOnTick");
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

                        if (hideAllPlayers == true && player.PlayerPawn.Value.Render != Color.FromArgb(0, 0, 0, 0))
                        {
                            player.PlayerPawn.Value.Render = Color.FromArgb(0, 0, 0, 0);
                        }

                        if (((PlayerFlags)player.Pawn.Value.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND)
                        {
                            playerTimer.TicksInAir++;
                            if (playerTimer.TicksInAir == 1)
                            {
                                playerTimer.PreSpeed = $"{playerSpeed.X} {playerSpeed.Y} {playerSpeed.Z}";
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
                            if (!playerTimer.IsTimerBlocked && (player.PlayerPawn.Value.MoveType == MoveType_t.MOVETYPE_OBSERVER || player.PlayerPawn.Value.ActualMoveType == MoveType_t.MOVETYPE_OBSERVER)) SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                        }

                        if (playerTimer.TicksSinceLastCmd < cmdCooldown) playerTimer.TicksSinceLastCmd++;
                        if (playerTimer.TicksSinceLastRankUpdate < 511) playerTimer.TicksSinceLastRankUpdate++;

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
            catch (Exception ex)
            {
                if (ex.Message != "Invalid game event") SharpTimerError($"Error in TimerOnTick: {ex.Message}");
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
                    PlayerButtons? playerButtons;
                    Vector targetSpeed = target.PlayerPawn.Value.AbsVelocity;
                    if (playerTimer.IsReplaying && playerReplays[target.Slot].replayFrames.Count > 0 &&
                        playerReplays[target.Slot].CurrentPlaybackFrame >= 0 &&
                        playerReplays[target.Slot].CurrentPlaybackFrame < playerReplays[target.Slot].replayFrames.Count)
                    {
                        playerButtons = playerReplays[target.Slot].replayFrames[playerReplays[target.Slot].CurrentPlaybackFrame].Buttons;
                    }
                    else
                    {
                        playerButtons = target.Buttons;
                    }

                    string formattedPlayerVel = Math.Round(use2DSpeed ? targetSpeed.Length2D()
                                                                        : targetSpeed.Length())
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

                    string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")}<font class='fontSize-s' color='{tertiaryHUDcolor}'>Speed:</font> <font class='' color='{secondaryHUDcolor}'>{formattedPlayerVel}</font> <font class='fontSize-s' color='gray'>({formattedPlayerPre})</font>{(playerTimer.IsTester ? playerTimer.TesterSparkleGif : "")} <br>";
                    string infoLine = !playerTimer.IsReplaying
                                        ? $"<font class='fontSize-s' color='gray'>{playerTimer.CachedPB} " + $"{playerTimer.CachedMapPlacement} | </font>" + $"{playerTimer.RankHUDIcon} <font class='fontSize-s' color='gray'>" +
                                          $"{(currentMapTier != null ? $" | Tier: {currentMapTier}" : "")}" +
                                          $"{(currentMapType != null ? $" | {currentMapType}" : "")}" +
                                          $"{((currentMapType == null && currentMapTier == null) ? $" {currentMapName} " : "")} </font> <br>"
                                        : $" <font class='fontSize-s' color='gray'>{playerTimers[target.Slot].ReplayHUDString}</font> <br>";

                    string keysLine = $"<font class='fontSize-l' color='{secondaryHUDcolor}'>{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Jump) != 0 || playerTimer.MovementService.OldJumpPressed ? "J" : "_")} " +
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
                if (ex.Message != "Invalid game event") SharpTimerError($"Error in SpectatorOnTick: {ex.Message}");
            }
        }

        public void PrintAllEnabledCommands(CCSPlayerController player)
        {
            SharpTimerDebug($"Printing Commands for {player.PlayerName}");
            player.PrintToChat($"{msgPrefix}Available Commands:");

            if (respawnEnabled) player.PrintToChat($"{msgPrefix}!r (css_r) - Respawns you");
            if (respawnEnabled && bonusRespawnPoses.Any()) player.PrintToChat($"{msgPrefix}!rb <#> / !b <#> (css_rb / css_b) - Respawns you to a bonus");
            if (topEnabled) player.PrintToChat($"{msgPrefix}!top (css_top) - Lists top 10 records on this map");
            if (topEnabled && bonusRespawnPoses.Any()) player.PrintToChat($"{msgPrefix}!topbonus <#> (css_topbonus) - Lists top 10 records of a bonus");
            if (rankEnabled) player.PrintToChat($"{msgPrefix}!rank (css_rank) - Shows your current rank and pb");
            if (globalRanksEnabled) player.PrintToChat($"{msgPrefix}!points (css_points) - Prints top 10 points");
            if (goToEnabled) player.PrintToChat($"{msgPrefix}!goto <name> (css_goto) - Teleports you to a player");
            if (stageTriggerPoses.Any()) player.PrintToChat($"{msgPrefix}!stage <#> (css_goto) - Teleports you to a stage");

            if (cpEnabled)
            {
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!saveloc (css_saveloc) - Saves a Loc" : "!cp (css_cp) - Sets a Checkpoint")}");
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!loadloc (css_loadloc) - Teleports you to the last Loc" : "!tp (css_tp) - Teleports you to the last Checkpoint")}");
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!prevloc (css_prevloc) - Teleports you one Loc back" : "!prevcp (css_prevcp) - Teleports you one Checkpoint back")}");
                player.PrintToChat($"{msgPrefix}{(currentMapName.Contains("surf_") ? "!nextloc (css_nextloc) - Teleports you one Loc forward" : "!nextcp (css_nextcp) - Teleports you one Checkpoint forward")}");
            }

            if (jumpStatsEnabled) player.PrintToChat($"{msgPrefix}!jumpstats (css_jumpstats) - Toggles JumpStats");
        }

        private void CheckPlayerCoords(CCSPlayerController? player, Vector playerSpeed)
        {
            if (player == null || !IsAllowedPlayer(player) || useTriggers == true) return;

            try
            {
                Vector? playerPos = player.Pawn?.Value.CBodyComponent?.SceneNode.AbsOrigin;

                if (playerPos == null || currentMapStartC1 != null || currentMapStartC2 != null || currentMapEndC1 != null || currentMapEndC2 != null)
                {
                    return;
                }

                bool isInsideStartBox = IsVectorInsideBox(playerPos, currentMapStartC1, currentMapStartC2);
                bool isInsideEndBox = IsVectorInsideBox(playerPos, currentMapEndC1, currentMapEndC2);

                if (!isInsideStartBox && isInsideEndBox)
                {
                    OnTimerStop(player);
                    if (enableReplays) OnRecordingStop(player);
                }
                else if (isInsideStartBox && !isInsideEndBox)
                {
                    OnTimerStart(player);
                    if (enableReplays) OnRecordingStart(player);

                    if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(playerSpeed.Length()) > maxStartingSpeed) ||
                        (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(playerSpeed.Length2D()) > maxStartingSpeed))
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, maxStartingSpeed, true);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in CheckPlayerCoords: {ex.Message}");
            }
        }

        private void CheckPlayerTriggerPushCoords(CCSPlayerController player, Vector playerSpeed)
        {
            try
            {
                if (player == null || !IsAllowedPlayer(player)) return;

                Vector? playerPos = player.Pawn?.Value.CBodyComponent?.SceneNode.AbsOrigin;

                if (playerPos == null) return;

                var data = GetTriggerPushDataForVector(playerPos);
                if (data != null)
                {
                    var (pushDirEntitySpace, pushSpeed) = data.Value;
                    float currentSpeed = playerSpeed.Length();
                    float speedDifference = pushSpeed - currentSpeed;

                    if (speedDifference > 0)
                    {
                        float velocityChange = speedDifference;
                        player.PlayerPawn.Value.AbsVelocity.X += pushDirEntitySpace.X * velocityChange;
                        player.PlayerPawn.Value.AbsVelocity.Y += pushDirEntitySpace.Y * velocityChange;
                        player.PlayerPawn.Value.AbsVelocity.Z += pushDirEntitySpace.Z * velocityChange;
                        SharpTimerDebug($"trigger_push fix: Player velocity adjusted for {player.PlayerName} by {speedDifference}");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in CheckPlayerTriggerPushCoords: {ex.Message}");
            }
        }

        public void ForcePlayerSpeed(CCSPlayerController player, string activeWeapon)
        {

            try
            {
                activeWeapon ??= "no_knife";
                if (!weaponSpeedLookup.TryGetValue(activeWeapon, out WeaponSpeedStats weaponStats) || !player.IsValid) return;

                player.PlayerPawn.Value.VelocityModifier = (float)(forcedPlayerSpeed / weaponStats.GetSpeed(player.PlayerPawn.Value.IsWalking));
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in ForcePlayerSpeed: {ex.Message}");
            }
        }

        private void AdjustPlayerVelocity(CCSPlayerController? player, float velocity, bool forceNoDebug = false)
        {
            if (!IsAllowedPlayer(player)) return;

            try
            {
                var currentX = player.PlayerPawn.Value.AbsVelocity.X;
                var currentY = player.PlayerPawn.Value.AbsVelocity.Y;
                var currentZ = player.PlayerPawn.Value.AbsVelocity.Z;

                var currentSpeedSquared = currentX * currentX + currentY * currentY + currentZ * currentZ;

                // Check if current speed is not zero to avoid division by zero
                if (currentSpeedSquared > 0)
                {
                    var currentSpeed = Math.Sqrt(currentSpeedSquared);

                    var normalizedX = currentX / currentSpeed;
                    var normalizedY = currentY / currentSpeed;
                    var normalizedZ = currentZ / currentSpeed;

                    var adjustedX = normalizedX * velocity; // Adjusted speed limit
                    var adjustedY = normalizedY * velocity; // Adjusted speed limit
                    var adjustedZ = normalizedZ * velocity; // Adjusted speed limit

                    player.PlayerPawn.Value.AbsVelocity.X = (float)adjustedX;
                    player.PlayerPawn.Value.AbsVelocity.Y = (float)adjustedY;
                    player.PlayerPawn.Value.AbsVelocity.Z = (float)adjustedZ;

                    if (!forceNoDebug) SharpTimerDebug($"Adjusted Velo for {player.PlayerName} to {player.PlayerPawn.Value.AbsVelocity}");
                }
                else
                {
                    if (!forceNoDebug) SharpTimerDebug($"Cannot adjust velocity for {player.PlayerName} because current speed is zero.");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in AdjustPlayerVelocity: {ex.Message}");
            }
        }

        private void AdjustPlayerVelocity2D(CCSPlayerController? player, float velocity, bool forceNoDebug = false)
        {
            if (!IsAllowedPlayer(player)) return;

            try
            {
                var currentX = player.PlayerPawn.Value.AbsVelocity.X;
                var currentY = player.PlayerPawn.Value.AbsVelocity.Y;

                var currentSpeedSquared = currentX * currentX + currentY * currentY;

                // Check if current speed is not zero to avoid division by zero
                if (currentSpeedSquared > 0)
                {
                    var currentSpeed2D = Math.Sqrt(currentSpeedSquared);

                    var normalizedX = currentX / currentSpeed2D;
                    var normalizedY = currentY / currentSpeed2D;

                    var adjustedX = normalizedX * velocity; // Adjusted speed limit
                    var adjustedY = normalizedY * velocity; // Adjusted speed limit

                    player.PlayerPawn.Value.AbsVelocity.X = (float)adjustedX;
                    player.PlayerPawn.Value.AbsVelocity.Y = (float)adjustedY;

                    if (!forceNoDebug) SharpTimerDebug($"Adjusted Velo for {player.PlayerName} to {player.PlayerPawn.Value.AbsVelocity}");
                }
                else
                {
                    if (!forceNoDebug) SharpTimerDebug($"Cannot adjust velocity for {player.PlayerName} because current speed is zero.");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in AdjustPlayerVelocity2D: {ex.Message}");
            }
        }

        private void RemovePlayerCollision(CCSPlayerController? player)
        {
            try
            {
                Server.NextFrame(() =>
                {
                    if (removeCollisionEnabled == false || !IsAllowedPlayer(player)) return;

                    player.Pawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                    player.Pawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;

                    Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(player, "CCollisionProperty", "m_collisionAttribute");

                    SharpTimerDebug($"Removed Collison for {player.PlayerName}");
                });
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in RemovePlayerCollision: {ex.Message}");
            }
        }

        private async Task HandlePlayerStageTimes(CCSPlayerController player, nint triggerHandle)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                {
                    return;
                }

                SharpTimerDebug($"Player {player.PlayerName} has a stage trigger with handle {triggerHandle}");

                if (stageTriggers.TryGetValue(triggerHandle, out int stageTrigger))
                {
                    var playerSlot = player.Slot;
                    var playerSteamID = player.SteamID.ToString();
                    var playerName = player.PlayerName;
                    var playerTimerTicks = playerTimers[playerSlot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (useMySQL == true)
                    {
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
                    }
                    else
                    {
                        (srSteamID, srPlayerName, srTime) = GetMapRecordSteamID();
                    }

                    if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                    {

                        if (playerTimer.CurrentMapStage == stageTrigger || playerTimer == null) return;

                        var (previousStageTime, previousStageSpeed) = GetStageTime(playerSteamID, stageTrigger);
                        var (srStageTime, srStageSpeed) = GetStageTime(srSteamID, stageTrigger);

                        string currentStageSpeed = Math.Round(use2DSpeed ? Math.Sqrt(player.PlayerPawn.Value.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y)
                                                                            : Math.Sqrt(player.PlayerPawn.Value.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y + player.PlayerPawn.Value.AbsVelocity.Z * player.PlayerPawn.Value.AbsVelocity.Z))
                                                                            .ToString("0000");

                        if (previousStageTime != 0)
                        {
                            Server.NextFrame(() =>
                            {
                                if (!IsAllowedPlayer(player)) return;
                                player.PrintToChat(msgPrefix + $" Entering Stage: {stageTrigger}");
                                player.PrintToChat(msgPrefix + $" Time: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                               $" [{FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                               $" {(previousStageTime != srStageTime ? $"[SR {FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentStageSpeed, out float speed) && speed >= 100) //workaround for staged maps with not telehops
                                    player.PrintToChat(msgPrefix + $" Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                                    $" [{FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                    $" {(previousStageSpeed != srStageSpeed ? $"[SR {FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");
                            });
                        }

                        if (playerTimer.StageVelos != null && playerTimer.StageTimes != null && playerTimer.IsTimerRunning == true && IsAllowedPlayer(player))
                        {
                            if (!playerTimer.StageTimes.ContainsKey(stageTrigger))
                            {
                                SharpTimerDebug($"Player {playerName} cleared StageTimes before (stageTrigger)");
                                playerTimer.StageTimes.Add(stageTrigger, playerTimerTicks);
                                playerTimer.StageVelos.Add(stageTrigger, $"{currentStageSpeed}");
                            }
                            else
                            {
                                playerTimer.StageTimes[stageTrigger] = playerTimerTicks;
                                playerTimer.StageVelos[stageTrigger] = $"{currentStageSpeed}";
                            }

                            Server.NextFrame(() =>
                            {
                                if (!IsAllowedPlayer(player)) return;
                                SharpTimerDebug($"Player {playerName} Entering stage {stageTrigger} Time {playerTimer.StageTimes[stageTrigger]}");
                            });
                        }

                        if (IsAllowedPlayer(player)) playerTimer.CurrentMapStage = stageTrigger;
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in HandlePlayerStageTimes: {ex.Message}");
            }
        }

        private async Task HandlePlayerCheckpointTimes(CCSPlayerController player, nint triggerHandle)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                {
                    return;
                }

                if (cpTriggers.TryGetValue(triggerHandle, out int cpTrigger))
                {

                    var playerSlot = player.Slot;
                    var playerSteamID = player.SteamID.ToString();
                    var playerName = player.PlayerName;
                    if (useStageTriggers == true) //use stagetime instead
                    {
                        playerTimers[playerSlot].CurrentMapCheckpoint = cpTrigger;
                        return;
                    }

                    SharpTimerDebug($"Player {playerName} has a checkpoint trigger with handle {triggerHandle}");

                    var playerTimerTicks = playerTimers[playerSlot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (useMySQL == true)
                    {
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
                    }
                    else
                    {
                        (srSteamID, srPlayerName, srTime) = GetMapRecordSteamID();
                    }

                    if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                    {

                        if (playerTimer.CurrentMapCheckpoint == cpTrigger || playerTimer == null) return;

                        var (previousStageTime, previousStageSpeed) = GetStageTime(playerSteamID, cpTrigger);
                        var (srStageTime, srStageSpeed) = GetStageTime(srSteamID, cpTrigger);

                        string currentStageSpeed = Math.Round(use2DSpeed ? Math.Sqrt(player.PlayerPawn.Value.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y)
                                                                            : Math.Sqrt(player.PlayerPawn.Value.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y + player.PlayerPawn.Value.AbsVelocity.Z * player.PlayerPawn.Value.AbsVelocity.Z))
                                                                            .ToString("0000");

                        if (previousStageTime != 0)
                        {
                            Server.NextFrame(() =>
                            {
                                if (!IsAllowedPlayer(player)) return;
                                player.PrintToChat(msgPrefix + $" Checkpoint: {cpTrigger}");
                                player.PrintToChat(msgPrefix + $" Time: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                               $" [{FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                               $" {(previousStageTime != srStageTime ? $"[SR {FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentStageSpeed, out float speed) && speed >= 100) //workaround for staged maps with not telehops
                                    player.PrintToChat(msgPrefix + $" Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                                   $" [{FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                   $" {(previousStageSpeed != srStageSpeed ? $"[SR {FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");
                            });
                        }

                        if (playerTimer.StageVelos != null && playerTimer.StageTimes != null && playerTimer.IsTimerRunning == true && IsAllowedPlayer(player))
                        {
                            if (!playerTimer.StageTimes.ContainsKey(cpTrigger))
                            {
                                SharpTimerDebug($"Player {playerName} cleared StageTimes before (cpTrigger)");
                                playerTimer.StageTimes.Add(cpTrigger, playerTimerTicks);
                                playerTimer.StageVelos.Add(cpTrigger, $"{currentStageSpeed}");
                            }
                            else
                            {
                                try
                                {
                                    playerTimer.StageTimes[cpTrigger] = playerTimerTicks;
                                    playerTimer.StageVelos[cpTrigger] = $"{currentStageSpeed}";
                                }
                                catch (Exception ex)
                                {
                                    SharpTimerError($"Error updating StageTimes dictionary: {ex.Message}");
                                    SharpTimerDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                }

                                Server.NextFrame(() =>
                                {
                                    if (!IsAllowedPlayer(player)) return;
                                    try
                                    {
                                        SharpTimerDebug($"Player {playerName} Entering checkpoint {cpTrigger} Time {playerTimer.StageTimes[cpTrigger]}");
                                    }
                                    catch (Exception ex)
                                    {
                                        SharpTimerError($"Error accessing StageTimes dictionary: {ex.Message}");
                                        SharpTimerDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                    }
                                });
                            }
                        }
                        if (IsAllowedPlayer(player)) playerTimer.CurrentMapCheckpoint = cpTrigger;
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in HandlePlayerCheckpointTimes: {ex.Message}");
            }
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

                        if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                        {
                            playerData[playerId].StageTimes = playerTimer.StageTimes;
                            playerData[playerId].StageVelos = playerTimer.StageVelos;
                        }
                        else
                        {
                            SharpTimerError($"Error in DumpPlayerStageTimesToJson: playerTimers does not have the requested playerSlot");
                        }

                        string updatedJson = JsonSerializer.Serialize(playerData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(playerStageRecordsPath, updatedJson);
                    }
                    else
                    {
                        Dictionary<string, PlayerStageData> playerData = new Dictionary<string, PlayerStageData>();

                        string playerId = player.SteamID.ToString();

                        if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                        {
                            playerData[playerId] = new PlayerStageData
                            {
                                StageTimes = playerTimers[player.Slot].StageTimes,
                                StageVelos = playerTimers[player.Slot].StageVelos
                            };
                        }
                        else
                        {
                            SharpTimerError($"Error in DumpPlayerStageTimesToJson: playerTimers does not have the requested playerSlot");
                        }

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

        public async Task<string> GetPlayerMapPlacementWithTotal(CCSPlayerController? player, string steamId, string playerName, bool getRankImg = false, bool getPlacementOnly = false)
        {
            if (IsAllowedPlayer(player) || IsAllowedSpectator(player))
            {
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
                    return unrankedIcon;
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

                double percentage = (double)placement / totalPlayers * 100;

                return CalculateRankStuff(totalPlayers, placement, percentage, getRankImg, getPlacementOnly);
            }
            else
            {
                return "";
            }
        }

        public async Task<string> GetPlayerServerPlacement(CCSPlayerController? player, string steamId, string playerName, bool getRankImg = false, bool getPlacementOnly = false, bool getPointsOnly = false)
        {
            if (IsAllowedPlayer(player) || IsAllowedSpectator(player))
            {
                int savedPlayerPoints;
                if (useMySQL == true)
                {
                    savedPlayerPoints = await GetPlayerPointsFromDatabase(player, steamId, playerName);
                }
                else
                {
                    savedPlayerPoints = 0;
                }

                if (getPointsOnly == true)
                {
                    return savedPlayerPoints.ToString();
                }

                if ((savedPlayerPoints == 0 || savedPlayerPoints <= minGlobalPointsForRank) && getRankImg == false)
                {
                    return "Unranked";
                }
                else if (savedPlayerPoints == 0 || savedPlayerPoints <= minGlobalPointsForRank)
                {
                    return unrankedIcon;
                }

                Dictionary<string, PlayerPoints> sortedPoints;
                if (useMySQL == true)
                {
                    sortedPoints = await GetSortedPointsFromDatabase();
                }
                else
                {
                    //sortedRecords = GetSortedRecords();
                    sortedPoints = new Dictionary<string, PlayerPoints>();
                }

                int placement = 1;

                foreach (var kvp in sortedPoints)
                {
                    int recordPoints = kvp.Value.GlobalPoints;

                    if (savedPlayerPoints < recordPoints)
                    {
                        placement++;
                    }
                    else
                    {
                        break;
                    }
                }

                int totalPlayers = sortedPoints.Count;

                double percentage = (double)placement / totalPlayers * 100;

                return CalculateRankStuff(totalPlayers, placement, percentage, getRankImg, getPlacementOnly);
            }
            else
            {
                return "";
            }
        }

        public string CalculateRankStuff(int totalPlayers, int placement, double percentage, bool getRankImg = false, bool getPlacementOnly = false)
        {
            if (getRankImg)
            {
                if (totalPlayers < 100)
                {
                    if (placement <= 1)
                        return god3Icon; // God 3
                    else if (placement <= 2)
                        return god2Icon; // God 2
                    else if (placement <= 3)
                        return god1Icon; // God 1
                    else if (placement <= 10)
                        return royalty3Icon; // Royal 3
                    else if (placement <= 15)
                        return royalty2Icon; // Royal 2
                    else if (placement <= 20)
                        return royalty1Icon; // Royal 1
                    else if (placement <= 25)
                        return legend3Icon; // Legend 3
                    else if (placement <= 30)
                        return legend2Icon; // Legend 2
                    else if (placement <= 35)
                        return legend1Icon; // Legend 1
                    else if (placement <= 40)
                        return master3Icon; // Master 3
                    else if (placement <= 45)
                        return master2Icon; // Master 2
                    else if (placement <= 50)
                        return master1Icon; // Master 1
                    else if (placement <= 55)
                        return diamond3Icon; // Diamond 3
                    else if (placement <= 60)
                        return diamond2Icon; // Diamond 2
                    else if (placement <= 65)
                        return diamond1Icon; // Diamond 1
                    else if (placement <= 70)
                        return platinum3Icon; // Platinum 3
                    else if (placement <= 75)
                        return platinum2Icon; // Platinum 2
                    else if (placement <= 80)
                        return platinum1Icon; // Platinum 1
                    else if (placement <= 85)
                        return gold3Icon; // Gold 3
                    else if (placement <= 90)
                        return gold2Icon; // Gold 2
                    else if (placement <= 95)
                        return gold1Icon; // Gold 1
                    else
                        return silver1Icon; // Silver 1
                }
                else
                {
                    if (placement <= 1)
                        return god3Icon; // God 3
                    else if (placement <= 2)
                        return god2Icon; // God 2
                    else if (placement <= 3)
                        return god1Icon; // God 1
                    else if (percentage <= 1)
                        return royalty3Icon; // Royal 3
                    else if (percentage <= 5.0)
                        return royalty2Icon; // Royalty 2
                    else if (percentage <= 10.0)
                        return royalty1Icon; // Royalty 1
                    else if (percentage <= 15.0)
                        return legend3Icon; // Legend 3
                    else if (percentage <= 20.0)
                        return legend2Icon; // Legend 2
                    else if (percentage <= 25.0)
                        return legend1Icon; // Legend 1
                    else if (percentage <= 30.0)
                        return master3Icon; // Master 3
                    else if (percentage <= 35.0)
                        return master2Icon; // Master 2
                    else if (percentage <= 40.0)
                        return master1Icon; // Master 1
                    else if (percentage <= 45.0)
                        return diamond3Icon; // Diamond 3
                    else if (percentage <= 50.0)
                        return diamond2Icon; // Diamond 2
                    else if (percentage <= 55.0)
                        return diamond1Icon; // Diamond 1
                    else if (percentage <= 60.0)
                        return platinum3Icon; // Platinum 3
                    else if (percentage <= 65.0)
                        return platinum2Icon; // Platinum 2
                    else if (percentage <= 70.0)
                        return platinum1Icon; // Platinum 1
                    else if (percentage <= 75.0)
                        return gold3Icon; // Gold 3
                    else if (percentage <= 80.0)
                        return gold2Icon; // Gold 2
                    else if (percentage <= 85.0)
                        return gold1Icon; // Gold 1
                    else if (percentage <= 90.0)
                        return silver3Icon; // Silver 3
                    else if (percentage <= 95.0)
                        return silver2Icon; // Silver 2
                    else
                        return silver1Icon; // Silver 1
                }
            }
            else
            {
                if (totalPlayers < 100)
                {
                    if (placement <= 1)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God III";
                    else if (placement <= 2)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God II";
                    else if (placement <= 3)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God I";
                    else if (placement <= 10)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty III";
                    else if (placement <= 15)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty II";
                    else if (placement <= 20)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty I";
                    else if (placement <= 25)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend III";
                    else if (placement <= 30)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend II";
                    else if (placement <= 35)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend I";
                    else if (placement <= 40)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master III";
                    else if (placement <= 45)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master II";
                    else if (placement <= 50)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master I";
                    else if (placement <= 55)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond III";
                    else if (placement <= 60)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond II";
                    else if (placement <= 65)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond I";
                    else if (placement <= 70)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum III";
                    else if (placement <= 75)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum II";
                    else if (placement <= 80)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum I";
                    else if (placement <= 85)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold III";
                    else if (placement <= 90)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold II";
                    else if (placement <= 95)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold I";
                    else
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver I";
                }
                else
                {
                    if (placement <= 1)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God III";
                    else if (placement <= 2)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God II";
                    else if (placement <= 3)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God I";
                    else if (percentage <= 1)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty III";
                    else if (percentage <= 5.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty II";
                    else if (percentage <= 10.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty I";
                    else if (percentage <= 15.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend III";
                    else if (percentage <= 20.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend II";
                    else if (percentage <= 25.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend I";
                    else if (percentage <= 30.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master III";
                    else if (percentage <= 35.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master II";
                    else if (percentage <= 40.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master I";
                    else if (percentage <= 45.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond III";
                    else if (percentage <= 50.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond II";
                    else if (percentage <= 55.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond I";
                    else if (percentage <= 60.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum III";
                    else if (percentage <= 65.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum II";
                    else if (percentage <= 70.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum I";
                    else if (percentage <= 75.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold III";
                    else if (percentage <= 80.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold II";
                    else if (percentage <= 85.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold I";
                    else if (percentage <= 90.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver III";
                    else if (percentage <= 95.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver II";
                    else
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver I";
                }
            }
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

        public void OnTimerStop(CCSPlayerController? player)
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
                        if (!useMySQL) _ = PrintMapTimeToChat(player, player.PlayerName, records.GetValueOrDefault(steamId)?.TimerTicks ?? 0, timerTicks, bonusX);

                        records[steamId] = new PlayerRecord
                        {
                            PlayerName = playerName,
                            TimerTicks = timerTicks
                        };

                        string updatedJson = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(mapRecordsPath, updatedJson);

                        if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0 && useMySQL == false) DumpPlayerStageTimesToJson(player);
                        if (enableReplays == true && useMySQL == false) DumpReplayToJson(player, bonusX);
                    }
                    else
                    {
                        if (!useMySQL) _ = PrintMapTimeToChat(player, player.PlayerName, records[steamId].TimerTicks, timerTicks, bonusX);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in SavePlayerTime: {ex.Message}");
            }
        }

        public async Task PrintMapTimeToChat(CCSPlayerController player, string playerName, int oldticks, int newticks, int bonusX = 0, int timesFinished = 0)
        {
            if (!IsAllowedPlayer(player))
            {
                SharpTimerError($"Error in PrintMapTimeToChat: Player {playerName} not allowed or not on server anymore");
                return;
            }

            string ranking = await GetPlayerMapPlacementWithTotal(player, player.SteamID.ToString(), playerName, false, true);

            string timeDifference = "";
            if (oldticks != 0) timeDifference = $"[{FormatTimeDifference(newticks, oldticks)}{ChatColors.White}] ";

            Server.NextFrame(() =>
            {
                if (IsAllowedPlayer(player) && timesFinished > maxGlobalFreePoints && globalRanksFreePointsEnabled == true && oldticks < newticks)
                    player.PrintToChat(msgPrefix + $"{ChatColors.White} You reached your maximum free points rewards of {primaryChatColor}{maxGlobalFreePoints}{ChatColors.White}!");

                if (GetNumberBeforeSlash(ranking) == 1 && oldticks > newticks)
                    Server.PrintToChatAll(msgPrefix + $"{primaryChatColor}{playerName} {ChatColors.White}set a new {(bonusX != 0 ? $"Bonus {bonusX} SR!" : "SR!")}");
                else if (oldticks > newticks)
                    Server.PrintToChatAll(msgPrefix + $"{primaryChatColor}{playerName} {ChatColors.White}set a new {(bonusX != 0 ? $"Bonus {bonusX} PB!" : "Map PB!")}");
                else
                    Server.PrintToChatAll(msgPrefix + $"{primaryChatColor}{playerName} {ChatColors.White}finished the {(bonusX != 0 ? $"Bonus {bonusX}!" : "Map!")}");

                if (useMySQL != false || bonusX != 0)
                    Server.PrintToChatAll(msgPrefix + $"{(bonusX != 0 ? $"" : $"Rank: [{primaryChatColor}{ranking}{ChatColors.White}] ")}{(timesFinished != 0 && useMySQL == true ? $"Times Finished: [{primaryChatColor}{timesFinished}{ChatColors.White}]" : "")}");

                Server.PrintToChatAll(msgPrefix + $"Time: [{primaryChatColor}{FormatTime(newticks)}{ChatColors.White}] {timeDifference}");

                if (IsAllowedPlayer(player) && playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {beepSound}");

                if (enableReplays == true && enableSRreplayBot == true && GetNumberBeforeSlash(ranking) == 1 && (oldticks > newticks || oldticks == 0))
                {
                    _ = SpawnReplayBot();
                }
            });
        }

        public void AddScoreboardTagToPlayer(CCSPlayerController player, string tag)
        {
            try
            {
                if (string.IsNullOrEmpty(tag))
                    return;

                if (player == null || !player.IsValid)
                    return;

                string originalPlayerName = player.PlayerName;

                string stripedClanTag = RemovePlayerTags(player.Clan ?? "");

                player.Clan = $"{stripedClanTag}{(playerTimers[player.Slot].IsVip ? $"[{customVIPTag}]" : "")}[{tag}]";
                //player.Clan = $"{stripedClanTag}[{tag}]";

                SchemaString<CBasePlayerController> playerName = new SchemaString<CBasePlayerController>(player, "m_iszPlayerName");
                playerName.Set(originalPlayerName + " ");

                AddTimer(0.1f, () =>
                {
                    if (player.IsValid)
                    {
                        Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
                        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                    }
                });

                AddTimer(0.2f, () =>
                {
                    if (player.IsValid) playerName.Set(originalPlayerName);
                });

                AddTimer(0.3f, () =>
                {
                    if (player.IsValid) Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                });

                SharpTimerDebug($"Set Scoreboard Tag for {player.Clan} {player.PlayerName}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in AddScoreboardTagToPlayer: {ex.Message}");
            }
        }

        public void ChangePlayerName(CCSPlayerController player, string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (player == null || !player.IsValid)
                return;

            SchemaString<CBasePlayerController> playerName = new SchemaString<CBasePlayerController>(player, "m_iszPlayerName");
            playerName.Set(name + " ");

            AddTimer(0.1f, () =>
            {
                if (player.IsValid)
                {
                    Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
                    Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                }
            });

            AddTimer(0.2f, () =>
            {
                if (player.IsValid) playerName.Set(name);
            });

            AddTimer(0.3f, () =>
            {
                if (player.IsValid) Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
            });

            SharpTimerDebug($"Changed PlayerName to {player.PlayerName}");
        }

        static void SetMoveType(CCSPlayerController player, MoveType_t nMoveType)
        {
            if (!player.IsValid) return;

            player.PlayerPawn.Value.MoveType = nMoveType; // necessary to maintain client prediction
            player.PlayerPawn.Value.ActualMoveType = nMoveType;
        }

        public char GetRankColorForChat(CCSPlayerController player)
        {
            try
            {
                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                {
                    if (string.IsNullOrEmpty(playerTimer.CachedRank)) return ChatColors.Default;

                    char color = ChatColors.Default;

                    if (playerTimer.CachedRank.Contains("Unranked"))
                        color = ChatColors.Default;
                    else if (playerTimer.CachedRank.Contains("Silver"))
                        color = ChatColors.Silver;
                    else if (playerTimer.CachedRank.Contains("Gold"))
                        color = ChatColors.LightYellow;
                    else if (playerTimer.CachedRank.Contains("Platinum"))
                        color = ChatColors.Green;
                    else if (playerTimer.CachedRank.Contains("Diamond"))
                        color = ChatColors.LightBlue;
                    else if (playerTimer.CachedRank.Contains("Master"))
                        color = ChatColors.Purple;
                    else if (playerTimer.CachedRank.Contains("Legend"))
                        color = ChatColors.Lime;
                    else if (playerTimer.CachedRank.Contains("Royalty"))
                        color = ChatColors.Orange;
                    else if (playerTimer.CachedRank.Contains("God"))
                        color = ChatColors.LightRed;

                    return color;
                }
                else
                {
                    return ChatColors.Default;
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetRankColorForChat: {ex.Message}");
                return ChatColors.Default;
            }
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