using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private async void ServerRecordADtimer()
        {
            if (isADTimerRunning) return;

            var timer = AddTimer(adTimer, async () =>
            {
                SharpTimerDebug($"Running Server Record AD...");

                Dictionary<string, PlayerRecord> sortedRecords;
                if (useMySQL == false)
                {
                    SharpTimerDebug($"Getting Server Record AD using json");
                    sortedRecords = GetSortedRecords();
                }
                else
                {
                    SharpTimerDebug($"Getting Server Record AD using datanase");
                    sortedRecords = await GetSortedRecordsFromDatabase();
                }

                if (sortedRecords.Count == 0)
                {
                    SharpTimerDebug($"No Server Records for this map yet!");
                    return;
                }


                Server.NextFrame(() => Server.PrintToChatAll($"{msgPrefix} Current Server Record on {primaryChatColor}{currentMapName}{ChatColors.White}: "));
                var serverRecord = sortedRecords.FirstOrDefault();
                string playerName = serverRecord.Value.PlayerName; // Get the player name from the dictionary value
                int timerTicks = serverRecord.Value.TimerTicks; // Get the timer ticks from the dictionary value
                Server.NextFrame(() => Server.PrintToChatAll(msgPrefix + $" {primaryChatColor}{playerName} {ChatColors.White}- {primaryChatColor}{FormatTime(timerTicks)}"));

                string[] adMessages = { $"{msgPrefix} Type {primaryChatColor}!sthelp{ChatColors.Default} to see all commands!",
                                        $"{(enableReplays ? $"{msgPrefix} Type {primaryChatColor}!replaypb{ChatColors.Default} to watch a replay of your personal best run!" : "")}",
                                        $"{(enableReplays ? $"{msgPrefix} Type {primaryChatColor}!replaysr{ChatColors.Default} to watch a replay of the SR on {primaryChatColor}{currentMapName}{ChatColors.Default}!" : "")}",
                                        $"{(enableReplays ? $"{msgPrefix} Type {primaryChatColor}!replaytop <#>{ChatColors.Default} to watch a replay top run on {primaryChatColor}{currentMapName}{ChatColors.Default}!" : "")}",
                                        $"{(globalRanksEnabled ? $"{msgPrefix} Type {primaryChatColor}!points{ChatColors.Default} to see the top 10 players with the most points!" : "")}",
                                        $"{(respawnEnabled ? $"{msgPrefix} Type {primaryChatColor}!r{ChatColors.Default} to respawn back to start!" : "")}",
                                        $"{(topEnabled ? $"{msgPrefix} Type {primaryChatColor}!top{ChatColors.Default} to see the top 10 players on {primaryChatColor}{currentMapName}{ChatColors.Default}!" : "")}",
                                        $"{(rankEnabled ? $"{msgPrefix} Type {primaryChatColor}!rank{ChatColors.Default} to see your current PB and Rank!" : "")}",
                                        $"{(cpEnabled ? $"{msgPrefix} Type {primaryChatColor}{(currentMapName.Contains("surf_") ? "!saveloc" : "!cp")}{ChatColors.Default} to {(currentMapName.Contains("surf_") ? "save a new loc" : "set a new checkpoint")}!" : "")}",
                                        $"{(cpEnabled ? $"{msgPrefix} Type {primaryChatColor}{(currentMapName.Contains("surf_") ? "!loadloc" : "!tp")}{ChatColors.Default} to {(currentMapName.Contains("surf_") ? "load the last loc" : "teleport to your last checkpoint")}!" : "")}",
                                        $"{(goToEnabled ? $"{msgPrefix} Type {primaryChatColor}!goto <name>{ChatColors.Default} to teleport to a player!" : "")}",
                                        $"{msgPrefix} Type {primaryChatColor}!fov <0-140>{ChatColors.Default} to change your field of view!",
                                        $"{msgPrefix} Type {primaryChatColor}!sounds{ChatColors.Default} to toggle timer sounds!",
                                        $"{msgPrefix} Type {primaryChatColor}!hud{ChatColors.Default} to toggle timer hud!",
                                        $"{msgPrefix} Type {primaryChatColor}!keys{ChatColors.Default} to toggle hud keys!",
                                        $"{(jumpStatsEnabled ? $"{msgPrefix} Type {primaryChatColor}!jumpstats{ChatColors.Default} to toggle JumpStats!" : "")}",};

                var nonEmptyAds = adMessages.Where(ad => !string.IsNullOrEmpty(ad)).ToArray();

                Server.NextFrame(() => Server.PrintToChatAll(nonEmptyAds[new Random().Next(nonEmptyAds.Length)]));


            }, TimerFlags.REPEAT);
            isADTimerRunning = true;
        }

        public void SharpTimerDebug(string msg)
        {
            if (enableDebug == true) Console.WriteLine($"\u001b[33m[SharpTimerDebug] \u001b[37m{msg}");
        }

        public void SharpTimerError(string msg)
        {
            Console.WriteLine($"\u001b[31m[SharpTimerERROR] \u001b[37m{msg}");
        }

        public void SharpTimerConPrint(string msg)
        {
            Console.WriteLine($"\u001b[36m[SharpTimer] \u001b[37m{msg}");
        }

        private static string FormatTime(int ticks)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(ticks / 64.0);

            string milliseconds = $"{(ticks % 64) * (1000.0 / 64.0):000}";

            int totalMinutes = (int)timeSpan.TotalMinutes;
            if (totalMinutes >= 60)
            {
                return $"{totalMinutes / 60:D1}:{totalMinutes % 60:D2}:{timeSpan.Seconds:D2}.{milliseconds}";
            }

            return $"{totalMinutes:D1}:{timeSpan.Seconds:D2}.{milliseconds}";
        }

        private static string FormatTimeDifference(int currentTicks, int previousTicks)
        {
            int differenceTicks = previousTicks - currentTicks;
            string sign = (differenceTicks > 0) ? "-" : "+";
            char signColor = (differenceTicks > 0) ? ChatColors.Green : ChatColors.Red;

            TimeSpan timeDifference = TimeSpan.FromSeconds(Math.Abs(differenceTicks) / 64.0);

            // Format seconds with three decimal points
            string secondsWithMilliseconds = $"{timeDifference.Seconds:D2}.{(Math.Abs(differenceTicks) % 64) * (1000.0 / 64.0):000}";

            int totalDifferenceMinutes = (int)timeDifference.TotalMinutes;
            if (totalDifferenceMinutes >= 60)
            {
                return $"{signColor}{sign}{totalDifferenceMinutes / 60:D1}:{totalDifferenceMinutes % 60:D2}:{secondsWithMilliseconds}";
            }

            return $"{signColor}{sign}{totalDifferenceMinutes:D1}:{secondsWithMilliseconds}";
        }

        private static string FormatSpeedDifferenceFromString(string currentSpeed, string previousSpeed)
        {
            if (int.TryParse(currentSpeed, out int currentSpeedInt) && int.TryParse(previousSpeed, out int previousSpeedInt))
            {
                int difference = previousSpeedInt - currentSpeedInt;
                string sign = (difference > 0) ? "-" : "+";
                char signColor = (difference < 0) ? ChatColors.Green : ChatColors.Red;

                return $"{signColor}{sign}{Math.Abs(difference)}";
            }
            else
            {
                return "n/a";
            }
        }

        public double CalculatePoints(int timerTicks)
        {
            double basePoints = 10000.0;
            double timeFactor = 0.0001;
            double tierMult = 0.1;

            if (currentMapTier != null)
            {
                tierMult = (double)(currentMapTier * 0.1);
            }

            double points = basePoints / (timerTicks * timeFactor);
            return points * tierMult;
        }

        public double CalculatePBPoints(int timerTicks)
        {
            double basePoints = 10000.0;
            double timeFactor = 0.01;
            double tierMult = 0.1;

            if (currentMapTier != null)
            {
                tierMult = (double)(currentMapTier * 0.1);
            }

            double points = basePoints / (timerTicks * timeFactor);
            return points * tierMult;
        }

        string ParseColorToSymbol(string input)
        {
            Dictionary<string, string> colorNameSymbolMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
             {
                 { "white", "" },
                 { "darkred", "" },
                 { "purple", "" },
                 { "darkgreen", "" },
                 { "lightgreen", "" },
                 { "green", "" },
                 { "red", "" },
                 { "lightgray", "" },
                 { "orange", "" },
                 { "darkpurple", "" },
                 { "lightred", "" }
             };

            string lowerInput = input.ToLower();

            if (colorNameSymbolMap.TryGetValue(lowerInput, out var symbol))
            {
                return symbol;
            }

            if (IsHexColorCode(input))
            {
                return ParseHexToSymbol(input);
            }

            return "\u0010";
        }

        bool IsHexColorCode(string input)
        {
            if (input.StartsWith("#") && (input.Length == 7 || input.Length == 9))
            {
                try
                {
                    Color color = ColorTranslator.FromHtml(input);
                    return true;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error parsing hex color code: {ex.Message}");
                }
            }
            else
            {
                SharpTimerError("Invalid hex color code format. Please check SharpTimer/config.cfg");
            }

            return false;
        }

        static string ParseHexToSymbol(string hexColorCode)
        {
            Color color = ColorTranslator.FromHtml(hexColorCode);

            Dictionary<string, string> predefinedColors = new Dictionary<string, string>
            {
                { "#FFFFFF", "" },  // White
                { "#8B0000", "" },  // Dark Red
                { "#800080", "" },  // Purple
                { "#006400", "" },  // Dark Green
                { "#00FF00", "" },  // Light Green
                { "#008000", "" },  // Green
                { "#FF0000", "" },  // Red
                { "#D3D3D3", "" },  // Light Gray
                { "#FFA500", "" },  // Orange
                { "#780578", "" },  // Dark Purple
                { "#FF4500", "" }   // Light Red
            };

            hexColorCode = hexColorCode.ToUpper();

            if (predefinedColors.TryGetValue(hexColorCode, out var colorName))
            {
                return colorName;
            }

            Color targetColor = ColorTranslator.FromHtml(hexColorCode);
            string closestColor = FindClosestColor(targetColor, predefinedColors.Keys);

            if (predefinedColors.TryGetValue(closestColor, out var symbol))
            {
                return symbol;
            }

            return "";
        }

        static string FindClosestColor(Color targetColor, IEnumerable<string> colorHexCodes)
        {
            double minDistance = double.MaxValue;
            string closestColor = null;

            foreach (var hexCode in colorHexCodes)
            {
                Color color = ColorTranslator.FromHtml(hexCode);
                double distance = ColorDistance(targetColor, color);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColor = hexCode;
                }
            }

            return closestColor;
        }

        static double ColorDistance(Color color1, Color color2)
        {
            int rDiff = color1.R - color2.R;
            int gDiff = color1.G - color2.G;
            int bDiff = color1.B - color2.B;

            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        public void DrawLaserBetween(Vector startPos, Vector endPos, string _color = "")
        {
            string beamColor = "";
            if (beamColorOverride == true)
            {
                beamColor = _color;
            }
            else
            {
                beamColor = primaryHUDcolor;
            }

            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam == null)
            {
                SharpTimerDebug($"Failed to create beam...");
                return;
            }

            if (IsHexColorCode(beamColor))
            {
                beam.Render = ColorTranslator.FromHtml(beamColor);
            }
            else
            {
                beam.Render = Color.FromName(beamColor);
            }

            beam.Width = 1.5f;

            beam.Teleport(startPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));

            beam.EndPos.X = endPos.X;
            beam.EndPos.Y = endPos.Y;
            beam.EndPos.Z = endPos.Z;

            beam.DispatchSpawn();
            SharpTimerDebug($"Beam Spawned at S:{startPos} E:{beam.EndPos}");
        }

        public void DrawWireframe3D(Vector corner1, Vector corner8, string _color)
        {
            Vector corner2 = new Vector(corner1.X, corner8.Y, corner1.Z);
            Vector corner3 = new Vector(corner8.X, corner8.Y, corner1.Z);
            Vector corner4 = new Vector(corner8.X, corner1.Y, corner1.Z);

            Vector corner5 = new Vector(corner8.X, corner1.Y, corner8.Z);
            Vector corner6 = new Vector(corner1.X, corner1.Y, corner8.Z);
            Vector corner7 = new Vector(corner1.X, corner8.Y, corner8.Z);

            //top square
            DrawLaserBetween(corner1, corner2, _color);
            DrawLaserBetween(corner2, corner3, _color);
            DrawLaserBetween(corner3, corner4, _color);
            DrawLaserBetween(corner4, corner1, _color);

            //bottom square
            DrawLaserBetween(corner5, corner6, _color);
            DrawLaserBetween(corner6, corner7, _color);
            DrawLaserBetween(corner7, corner8, _color);
            DrawLaserBetween(corner8, corner5, _color);

            //connect them both to build a cube
            DrawLaserBetween(corner1, corner6, _color);
            DrawLaserBetween(corner2, corner7, _color);
            DrawLaserBetween(corner3, corner8, _color);
            DrawLaserBetween(corner4, corner5, _color);
        }

        private bool IsVectorInsideBox(Vector playerVector, Vector corner1, Vector corner2)
        {
            float minX = Math.Min(corner1.X, corner2.X);
            float minY = Math.Min(corner1.Y, corner2.Y);
            float minZ = Math.Min(corner1.Z, corner2.Z);

            float maxX = Math.Max(corner1.X, corner2.X);
            float maxY = Math.Max(corner1.Y, corner2.Y);
            float maxZ = Math.Max(corner1.Z, corner2.Z + fakeTriggerHeight);

            return playerVector.X >= minX && playerVector.X <= maxX &&
                   playerVector.Y >= minY && playerVector.Y <= maxY &&
                   playerVector.Z >= minZ && playerVector.Z <= maxZ;
        }

        static Vector CalculateMiddleVector(Vector corner1, Vector corner2)
        {
            float middleX = (corner1.X + corner2.X) / 2;
            float middleY = (corner1.Y + corner2.Y) / 2;
            float middleZ = (corner1.Z + corner2.Z) / 2;
            return new Vector(middleX, middleY, middleZ);
        }

        private static Vector ParseVector(string vectorString)
        {
            const char separator = ' ';

            var values = vectorString.Split(separator);

            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new Vector(x, y, z);
            }

            return new Vector(0, 0, 0);
        }

        private static QAngle ParseQAngle(string qAngleString)
        {
            const char separator = ' ';

            var values = qAngleString.Split(separator);

            if (values.Length == 3 &&
                float.TryParse(values[0], out float pitch) &&
                float.TryParse(values[1], out float yaw) &&
                float.TryParse(values[2], out float roll))
            {
                return new QAngle(pitch, yaw, roll);
            }

            return new QAngle(0, 0, 0);
        }

        public static double Distance(Vector vector1, Vector vector2)
        {
            double deltaX = vector1.X - vector2.X;
            double deltaY = vector1.Y - vector2.Y;
            double deltaZ = vector1.Z - vector2.Z;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        public static bool IsVectorHigherThan(Vector vector1, Vector vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                return false;
            }

            return vector1.Z >= vector2.Z;
        }

        public static Matrix4x4 ToRotationMatrix(QAngle qAngle)
        {
            float x = qAngle.X * (float)Math.PI / 180.0f;
            float y = qAngle.Y * (float)Math.PI / 180.0f;
            float z = qAngle.Z * (float)Math.PI / 180.0f;

            float sx = (float)Math.Sin(x);
            float cx = (float)Math.Cos(x);
            float sy = (float)Math.Sin(y);
            float cy = (float)Math.Cos(y);
            float sz = (float)Math.Sin(z);
            float cz = (float)Math.Cos(z);

            Matrix4x4 rotationMatrix = new Matrix4x4
            {
                M11 = cy * cz,
                M12 = -cy * sz,
                M13 = sy,
                M21 = cz * sx * sy + cx * sz,
                M22 = cx * cz - sx * sy * sz,
                M23 = -cy * sx,
                M31 = -cx * cz * sy + sx * sz,
                M32 = cz * sx + cx * sy * sz,
                M33 = cx * cy
            };

            return rotationMatrix;
        }

        public static Vector RotateVector(Vector vector, Matrix4x4 rotationMatrix)
        {
            Vector rotatedVector = new Vector(vector.X * rotationMatrix.M11 + vector.Y * rotationMatrix.M12 + vector.Z * rotationMatrix.M13, vector.X * rotationMatrix.M21 + vector.Y * rotationMatrix.M22 + vector.Z * rotationMatrix.M23, vector.X * rotationMatrix.M31 + vector.Y * rotationMatrix.M32 + vector.Z * rotationMatrix.M33);
            return rotatedVector;
        }

        public static double Dot(Vector v1, Vector v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static Vector ScalarMultiply(Vector vector, double scalar)
        {
            return new Vector((nint)(vector.X * scalar), (nint)(vector.Y * scalar), (nint)(vector.Z * scalar));
        }

        public Dictionary<string, PlayerRecord> GetSortedRecords(int bonusX = 0)
        {
            string mapRecordsPath = Path.Combine(playerRecordsPath, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");

            Dictionary<string, PlayerRecord> records;

            try
            {
                using (JsonDocument jsonDocument = LoadJson(mapRecordsPath))
                {
                    if (jsonDocument != null)
                    {
                        records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(jsonDocument.RootElement.GetRawText()) ?? new Dictionary<string, PlayerRecord>();
                    }
                    else
                    {
                        records = new Dictionary<string, PlayerRecord>();
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetSortedRecords: {ex.Message}");
                records = new Dictionary<string, PlayerRecord>();
            }

            var sortedRecords = records
                .OrderBy(record => record.Value.TimerTicks)
                .ToDictionary(record => record.Key, record => new PlayerRecord
                {
                    PlayerName = record.Value.PlayerName,
                    TimerTicks = record.Value.TimerTicks
                });

            return sortedRecords;
        }

        public (string, string, string) GetMapRecordSteamID(int bonusX = 0, int top10 = 0)
        {
            string mapRecordsPath = Path.Combine(playerRecordsPath, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");

            Dictionary<string, PlayerRecord> records;

            try
            {
                using (JsonDocument jsonDocument = LoadJson(mapRecordsPath))
                {
                    if (jsonDocument != null)
                    {
                        records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(jsonDocument.RootElement.GetRawText()) ?? new Dictionary<string, PlayerRecord>();
                    }
                    else
                    {
                        records = new Dictionary<string, PlayerRecord>();
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetSortedRecords: {ex.Message}");
                records = new Dictionary<string, PlayerRecord>();
            }

            string steamId64 = "null";
            string playerName = "null";
            string timerTicks = "null";

            if (top10 != 0 && top10 <= records.Count)
            {
                var sortedRecords = records.OrderBy(x => x.Value.TimerTicks).ToList();
                var record = sortedRecords[top10 - 1];
                steamId64 = record.Key;
                playerName = record.Value.PlayerName;
                timerTicks = FormatTime(record.Value.TimerTicks);
            }
            else
            {
                var minTimerTicksRecord = records.OrderBy(x => x.Value.TimerTicks).FirstOrDefault();
                if (minTimerTicksRecord.Key != null)
                {
                    steamId64 = minTimerTicksRecord.Key;
                    playerName = minTimerTicksRecord.Value.PlayerName;
                    timerTicks = FormatTime(minTimerTicksRecord.Value.TimerTicks);
                }
                else
                {
                    steamId64 = "null";
                    playerName = "null";
                    timerTicks = "null";
                }
            }

            return (steamId64, playerName, timerTicks);
        }

        private async Task<(int? Tier, string? Type)> FindMapInfoFromHTTP(string url)
        {
            try
            {
                SharpTimerDebug($"Trying to fetch remote_data for {currentMapName} from {url}");

                var response = await httpClient.GetStringAsync(url);

                using (var jsonDocument = JsonDocument.Parse(response))
                {
                    if (jsonDocument.RootElement.TryGetProperty(currentMapName, out var mapInfo))
                    {
                        int? tier = null;
                        string? type = null;

                        if (mapInfo.TryGetProperty("Tier", out var tierElement))
                        {
                            tier = tierElement.GetInt32();
                        }

                        if (mapInfo.TryGetProperty("Type", out var typeElement))
                        {
                            type = typeElement.GetString();
                        }

                        SharpTimerDebug($"Fetched remote_data success! {tier} {type}");

                        return (tier, type);
                    }
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error Getting Remote Data for {currentMapName}: {ex.Message}");
                return (null, null);
            }
        }

        private async Task GetMapInfo()
        {
            string mapInfoSource = GetMapInfoSource();
            var (mapTier, mapType) = await FindMapInfoFromHTTP(mapInfoSource);
            currentMapTier = mapTier;
            currentMapType = mapType;
            string tierString = currentMapTier != null ? $" | Tier: {currentMapTier}" : "";
            string typeString = currentMapType != null ? $" | {currentMapType}" : "";

            if (autosetHostname == true)
            {
                Server.NextFrame(() =>
                {
                    Server.ExecuteCommand($"hostname {defaultServerHostname}{tierString}{typeString}");
                    SharpTimerDebug($"SharpTimer Hostname Updated to: {ConVar.Find("hostname").StringValue}");
                });
            }


        }

        private string GetMapInfoSource()
        {
            return currentMapName switch
            {
                var name when name.StartsWith("kz_") => remoteKZDataSource,
                var name when name.StartsWith("bhop_") => remoteBhopDataSource,
                var name when name.StartsWith("surf_") => remoteSurfDataSource,
                _ => null
            };
        }

        private void KillServerCommandEnts()
        {
            if (killServerCommands == true)
            {
                var pointServerCommands = Utilities.FindAllEntitiesByDesignerName<CPointServerCommand>("point_servercommand");

                foreach (var servercmd in pointServerCommands)
                {
                    if (servercmd == null) continue;
                    SharpTimerDebug($"Killed point_servercommand ent: {servercmd.Handle}");
                    servercmd.Remove();
                }
            }
        }

        private void OnMapStartHandler(string mapName)
        {
            Server.NextFrame(() =>
            {
                SharpTimerDebug("OnMapStart:");
                SharpTimerDebug("Executing SharpTimer/config");
                Server.ExecuteCommand("sv_autoexec_mapname_cfg 0");
                Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");

                //delay custom_exec so it executes after map exec
                SharpTimerDebug("Creating custom_exec 1sec delay");
                var custom_exec_delay = AddTimer(1.0f, () =>
                {
                    SharpTimerDebug("Re-Executing SharpTimer/custom_exec");
                    Server.ExecuteCommand("execifexists SharpTimer/custom_exec.cfg");

                    if (execCustomMapCFG == true)
                    {
                        string MapExecFile = GetClosestMapCFGMatch();
                        if (!string.IsNullOrEmpty(MapExecFile))
                            Server.ExecuteCommand($"execifexists SharpTimer/MapData/MapExecs/{MapExecFile}");
                        else
                            SharpTimerError("MapExec Error: file name returned null");
                    }

                    if (hideAllPlayers == true) Server.ExecuteCommand($"mp_teammates_are_enemies 1");
                    if (enableSRreplayBot)
                    {
                        Server.NextFrame(() =>
                        {
                            Server.ExecuteCommand($"sv_hibernate_when_empty 0");
                            Server.ExecuteCommand($"bot_join_after_player 0");
                        });
                    }
                });

                if (enableReplays == true && enableSRreplayBot == true)
                {
                    AddTimer(5.0f, () =>
                    {
                        if (ConVar.Find("mp_force_pick_time").GetPrimitiveValue<float>() == 1.0)
                            _ = SpawnReplayBot();
                        else
                        {
                            Server.PrintToChatAll($" {ChatColors.LightRed}Couldnt Spawn Replay bot!");
                            Server.PrintToChatAll($" {ChatColors.LightRed}Please make sure mp_force_pick_time is set to 1");
                            Server.PrintToChatAll($" {ChatColors.LightRed}in your custom_exec.cfg");
                            SharpTimerError("Couldnt Spawn Replay bot! Please make sure mp_force_pick_time is set to 1 in your custom_exec.cfg");
                        }
                    });
                }

                if (removeCrouchFatigueEnabled == true) Server.ExecuteCommand("sv_timebetweenducks 0");

                bonusRespawnPoses.Clear();
                bonusRespawnAngs.Clear();

                cpTriggers.Clear();         // make sure old data is flushed in case new map uses fake zones
                stageTriggers.Clear();
                stageTriggerAngs.Clear();
                stageTriggerPoses.Clear();

                _ = CheckTablesAsync();

                KillServerCommandEnts();
            });
        }

        private void LoadMapData()
        {
            Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");
            SharpTimerDebug("Re-Executing custom_exec with 1sec delay...");
            var custom_exec_delay = AddTimer(1.0f, () =>
            {
                SharpTimerDebug("Re-Executing SharpTimer/custom_exec");
                Server.ExecuteCommand("execifexists SharpTimer/custom_exec.cfg");

                if (execCustomMapCFG == true)
                {
                    string MapExecFile = GetClosestMapCFGMatch();
                    if (!string.IsNullOrEmpty(MapExecFile))
                        Server.ExecuteCommand($"execifexists SharpTimer/MapData/MapExecs/{MapExecFile}");
                    else
                        SharpTimerError("MapExec Error: file name returned null");
                }

                if (hideAllPlayers == true) Server.ExecuteCommand($"mp_teammates_are_enemies 1");
                if (enableSRreplayBot)
                {
                    Server.NextFrame(() =>
                    {
                        Server.ExecuteCommand($"sv_hibernate_when_empty 0");
                        Server.ExecuteCommand($"bot_join_after_player 0");
                    });
                }
            });

            if (srEnabled == true) ServerRecordADtimer();

            currentMapName = Server.MapName;

            string recordsFileName = $"SharpTimer/PlayerRecords/";
            playerRecordsPath = Path.Join(gameDir + "/csgo/cfg", recordsFileName);

            string mysqlConfigFileName = "SharpTimer/mysqlConfig.json";
            mySQLpath = Path.Join(gameDir + "/csgo/cfg", mysqlConfigFileName);

            string mapdataFileName = $"SharpTimer/MapData/{currentMapName}.json";
            string mapdataPath = Path.Join(gameDir + "/csgo/cfg", mapdataFileName);

            entityCache = new EntityCache();
            UpdateEntityCache();

            SortedCachedRecords = GetSortedRecords();

            ClearMapData();

            _ = GetMapInfo();

            primaryChatColor = ParseColorToSymbol(primaryHUDcolor);

            try
            {
                using (JsonDocument jsonDocument = LoadJson(mapdataPath))
                {
                    if (jsonDocument != null)
                    {
                        var mapInfo = JsonSerializer.Deserialize<MapInfo>(jsonDocument.RootElement.GetRawText());
                        SharpTimerConPrint($"Map data json found for map: {currentMapName}!");

                        if (!string.IsNullOrEmpty(mapInfo.MapStartC1) && !string.IsNullOrEmpty(mapInfo.MapStartC2) && !string.IsNullOrEmpty(mapInfo.MapEndC1) && !string.IsNullOrEmpty(mapInfo.MapEndC2))
                        {
                            currentMapStartC1 = ParseVector(mapInfo.MapStartC1);
                            currentMapStartC2 = ParseVector(mapInfo.MapStartC2);
                            currentMapEndC1 = ParseVector(mapInfo.MapEndC1);
                            currentMapEndC2 = ParseVector(mapInfo.MapEndC2);
                            currentEndPos = CalculateMiddleVector(currentMapEndC1, currentMapEndC2);
                            useTriggers = false;
                            SharpTimerConPrint($"Found Fake Trigger Corners: START {currentMapStartC1}, {currentMapStartC2} | END {currentMapEndC1}, {currentMapEndC2}");
                        }

                        if (!string.IsNullOrEmpty(mapInfo.MapStartTrigger) && !string.IsNullOrEmpty(mapInfo.MapEndTrigger))
                        {
                            currentMapStartTrigger = mapInfo.MapStartTrigger;
                            currentMapEndTrigger = mapInfo.MapEndTrigger;
                            useTriggers = true;
                            SharpTimerConPrint($"Found Trigger Names: START {currentMapStartTrigger} | END {currentMapEndTrigger}");
                        }

                        if (!string.IsNullOrEmpty(mapInfo.RespawnPos))
                        {
                            currentRespawnPos = ParseVector(mapInfo.RespawnPos);
                            SharpTimerConPrint($"Found RespawnPos: {currentRespawnPos}");
                        }
                        else
                        {
                            (currentRespawnPos, currentRespawnAng) = FindStartTriggerPos();

                            FindBonusStartTriggerPos();
                            FindStageTriggers();
                            FindCheckpointTriggers();
                            SharpTimerConPrint($"RespawnPos not found, trying to hook trigger pos instead");
                            if (currentRespawnPos == null)
                            {
                                SharpTimerConPrint($"Hooking Trigger RespawnPos Failed!");
                            }
                            else
                            {
                                SharpTimerConPrint($"Hooking Trigger RespawnPos Success! {currentRespawnPos}");
                            }
                        }

                        /* if (mapInfo.OverrideDisableTelehop != null && mapInfo.OverrideDisableTelehop.Any())
                        {
                            try
                            {
                                SharpTimerConPrint($"Overriding Telehop...");
                                currentMapOverrideDisableTelehop = mapInfo.OverrideDisableTelehop
                                    .Split(',')
                                    .Select(color => color.Trim())
                                    .ToArray();

                                foreach (var trigger in currentMapOverrideDisableTelehop)
                                {
                                    SharpTimerConPrint($"OverrideDisableTelehop for trigger: {trigger}");
                                }

                            }
                            catch (Exception ex)
                            {
                                SharpTimerError($"Error parsing OverrideDisableTelehop array: {ex.Message}");
                            }
                        }
                        else
                        {
                            currentMapOverrideDisableTelehop = new string[0];
                        } */

                        if (!string.IsNullOrEmpty(mapInfo.OverrideDisableTelehop))
                        {
                            try
                            {
                                currentMapOverrideDisableTelehop = bool.Parse(mapInfo.OverrideDisableTelehop);
                                SharpTimerConPrint($"Overriding OverrideDisableTelehop...");
                            }
                            catch (FormatException)
                            {
                                SharpTimerError("Invalid boolean string format for OverrideDisableTelehop");
                            }
                        }
                        else
                        {
                            currentMapOverrideStageRequirement = false;
                        }

                        if (mapInfo.OverrideMaxSpeedLimit != null && mapInfo.OverrideMaxSpeedLimit.Any())
                        {
                            try
                            {
                                SharpTimerConPrint($"Overriding MaxSpeedLimit...");
                                currentMapOverrideMaxSpeedLimit = mapInfo.OverrideMaxSpeedLimit
                                    .Split(',')
                                    .Select(color => color.Trim())
                                    .ToArray();

                                foreach (var trigger in currentMapOverrideMaxSpeedLimit)
                                {
                                    SharpTimerConPrint($"OverrideMaxSpeedLimit for trigger: {trigger}");
                                }

                            }
                            catch (Exception ex)
                            {
                                SharpTimerError($"Error parsing OverrideMaxSpeedLimit array: {ex.Message}");
                            }
                        }
                        else
                        {
                            currentMapOverrideMaxSpeedLimit = new string[0];
                        }

                        if (!string.IsNullOrEmpty(mapInfo.OverrideStageRequirement))
                        {
                            try
                            {
                                currentMapOverrideStageRequirement = bool.Parse(mapInfo.OverrideStageRequirement);
                                SharpTimerConPrint($"Overriding StageRequirement...");
                            }
                            catch (FormatException)
                            {
                                SharpTimerError("Invalid boolean string format for OverrideStageRequirement");
                            }
                        }
                        else
                        {
                            currentMapOverrideStageRequirement = false;
                        }

                        if (!string.IsNullOrEmpty(mapInfo.OverrideTriggerPushFix))
                        {
                            try
                            {
                                currentMapOverrideTriggerPushFix = bool.Parse(mapInfo.OverrideTriggerPushFix);
                                SharpTimerConPrint($"Overriding TriggerPushFix...");
                            }
                            catch (FormatException)
                            {
                                SharpTimerError("Invalid boolean string format for OverrideTriggerPushFix");
                            }
                        }
                        else
                        {
                            currentMapOverrideTriggerPushFix = false;
                        }

                        if (!string.IsNullOrEmpty(mapInfo.GlobalPointsMultiplier))
                        {
                            try
                            {
                                globalPointsMultiplier = float.Parse(mapInfo.GlobalPointsMultiplier);
                                SharpTimerConPrint($"Set global points multiplier to x{globalPointsMultiplier}");
                            }
                            catch (FormatException)
                            {
                                SharpTimerError("Invalid float string format for GlobalPointsMultiplier");
                            }
                        }

                        if (!string.IsNullOrEmpty(mapInfo.MapTier))
                        {
                            AddTimer(10.0f, () => //making sure this happens after remote_data is fetched due to github being slow sometimes
                            {
                                try
                                {
                                    currentMapTier = int.Parse(mapInfo.MapTier);
                                    SharpTimerConPrint($"Overriding MapTier to {currentMapTier}");
                                }
                                catch (FormatException)
                                {
                                    SharpTimerError("Invalid int string format for MapTier");
                                }
                            });
                        }

                        if (!string.IsNullOrEmpty(mapInfo.MapType))
                        {
                            AddTimer(10.0f, () => //making sure this happens after remote_data is fetched due to github being slow sometimes
                            {
                                try
                                {
                                    currentMapType = mapInfo.MapType;
                                    SharpTimerConPrint($"Overriding MapType to {currentMapType}");
                                }
                                catch (FormatException)
                                {
                                    SharpTimerError("Invalid string format for MapType");
                                }
                            });
                        }
                    }
                    else
                    {
                        SharpTimerConPrint($"Map data json not found for map: {currentMapName}!");
                        SharpTimerConPrint($"Trying to hook Triggers supported by default!");
                        (currentRespawnPos, currentRespawnAng) = FindStartTriggerPos();
                        currentEndPos = FindEndTriggerPos();
                        FindBonusStartTriggerPos();
                        FindStageTriggers();
                        FindCheckpointTriggers();
                        if (currentRespawnPos == null)
                        {
                            SharpTimerConPrint($"Hooking Trigger RespawnPos Failed!");
                        }
                        else
                        {
                            SharpTimerConPrint($"Hooking Trigger RespawnPos Success! {currentRespawnPos}");
                        }
                        useTriggers = true;
                    }

                    if (useTriggers == false && currentMapStartC1 != null && currentMapStartC2 != null && currentMapEndC1 != null && currentMapEndC2 != null)
                    {
                        DrawWireframe3D(currentMapStartC1, currentMapStartC2, startBeamColor);
                        DrawWireframe3D(currentMapEndC1, currentMapEndC2, endBeamColor);
                    }
                    else
                    {
                        var (startRight, startLeft, endRight, endLeft) = FindTriggerBounds();

                        if (startRight == null || startLeft == null || endRight == null || endLeft == null) return;

                        DrawWireframe3D(startRight, startLeft, startBeamColor);
                        DrawWireframe3D(endRight, endLeft, endBeamColor);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in LoadMapData: {ex.Message}");
            }

            if (useTriggers == false && currentMapStartC1 != null && currentMapStartC2 != null && currentMapEndC1 != null && currentMapEndC2 != null)
            {
                DrawWireframe3D(currentMapStartC1, currentMapStartC2, startBeamColor);
                DrawWireframe3D(currentMapEndC1, currentMapEndC2, endBeamColor);
            }
            else
            {
                var (startRight, startLeft, endRight, endLeft) = FindTriggerBounds();

                if (startRight == null || startLeft == null || endRight == null || endLeft == null) return;

                DrawWireframe3D(startRight, startLeft, startBeamColor);
                DrawWireframe3D(endRight, endLeft, endBeamColor);
            }

            if (triggerPushFixEnabled == true && currentMapOverrideTriggerPushFix == false) FindTriggerPushData();

            KillServerCommandEnts();
        }

        public void ClearMapData()
        {
            cpTriggers.Clear();
            stageTriggers.Clear();
            stageTriggerAngs.Clear();
            stageTriggerPoses.Clear();

            stageTriggerCount = 0;
            useStageTriggers = false;

            currentMapStartC1 = null;
            currentMapStartC2 = null;
            currentMapEndC1 = null;
            currentMapEndC2 = null;

            currentRespawnPos = null;
            currentRespawnAng = null;

            currentEndPos = null;

            currentMapStartTriggerMaxs = null;
            currentMapStartTriggerMins = null;

            currentMapTier = null; //making sure previous map tier and type are wiped
            currentMapType = null;
            currentMapOverrideDisableTelehop = false; //making sure previous map overrides are reset
            currentMapOverrideMaxSpeedLimit = new string[0];
            currentMapOverrideStageRequirement = false;
            currentMapOverrideTriggerPushFix = false;

            globalPointsMultiplier = 1.0f;

            startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile = false;
        }

        private JsonDocument LoadJson(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonDocument.Parse(json);
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error parsing JSON file: {path}, Error: {ex.Message}");
                }
            }

            return null;
        }

        public string RemovePlayerTags(string input)
        {
            string originalTag = input;
            string[] playerTagsToRemove =   {   $"[{customVIPTag}]",
                                                "[Unranked]",
                                                "[Silver I]", "[Silver II]", "[Silver III]",
                                                "[Gold I]", "[Gold II]", "[Gold III]",
                                                "[Platinum I]", "[Platinum II]", "[Platinum III]",
                                                "[Diamond I]", "[Diamond II]", "[Diamond III]",
                                                "[Master I]", "[Master II]", "[Master III]",
                                                "[Legend I]", "[Legend II]", "[Legend III]",
                                                "[Royalty I]", "[Royalty II]", "[Royalty III]",
                                                "[God I]", "[God II]", "[God III]"
                                            };

            if (!string.IsNullOrEmpty(input))
            {
                foreach (var strToRemove in playerTagsToRemove)
                {
                    if (input.Contains(strToRemove))
                    {
                        input = Regex.Replace(input, Regex.Escape(strToRemove), string.Empty, RegexOptions.IgnoreCase).Trim();
                    }
                }
            }

            SharpTimerDebug($"Removing tags... I: {originalTag} O: {input}");

            return input;
        }

        static string FormatOrdinal(int number)
        {
            if (number % 100 >= 11 && number % 100 <= 13)
            {
                return number + "th";
            }

            switch (number % 10)
            {
                case 1:
                    return number + "st";
                case 2:
                    return number + "nd";
                case 3:
                    return number + "rd";
                default:
                    return number + "th";
            }
        }

        static int GetNumberBeforeSlash(string input)
        {
            string[] parts = input.Split('/');

            if (parts.Length == 2 && int.TryParse(parts[0], out int result))
            {
                return result;
            }
            else
            {
                return -1;
            }
        }

        public string GetClosestMapCFGMatch()
        {
            string[] configFiles;
            try
            {
                configFiles = Directory.GetFiles(gameDir + "/csgo/cfg/SharpTimer/MapData/MapExecs", "*.cfg");
            }
            catch (Exception ex)
            {
                SharpTimerError("Error accessing MapExec directory: " + ex.Message);
                return null;
            }

            if (configFiles == null || configFiles.Length == 0)
            {
                SharpTimerError("No MapExec files found.");
                return null;
            }

            string closestMatch = string.Empty;
            int closestMatchLength = int.MaxValue;

            foreach (string file in configFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (currentMapName == fileName)
                {
                    return fileName + ".cfg";
                }

                if (currentMapName.StartsWith(fileName) && fileName.Length < closestMatchLength)
                {
                    closestMatch = fileName + ".cfg";
                    closestMatchLength = fileName.Length;
                }
            }

            if (string.IsNullOrEmpty(closestMatch))
            {
                SharpTimerError("No closest MapExec match found.");
                return null;
            }

            return closestMatch;
        }
    }
}