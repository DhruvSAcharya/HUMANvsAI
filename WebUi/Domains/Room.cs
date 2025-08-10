using System.Timers;
using Timer = System.Timers.Timer;

namespace WebUi.Domains
{
    public class Room
    {
        public Room()
        {
            Players = new List<Player>();
            RoomTimer = new Timer(1000);
            RoomTimer.AutoReset = true;
            RoomTimer.Elapsed += OnTimerElapsed;
        }
        public int RoomId { get; set; }
        public List<Player> Players { get; set; }
        public int RoundNumber { get; set; }
        public RoomHeat Severity { get; set; }

        public Timer RoomTimer { get; set; }

        public int _remainingSeconds;

        public event Action<int> TimeChanged;
        public event Action TimerFinished;

        public int GetCurrentBotCount()
        {
            return Players.Where(x => x.TypeOfPlayer == PlayerType.Bot).Count();
        }

        public int GetCurrentHumanCount()
        {
            return Players.Where(x => x.TypeOfPlayer == PlayerType.Human).Count();
        }

        public string GetWinRate()
        {
            int botCount = Players.Count(x => x.TypeOfPlayer == PlayerType.Bot);
            int humanCount = Players.Count(x => x.TypeOfPlayer == PlayerType.Human);
            int total = botCount + humanCount;

            if (total == 0)
                return "No players available";

            double botPercentage = (botCount * 100.0) / total;
            double humanPercentage = (humanCount * 100.0) / total;

            if (botCount > humanCount)
                return $" {botPercentage:F0}% Bots";
            else if (humanCount > botCount)
                return $" {humanPercentage:F0}% Human";
            else
                return "50% Bots / 50% Human";
        }

        public void Start(int seconds)
        {
            _remainingSeconds = seconds;
            RoomTimer.Start();
        }

        public void StopTimer()
        {
            RoomTimer.Stop();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _remainingSeconds--;

            if (_remainingSeconds <= 0)
            {
                RoomTimer.Stop();
                TimerFinished?.Invoke();
            }
            else
            {
                TimeChanged?.Invoke(_remainingSeconds);
            }
        }
    }

    public enum RoomHeat
    {
        Intense = 1,
        Balanced = 2
    }
}
