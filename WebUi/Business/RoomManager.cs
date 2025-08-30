using WebUi.Common;
using WebUi.Domains;

namespace WebUi.Business
{
    public class RoomManager
    {
        private List<Room> _rooms;
        private int RoomsCount = 100;
        Random _random;
        public RoomManager()
        {
            _rooms = new List<Room>();
            _random = new Random();
        }

        public List<Room> GetRooms()
        {
            return _rooms;
        }

        public Room GetRoomById(int RoomId)
        {
            return _rooms.Where(x => x.RoomId == RoomId).FirstOrDefault();
        }

        public Room CreateRoom()
        {

            Room newRoom = new Room()
            {
                RoomId = RoomsCount++,
                RoundNumber = 0
            };
            _rooms.Add(newRoom);
            return newRoom;
        }

        public Room GetRunningRoom()
        {
            Random random = new Random();

            var availableRooms = _rooms.Where(x => x.Players.Count < 5).ToList();

            Room? room = null;
            if (availableRooms.Any())
            {
                int index = random.Next(availableRooms.Count);
                room = availableRooms[index];
            }

            if (room != null)
            {
                if (room.Players.Count == 4)
                {
                    room.StartTimer(Constants.RoomTimerDuration);
                    room.RoundNumber++;
                }
                return room;
            }
            else
            {
                return CreateRoom();
            }
        }

        public bool RemovePlayerFromRoom(int roomId, int playerId)
        {
            var room = _rooms.FirstOrDefault(x => x.RoomId == roomId);
            if (room != null)
            {
                var player = room.Players.FirstOrDefault(x => x.PlayerId == playerId);
                if (player != null)
                {
                    room.Players.Remove(player);
                    return true;
                }
            }
            return false;
        }

        public bool RemoveEmptyRoom()
        {
            var roomsToRemove = _rooms.Where(x => x.Players.Count == 0).ToList();
            if (roomsToRemove.Any())
            {
                foreach (var room in roomsToRemove)
                {
                    _rooms.Remove(room);
                }
                return true;
            }
            return false;
        }

        public Player AddPlayerToRoom(int roomId, Player player)
        {
            var room = _rooms.FirstOrDefault(y => y.RoomId == roomId);
            if (room != null)
            {
                if (room.Players.Count == 4)
                {
                    room.StartTimer(Constants.RoomTimerDuration);
                    room.RoundNumber++;
                }
                room.Players.Add(player);
            }
            return player;
        }

        public List<Player> GetPlayersByRoomId(int roomId)
        {
            var room = _rooms.FirstOrDefault(x => x.RoomId == roomId);
            if (room != null)
            {
                return room.Players;
            }
            else
            {
                return new List<Player>();
            }
        }
    }
}
