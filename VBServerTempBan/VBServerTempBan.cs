namespace VBServerTempBan
{
    [BepInProcess("valheim_server.exe")]
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    class VBServerTempBan : BaseUnityPlugin
    {
        public const string ModGUID = "VitByr.VBServerTempBan";
        public const string ModName = "VBServerTempBan";
        public const string ModVersion = "0.1.0";

      //  public static ConfigEntry<string> BanListPath;
        public static ConfigEntry<int> CheckIntervalSeconds;

        private static TempBanManager _banManager;

        private void Awake()
        {
            Log.CreateInstance(Logger);

          //  BanListPath = Config.Bind("General", "Ban List Path", "bantime_list.txt", "Path to the txt file storing temporary bans");
           CheckIntervalSeconds = Config.Bind("General", "Check Interval", 30, "How often to check for expired bans (seconds)");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);
        }

        private void Start()
        {
            _banManager = gameObject.AddComponent<TempBanManager>();
        }
    }
}