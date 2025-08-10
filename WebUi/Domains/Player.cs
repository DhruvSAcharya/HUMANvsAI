using WebUi.Business;

namespace WebUi.Domains
{
    public class Player
    {
        private VoteManager _voteManager;
        public Player(VoteManager voteManager)
        {
            _voteManager = voteManager;
        }
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public int RoomId { get; set; }
        public decimal CurrentAvgVote
        {
            get
            {
                if (RoomId == 0 || PlayerId == 0)
                    return 0;

                return _voteManager.GetAverageVote(RoomId, PlayerId);
            }
        }
        public PlayerType TypeOfPlayer { get; set; }
    }

    public enum PlayerType
    {
        Bot = 1,
        Human = 2
    }
}
