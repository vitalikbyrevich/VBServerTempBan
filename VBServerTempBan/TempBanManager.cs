namespace VBServerTempBan
{
    public class TempBanManager : MonoBehaviour
    {
        private readonly List<BanEntry> _tempBans = new List<BanEntry>(); 
        private string _tempBanFilePath;

        private float _checkTimer;
        private float _fileCheckTimer;
        private DateTime _lastFileWrite = DateTime.MinValue;

        private bool _znetReady;
        private static readonly string DateFormat = "yyyy-MM-dd HH:mm:ss";

        private void Awake()
        {
            Log.Message("=== TempBanManager Initializing ===");

            _tempBanFilePath = Path.Combine(Paths.ConfigPath, "VitByr/VBServerTempBan/bantime_list.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(_tempBanFilePath));

            LoadTempBanFile();

            Log.Message($"Temp ban file: {_tempBanFilePath}");
            Log.Message("=== Initialization Complete ===");
        }

        private void Update()
        {
            if (!_znetReady && ZNet.instance && ZNet.instance.m_bannedList != null)
            {
                _znetReady = true;
                Log.Message("ZNet is ready, applying active temp bans...");
                ApplyActiveTempBans();
            }

            _fileCheckTimer += Time.deltaTime;
            if (_fileCheckTimer > 5f)
            {
                _fileCheckTimer = 0f;
                CheckFileChanges();
            }

            _checkTimer += Time.deltaTime;
            if (_checkTimer > VBServerTempBan.CheckIntervalSeconds.Value)
            {
                _checkTimer = 0f;
                CheckExpiredBans();
            }
        }

        private void CheckFileChanges()
        {
            try
            {
                if (!File.Exists(_tempBanFilePath)) return;

                var lastWrite = File.GetLastWriteTimeUtc(_tempBanFilePath);
                if (lastWrite <= _lastFileWrite) return;

                _lastFileWrite = lastWrite;
                Log.Message($"Temp ban file changed at {lastWrite}, reloading...");
                LoadTempBanFile();

                if (_znetReady) ApplyActiveTempBans();
            }
            catch (Exception e)
            {
                Log.Error($"Error checking temp ban file changes: {e}");
            }
        }

        private void LoadTempBanFile()
        {
            _tempBans.Clear();

            if (!File.Exists(_tempBanFilePath))
            {
                CreateExampleFile();
                return;
            }

            try
            {
                var lines = File.ReadAllLines(_tempBanFilePath);
                Log.Message("========== LOADING TEMP BANS ==========");
                Log.Message($"Read {lines.Length} lines from file");

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var parts = line.Split('|');
                    if (parts.Length < 2)
                    {
                        Log.Message($"Skipping invalid line: {line}");
                        continue;
                    }

                    var id = parts[0].Trim();
                    var dateStr = parts[1].Trim();
                    var reason = parts.Length >= 3 ? parts[2].Trim() : "";

                    if (!DateTime.TryParseExact(
                            dateStr, DateFormat, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out var unbanTime))
                    {
                        Log.Message($"Cannot parse date '{dateStr}'");
                        continue;
                    }

                    _tempBans.Add(new BanEntry(id, unbanTime, reason));
                }

                Log.Message($"Loaded {_tempBans.Count} temp bans (active + expired)");
                Log.Message("========== LOADING COMPLETE ==========");
            }
            catch (Exception e)
            {
                Log.Error($"Error loading temp bans: {e}");
            }
        }

        private void CreateExampleFile()
        {
            try
            {
                var lines = new List<string>
                {
                    "# ===============================================================",
                    "#  Temporary Ban List (VBServerTempBan)",
                    "#",
                    "#  FORMAT:",
                    "#      SteamID64 | UnbanTimeUTC | Reason",
                    "#",
                    "#  TIME FORMAT (UTC ONLY):",
                    "#      YYYY-MM-DD HH:mm:ss",
                    "#",
                    "#  IMPORTANT:",
                    "#  - All times MUST be in UTC.",
                    "#  - When the unban time passes, the SteamID will be automatically",
                    "#    removed from the server's bannedlist.txt.",
                    "#  - Permanent bans should NOT be added here — only temporary ones.",
                    "#",
                    "#  EXAMPLES:",
                    "#",
                    "#  Active ban (expires in the future):",
                    "#      76561198000000001 | 2030-01-01 00:00:00 | Example future ban",
                    "#",
                    "#  Expired ban (will be removed on next check):",
                    "#      76561198000000002 | 2020-01-01 00:00:00 | Old expired ban",
                    "#",
                    "# ===============================================================",
                    "",
                    "# Add your temporary bans below:",
                    ""
                };

                File.WriteAllLines(_tempBanFilePath, lines);
                _lastFileWrite = File.GetLastWriteTimeUtc(_tempBanFilePath);

                Log.Message($"Created example temp ban file at {_tempBanFilePath}");

                LoadTempBanFile();
            }
            catch (Exception e)
            {
                Log.Error($"Error creating example temp ban file: {e}");
            }
        }


        private void ApplyActiveTempBans()
        {
            try
            {
                if (!ZNet.instance || ZNet.instance.m_bannedList == null) return;

                var now = DateTime.UtcNow;
                var current = ZNet.instance.m_bannedList.GetList();

                var changed = false;

                foreach (var ban in _tempBans.Where(b => b.UnbanTime > now))
                {
                    if (!current.Contains(ban.PlayerId))
                    {
                        Log.Message($"Adding temp ban: {ban.PlayerId}");
                        ZNet.instance.m_bannedList.Add(ban.PlayerId);
                        changed = true;
                    }
                }

                if (changed)
                {
                    ZNet.instance.m_bannedList.Save();
                    Log.Message("Ban list saved after applying temp bans");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error applying temp bans: {e}");
            }
        }

        private void CheckExpiredBans()
        {
            try
            {
                if (!_znetReady || !ZNet.instance || ZNet.instance.m_bannedList == null) return;

                var now = DateTime.UtcNow;
                var gameBans = ZNet.instance.m_bannedList.GetList();

                var expired = _tempBans.Where(b => b.UnbanTime <= now).ToList();

                if (!expired.Any()) return;

                var changed = false;

                foreach (var ban in expired)
                {
                    if (gameBans.Contains(ban.PlayerId))
                    {
                        Log.Message($"Removing expired temp ban: {ban.PlayerId}");
                        ZNet.instance.m_bannedList.Remove(ban.PlayerId);
                        changed = true;
                    }

                    _tempBans.Remove(ban);
                }

                if (changed)
                {
                    ZNet.instance.m_bannedList.Save();
                    Log.Message("Ban list saved after removing expired temp bans");
                }

                SaveTempBanFile();
            }
            catch (Exception e)
            {
                Log.Error($"Error checking expired temp bans: {e}");
            }
        }

        private void SaveTempBanFile()
        {
            try
            {
                var lines = new List<string>
                {
                    "# ===============================================================",
                    "#  Temporary Ban List (VBServerTempBan)",
                    "#",
                    "#  FORMAT:",
                    "#      SteamID64 | UnbanTimeUTC | Reason",
                    "#",
                    "#  TIME FORMAT (UTC ONLY):",
                    "#      YYYY-MM-DD HH:mm:ss",
                    "#",
                    "#  IMPORTANT:",
                    "#  - All times MUST be in UTC.",
                    "#  - When the unban time passes, the SteamID will be automatically",
                    "#    removed from the server's bannedlist.txt.",
                    "#  - Permanent bans should NOT be added here — only temporary ones.",
                    "#",
                    "#  EXAMPLES:",
                    "#",
                    "#  Active ban (expires in the future):",
                    "#      76561198000000001 | 2030-01-01 00:00:00 | Example future ban",
                    "#",
                    "#  Expired ban (will be removed on next check):",
                    "#      76561198000000002 | 2020-01-01 00:00:00 | Old expired ban",
                    "#",
                    "# ===============================================================",
                    "",
                    "# Add your temporary bans below:",
                    ""
                };

                foreach (var ban in _tempBans)
                {
                    lines.Add($"{ban.PlayerId}|{ban.UnbanTime:yyyy-MM-dd HH:mm:ss}|{ban.Reason}");
                }

                File.WriteAllLines(_tempBanFilePath, lines);
                _lastFileWrite = File.GetLastWriteTimeUtc(_tempBanFilePath);

                Log.Message($"Saved {_tempBans.Count} temp bans to bantime_list.yml");
            }
            catch (Exception e)
            {
                Log.Error($"Error saving temp bans: {e}");
            }
        }
    }
}
