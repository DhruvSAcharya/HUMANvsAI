namespace WebUi.Domains
{
    public class Vote
    {
        public int RoomId { get; set; }
        public int FromPlayerId { get; set; }
        public int ToPlayerId { get; set; }
        public int Star { get; set; }
    }
}
