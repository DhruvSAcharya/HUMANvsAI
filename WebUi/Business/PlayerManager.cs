using WebUi.Domains;

namespace WebUi.Business
{
    public class PlayerManager
    {
        private List<Player> _players;
        private int PlayerCount = 100;
        private VoteManager _voteManager;
        public PlayerManager(VoteManager voteManager)
        {
            _voteManager = voteManager;
            _players = new List<Player>();
            _players.Add(new Player(_voteManager)
            {
                PlayerId = 0,
                Name = "System",
                RoomId = 0,
                TypeOfPlayer = PlayerType.Bot
            });
        }

        public Player AddPlayer(Player player)
        {
            player.PlayerId = PlayerCount++;
            _players.Add(player);
            return player;
        }

        public string GetPlayerName(int playerId)
        {
            var player = _players.FirstOrDefault(x => x.PlayerId == playerId);
            if (player != null)
            {
                return player.Name;
            }
            return string.Empty;
        }

        public int GetPlayerIdByName(string Name)
        {
            var player = _players.FirstOrDefault(x => x.Name.Equals(Name));
            if (player != null)
            {
                return player.PlayerId;
            }
            return 0;
        }
        public bool RemovePlayer(int playerId)
        {
            var player = _players.FirstOrDefault(x => x.PlayerId == playerId);
            if (player != null)
            {
                _players.Remove(player);
                return true;
            }
            return false;
        }

        public bool CheckIfPlayerNameExists(string playerName)
        {
            return _players.Any(x => x.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        }

        public int getTotalBotCount()
        {
            return _players.Count(x => x.TypeOfPlayer == PlayerType.Bot);
        }

        public int getTotalHumanCount()
        {
            return _players.Count(x => x.TypeOfPlayer == PlayerType.Human);
        }
    }
}
