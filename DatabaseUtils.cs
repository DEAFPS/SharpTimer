using System.Text.Json;
using MySqlConnector;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace SharpTimer
{
    partial class SharpTimer
    {
        private async Task<MySqlConnection> OpenDatabaseConnectionAsync()
        {
            var connection = new MySqlConnection(GetConnectionStringFromConfigFile(mySQLpath));
            await connection.OpenAsync();
            return connection;
        }

        private async Task CheckTablesAsync()
        {
            string[] playerRecordsColumns =     {   "MapName VARCHAR(255) DEFAULT ''",
                                                    "SteamID VARCHAR(255) DEFAULT ''",
                                                    "PlayerName VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT ''",
                                                    "TimerTicks INT DEFAULT 0",
                                                    "FormattedTime VARCHAR(255) DEFAULT ''",
                                                    "UnixStamp INT DEFAULT 0"
                                                };

            string[] playerStatsColumns =       {   "SteamID VARCHAR(255) DEFAULT ''",
                                                    "PlayerName VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT ''",
                                                    "TimesConnected INT DEFAULT 0",
                                                    "LastConnected INT DEFAULT 0",
                                                    "HideTimerHud BOOL DEFAULT false",
                                                    "HideKeys BOOL DEFAULT false",
                                                    "SoundsEnabled BOOL DEFAULT false",
                                                    "IsVip BOOL DEFAULT false",
                                                    "BigGifID VARCHAR(255) DEFAULT 'x'"
                                                };

            using (var connection = await OpenDatabaseConnectionAsync())
            {
                try
                {
                    //check PlayerRecords
                    SharpTimerDebug($"Checking PlayerRecords Table...");
                    await CreatePlayerRecordsTableAsync(connection);
                    await UpdateTableColumnsAsync(connection, "PlayerRecords", playerRecordsColumns);

                    //check PlayerStats
                    SharpTimerDebug($"Checking PlayerStats Table...");
                    await CreatePlayerStatsTableAsync(connection);
                    await UpdateTableColumnsAsync(connection, "PlayerStats", playerStatsColumns);
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CheckTablesAsync: {ex.Message}");
                }
            }
        }

        private async Task UpdateTableColumnsAsync(MySqlConnection connection, string tableName, string[] columns)
        {
            if (await TableExistsAsync(connection, tableName))
            {
                foreach (string columnDefinition in columns)
                {
                    string columnName = columnDefinition.Split(' ')[0];
                    if (!await ColumnExistsAsync(connection, tableName, columnName))
                    {
                        await AddColumnToTableAsync(connection, tableName, columnDefinition);
                    }
                }
            }
        }

        private async Task<bool> TableExistsAsync(MySqlConnection connection, string tableName)
        {
            string query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in TableExistsAsync: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task<bool> ColumnExistsAsync(MySqlConnection connection, string tableName, string columnName)
        {
            string query = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}' AND column_name = '{columnName}'";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in ColumnExistsAsync: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task AddColumnToTableAsync(MySqlConnection connection, string tableName, string columnDefinition)
        {
            string query = $"ALTER TABLE {tableName} ADD COLUMN {columnDefinition}";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in AddColumnToTableAsync: {ex.Message}");
                }
            }
        }

        private async Task CreatePlayerRecordsTableAsync(MySqlConnection connection)
        {
            string createTableQuery = @"CREATE TABLE IF NOT EXISTS PlayerRecords (
                                            MapName VARCHAR(255),
                                            SteamID VARCHAR(255),
                                            PlayerName VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                                            TimerTicks INT,
                                            FormattedTime VARCHAR(255),
                                            UnixStamp INT,
                                            TimesFinished INT,
                                            LastFinished INT,
                                            PRIMARY KEY (MapName, SteamID)
                                        )";
            using (var createTableCommand = new MySqlCommand(createTableQuery, connection))
            {
                try
                {
                    await createTableCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CreatePlayerRecordsTableAsync: {ex.Message}");
                }
            }
        }

        private async Task CreatePlayerStatsTableAsync(MySqlConnection connection)
        {
            string createTableQuery = @"CREATE TABLE IF NOT EXISTS PlayerStats (
                                            SteamID VARCHAR(255),
                                            PlayerName VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                                            TimesConnected INT,
                                            LastConnected INT,
                                            HideTimerHud BOOL,
                                            HideKeys BOOL,
                                            SoundsEnabled BOOL,
                                            IsVip BOOL,
                                            BigGifID VARCHAR(255),
                                            PRIMARY KEY (SteamID)
                                        )";
            using (var createTableCommand = new MySqlCommand(createTableQuery, connection))
            {
                try
                {
                    await createTableCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CreatePlayerStatsTableAsync: {ex.Message}");
                }
            }
        }

        private string GetConnectionStringFromConfigFile(string mySQLpath)
        {
            try
            {
                using (JsonDocument jsonConfig = LoadJson(mySQLpath))
                {
                    if (jsonConfig != null)
                    {
                        JsonElement root = jsonConfig.RootElement;

                        string host = root.GetProperty("MySqlHost").GetString();
                        string database = root.GetProperty("MySqlDatabase").GetString();
                        string username = root.GetProperty("MySqlUsername").GetString();
                        string password = root.GetProperty("MySqlPassword").GetString();
                        int port = root.GetProperty("MySqlPort").GetInt32();

                        return $"Server={host};Database={database};User ID={username};Password={password};Port={port};CharSet=utf8mb4;";
                    }
                    else
                    {
                        SharpTimerError($"mySQL json was null");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetConnectionString: {ex.Message}");
            }

            return "Server=localhost;Database=database;User ID=root;Password=root;Port=3306;CharSet=utf8mb4;";
        }

        public async Task SavePlayerTimeToDatabase(CCSPlayerController? player, int timerTicks, string steamId, string playerName, int playerSlot, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;
            if ((bonusX == 0 && !playerTimers[playerSlot].IsTimerRunning) || (bonusX != 0 && !playerTimers[playerSlot].IsBonusTimerRunning)) return;

            string currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";



            SharpTimerDebug($"Trying to save player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to MySQL for {playerName} {timerTicks}");
            try
            {
                int timeNowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // get player columns
                int dBtimesFinished = 0;
                int dBlastFinished = 0;
                int dBunixStamp = 0;
                int dBtimerTicks = 0;
                string dBFormattedTime;

                // store new value separatley
                int new_dBtimerTicks = 0;

                using (var connection = await OpenDatabaseConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    string formattedTime = FormatTime(timerTicks);

                    // Check if the record already exists or has a higher timer value
                    string selectQuery = "SELECT TimesFinished, LastFinished, FormattedTime, TimerTicks, UnixStamp FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID";
                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MapName", currentMapNamee);
                        selectCommand.Parameters.AddWithValue("@SteamID", steamId);

                        var row = await selectCommand.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            // get player columns
                            dBtimesFinished = row.GetInt32("TimesFinished");
                            dBlastFinished = row.GetInt32("LastFinished");
                            dBunixStamp = row.GetInt32("UnixStamp");
                            dBtimerTicks = row.GetInt32("TimerTicks");
                            dBFormattedTime = row.GetString("FormattedTime");

                            // Modify the stats
                            dBtimesFinished++;
                            dBlastFinished = timeNowUnix;
                            if (timerTicks < dBtimerTicks)
                            {
                                new_dBtimerTicks = timerTicks;
                                dBunixStamp = timeNowUnix;
                                dBFormattedTime = formattedTime;
                                if (enableReplays == true && useMySQL == true) DumpReplayToJson(player, bonusX);
                            }
                            else
                            {
                                new_dBtimerTicks = dBtimerTicks;
                            }

                            await row.CloseAsync();
                            // Update or insert the record
                            string upsertQuery = "REPLACE INTO PlayerRecords (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp) VALUES (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp)";
                            using (var upsertCommand = new MySqlCommand(upsertQuery, connection))
                            {
                                upsertCommand.Parameters.AddWithValue("@MapName", currentMapNamee);
                                upsertCommand.Parameters.AddWithValue("@PlayerName", playerName);
                                upsertCommand.Parameters.AddWithValue("@TimesFinished", dBtimesFinished);
                                upsertCommand.Parameters.AddWithValue("@LastFinished", dBlastFinished);
                                upsertCommand.Parameters.AddWithValue("@TimerTicks", new_dBtimerTicks);
                                upsertCommand.Parameters.AddWithValue("@FormattedTime", dBFormattedTime);
                                upsertCommand.Parameters.AddWithValue("@UnixStamp", dBunixStamp);
                                upsertCommand.Parameters.AddWithValue("@SteamID", steamId);

                                await upsertCommand.ExecuteNonQueryAsync();
                                Server.NextFrame(() => SharpTimerDebug($"Saved player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to MySQL for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
                            }
                        }
                        else
                        {
                            Server.NextFrame(() => SharpTimerDebug($"No player record yet"));
                            if (enableReplays == true && useMySQL == true) DumpReplayToJson(player, bonusX);
                            await row.CloseAsync();
                            string upsertQuery = "REPLACE INTO PlayerRecords (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp) VALUES (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp)";
                            using (var upsertCommand = new MySqlCommand(upsertQuery, connection))
                            {
                                upsertCommand.Parameters.AddWithValue("@MapName", currentMapNamee);
                                upsertCommand.Parameters.AddWithValue("@PlayerName", playerName);
                                upsertCommand.Parameters.AddWithValue("@TimesFinished", 1);
                                upsertCommand.Parameters.AddWithValue("@LastFinished", timeNowUnix);
                                upsertCommand.Parameters.AddWithValue("@TimerTicks", timerTicks);
                                upsertCommand.Parameters.AddWithValue("@FormattedTime", formattedTime);
                                upsertCommand.Parameters.AddWithValue("@UnixStamp", timeNowUnix);
                                upsertCommand.Parameters.AddWithValue("@SteamID", steamId);
                                await upsertCommand.ExecuteNonQueryAsync();
                                Server.NextFrame(() => SharpTimerDebug($"Saved player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to MySQL for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
                            }
                        }
                    }
                }

                if (useMySQL == true) _ = RankCommandHandler(player, steamId, playerSlot, playerName, true);
                Server.NextFrame(() => _ = PrintMapTimeToChat(player, dBtimerTicks, timerTicks, bonusX));
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => SharpTimerError($"Error saving player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to MySQL: {ex.Message}"));
            }
        }

        public async Task GetPlayerStats(CCSPlayerController? player, string steamId, string playerName, int playerSlot)
        {
            if (player == null || !player.IsValid || player.IsBot) return;
            if (!(connectedPlayers.ContainsKey(playerSlot) && playerTimers.ContainsKey(playerSlot))) return;

            SharpTimerDebug($"Trying to get player stats from MySQL for {playerName}");
            try
            {
                int timeNowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // get player columns
                int timesConnected = 0;
                int lastConnected = 0;
                bool hideTimerHud;
                bool hideKeys;
                bool soundsEnabled;
                bool isVip; 
                string bigGif; 

                using (var connection = await OpenDatabaseConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    
                    //string selectQuery = "SELECT PlayerName, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled FROM PlayerStats WHERE SteamID = @SteamID";
                    string selectQuery = "SELECT PlayerName, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, IsVip, BigGifID FROM PlayerStats WHERE SteamID = @SteamID";
                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@SteamID", steamId);

                        var row = await selectCommand.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            // get player columns
                            timesConnected = row.GetInt32("TimesConnected");
                            hideTimerHud = row.GetBoolean("HideTimerHud");
                            hideKeys = row.GetBoolean("HideKeys");
                            soundsEnabled = row.GetBoolean("SoundsEnabled");
                            isVip = row.GetBoolean("IsVip");
                            bigGif = row.GetString("BigGifID");

                            // Modify the stats
                            timesConnected++;
                            lastConnected = timeNowUnix;

                            playerTimers[playerSlot].HideTimerHud = hideTimerHud;
                            playerTimers[playerSlot].HideKeys = hideKeys;
                            playerTimers[playerSlot].SoundsEnabled = soundsEnabled;
                            playerTimers[playerSlot].IsVip = isVip;
                            playerTimers[playerSlot].VipPausedGif = bigGif;


                            await row.CloseAsync();
                            // Update or insert the record
                            
                            //string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled)";
                            string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, IsVip, BigGifID) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @IsVip, @BigGifID)";
                            using (var upsertCommand = new MySqlCommand(upsertQuery, connection))
                            {
                                upsertCommand.Parameters.AddWithValue("@PlayerName", playerName);
                                upsertCommand.Parameters.AddWithValue("@SteamID", steamId);
                                upsertCommand.Parameters.AddWithValue("@TimesConnected", timesConnected);
                                upsertCommand.Parameters.AddWithValue("@LastConnected", lastConnected);
                                upsertCommand.Parameters.AddWithValue("@HideTimerHud", hideTimerHud);
                                upsertCommand.Parameters.AddWithValue("@HideKeys", hideKeys);
                                upsertCommand.Parameters.AddWithValue("@SoundsEnabled", soundsEnabled);
                                upsertCommand.Parameters.AddWithValue("@IsVip", isVip);
                                upsertCommand.Parameters.AddWithValue("@BigGifID", bigGif);

                                await upsertCommand.ExecuteNonQueryAsync();
                                Server.NextFrame(() => SharpTimerDebug($"Got player stats from MySQL for {playerName}"));
                            }
                        }
                        else
                        {
                            Server.NextFrame(() => SharpTimerDebug($"No player stats yet"));
                            await row.CloseAsync();
                            
                            //string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled)";
                            string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, IsVip, BigGifID) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @IsVip, @BigGifID)";
                            using (var upsertCommand = new MySqlCommand(upsertQuery, connection))
                            {
                                upsertCommand.Parameters.AddWithValue("@PlayerName", playerName);
                                upsertCommand.Parameters.AddWithValue("@SteamID", steamId);
                                upsertCommand.Parameters.AddWithValue("@TimesConnected", timesConnected);
                                upsertCommand.Parameters.AddWithValue("@LastConnected", lastConnected);
                                upsertCommand.Parameters.AddWithValue("@HideTimerHud", false);
                                upsertCommand.Parameters.AddWithValue("@HideKeys", false);
                                upsertCommand.Parameters.AddWithValue("@SoundsEnabled", false);
                                upsertCommand.Parameters.AddWithValue("@IsVip", false);
                                upsertCommand.Parameters.AddWithValue("@BigGifID", "x");

                                await upsertCommand.ExecuteNonQueryAsync();
                                Server.NextFrame(() => SharpTimerDebug($"Got player stats from MySQL for {playerName}"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => SharpTimerError($"Error getting player stats from MySQL for {playerName}: {ex}"));
            }
        }

        public async Task SetPlayerStats(CCSPlayerController? player, string steamId, string playerName, int playerSlot)
        {
            if (!IsAllowedPlayer(player)) return;

            SharpTimerDebug($"Trying to set player stats to MySQL for {playerName}");
            try
            {
                int timeNowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // get player columns
                int timesConnected = 0;
                int lastConnected = 0;
                bool hideTimerHud;
                bool hideKeys;
                bool soundsEnabled;
                bool isVip; 
                string bigGif; 

                using (var connection = await OpenDatabaseConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    
                    //string selectQuery = "SELECT PlayerName, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled FROM PlayerStats WHERE SteamID = @SteamID";
                    string selectQuery = "SELECT PlayerName, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, IsVip, BigGifID FROM PlayerStats WHERE SteamID = @SteamID";
                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@SteamID", steamId);

                        var row = await selectCommand.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            // get player columns
                            timesConnected = row.GetInt32("TimesConnected");
                            lastConnected = row.GetInt32("LastConnected");
                            hideTimerHud = row.GetBoolean("HideTimerHud");
                            hideKeys = row.GetBoolean("HideKeys");
                            soundsEnabled = row.GetBoolean("SoundsEnabled");
                            isVip = row.GetBoolean("IsVip");
                            bigGif = row.GetString("BigGifID");

                            await row.CloseAsync();
                            // Update or insert the record
                            
                            //string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled)";
                            string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, IsVip, BigGifID) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @IsVip, @BigGifID)";
                            using (var upsertCommand = new MySqlCommand(upsertQuery, connection))
                            {
                                upsertCommand.Parameters.AddWithValue("@PlayerName", playerName);
                                upsertCommand.Parameters.AddWithValue("@SteamID", steamId);
                                upsertCommand.Parameters.AddWithValue("@TimesConnected", timesConnected);
                                upsertCommand.Parameters.AddWithValue("@LastConnected", lastConnected);
                                upsertCommand.Parameters.AddWithValue("@HideTimerHud", playerTimers[playerSlot].HideTimerHud);
                                upsertCommand.Parameters.AddWithValue("@HideKeys", playerTimers[playerSlot].HideKeys);
                                upsertCommand.Parameters.AddWithValue("@SoundsEnabled", playerTimers[playerSlot].SoundsEnabled);
                                upsertCommand.Parameters.AddWithValue("@IsVip", isVip);
                                upsertCommand.Parameters.AddWithValue("@BigGifID", bigGif);

                                await upsertCommand.ExecuteNonQueryAsync();
                                Server.NextFrame(() => SharpTimerDebug($"Set player stats to MySQL for {playerName}"));
                            }
                        }
                        else
                        {
                            Server.NextFrame(() => SharpTimerDebug($"No player stats yet"));
                            await row.CloseAsync();
                            
                            //string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled)";
                            string upsertQuery = "REPLACE INTO PlayerStats (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, IsVip, BigGifID) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @IsVip, @BigGifID)";
                            using (var upsertCommand = new MySqlCommand(upsertQuery, connection))
                            {
                                upsertCommand.Parameters.AddWithValue("@PlayerName", playerName);
                                upsertCommand.Parameters.AddWithValue("@SteamID", steamId);
                                upsertCommand.Parameters.AddWithValue("@TimesConnected", 1);
                                upsertCommand.Parameters.AddWithValue("@LastConnected", timeNowUnix);
                                upsertCommand.Parameters.AddWithValue("@HideTimerHud", playerTimers[playerSlot].HideTimerHud);
                                upsertCommand.Parameters.AddWithValue("@HideKeys", playerTimers[playerSlot].HideKeys);
                                upsertCommand.Parameters.AddWithValue("@SoundsEnabled", playerTimers[playerSlot].SoundsEnabled);
                                upsertCommand.Parameters.AddWithValue("@IsVip", false);
                                upsertCommand.Parameters.AddWithValue("@BigGifID", "x");

                                await upsertCommand.ExecuteNonQueryAsync();
                                Server.NextFrame(() => SharpTimerDebug($"Set player stats to MySQL for {playerName}"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => SharpTimerError($"Error setting player stats from MySQL for {playerName}: {ex}"));
            }
        }

        
        public async Task GetReplayVIPGif(string steamId, int playerSlot)
        {
            SharpTimerDebug($"Trying to get replay VIP Gif");
            try
            {
                using (var connection = await OpenDatabaseConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);
                    string selectQuery = "SELECT IsVip, BigGifID FROM PlayerStats WHERE SteamID = @SteamID";
                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@SteamID", steamId);

                        var row = await selectCommand.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            // get player columns
                            bool isVip = row.GetBoolean("IsVip");
                            if (isVip)
                            {
                                SharpTimerDebug($"Replay is VIP setting gif...");
                                playerTimers[playerSlot].VipReplayGif = row.GetString("BigGifID");
                            }
                            else
                            {
                                SharpTimerDebug($"Replay is not VIP...");
                                playerTimers[playerSlot].VipReplayGif = "x";
                            }

                            await row.CloseAsync();
                        }
                        else
                        {
                            await row.CloseAsync();
                            SharpTimerDebug($"Replay is not VIP... goofy");
                            playerTimers[playerSlot].VipReplayGif = "x";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => SharpTimerError($"Error getting ReplayVIPGif from MySQL: {ex}"));
            }
        }

        

        public async Task<(string, string, string)> GetMapRecordSteamIDFromDatabase(int bonusX = 0, int top10 = 0)
        {
            SharpTimerDebug($"Trying to get replay VIP Gif");
            try
            {
                using (var connection = await OpenDatabaseConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);
                    string selectQuery;

                    if (top10 != 0)
                    {
                        // Get the top N records based on TimerTicks
                        selectQuery = $"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName ORDER BY TimerTicks DESC LIMIT {top10}";
                    }
                    else
                    {
                        // Get the overall top player
                        selectQuery = $"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName ORDER BY TimerTicks LIMIT 1";
                    }

                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MapName", currentMapName);

                        var row = await selectCommand.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            string steamId64 = row.GetString("SteamID");
                            string playerName = row.GetString("PlayerName");
                            string timerTicks = FormatTime(row.GetInt32("TimerTicks"));
                            
                            
                            await row.CloseAsync();
                            return (steamId64, playerName, timerTicks);
                        }
                        else
                        {
                            await row.CloseAsync();
                            return ("null", "null", "null");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => SharpTimerError($"Error getting ReplayVIPGif from MySQL: {ex}"));
                return ("null", "null", "null");
            }
        }

        public async Task<int> GetPreviousPlayerRecordFromDatabase(CCSPlayerController? player, string steamId, string currentMapName, string playerName, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player))
            {
                return 0;
            }

            string currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";

            SharpTimerDebug($"Trying to get Previous {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from MySQL for {playerName}");
            try
            {
                using (var connection = await OpenDatabaseConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    // Retrieve the TimerTicks value for the specified player on the current map
                    string selectQuery = "SELECT TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID";
                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MapName", currentMapNamee);
                        selectCommand.Parameters.AddWithValue("@SteamID", steamId);

                        var result = await selectCommand.ExecuteScalarAsync();

                        // Check for DBNull
                        if (result != null && result != DBNull.Value)
                        {
                            SharpTimerDebug($"Got Previous Time from MySQL for {playerName}");
                            return Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error getting previous player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from MySQL: {ex.Message}");
            }

            return 0;
        }

        public async Task<Dictionary<string, PlayerRecord>> GetSortedRecordsFromDatabase(int bonusX = 0)
        {
            string currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";
            SharpTimerDebug($"Trying GetSortedRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from MySQL");
            using (var connection = await OpenDatabaseConnectionAsync())
            {
                try
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    // Retrieve and sort records for the current map
                    string selectQuery = "SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName";
                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MapName", currentMapNamee);
                        using (var reader = await selectCommand.ExecuteReaderAsync())
                        {
                            var sortedRecords = new Dictionary<string, PlayerRecord>();
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                int timerTicks = reader.GetInt32(2);
                                sortedRecords.Add(steamId, new PlayerRecord
                                {
                                    PlayerName = playerName,
                                    TimerTicks = timerTicks
                                });
                            }

                            // Sort the records by TimerTicks
                            sortedRecords = sortedRecords.OrderBy(record => record.Value.TimerTicks)
                                                         .ToDictionary(record => record.Key, record => record.Value);

                            SharpTimerDebug($"Got GetSortedRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from MySQL");
                            return sortedRecords;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error getting sorted records from MySQL: {ex.Message}");
                }
            }
            return new Dictionary<string, PlayerRecord>();
        }

        public void SavePlayerSettingToDatabase(string SteamID, string setting, bool state)
        {

        }

        public bool GetPlayerSettingFromDatabase(string SteamID, string setting)
        {

            return false;
        }

        [ConsoleCommand("css_jsontodatabase", " ")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void AddJsonTimesToDatabaseCommand(CCSPlayerController? player, CommandInfo command)
        {
            _ = AddJsonTimesToDatabaseAsync();
        }

        public async Task AddJsonTimesToDatabaseAsync()
        {
            try
            {
                string recordsDirectoryNamee = "SharpTimer/PlayerRecords";
                string playerRecordsPathh = Path.Combine(gameDir, "csgo", "cfg", recordsDirectoryNamee);

                if (!Directory.Exists(playerRecordsPathh))
                {
                    SharpTimerDebug($"Error: Directory not found at {playerRecordsPathh}");
                    return;
                }

                string connectionString = GetConnectionStringFromConfigFile(mySQLpath);

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the table exists, and create it if necessary
                    string createTableQuery = "CREATE TABLE IF NOT EXISTS PlayerRecords (SteamID VARCHAR(255), PlayerName VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci, TimerTicks INT, FormattedTime VARCHAR(255), MapName VARCHAR(255), PRIMARY KEY (SteamID, MapName))";
                    using (var createTableCommand = new MySqlCommand(createTableQuery, connection))
                    {
                        await createTableCommand.ExecuteNonQueryAsync();
                    }

                    foreach (var filePath in Directory.EnumerateFiles(playerRecordsPathh, "*.json"))
                    {
                        string json = await File.ReadAllTextAsync(filePath);
                        var records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(json);

                        if (records == null)
                        {
                            SharpTimerDebug($"Error: Failed to deserialize JSON data from {filePath}");
                            continue;
                        }

                        foreach (var recordEntry in records)
                        {
                            string steamId = recordEntry.Key;
                            PlayerRecord playerRecord = recordEntry.Value;

                            // Extract MapName from the filename (remove extension)
                            string mapName = Path.GetFileNameWithoutExtension(filePath);

                            // Check if the player is already in the database
                            string insertOrUpdateQuery = @"
                                INSERT INTO PlayerRecords (SteamID, PlayerName, TimerTicks, FormattedTime, MapName, UnixStamp)
                                VALUES (@SteamID, @PlayerName, @TimerTicks, @FormattedTime, @MapName, @UnixStamp)
                                ON DUPLICATE KEY UPDATE
                                TimerTicks = IF(@TimerTicks < TimerTicks, @TimerTicks, TimerTicks),
                                FormattedTime = IF(@TimerTicks < TimerTicks, @FormattedTime, FormattedTime)";

                            using (var insertOrUpdateCommand = new MySqlCommand(insertOrUpdateQuery, connection))
                            {
                                insertOrUpdateCommand.Parameters.AddWithValue("@SteamID", steamId);
                                insertOrUpdateCommand.Parameters.AddWithValue("@PlayerName", playerRecord.PlayerName);
                                insertOrUpdateCommand.Parameters.AddWithValue("@TimerTicks", playerRecord.TimerTicks);
                                insertOrUpdateCommand.Parameters.AddWithValue("@FormattedTime", FormatTime(playerRecord.TimerTicks));
                                insertOrUpdateCommand.Parameters.AddWithValue("@MapName", mapName);
                                insertOrUpdateCommand.Parameters.AddWithValue("@UnixStamp", 0);

                                await insertOrUpdateCommand.ExecuteNonQueryAsync();
                            }
                        }

                        SharpTimerDebug($"JSON times from {Path.GetFileName(filePath)} successfully added to the database.");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error adding JSON times to the database: {ex.Message}");
            }
        }

        [ConsoleCommand("css_databasetojson", " ")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void ExportDatabaseToJsonCommand(CCSPlayerController? player, CommandInfo command)
        {
            _ = ExportDatabaseToJsonAsync();
        }

        public async Task ExportDatabaseToJsonAsync()
        {
            string recordsDirectoryNamee = "SharpTimer/PlayerRecords";
            string playerRecordsPathh = Path.Combine(gameDir, "csgo", "cfg", recordsDirectoryNamee);

            try
            {
                string connectionString = GetConnectionStringFromConfigFile(mySQLpath);

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string selectQuery = "SELECT SteamID, PlayerName, TimerTicks, MapName FROM PlayerRecords";
                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        using (var reader = await selectCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.GetString(1);
                                int timerTicks = reader.GetInt32(2);
                                string mapName = reader.GetString(3);

                                Directory.CreateDirectory(playerRecordsPathh);

                                Dictionary<string, PlayerRecord> records;
                                string filePath = Path.Combine(playerRecordsPathh, $"{mapName}.json");
                                if (File.Exists(filePath))
                                {
                                    string existingJson = await File.ReadAllTextAsync(filePath);
                                    records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(existingJson) ?? new Dictionary<string, PlayerRecord>();
                                }
                                else
                                {
                                    records = new Dictionary<string, PlayerRecord>();
                                }

                                records[steamId] = new PlayerRecord
                                {
                                    PlayerName = playerName,
                                    TimerTicks = timerTicks
                                };

                                string updatedJson = JsonSerializer.Serialize(records, new JsonSerializerOptions
                                {
                                    WriteIndented = true
                                });

                                await File.WriteAllTextAsync(filePath, updatedJson);

                                SharpTimerDebug($"Player records for map {mapName} successfully exported to JSON.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error exporting player records to JSON: {ex.Message}");
            }
        }
    }
}