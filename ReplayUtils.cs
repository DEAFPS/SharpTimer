using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Security;
using System.Text.Json;
using System.Text.RegularExpressions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private void ReplayUpdate(CCSPlayerController player, int timerTicks)
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

        private void ReplayPlayback(CCSPlayerController player, int plackbackTick)
        {
            if (!IsAllowedPlayer(player)) return;

            //player.LerpTime = 0.0078125f;

            var replayFrame = playerReplays[player.Slot].replayFrames[plackbackTick];

            if (((PlayerFlags)replayFrame.Flags & PlayerFlags.FL_ONGROUND) != 0)
                player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
            else
                player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_OBSERVER;

            if (Distance(player.Pawn.Value.CBodyComponent.SceneNode.AbsOrigin, ParseVector(replayFrame.PositionString)) > 20)
                player.PlayerPawn.Value.Teleport(ParseVector(replayFrame.PositionString), ParseQAngle(replayFrame.RotationString), ParseVector(replayFrame.SpeedString));
            else
                player.PlayerPawn.Value.Teleport(new Vector(nint.Zero), ParseQAngle(replayFrame.RotationString), ParseVector(replayFrame.SpeedString));

            var replayButtons = $"{((replayFrame.Buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                $"{((replayFrame.Buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                $"{((replayFrame.Buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                $"{((replayFrame.Buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                $"{((replayFrame.Buttons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                                $"{((replayFrame.Buttons & PlayerButtons.Duck) != 0 ? "C" : "_")}";

            if (playerTimers[player.Slot].HideKeys != true && playerTimers[player.Slot].IsReplaying == true && keysOverlayEnabled == true)
            {
                player.PrintToCenter(replayButtons);
            }

            //VirtualFunctions.ClientPrint(player.Handle, HudDestination.Alert, , 0, 0, 0, 0);
        }

        private void ReplayPlay(CCSPlayerController player)
        {
            int totalFrames = playerReplays[player.Slot].replayFrames.Count;

            if (playerReplays[player.Slot].CurrentPlaybackFrame >= totalFrames)
            {
                playerReplays[player.Slot].CurrentPlaybackFrame = 0;
                Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                adjustVelocity(player, 0, false);
            }

            ReplayPlayback(player, playerReplays[player.Slot].CurrentPlaybackFrame);

            playerReplays[player.Slot].CurrentPlaybackFrame++;
        }

        private void OnRecordingStart(CCSPlayerController player, int bonusX = 0)
        {
            //playerReplays[player.Slot].replayFrames.Clear();
            playerReplays.Remove(player.Slot);
            playerReplays[player.Slot] = new PlayerReplays
            {
                BonusX = bonusX
            };
            playerTimers[player.Slot].IsRecordingReplay = true;
        }

        private void OnRecordingStop(CCSPlayerController player)
        {
            playerTimers[player.Slot].IsRecordingReplay = false;
        }

        private void DumpReplayToJson(CCSPlayerController player, int bonusX = 0)
        {
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
                Console.WriteLine($"Error during serialization: {ex.Message}");
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
                        Console.WriteLine($"Error: Failed to deserialize replay frames from {playerReplaysPath}");
                        Server.NextFrame(() => player.PrintToChat(msgPrefix + $" The requested replay seems to be corrupted"));
                    }
                }
                else
                {
                    Console.WriteLine($"File does not exist: {playerReplaysPath}");
                    Server.NextFrame(() => player.PrintToChat(msgPrefix + $" The requested replay does not exist"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
            }
        }
    }
}