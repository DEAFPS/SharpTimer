using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("css_dp_timers", "Replay your last pb")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void DeepPrintPlayerTimers(CCSPlayerController? player, CommandInfo command)
        {
            Console.WriteLine("Printing Player Timers:");
            foreach (var kvp in playerTimers)
            {
                Console.WriteLine($"PlayerSlot: {kvp.Key}");
                Console.WriteLine($"  IsTimerRunning: {kvp.Value.IsTimerRunning}");
                Console.WriteLine($"  IsTimerBlocked: {kvp.Value.IsTimerBlocked}");
                Console.WriteLine($"  TimerTicks: {kvp.Value.TimerTicks}");
                Console.WriteLine($"  IsBonusTimerRunning: {kvp.Value.IsBonusTimerRunning}");
                Console.WriteLine($"  BonusTimerTicks: {kvp.Value.BonusTimerTicks}");
                Console.WriteLine($"  BonusStage: {kvp.Value.BonusStage}");

                Console.WriteLine($"  IsReplaying: {kvp.Value.IsReplaying}");
                Console.WriteLine($"  IsRecordingReplay: {kvp.Value.IsRecordingReplay}");

                Console.WriteLine($"  ReplayHUDString: {kvp.Value.ReplayHUDString}");
                Console.WriteLine($"  RankHUDString: {kvp.Value.RankHUDString}");
                Console.WriteLine($"  IsRankPbCached: {kvp.Value.IsRankPbCached}");
                Console.WriteLine($"  IsSpecTargetCached: {kvp.Value.IsSpecTargetCached}");
                Console.WriteLine($"  PreSpeed: {kvp.Value.PreSpeed}");
                Console.WriteLine($"  PB: {kvp.Value.PB}");

                Console.WriteLine($"  TicksInAir: {kvp.Value.TicksInAir}");
                Console.WriteLine($"  CheckpointIndex: {kvp.Value.CheckpointIndex}");

                Console.WriteLine($"  StageTimes:");
                if (kvp.Value.StageTimes != null)
                {
                    foreach (var stageTime in kvp.Value.StageTimes)
                    {
                        Console.WriteLine($"    Stage {stageTime.Key}: {stageTime.Value}");
                    }
                }

                Console.WriteLine($"  StageVelos:");
                if (kvp.Value.StageVelos != null)
                {
                    foreach (var stageVelo in kvp.Value.StageVelos)
                    {
                        Console.WriteLine($"    Stage {stageVelo.Key}: {stageVelo.Value}");
                    }
                }

                Console.WriteLine($"  CurrentMapStage: {kvp.Value.CurrentMapStage}");
                Console.WriteLine($"  CurrentMapCheckpoint: {kvp.Value.CurrentMapCheckpoint}");
                Console.WriteLine($"  MovementService: {kvp.Value.MovementService}");

                Console.WriteLine($"  Azerty: {kvp.Value.Azerty}");
                Console.WriteLine($"  HideTimerHud: {kvp.Value.HideTimerHud}");
                Console.WriteLine($"  HideKeys: {kvp.Value.HideKeys}");
                Console.WriteLine($"  SoundsEnabled: {kvp.Value.SoundsEnabled}");
                Console.WriteLine($"  TimesConnected: {kvp.Value.TimesConnected}");
                Console.WriteLine($"  TicksSinceLastCmd: {kvp.Value.TicksSinceLastCmd}");

                Console.WriteLine($"  IsTester: {kvp.Value.IsTester}");
                Console.WriteLine($"  TesterSparkleGif: {kvp.Value.TesterSparkleGif}");
                Console.WriteLine($"  TesterPausedGif: {kvp.Value.TesterPausedGif}");

                Console.WriteLine($"  IsVip: {kvp.Value.IsVip}");
                Console.WriteLine($"  VipReplayGif: {kvp.Value.VipReplayGif}");
                Console.WriteLine($"  VipPausedGif: {kvp.Value.VipPausedGif}");

                Console.WriteLine($"  IsNoclipEnabled: {kvp.Value.IsNoclipEnabled}");
                Console.WriteLine($"  IsAddingStartZone: {kvp.Value.IsAddingStartZone}");
                Console.WriteLine($"  StartZoneC1: {kvp.Value.StartZoneC1}");
                Console.WriteLine($"  StartZoneC2: {kvp.Value.StartZoneC2}");
                Console.WriteLine($"  IsAddingEndZone: {kvp.Value.IsAddingEndZone}");
                Console.WriteLine($"  EndZoneC1: {kvp.Value.EndZoneC1}");
                Console.WriteLine($"  EndZoneC2: {kvp.Value.EndZoneC2}");
                Console.WriteLine($"  RespawnPos: {kvp.Value.RespawnPos}");

                Console.WriteLine(); // Add a line break between players
            }
            Console.WriteLine("End of Player Timers");
        }

        [ConsoleCommand("css_replaypb", "Replay your last pb")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplaySelfCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (!playerTimers[player.Slot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            string fileName = $"{player.SteamID}_replay.json"; //dirty temp fix
            string playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", currentMapName, fileName);
            if (!File.Exists(playerReplaysPath))
            {
                player.PrintToChat(msgPrefix + $" You dont have any saved PB replay yet!");
                return;
            }

            playerReplays.Remove(player.Slot);
            playerReplays[player.Slot] = new PlayerReplays();
            ReadReplayFromJson(player, player.SteamID.ToString());

            playerTimers[player.Slot].IsReplaying = playerTimers[player.Slot].IsReplaying ? false : true;
            playerTimers[player.Slot].ReplayHUDString = $"{player.PlayerName} | {playerTimers[player.Slot].PB}";

            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;
            playerReplays[player.Slot].CurrentPlaybackFrame = 0;
            if (stageTriggers.Any()) playerTimers[player.Slot].StageTimes.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Any()) playerTimers[player.Slot].StageVelos.Clear(); //remove previous stage times if the map has stages
            player.PrintToChat(msgPrefix + $" Replaying your Personal Best, type {primaryChatColor}!stopreplay {ChatColors.White}to exit the replay");
        }

        [ConsoleCommand("css_replaysr", "Replay server map record")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public async void ReplaySRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (!playerTimers[player.Slot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            _ = ReplaySRHandler(player);
        }

        public async Task ReplaySRHandler(CCSPlayerController player)
        {
            playerReplays.Remove(player.Slot);
            playerReplays[player.Slot] = new PlayerReplays();

            var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");

            if (useMySQL)
            {
                (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
            }
            else
            {
                (srSteamID, srPlayerName, srTime) = GetMapRecordSteamID();
            }

            if (srSteamID == "null" || srPlayerName == "null" || srTime == "null")
            {
                Server.NextFrame(() => player.PrintToChat(msgPrefix + $"No Server Record to replay!"));
                return;
            }

            ReadReplayFromJson(player, srSteamID);

            if (!playerReplays[player.Slot].replayFrames.Any()) return;

            if (useMySQL) _ = GetReplayVIPGif(srSteamID, player.Slot);

            playerTimers[player.Slot].IsReplaying = playerTimers[player.Slot].IsReplaying ? false : true;
            playerTimers[player.Slot].ReplayHUDString = $"{srPlayerName} | {srTime}";

            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;
            playerReplays[player.Slot].CurrentPlaybackFrame = 0;
            if (stageTriggers.Any()) playerTimers[player.Slot].StageTimes.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Any()) playerTimers[player.Slot].StageVelos.Clear(); //remove previous stage times if the map has stages
            Server.NextFrame(() => player.PrintToChat(msgPrefix + $" Replaying the Server Map Record, type {primaryChatColor}!stopreplay {ChatColors.White}to exit the replay"));
        }

        [ConsoleCommand("css_replaytop", "Replay a top 10 server map record")]
        [CommandHelper(minArgs: 1, usage: "[1-10]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayTop10SRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (!playerTimers[player.Slot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            string arg = command.ArgByIndex(1);

            _ = ReplayTop10SRHandler(player, arg);
        }

        public async Task ReplayTop10SRHandler(CCSPlayerController player, string arg)
        {
            if (int.TryParse(arg, out int top10) && top10 > 0 && top10 <= 10)
            {
                playerReplays.Remove(player.Slot);
                playerReplays[player.Slot] = new PlayerReplays();

                var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");

                if (useMySQL)
                {
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase(0, top10);
                }
                else
                {
                    (srSteamID, srPlayerName, srTime) = GetMapRecordSteamID();
                }

                if (srSteamID == "null" || srPlayerName == "null" || srTime == "null")
                {
                    Server.NextFrame(() => player.PrintToChat(msgPrefix + $"No Server Record to replay!"));
                    return;
                }

                ReadReplayFromJson(player, srSteamID);

                if (!playerReplays[player.Slot].replayFrames.Any()) return;

                if (useMySQL) _ = GetReplayVIPGif(srSteamID, player.Slot);

                playerTimers[player.Slot].IsReplaying = playerTimers[player.Slot].IsReplaying ? false : true;
                playerTimers[player.Slot].ReplayHUDString = $"{srPlayerName} | {srTime}";

                playerTimers[player.Slot].IsTimerRunning = false;
                playerTimers[player.Slot].TimerTicks = 0;
                playerTimers[player.Slot].IsBonusTimerRunning = false;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerReplays[player.Slot].CurrentPlaybackFrame = 0;
                if (stageTriggers.Any()) playerTimers[player.Slot].StageTimes.Clear(); //remove previous stage times if the map has stages
                if (stageTriggers.Any()) playerTimers[player.Slot].StageVelos.Clear(); //remove previous stage times if the map has stages
                Server.NextFrame(() => player.PrintToChat(msgPrefix + $" Replaying the Server Top {top10}, type {primaryChatColor}!stopreplay {ChatColors.White}to exit the replay"));
            }
            else
            {
                Server.NextFrame(() => player.PrintToChat(msgPrefix + $"Please enter a valid top 10 replay!"));
            }
        }

        [ConsoleCommand("css_stopreplay", "stops the current replay")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void StopReplayCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (!playerTimers[player.Slot].IsTimerBlocked || !playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" No Replay playing currently");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Ending Replay!");
                playerTimers[player.Slot].IsReplaying = false;
                if (player.PlayerPawn.Value.MoveType != MoveType_t.MOVETYPE_WALK) player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                RespawnPlayer(player, command);
                playerReplays.Remove(player.Slot);
                playerReplays[player.Slot] = new PlayerReplays();
                playerTimers[player.Slot].IsTimerRunning = false;
                playerTimers[player.Slot].TimerTicks = 0;
                playerTimers[player.Slot].IsBonusTimerRunning = false;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerReplays[player.Slot].CurrentPlaybackFrame = 0;
                if (stageTriggers.Any()) playerTimers[player.Slot].StageTimes.Clear(); //remove previous stage times if the map has stages
                if (stageTriggers.Any()) playerTimers[player.Slot].StageVelos.Clear(); //remove previous stage times if the map has stages
            }
        }

        [ConsoleCommand("css_sthelp", "Prints all commands for the player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void HelpCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            PrintAllEnabledCommands(player);
        }

        /* [ConsoleCommand("css_spec", "Moves you to Spectator")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SpecCommand(CCSPlayerController? player, CommandInfo command)
        {
            if ((CsTeam)player.TeamNum == CsTeam.Spectator)
            {
                player.ChangeTeam(CsTeam.CounterTerrorist);
                player.PrintToChat(msgPrefix + $"Moving you to CT");
            }
            else
            {
                player.ChangeTeam(CsTeam.Spectator);
                player.PrintToChat(msgPrefix + $"Moving you to Spectator");
            }
        } */

        [ConsoleCommand("css_addstartzone", "Adds a startzone to the mapdata.json file")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddStartZoneCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (playerTimers[player.Slot].IsAddingStartZone == true)
            {
                playerTimers[player.Slot].IsAddingStartZone = false;
                playerTimers[player.Slot].IsAddingEndZone = false;
                playerTimers[player.Slot].StartZoneC1 = "";
                playerTimers[player.Slot].StartZoneC2 = "";
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Grey}Startzone cancelled...");
            }
            else
            {
                playerTimers[player.Slot].StartZoneC1 = "";
                playerTimers[player.Slot].StartZoneC2 = "";
                playerTimers[player.Slot].IsAddingStartZone = true;
                playerTimers[player.Slot].IsAddingEndZone = false;
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default} Please stand on one of the opposite start zone corners and type {primaryChatColor}!c1 & !c2");
                player.PrintToChat($" {ChatColors.Grey}Type !addendzone again to cancel...");
                player.PrintToChat($" {ChatColors.Grey}Commands:!addstartzone, !addendzone,");
                player.PrintToChat($" {ChatColors.Grey}!addrespawnpos, !savezones");
            }
        }

        [ConsoleCommand("css_addendzone", "Adds a endzone to the mapdata.json file")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddEndZoneCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (playerTimers[player.Slot].IsAddingEndZone == true)
            {
                playerTimers[player.Slot].IsAddingStartZone = false;
                playerTimers[player.Slot].IsAddingEndZone = false;
                playerTimers[player.Slot].EndZoneC1 = "";
                playerTimers[player.Slot].EndZoneC2 = "";
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Grey}Endzone cancelled...");
            }
            else
            {
                playerTimers[player.Slot].EndZoneC1 = "";
                playerTimers[player.Slot].EndZoneC2 = "";
                playerTimers[player.Slot].IsAddingStartZone = false;
                playerTimers[player.Slot].IsAddingEndZone = true;
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default} Please stand on one of the opposite end zone corners and type {primaryChatColor}!c1 & !c2");
                player.PrintToChat($" {ChatColors.Grey}Type !addendzone again to cancel...");
                player.PrintToChat($" {ChatColors.Grey}Commands:!addstartzone, !addendzone,");
                player.PrintToChat($" {ChatColors.Grey}!addrespawnpos, !savezones");
            }
        }

        [ConsoleCommand("css_addrespawnpos", "Adds a RespawnPos to the mapdata.json file")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddRespawnPosCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            // Get the player's current position
            Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);

            // Convert position
            string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
            playerTimers[player.Slot].RespawnPos = positionString;
            player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default} RespawnPos added!");
        }

        [ConsoleCommand("css_savezones", "Saves defined zones")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SaveZonesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (playerTimers[player.Slot].EndZoneC1 == null || playerTimers[player.Slot].EndZoneC2 == null || playerTimers[player.Slot].StartZoneC1 == null || playerTimers[player.Slot].StartZoneC2 == null || playerTimers[player.Slot].RespawnPos == null)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Red} Please make sure you have done all 3 zoning steps (!addstartzone, !addendzone, !addrespawnpos)");
                return;
            }

            // Create a new MapInfo object with the necessary data
            MapInfo newMapInfo = new MapInfo
            {
                MapStartC1 = playerTimers[player.Slot].StartZoneC1,
                MapStartC2 = playerTimers[player.Slot].StartZoneC2,
                MapEndC1 = playerTimers[player.Slot].EndZoneC1,
                MapEndC2 = playerTimers[player.Slot].EndZoneC2,
                RespawnPos = playerTimers[player.Slot].RespawnPos
            };

            // Load existing map data from the JSON file
            string mapdataFileName = $"SharpTimer/MapData/{currentMapName}.json"; // Use the map name in the filename
            string mapdataPath = Path.Join(gameDir + "/csgo/cfg", mapdataFileName);

            // Save the updated data back to the JSON file without including the map name inside the JSON
            string updatedJson = JsonSerializer.Serialize(newMapInfo, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(mapdataPath, updatedJson);

            // For example, you might want to print a message to the player
            player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default}Zones saved successfully! {ChatColors.Grey}Reloading data...");
            Server.ExecuteCommand("mp_restartgame 1");
        }

        [ConsoleCommand("css_c1", "Adds a zone to the mapdata.json file")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddZoneCorner1Command(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            // Get the player's current position
            Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);

            // Convert position
            string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";

            //Start Zone
            if (playerTimers[player.Slot].IsAddingStartZone == true)
            {
                playerTimers[player.Slot].StartZoneC1 = positionString;
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default} Start Zone Corner 1 added!");
            }

            //End Zone
            if (playerTimers[player.Slot].IsAddingEndZone == true)
            {
                playerTimers[player.Slot].EndZoneC1 = positionString;
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default} End Zone Corner 1 added!");
            }
        }

        [ConsoleCommand("css_c2", "Adds a zone to the mapdata.json file")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddZoneCorner2Command(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            // Get the player's current position
            Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);

            // Convert position
            string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";

            //Start Zone
            if (playerTimers[player.Slot].IsAddingStartZone == true)
            {
                playerTimers[player.Slot].StartZoneC2 = positionString;
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default} Start Zone Corner 2 added!");
                player.PrintToChat($" {ChatColors.Grey}Commands:!addstartzone, !addendzone, !addrespawnpos, !savezones");
                playerTimers[player.Slot].IsAddingStartZone = false;
            }

            //End Zone
            if (playerTimers[player.Slot].IsAddingEndZone == true)
            {
                playerTimers[player.Slot].EndZoneC2 = positionString;
                player.PrintToChat($" {ChatColors.LightPurple}[ZONE TOOL]{ChatColors.Default}End Zone Corner 2 added!");
                player.PrintToChat($" {ChatColors.Grey}Commands:!addstartzone, !addendzone, !addrespawnpos, !savezones");
                playerTimers[player.Slot].IsAddingEndZone = false;
            }
        }

        [ConsoleCommand("css_hud", "Draws/Hides The timer HUD")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void HUDSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;
            SharpTimerDebug($"{player.PlayerName} calling css_hud...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            playerTimers[player.Slot].HideTimerHud = playerTimers[player.Slot].HideTimerHud ? false : true;

            player.PrintToChat($"Hide Timer HUD set to: {primaryChatColor}{playerTimers[player.Slot].HideTimerHud}");
            SharpTimerDebug($"Hide Timer HUD set to: {playerTimers[player.Slot].HideTimerHud} for {player.PlayerName}");

            if (useMySQL == true)
            {
                _ = SetPlayerStats(player, player.SteamID.ToString(), player.PlayerName, player.Slot);
            }
        }

        [ConsoleCommand("css_keys", "Draws/Hides HUD Keys")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void KeysSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;
            SharpTimerDebug($"{player.PlayerName} calling css_keys...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            playerTimers[player.Slot].HideKeys = playerTimers[player.Slot].HideKeys ? false : true;

            player.PrintToChat($"Hide Timer HUD set to: {primaryChatColor}{playerTimers[player.Slot].HideKeys}");
            SharpTimerDebug($"Hide Timer HUD set to: {playerTimers[player.Slot].HideKeys} for {player.PlayerName}");

            if (useMySQL == true)
            {
                _ = SetPlayerStats(player, player.SteamID.ToString(), player.PlayerName, player.Slot);
            }

        }

        [ConsoleCommand("css_sounds", "Toggles Sounds")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SoundsSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;
            SharpTimerDebug($"{player.PlayerName} calling css_sounds...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            playerTimers[player.Slot].SoundsEnabled = playerTimers[player.Slot].SoundsEnabled ? false : true;

            player.PrintToChat($"Timer Sounds set to: {primaryChatColor}{playerTimers[player.Slot].SoundsEnabled}");
            SharpTimerDebug($"Timer Sounds set to: {playerTimers[player.Slot].SoundsEnabled} for {player.PlayerName}");

            if (useMySQL == true)
            {
                _ = SetPlayerStats(player, player.SteamID.ToString(), player.PlayerName, player.Slot);
            }

        }

        [ConsoleCommand("css_fov", "Sets the player's FOV")]
        [CommandHelper(minArgs: 1, usage: "[fov]")]
        public void FovCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || fovChangerEnabled == false) return;

            if (!Int32.TryParse(command.GetArg(1), out var desiredFov)) return;

            player.DesiredFOV = (uint)desiredFov;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
        }

        [ConsoleCommand("css_top", "Prints top players of this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopRecords(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || topEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_top...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            _ = PrintTopRecordsHandler(player);
        }

        [ConsoleCommand("css_points", "Prints top points")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopPoints(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || globalRankeEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_points...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            _ = PrintTop10PlayerPoints(player);
        }

        [ConsoleCommand("css_topbonus", "Prints top players of this map bonus")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopBonusRecords(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || topEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_topbonus...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!int.TryParse(command.ArgString, out int bonusX))
            {
                SharpTimerDebug("css_topbonus conversion failed. The input string is not a valid integer.");
                player.PrintToChat(msgPrefix + $" Please enter a valid Bonus stage i.e: {primaryChatColor}!topbonus 1");
                return;
            }

            _ = PrintTopRecordsHandler(player, bonusX);
        }

        public async Task PrintTopRecordsHandler(CCSPlayerController? player, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player) || topEnabled == false) return;
            SharpTimerDebug($"Handling !top for {player.PlayerName}");
            string currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";
            Dictionary<string, PlayerRecord> sortedRecords;
            if (useMySQL == true)
            {
                sortedRecords = await GetSortedRecordsFromDatabase(bonusX);
            }
            else
            {
                sortedRecords = GetSortedRecords(bonusX);
            }

            if (sortedRecords.Count == 0 && IsAllowedPlayer(player))
            {
                Server.NextFrame(() => player.PrintToChat(msgPrefix + $" No records available for{(bonusX != 0 ? $" Bonus {bonusX} on" : "")} {currentMapName}."));
                return;
            }

            Server.NextFrame(() =>
            {
                if (IsAllowedPlayer(player)) player.PrintToChat($"{msgPrefix} Top 10 Records for{(bonusX != 0 ? $" Bonus {bonusX} on" : "")} {currentMapName}:");
            });

            int rank = 1;

            foreach (var kvp in sortedRecords.Take(10))
            {
                string playerName = kvp.Value.PlayerName; // Get the player name from the dictionary value
                int timerTicks = kvp.Value.TimerTicks; // Get the timer ticks from the dictionary value

                Server.NextFrame(() =>
                {
                    if (IsAllowedPlayer(player)) player.PrintToChat(msgPrefix + $" #{rank}: {primaryChatColor}{playerName} {ChatColors.White}- {primaryChatColor}{FormatTime(timerTicks)}");
                    rank++;
                });
            }
        }

        [ConsoleCommand("css_rank", "Tells you your rank on this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RankCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || rankEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_rank...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            _ = RankCommandHandler(player, player.SteamID.ToString(), player.Slot, player.PlayerName);
        }

        public async Task RankCommandHandler(CCSPlayerController? player, string steamId, int playerSlot, string playerName, bool sendRankToHUD = false)
        {
            if (!IsAllowedPlayer(player)) return;
            SharpTimerDebug($"Handling !rank for {playerName}...");
            string ranking = await GetPlayerPlacementWithTotal(player, steamId, playerName);
            string rankIcon = await GetPlayerPlacementWithTotal(player, steamId, playerName, true);

            int pbTicks;
            if (useMySQL == false)
            {
                pbTicks = GetPreviousPlayerRecord(player);
            }
            else
            {
                pbTicks = await GetPreviousPlayerRecordFromDatabase(player, steamId, currentMapName, playerName);
            }

            string rankHUDstring = $" {(!string.IsNullOrEmpty(rankIcon) ? $" {rankIcon}" : "")}" +
                       $"<font class='fontSize-s' color='gray'>" +
                       $"{((!string.IsNullOrEmpty(ranking) && string.IsNullOrEmpty(rankIcon)) ? $" {ranking}" : "")}" +
                       $"{((!string.IsNullOrEmpty(ranking) && !string.IsNullOrEmpty(rankIcon)) ? " | " + ranking : "")}" +
                       $"{(pbTicks != 0 ? $" | {FormatTime(pbTicks)}" : "")}";

            Server.NextFrame(() =>
            {
                if (!IsAllowedPlayer(player)) return;
                playerTimers[playerSlot].RankHUDString = rankHUDstring;
                playerTimers[playerSlot].PB = FormatTime(pbTicks);
            });

            if (sendRankToHUD == false)
            {
                Server.NextFrame(() =>
                {
                    if (!IsAllowedPlayer(player)) return;
                    player.PrintToChat(msgPrefix + $" You are currently {primaryChatColor}{ranking}");
                    if (pbTicks != 0) player.PrintToChat(msgPrefix + $" Your current PB: {primaryChatColor}{FormatTime(pbTicks)}");
                });
            }
        }

        [ConsoleCommand("css_sr", "Tells you the Server record on this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || rankEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_sr...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            _ = SRCommandHandler(player);
        }

        public async Task SRCommandHandler(CCSPlayerController? player)
        {
            if (!IsAllowedPlayer(player) || rankEnabled == false) return;
            SharpTimerDebug($"Handling !sr for {player.PlayerName}...");
            Dictionary<string, PlayerRecord> sortedRecords;
            if (useMySQL == false)
            {
                sortedRecords = GetSortedRecords();
            }
            else
            {
                sortedRecords = await GetSortedRecordsFromDatabase();
            }

            if (sortedRecords.Count == 0)
            {
                return;
            }

            Server.NextFrame(() =>
            {
                if (!IsAllowedPlayer(player)) return;
                player.PrintToChat($"{msgPrefix} Current Server Record on {primaryChatColor}{currentMapName}{ChatColors.White}: ");
            });

            foreach (var kvp in sortedRecords.Take(1))
            {
                string playerName = kvp.Value.PlayerName; // Get the player name from the dictionary value
                int timerTicks = kvp.Value.TimerTicks; // Get the timer ticks from the dictionary value
                Server.NextFrame(() =>
                {
                    if (!IsAllowedPlayer(player)) return;
                    player.PrintToChat(msgPrefix + $" {primaryChatColor}{playerName} {ChatColors.White}- {primaryChatColor}{FormatTime(timerTicks)}");
                });
            }
        }

        [ConsoleCommand("css_rb", "Teleports you to Bonus start")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RespawnBonusPlayer(CCSPlayerController? player, CommandInfo command)
        {
            try
            {
                if (!IsAllowedPlayer(player) || respawnEnabled == false) return;
                SharpTimerDebug($"{player.PlayerName} calling css_rb...");

                if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
                {
                    player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                    return;
                }

                if (playerTimers[player.Slot].IsReplaying)
                {
                    player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                    return;
                }

                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                if (!int.TryParse(command.ArgString, out int bonusX))
                {
                    SharpTimerDebug("css_rb conversion failed. The input string is not a valid integer.");
                    player.PrintToChat(msgPrefix + $" Please enter a valid Bonus stage i.e: {primaryChatColor}!rb <index>");
                    return;
                }

                // Remove checkpoints for the current player
                playerCheckpoints.Remove(player.Slot);

                if (bonusRespawnPoses[bonusX] != null)
                {
                    if (bonusRespawnAngs.TryGetValue(bonusX, out QAngle bonusAng) && bonusAng != null)
                    {
                        player.PlayerPawn.Value.Teleport(bonusRespawnPoses[bonusX], bonusRespawnAngs[bonusX], new Vector(0, 0, 0));
                    }
                    else
                    {
                        player.PlayerPawn.Value.Teleport(bonusRespawnPoses[bonusX], new QAngle(player.PlayerPawn.Value.EyeAngles.X, player.PlayerPawn.Value.EyeAngles.Y, player.PlayerPawn.Value.EyeAngles.Z) ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    }
                    SharpTimerDebug($"{player.PlayerName} css_rb {bonusX} to {bonusRespawnPoses[bonusX]}");
                }
                else
                {
                    player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnBonusPos with index {bonusX} found for current map!");
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                });

                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {respawnSound}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in RespawnBonusPlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_setresp", "Saves a custom respawn point within the start trigger")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetRespawnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEnabled == false) return;

            SharpTimerDebug($"{player.PlayerName} calling css_rank...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            if (useTriggers == false)
            {
                player.PrintToChat(msgPrefix + $" Current Map is using manual zones");
                return;
            }

            // Get the player's current position and rotation
            Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
            QAngle currentRotation = player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0);

            if (IsVectorInsideBox(currentPosition, currentMapStartTriggerMaxs, currentMapStartTriggerMins))
            {
                // Convert position and rotation to strings
                string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
                string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";

                playerTimers[player.Slot].SetRespawnPos = positionString;
                playerTimers[player.Slot].SetRespawnAng = rotationString;
                player.PrintToChat(msgPrefix + $" Saved custom Start Zone RespawnPos!");
            }
            else
            {
                player.PrintToChat(msgPrefix + $" You are not inside the Start Zone!");
            }
        }

        [ConsoleCommand("css_stage", "Teleports you to a stage")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TPtoStagePlayer(CCSPlayerController? player, CommandInfo command)
        {
            try
            {
                if (!IsAllowedPlayer(player) || respawnEnabled == false) return;
                SharpTimerDebug($"{player.PlayerName} calling css_stage...");

                if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
                {
                    player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                    return;
                }

                if (playerTimers[player.Slot].IsReplaying)
                {
                    player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                    return;
                }

                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                if (playerTimers[player.Slot].IsTimerBlocked == false)
                {
                    SharpTimerDebug($"css_stage failed. Player {player.PlayerName} had timer running.");
                    player.PrintToChat(msgPrefix + $" Please stop your timer first using: {primaryChatColor}!timer");
                    return;
                }

                if (!int.TryParse(command.ArgString, out int stageX))
                {
                    SharpTimerDebug("css_stage conversion failed. The input string is not a valid integer.");
                    player.PrintToChat(msgPrefix + $" Please enter a valid stage i.e: {primaryChatColor}!stage <index>");
                    return;
                }

                if (useStageTriggers == false)
                {
                    SharpTimerDebug("css_stage failed useStages is false.");
                    player.PrintToChat(msgPrefix + $" Stages unavalible");
                    return;
                }

                // Remove checkpoints for the current player
                playerCheckpoints.Remove(player.Slot);

                if (stageTriggerPoses.TryGetValue(stageX, out Vector stagePos) && stagePos != null)
                {
                    player.PlayerPawn.Value.Teleport(stagePos, stageTriggerAngs[stageX] ?? player.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 0));
                    SharpTimerDebug($"{player.PlayerName} css_stage {stageX} to {stagePos}");
                }
                else
                {
                    player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnStagePos with index {stageX} found for current map!");
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                });

                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {respawnSound}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in TPtoStagePlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_r", "Teleports you to start")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RespawnPlayer(CCSPlayerController? player, CommandInfo command)
        {
            try
            {
                if (!IsAllowedPlayer(player) || respawnEnabled == false) return;
                SharpTimerDebug($"{player.PlayerName} calling css_r...");

                if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
                {
                    player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                    return;
                }

                if (playerTimers[player.Slot].IsReplaying)
                {
                    player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                    return;
                }

                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                // Remove checkpoints for the current player
                playerCheckpoints.Remove(player.Slot);
                if (stageTriggerCount != 0 || cpTriggerCount != 0)//remove previous stage times if the map has stages
                {
                    playerTimers[player.Slot].StageTimes.Clear();
                }

                if (currentRespawnPos != null && playerTimers[player.Slot].SetRespawnPos == null)
                {
                    if (currentRespawnAng != null)
                    {
                        player.PlayerPawn.Value.Teleport(currentRespawnPos, currentRespawnAng, new Vector(0, 0, 0));
                    }
                    else
                    {
                        player.PlayerPawn.Value.Teleport(currentRespawnPos, player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    }
                    SharpTimerDebug($"{player.PlayerName} css_r to {currentRespawnPos}");
                }
                else
                {
                    if (playerTimers[player.Slot].SetRespawnPos != null && playerTimers[player.Slot].SetRespawnAng != null)
                    {
                        player.PlayerPawn.Value.Teleport(ParseVector(playerTimers[player.Slot].SetRespawnPos), ParseQAngle(playerTimers[player.Slot].SetRespawnAng), new Vector(0, 0, 0));
                    }
                    else
                    {
                        player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnPos found for current map!");
                    }
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                });
                SortedCachedRecords = GetSortedRecords();
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {respawnSound}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in RespawnPlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_timer", "Stops your timer")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ForceStopTimer(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;
            SharpTimerDebug($"{player.PlayerName} calling css_timer...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            // Remove checkpoints for the current player
            playerCheckpoints.Remove(player.Slot);

            playerTimers[player.Slot].IsTimerBlocked = playerTimers[player.Slot].IsTimerBlocked ? false : true;
            playerTimers[player.Slot].IsRecordingReplay = false;
            player.PrintToChat(msgPrefix + $" Stop timer set to: {primaryChatColor}{playerTimers[player.Slot].IsTimerBlocked}");
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;
            SortedCachedRecords = GetSortedRecords();

            if (stageTriggers.Any()) playerTimers[player.Slot].StageTimes.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Any()) playerTimers[player.Slot].StageVelos.Clear(); //remove previous stage times if the map has stages
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {beepSound}");
            SharpTimerDebug($"{player.PlayerName} css_timer to {playerTimers[player.Slot].IsTimerBlocked}");
        }

        [ConsoleCommand("css_ver", "Prints SharpTimer Version")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void STVerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
            {
                SharpTimerConPrint($"This server is running SharpTimer v{ModuleVersion}");
                return;
            }

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            player.PrintToChat($"This server is running SharpTimer v{ModuleVersion}");
        }

        [ConsoleCommand("css_goto", "Teleports you to a player")]
        [CommandHelper(minArgs: 1, usage: "[name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void GoToPlayer(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || goToEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_goto...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            if (!playerTimers[player.Slot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            var name = command.GetArg(1);
            bool isPlayerFound = false;
            CCSPlayerController foundPlayer = null;


            foreach (var playerEntry in connectedPlayers.Values)
            {
                if (playerEntry.PlayerName == name)
                {
                    foundPlayer = playerEntry;
                    isPlayerFound = true;
                    break;
                }
            }

            if (!isPlayerFound)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Player name not found! If the name contains spaces please try {primaryChatColor}!goto 'some name'");
                return;
            }


            playerCheckpoints.Remove(player.Slot);
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;

            if (playerTimers[player.Slot].SoundsEnabled != false)
                player.ExecuteClientCommand($"play {respawnSound}");

            if (foundPlayer != null && playerTimers[player.Slot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $"Teleporting to {primaryChatColor}{foundPlayer.PlayerName}");

                if (player != null && IsAllowedPlayer(foundPlayer) && playerTimers[player.Slot].IsTimerBlocked)
                {
                    player.PlayerPawn.Value.Teleport(foundPlayer.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0),
                        foundPlayer.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    SharpTimerDebug($"{player.PlayerName} css_goto to {foundPlayer.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0)}");
                }
            }
            else
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Player name not found! If the name contains spaces please try {primaryChatColor}!goto 'some name'");
            }
        }

        [ConsoleCommand("css_cp", "Sets a checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_cp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            if (((PlayerFlags)player.Pawn.Value.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND && removeCpRestrictEnabled == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant set {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} while in air");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant set {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            // Get the player's current position and rotation
            Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
            Vector currentSpeed = player.PlayerPawn.Value.AbsVelocity ?? new Vector(0, 0, 0);
            QAngle currentRotation = player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0);

            // Convert position and rotation to strings
            string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
            string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";
            string speedString = $"{currentSpeed.X} {currentSpeed.Y} {currentSpeed.Z}";

            // Add the current position and rotation strings to the player's checkpoint list
            if (!playerCheckpoints.ContainsKey(player.Slot))
            {
                playerCheckpoints[player.Slot] = new List<PlayerCheckpoint>();
            }

            playerCheckpoints[player.Slot].Add(new PlayerCheckpoint
            {
                PositionString = positionString,
                RotationString = rotationString,
                SpeedString = speedString
            });

            // Get the count of checkpoints for this player
            int checkpointCount = playerCheckpoints[player.Slot].Count;

            // Print the chat message with the checkpoint count
            player.PrintToChat(msgPrefix + $"{(currentMapName.Contains("surf_") ? "Loc" : "Checkpoint")} set! {primaryChatColor}#{checkpointCount}");
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSound}");
            SharpTimerDebug($"{player.PlayerName} css_cp to {checkpointCount} {positionString} {rotationString} {speedString}");
        }

        [ConsoleCommand("css_tp", "Tp to the most recent checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_tp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant use {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            // Check if the player has any checkpoints
            if (!playerCheckpoints.ContainsKey(player.Slot) || playerCheckpoints[player.Slot].Count == 0)
            {
                player.PrintToChat(msgPrefix + $"No {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} set!");
                return;
            }

            // Get the most recent checkpoint from the player's list
            PlayerCheckpoint lastCheckpoint = playerCheckpoints[player.Slot].Last();

            // Convert position and rotation strings to Vector and QAngle
            Vector position = ParseVector(lastCheckpoint.PositionString ?? "0 0 0");
            QAngle rotation = ParseQAngle(lastCheckpoint.RotationString ?? "0 0 0");
            Vector speed = ParseVector(lastCheckpoint.SpeedString ?? "0 0 0");

            // Teleport the player to the most recent checkpoint, including the saved rotation
            if (removeCpRestrictEnabled == true)
            {
                player.PlayerPawn.Value.Teleport(position, rotation, speed);
            }
            else
            {
                player.PlayerPawn.Value.Teleport(position, rotation, new Vector(0, 0, 0));
            }

            // Play a sound or provide feedback to the player
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
            player.PrintToChat(msgPrefix + $"Teleported to most recent {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")}!");
            SharpTimerDebug($"{player.PlayerName} css_tp to {position} {rotation} {speed}");
        }

        [ConsoleCommand("css_prevcp", "Tp to the previous checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPreviousCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_prevcp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant use {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!playerCheckpoints.TryGetValue(player.Slot, out List<PlayerCheckpoint> checkpoints) || checkpoints.Count == 0)
            {
                player.PrintToChat(msgPrefix + $"No {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} set!");
                return;
            }

            int index = playerTimers.TryGetValue(player.Slot, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command);
            }
            else
            {
                // Calculate the index of the previous checkpoint, circling back if necessary
                index = (index - 1 + checkpoints.Count) % checkpoints.Count;

                PlayerCheckpoint previousCheckpoint = checkpoints[index];

                // Update the player's checkpoint index
                playerTimers[player.Slot].CheckpointIndex = index;

                // Convert position and rotation strings to Vector and QAngle
                Vector position = ParseVector(previousCheckpoint.PositionString ?? "0 0 0");
                QAngle rotation = ParseQAngle(previousCheckpoint.RotationString ?? "0 0 0");

                // Teleport the player to the previous checkpoint, including the saved rotation
                player.PlayerPawn.Value.Teleport(position, rotation, new Vector(0, 0, 0));
                // Play a sound or provide feedback to the player
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
                player.PrintToChat(msgPrefix + $"Teleported to the previous {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")}!");
                SharpTimerDebug($"{player.PlayerName} css_prevcp to {position} {rotation}");
            }
        }

        [ConsoleCommand("css_nextcp", "Tp to the next checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpNextCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player.PlayerName} calling css_nextcp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stopreplay");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant use {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!playerCheckpoints.TryGetValue(player.Slot, out List<PlayerCheckpoint> checkpoints) || checkpoints.Count == 0)
            {
                player.PrintToChat(msgPrefix + $"No {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")} set!");
                return;
            }

            int index = playerTimers.TryGetValue(player.Slot, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command);
            }
            else
            {
                // Calculate the index of the next checkpoint, circling back if necessary
                index = (index + 1) % checkpoints.Count;

                PlayerCheckpoint nextCheckpoint = checkpoints[index];

                // Update the player's checkpoint index
                playerTimers[player.Slot].CheckpointIndex = index;

                // Convert position and rotation strings to Vector and QAngle
                Vector position = ParseVector(nextCheckpoint.PositionString ?? "0 0 0");
                QAngle rotation = ParseQAngle(nextCheckpoint.RotationString ?? "0 0 0");

                // Teleport the player to the next checkpoint, including the saved rotation
                player.PlayerPawn.Value.Teleport(position, rotation, new Vector(0, 0, 0));

                // Play a sound or provide feedback to the player
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
                player.PrintToChat(msgPrefix + $"Teleported to the next {(currentMapName.Contains("surf_") ? "loc" : "checkpoint")}!");
                SharpTimerDebug($"{player.PlayerName} css_nextcp to {position} {rotation}");
            }
        }
    }
}