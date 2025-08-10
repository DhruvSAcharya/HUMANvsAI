using WebUi.Domains;

namespace WebUi.Business
{
    public class VoteManager
    {
        public Dictionary<(int RoomId, int FromPlayerId, int ToPlayerId), Vote> _votes { get; set; }
        public VoteManager()
        {
            _votes = new Dictionary<(int RoomId, int FromPlayerId, int ToPlayerId), Vote>();
        }

        public Vote AddVote(Vote vote)
        {
            var key = (vote.RoomId, vote.FromPlayerId, vote.ToPlayerId);

            if (_votes.TryGetValue(key, out var existingVote))
            {
                existingVote.Star = vote.Star; // update
                return existingVote;
            }

            _votes[key] = vote; // add new
            return vote;
        }

        public int GetStar(int roomId, int fromPlayerId, int toPlayerId)
        {
            var key = (roomId, fromPlayerId, toPlayerId);
            return _votes.TryGetValue(key, out var vote) ? vote.Star : 0;
        }

        public decimal GetAverageVote(int roomId, int toPlayerId)
        {
            var votes = _votes.Values
                .Where(v => v.RoomId == roomId && v.ToPlayerId == toPlayerId)
                .Select(v => v.Star);
            //return the average value with precision 2
            return votes.Any() ? Math.Round(Convert.ToDecimal(votes.Average()), 2) : 0;
        }
    }
}
