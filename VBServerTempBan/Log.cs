namespace VBServerTempBan
{
    internal static class Log
    {
        private static ManualLogSource _source;

        public static void CreateInstance(ManualLogSource source) => _source = source;

        public static void Info(object msg) => _source.LogInfo($"[VBServerTempBan] {msg}");
        public static void Message(object msg) => _source.LogMessage($"[VBServerTempBan] {msg}");
        public static void Debug(object msg) => _source.LogDebug($"[VBServerTempBan] {msg}");
        public static void Warning(object msg) => _source.LogWarning($"[VBServerTempBan] {msg}");
        public static void Error(object msg) => _source.LogError($"[VBServerTempBan] {msg}");
    }
}