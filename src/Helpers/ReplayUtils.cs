using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private void ReplayUpdate(CCSPlayerController player, int timerTicks)
        {
            try
            {
                if (!IsAllowedPlayer(player)) return;

                // Get the player's current position and rotation
                Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
                Vector currentSpeed = player.PlayerPawn.Value.AbsVelocity ?? new Vector(0, 0, 0);
                QAngle currentRotation = player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0);

                // Convert position and rotation to strings
                string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
                string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";
                string speedString = $"{currentSpeed.X} {currentSpeed.Y} {currentSpeed.Z}";

                var buttons = player.Buttons;
                var flags = player.Pawn.Value.Flags;
                var moveType = player.Pawn.Value.MoveType;

                var ReplayFrame = new PlayerReplays.ReplayFrames
                {
                    PositionString = positionString,
                    RotationString = rotationString,
                    SpeedString = speedString,
                    Buttons = buttons,
                    Flags = flags,
                    MoveType = moveType
                };

                playerReplays[player.Slot].replayFrames.Add(ReplayFrame);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in ReplayUpdate: {ex.Message}");
            }
        }

        private void ReplayPlayback(CCSPlayerController player, int plackbackTick)
        {
            try
            {
                if (!IsAllowedPlayer(player)) return;

                //player.LerpTime = 0.0078125f;

                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? value))
                {
                    var replayFrame = playerReplays[player.Slot].replayFrames[plackbackTick];

                    if (((PlayerFlags)replayFrame.Flags & PlayerFlags.FL_ONGROUND) != 0)
                    {
                        SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                    }
                    else
                    {
                        SetMoveType(player, MoveType_t.MOVETYPE_OBSERVER);
                    }

                    if (((PlayerFlags)replayFrame.Flags & PlayerFlags.FL_DUCKING) != 0)
                    {
                        value.MovementService.DuckAmount = 1;
                    }
                    else
                    {
                        value.MovementService.DuckAmount = 0;
                    }

                    player.PlayerPawn.Value.Teleport(ParseVector(replayFrame.PositionString), ParseQAngle(replayFrame.RotationString), ParseVector(replayFrame.SpeedString));

                    var replayButtons = $"{((replayFrame.Buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Duck) != 0 ? "C" : "_")}";

                    if (value.HideKeys != true && value.IsReplaying == true && keysOverlayEnabled == true)
                    {
                        player.PrintToCenter(replayButtons);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in ReplayPlayback: {ex.Message}");
            }
        }

        private void ReplayPlay(CCSPlayerController player)
        {
            try
            {
                int totalFrames = playerReplays[player.Slot].replayFrames.Count;

                if (totalFrames <= 128)
                {
                    OnRecordingStop(player);
                }

                if (playerReplays[player.Slot].CurrentPlaybackFrame >= totalFrames)
                {
                    playerReplays[player.Slot].CurrentPlaybackFrame = 0;
                    Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                    adjustVelocity(player, 0, false);
                }

                if (jumpStatsEnabled) InvalidateJS(player.Slot);
                ReplayPlayback(player, playerReplays[player.Slot].CurrentPlaybackFrame);

                playerReplays[player.Slot].CurrentPlaybackFrame++;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in ReplayPlay: {ex.Message}");
            }
        }

        private void OnRecordingStart(CCSPlayerController player, int bonusX = 0)
        {
            //playerReplays[player.Slot].replayFrames.Clear();
            try
            {
                playerReplays.Remove(player.Slot);
                playerReplays[player.Slot] = new PlayerReplays
                {
                    BonusX = bonusX
                };
                playerTimers[player.Slot].IsRecordingReplay = true;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in OnRecordingStart: {ex.Message}");
            }
        }

        private void OnRecordingStop(CCSPlayerController player)
        {
            try
            {
                playerTimers[player.Slot].IsRecordingReplay = false;
                SetMoveType(player, MoveType_t.MOVETYPE_WALK);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in OnRecordingStop: {ex.Message}");
            }
        }

        private void DumpReplayToJson(CCSPlayerController player, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player))
            {
                SharpTimerError($"Error in DumpReplayToJson: Player not allowed or not on server anymore");
                return;
            }

            string fileName = $"{player.SteamID.ToString()}_replay.json";
            string playerReplaysDirectory = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", bonusX == 0 ? $"{currentMapName}" : $"{currentMapName}_bonus{bonusX}");
            string playerReplaysPath = Path.Join(playerReplaysDirectory, fileName);

            try
            {
                if (!Directory.Exists(playerReplaysDirectory))
                {
                    Directory.CreateDirectory(playerReplaysDirectory);
                }

                var indexedReplayFrames = playerReplays[player.Slot].replayFrames
                    .Select((frame, index) => new IndexedReplayFrames { Index = index, Frame = frame })
                    .ToList();

                using (Stream stream = new FileStream(playerReplaysPath, FileMode.Create))
                {
                    JsonSerializer.Serialize(stream, indexedReplayFrames);
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error during serialization: {ex.Message}");
            }
        }

        private void ReadReplayFromJson(CCSPlayerController player, string steamId)
        {
            string fileName = $"{steamId}_replay.json";
            string playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", currentMapName, fileName);

            try
            {
                if (File.Exists(playerReplaysPath))
                {
                    string jsonString = File.ReadAllText(playerReplaysPath);
                    var indexedReplayFrames = JsonSerializer.Deserialize<List<IndexedReplayFrames>>(jsonString);

                    if (indexedReplayFrames != null)
                    {
                        var replayFrames = indexedReplayFrames
                            .OrderBy(frame => frame.Index)
                            .Select(frame => frame.Frame)
                            .ToList();

                        if (!playerReplays.ContainsKey(player.Slot))
                        {
                            playerReplays[player.Slot] = new PlayerReplays();
                        }
                        playerReplays[player.Slot].replayFrames = replayFrames;
                    }
                    else
                    {
                        SharpTimerError($"Error: Failed to deserialize replay frames from {playerReplaysPath}");
                        Server.NextFrame(() => player.PrintToChat(msgPrefix + $" The requested replay seems to be corrupted"));
                    }
                }
                else
                {
                    SharpTimerError($"File does not exist: {playerReplaysPath}");
                    Server.NextFrame(() => player.PrintToChat(msgPrefix + $" The requested replay does not exist"));
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error during deserialization: {ex.Message}");
            }
        }

        private async Task SpawnReplayBot()
        {
            try
            {
                if (await CheckSRReplay() != true) return;

                Server.NextFrame(() =>
                {
                    startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile = false;
                    foreach (CCSPlayerController bot in connectedReplayBots.Values.ToList())
                    {
                        if (bot != null)
                        {
                            OnPlayerDisconnect(bot, true);
                            if (connectedReplayBots.TryGetValue(bot.Slot, out var someValue)) connectedReplayBots.Remove(bot.Slot);
                        }
                    }

                    Server.ExecuteCommand("sv_cheats 1");
                    Server.ExecuteCommand("bot_kick");
                    Server.ExecuteCommand("bot_quota_mode fill");
                    Server.ExecuteCommand("bot_quota 1");
                    Server.ExecuteCommand("bot_join_team CT");
                    Server.ExecuteCommand("bot_add_ct");
                    Server.ExecuteCommand("bot_stop 1");
                    Server.ExecuteCommand("bot_controllable 0");
                    Server.ExecuteCommand("sv_cheats 0");

                    AddTimer(3.0f, () =>
                    {
                        bool foundBot = false;
                        SharpTimerDebug($"Trying to find replay bot!");
                        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

                        foreach (var tempPlayer in playerEntities)
                        {
                            if (tempPlayer == null || !tempPlayer.IsValid || !tempPlayer.IsBot || tempPlayer.IsHLTV) continue;

                            if (tempPlayer.UserId.HasValue)
                            {
                                if (foundBot == true)
                                {
                                    OnPlayerDisconnect(tempPlayer, true);
                                    Server.ExecuteCommand($"kickid {tempPlayer.Slot}");
                                    SharpTimerDebug($"Kicking unused replay bot!");
                                }
                                else
                                {
                                    SharpTimerDebug($"Found replay bot!");
                                    OnReplayBotConnect(tempPlayer);
                                    tempPlayer.PlayerPawn.Value.Bot.IsSleeping = true;
                                    tempPlayer.PlayerPawn.Value.Bot.AllowActive = false;
                                    tempPlayer.RemoveWeapons();
                                    foundBot = true;
                                    startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile = true;
                                }
                            }
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in SpawnReplayBot: {ex.Message}");
            }
        }

        private void OnReplayBotConnect(CCSPlayerController bot)
        {
            try
            {
                AddTimer(3.0f, () =>
                {
                    OnPlayerConnect(bot, true);
                    connectedReplayBots[bot.Slot] = new CCSPlayerController(bot.Handle);
                    ChangePlayerName(bot, "SERVER RECORD REPLAY");
                    playerTimers[bot.Slot].IsTimerBlocked = true;
                    _ = ReplaySRHandler(bot);
                    SharpTimerDebug($"Starting replay for {bot.PlayerName}");
                });
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in OnReplayBotConnect: {ex.Message}");
            }
        }

        public async Task<bool> CheckSRReplay(string topSteamID = "x")
        {
            var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");

            if (useMySQL)
            {
                (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
            }
            else
            {
                (srSteamID, srPlayerName, srTime) = GetMapRecordSteamID();
            }

            if ((srSteamID == "null" || srPlayerName == "null" || srTime == "null") && topSteamID != "x") return false;

            string fileName = $"{(topSteamID == "x" ? $"{srSteamID}" : $"{topSteamID}")}_replay.json";
            string playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", currentMapName, fileName);

            try
            {
                if (File.Exists(playerReplaysPath))
                {
                    string jsonString = File.ReadAllText(playerReplaysPath);
                    var indexedReplayFrames = JsonSerializer.Deserialize<List<IndexedReplayFrames>>(jsonString);

                    if (indexedReplayFrames != null)
                    {
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Error: Failed to deserialize replay frames from {playerReplaysPath}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"File does not exist: {playerReplaysPath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
                return false;
            }
        }
    }
}