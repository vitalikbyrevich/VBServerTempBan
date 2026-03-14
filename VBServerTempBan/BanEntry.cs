namespace VBServerTempBan
{
    public class BanEntry
    {
        public string PlayerId { get; set; }
        public DateTime UnbanTime { get; set; }
        public string Reason { get; set; }

        public BanEntry() { }

        public BanEntry(string playerId, DateTime unbanTime, string reason)
        {
            PlayerId = playerId;
            UnbanTime = unbanTime;
            Reason = reason;
        }

        public bool IsExpired => DateTime.UtcNow >= UnbanTime;
    }
}