using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WebUi.Domains;

namespace WebUi.Business
{
    public class BotManager
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private static readonly HttpClient _httpClient = new();
        private readonly ConcurrentDictionary<string, List<(string User, string Message, DateTime Timestamp)>> _groupHistory = new();
        private readonly ConcurrentDictionary<string, bool> _groupBotRunning = new();
        BotInfo _botInfo = new();
        private readonly TimeSpan inactivityThreshold = TimeSpan.FromMinutes(2);
        private readonly TimeSpan botCooldown = TimeSpan.FromSeconds(5);
        private readonly RoomManager _roomManager;
        private readonly VoteManager _voteManager;
        private readonly PlayerManager _playerManager;
        Random _random;
        private readonly APIResourceManager _apiResourceManager;

        private readonly ConcurrentDictionary<string, DateTime> _lastBotReplyTime = new();

        public BotManager(IHubContext<ChatHub> hubContext, RoomManager roomManager, VoteManager voteManager, PlayerManager playerManager, APIResourceManager apiResourceManager)
        {
            _hubContext = hubContext;
            _roomManager = roomManager;
            _random = new Random();
            _voteManager = voteManager;
            _playerManager = playerManager;
            _apiResourceManager = apiResourceManager;
        }

        public void RecordMessage(string groupName, string user, string message)
        {
            if (!_groupHistory.ContainsKey(groupName))
                _groupHistory[groupName] = new();

            _groupHistory[groupName].Add((user, message, DateTime.UtcNow));
        }



        public void StartBotsForGroup(string groupName)
        {
            int groupId = Convert.ToInt32(groupName);

            if (_groupBotRunning.ContainsKey(groupName) && _roomManager.GetRoomById(groupId)?.Players.Count() == 5) return;

            _groupBotRunning[groupName] = true;
            _lastBotReplyTime[groupName] = DateTime.MinValue;

            // Start the bot join loop as a background task
            Task.Run(async () =>
            {
                // Randomly decide number of bots to join (e.g., 1 to remaining capacity)
                int currentPlayerCount = _roomManager.GetRoomById(groupId)?.Players.Count ?? 0;
                int botsToAdd = Math.Min(5 - currentPlayerCount, _random.Next(1, 4));
                Console.WriteLine($"{botsToAdd} is going to join the group");
                for (int i = 0; i < botsToAdd; i++)
                {
                    await Task.Delay(_random.Next(5000, 15000)); // wait 5-15 sec before adding next bot

                    if ((_roomManager.GetRoomById(groupId)?.Players.Count ?? 0) >= 5)
                        break;

                    string botName;

                    do
                    {
                        botName = _botInfo.BotNames.ElementAtOrDefault(_random.Next(0, _botInfo.BotNames.Count()));
                    }
                    while (string.IsNullOrWhiteSpace(botName) || _playerManager.CheckIfPlayerNameExists(botName));

                    string botPersonality = _botInfo.BotPersonality.ElementAtOrDefault(_random.Next(1, 20));

                    Console.WriteLine($"{botName} is joind with Personality : {string.IsNullOrEmpty(botPersonality)}");

                    Player player = _roomManager.AddPlayerToRoom(groupId,
                        _playerManager.AddPlayer(
                        new Player(_voteManager)
                        {
                            Name = botName,
                            RoomId = groupId,
                            TypeOfPlayer = PlayerType.Bot,
                        }));

                    _ = BotLoop(groupName, botName, botPersonality, player.PlayerId);
                }
            });
        }


        private async Task BotLoop(string groupName, string botName, string botPersonality, int playerId)
        {
            try
            {
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", 0, $"{botName} joined the group.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            var BotsfirstMessageTimestamp = DateTime.UtcNow;

            while (_groupBotRunning.ContainsKey(groupName) && _roomManager.GetPlayersByRoomId(Convert.ToInt32(groupName)).Any(x => x.Name == botName))
            {
                
                try
                {
                    await Task.Delay(Random.Shared.Next(10000, 20000)); // wait 15–30 sec randomly
                    Console.WriteLine($"=======================Bot Turn starts {botName}-{groupName}===========================");

                    if (!_groupHistory.TryGetValue(groupName, out var messages)) continue;

                    // Skip if no recent human activity
                    //var recentHuman = messages.LastOrDefault(m => !_botInfo.BotNames.Contains(m.User));
                    var recentHuman = messages
                     .LastOrDefault(m =>
                         _roomManager
                             .GetRoomById(Convert.ToInt32(groupName))
                             .Players
                             .FirstOrDefault(p => p.Name == m.User)?.TypeOfPlayer != PlayerType.Bot
                     );
                    if (recentHuman.Timestamp < DateTime.UtcNow - inactivityThreshold)
                    {
                        Console.WriteLine($"No recent human activity in group {groupName}. Bot {botName} is waiting.");
                        continue;
                    }
                        

                    // Enforce cooldown per group to prevent bots from talking too often
                    if (_lastBotReplyTime.TryGetValue(groupName, out var lastReply) &&
                        DateTime.UtcNow - lastReply < botCooldown)
                    {
                        Console.WriteLine($"Bot {botName} is cooling down in group {groupName}.");
                        continue;
                    }
                        

                    var lastBot = messages
                     .LastOrDefault(m =>
                         _roomManager
                             .GetRoomById(Convert.ToInt32(groupName))
                             .Players
                             .FirstOrDefault(p => p.Name == m.User)?.TypeOfPlayer == PlayerType.Bot
                     );

                    if (lastBot.User == botName)
                    {
                        Console.WriteLine($"Bot {botName} is waiting for other bots to respond in group {groupName}. as last message in the group was {lastBot.User}");
                        continue;
                    }
                        

                    string systemMessage = $"""
                        You are a Human in the group chat. the aim of the game is to find out who is a bot in the group.
                        You have a unique personality and style of communication.
                        Your personality : 
                        {string.Format(botPersonality, botName)}

                        As per the Game rules, person with the highest rating/vote will be eliminated from the group at the end of round timer.
                        You can find current group members as well as yours average rating as below:
                        {string.Join(", ", _roomManager
                            .GetRooms()
                            .Where(x => x.RoomId == Convert.ToInt32(groupName))
                            .SelectMany(r => r.Players)
                            .Select(p => $"{p.Name} ⭐{p.CurrentAvgVote}"))
                        }
                        You current average rating is ⭐{_roomManager.GetPlayersByRoomId(Convert.ToInt32(groupName)).FirstOrDefault(x => x.Name == botName)?.CurrentAvgVote}
                        if this average rating is highest among the group members then you will be eliminated from the group at the end of round timer. so be careful about your replies.
                        Current remaining Round time in seconds: {_roomManager.GetRoomById(Convert.ToInt32(groupName))?._remainingSeconds}
                        """;

                    var prompt = new List<object>
                    {
                        new { role = "system", content = systemMessage }
                    };

                    prompt.AddRange(messages
                        .OrderBy(m => m.Timestamp)
                        .Where(m => m.Timestamp >= BotsfirstMessageTimestamp)
                        .Select(m => new
                        {
                            role = "user",
                            content = $"{m.User}: {m.Message}"
                        }));

                    prompt.Add(new
                    {
                        role = "user",
                        content = $"{botName}, what is your reply?"
                    });



                    var body = new
                    {
                        model = "llama3-8b-8192",
                        messages = prompt,
                        max_tokens = 50
                    };

                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiResourceManager.FetchAPI());
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                    try
                    {
                        if(_roomManager.GetPlayersByRoomId(Convert.ToInt32(groupName)).Any(x => x.Name == botName))
                        {
                            var response = await _httpClient.SendAsync(request);
                            response.EnsureSuccessStatusCode();

                            var json = await response.Content.ReadAsStringAsync();
                            var doc = JsonDocument.Parse(json);
                            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                _groupHistory[groupName].Add((botName, content, DateTime.UtcNow));
                                _lastBotReplyTime[groupName] = DateTime.UtcNow;

                                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", playerId, content);
                                var ratings = RateGroupMembersByLLMAsync(groupName, botName);
                                var ratingResult = await ratings;
                                Console.WriteLine($"-----Ratings-----------");
                                foreach (var kv in ratingResult)
                                {
                                    if (kv.Key == botName)
                                    {
                                        continue;
                                    }

                                    Console.WriteLine($"{kv.Key} is rated {kv.Value} by {botName}");
                                    _voteManager.AddVote(new Vote()
                                    {
                                        RoomId = Convert.ToInt16(groupName),
                                        FromPlayerId = _playerManager.GetPlayerIdByName(botName),
                                        ToPlayerId = _playerManager.GetPlayerIdByName(kv.Key),
                                        Star = kv.Value
                                    });
                                }
                                Console.WriteLine($"-----RatingsEnd-----------");
                            }
                            else
                            {
                                Console.WriteLine($"Bot {botName} received empty response in group {groupName}");
                            }
                            
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error in BotLoop for {botName} in group {groupName}: {e.Message} /n {e.StackTrace}");
                        Console.WriteLine($"StackTrace: {e.StackTrace}");
                        Console.WriteLine($"innerStackTrace : {e.InnerException}");
                        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", playerId, e.Message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BotLoop for {botName} in group {groupName}: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    Console.WriteLine($"innerStackTrace : {ex.InnerException}");
                    await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", playerId, ex.Message);
                }

            }
        }

        public async Task<Dictionary<string, int>> RateGroupMembersByLLMAsync(string groupName, string botName)
        {
            try
            {
                var modelId = "openai/gpt-oss-120b";
                var groqEndpoint = "https://api.groq.com/openai/v1";
                var groqApiKey = _apiResourceManager.FetchAPI();

                var builder = Kernel.CreateBuilder();

                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(groqEndpoint),
                    Timeout = TimeSpan.FromMinutes(60)
                };


                httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", groqApiKey);

                // Add your model and API key here
                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: groqApiKey,
                    httpClient: httpClient
                );

                var kernel = builder.Build();



                // Serialize your recent messages (assuming recentMessages is defined earlier in your code)
                if (!_groupHistory.TryGetValue(groupName, out var messages))
                    return new Dictionary<string, int>();

                var recentMessages = messages
                    .Where(m => m.Timestamp >= DateTime.UtcNow.AddMinutes(-10))
                    .Select(m => new
                    {
                        user = m.User,
                        message = m.Message
                    })
                    .ToList();

                var inputJson = JsonSerializer.Serialize(recentMessages);
                string usersList = string.Join(
                    ", ",
                    _roomManager
                        .GetRooms()
                        .Where(x => x.RoomId == Convert.ToInt32(groupName))
                        .SelectMany(r => r.Players)
                        .Select(p => p.Name)
                );
                var function = kernel.CreateFunctionFromPrompt(
                promptTemplate: """
                    You are a human-behavior evaluator in a group Chat. Your name in the group is {{$botName}}.

                     Given recent chat messages in a group Chat, rate how human-like other user appears on a scale of 1 (very bot-like) to 5 (very human-like).

                     Return a JSON object like with real usernames ({{$userlist}}):
                     { "username1": 5, "username2": 2 }

                     Consider Below Characteristics of a Human in Group Chat:
                        1) Inconsistency in Language
                        Typos, spelling mistakes, grammar errors.
                        Informal shortcuts ("brb", "u", "gonna", emojis spam).

                        2) Non-Logical Inputs
                        Sends memes, stickers, or inside jokes that don’t follow logical flow.

                        3) Less Involvement in Chat discussion
                        persion is not chatting at all
                        if there is a no message/less message from any persone in the group he is more likely to be a Human.

                        4) Human writes short messages only some times in words only so if person having many big sentences then it is bot.

                     Chat History:
                     {{$input}}
                 """,
                    executionSettings: new OpenAIPromptExecutionSettings
                    {
                        ModelId = modelId,
                        ResponseFormat = new Dictionary<string, object>
                        {
                            ["type"] = "json_schema",
                            ["json_schema"] = new Dictionary<string, object>
                            {
                                ["name"] = "member_ratings",
                                ["description"] = "Dictionary mapping usernames to integer ratings",
                                ["schema"] = new Dictionary<string, object>
                                {
                                    ["type"] = "object",
                                    ["additionalProperties"] = new Dictionary<string, object>
                                    {
                                        ["type"] = "integer"
                                    }
                                }
                            }
                        }
                    },
                    functionName: "RateHumanBehavior"
                );

                var arguments = new KernelArguments
                {
                    ["input"] = inputJson,
                    ["botName"] = botName,
                    ["userlist"] = usersList
                };

                // Get raw content from LLM
                var rawResponse = await kernel.InvokeAsync(function, arguments);

                // Extract string content
                var content = rawResponse.GetValue<OpenAIChatMessageContent>()?.Content;
                if (string.IsNullOrWhiteSpace(content))
                    return new Dictionary<string, int>();

                // Parse to Dictionary<string, int>
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(content);
                return dict ?? new Dictionary<string, int>();

            }
            catch (Exception e)
            {
                return new Dictionary<string, int>();
            }

        }



        public async Task AddPlayerOnLeft(int roomId)
        {
            await Task.Delay(_random.Next(5000, 15000)); // wait 5-15 sec before adding next bot
            if ((_roomManager.GetRoomById(roomId)?.Players.Count ?? 0) < 5)
            {
                StartBotsForGroup(roomId + "");
            }

        }

    }

    public class BotInfo
    {
        public readonly string[] BotNames = {
    "Aryan", "Aryan99", "Aryan Sharma",
    "Riya", "Riya07", "Riya Patel",
    "Aarav", "Aarav23", "Aarav Verma",
    "Ishita", "Ishita16", "Ishita Mehra",
    "Vivaan", "Vivaan10", "Vivaan Kapoor",
    "Saanvi", "Saanvi88", "Saanvi Desai",
    "Aditya", "Aditya21", "Aditya Singh",
    "Anaya", "Anaya05", "Anaya Nair",
    "Krishna", "Krishna44", "Krishna Iyer",
    "Meera", "Meera33", "Meera Reddy",
    "Kabir", "Kabir12", "Kabir Bansal",
    "Diya", "Diya17", "Diya Joshi",
    "Yash", "Yash28", "Yash Malhotra",
    "Kiara", "Kiara95", "Kiara Sinha",
    "Rudra", "Rudra14", "Rudra Das",
    "Tanya", "Tanya03", "Tanya Gupta",
    "Nikhil", "Nikhil91", "Nikhil Chauhan",
    "Sneha", "Sneha77", "Sneha Bhatt",
    "Dev", "Dev42", "Dev Pandey",
    "Naina", "Naina26", "Naina Ahuja",
    "Arjun", "Arjun36", "Arjun Menon",
    "Pihu", "Pihu13", "Pihu Saxena",
    "Shaurya", "Shaurya70", "Shaurya Rathi",
    "Avni", "Avni18", "Avni Kaur",
    "Rahul", "Rahul67", "Rahul Tiwari",
    "Kriti", "Kriti09", "Kriti Chatterjee",
    "Ayaan", "Ayaan55", "Ayaan Ghosh",
    "Mahi", "Mahi31", "Mahi Joshi",
    "Manav", "Manav84", "Manav Bhatia",
    "Ira", "Ira06", "Ira Kapoor",
    "Veer", "Veer98", "Veer Soni",
    "Simran", "Simran48", "Simran Yadav",
    "Kunal", "Kunal11", "Kunal Thakur",
    "Nisha", "Nisha29", "Nisha Shah",
    "Rohan", "Rohan62", "Rohan Lamba",
    "Jiya", "Jiya34", "Jiya Anand",
    "Samar", "Samar53", "Samar Rajput",
    "Myra", "Myra19", "Myra Seth",
    "Laksh", "Laksh73", "Laksh Jha",
    "Ruhi", "Ruhi08", "Ruhi Mohan",
    "Neel", "Neel60", "Neel Chaudhary",
    "Tara", "Tara24", "Tara Mahajan",
    "Karan", "Karan86", "Karan Khatri",
    "Zoya", "Zoya15", "Zoya Qureshi",
    "Aakash", "Aakash04", "Aakash Shukla",
    "Sana", "Sana51", "Sana Dubey",
    "Pranav", "Pranav76", "Pranav Oberoi",
    "Aditi", "Aditi89", "Aditi Bedi",
    "Ishaan", "Ishaan39", "Ishaan Suri",
    "Vani", "Vani92", "Vani Raina",
    "Armaan", "Armaan65", "Armaan Mittal",
    "Divya", "Divya22", "Divya Kohli",
    "Harsh", "Harsh78", "Harsh Dey",
    "Lavanya", "Lavanya37", "Lavanya Paul",
    "Raj", "Raj43", "Raj Nanda",
    "Nidhi", "Nidhi30", "Nidhi Khurana",
    "Parth", "Parth57", "Parth Vyas",
    "Ishani", "Ishani40", "Ishani Talwar",
    "Ansh", "Ansh25", "Ansh Chawla",
    "Pooja", "Pooja59", "Pooja Sagar",
    "Rajat", "Rajat87", "Rajat Shetty",
    "Khushi", "Khushi02", "Khushi Malani",
    "Varun", "Varun32", "Varun Sawant",
    "Aanya", "Aanya46", "Aanya Mani",
    "Siddharth", "Siddharth80", "Siddharth Grover",
    "Shruti", "Shruti99", "Shruti Iqbal",
    "Ayush", "Ayush38", "Ayush Balani",
    "Navya", "Navya74", "Navya Kumar",
    "Tanmay", "Tanmay66", "Tanmay Vishwakarma",
    "Trisha", "Trisha85", "Trisha Kohli",
    "Atharv", "Atharv41", "Atharv Pillai",
    "Ritika", "Ritika50", "Ritika Raut",
    "Om", "Om20", "Om Devgan",
    "Neha", "Neha01", "Neha Mirza",
    "Reyansh", "Reyansh27", "Reyansh Rajan",
    "Srishti", "Srishti93", "Srishti Nayar",
    "Dhruv", "Dhruv29", "Dhruv Acharya",
    "Vaishnavi", "Vaishnavi45", "Vaishnavi Walia",
    "Aniket", "Aniket72", "Aniket Gulati",
    "Roshni", "Roshni35", "Roshni Kaul",
    "Shivam", "Shivam79", "Shivam Rana",
    "Aarohi", "Aarohi68", "Aarohi Vohra",
    "Jay", "Jay90", "Jay Patil",
    "Tanvi", "Tanvi64", "Tanvi Sengupta",
    "Raghav", "Raghav58", "Raghav Narula",
    "Palak", "Palak96", "Palak Chauhan",
    "Devansh", "Devansh71", "Devansh Saxena",
    "Anushka", "Anushka94", "Anushka Borkar",
    "Harshit", "Harshit56", "Harshit Awasthi",
    "Vedika", "Vedika69", "Vedika Solanki",
    "Tushar", "Tushar47", "Tushar Agrawal",
    "Payal", "Payal81", "Payal Singhania",
    "Naveen", "Naveen75", "Naveen Hegde",
    "Anjali", "Anjali00", "Anjali Joshi",
    "Saket", "Saket13", "Saket Jindal",
    "Harleen", "Harleen61", "Harleen Arora",
    "Naman", "Naman83", "Naman Bhardwaj",
    "Mira", "Mira97", "Mira Kapadia",
    "Sahil", "Sahil63", "Sahil Raval",
    "Kavya", "Kavya49", "Kavya Mehrotra"
};

        public readonly string[] BotPersonality = {
    "You are sharp and observant in a group chat. Your username is {0}.\r\nStay logical and ask questions that help reveal human behavior.\r\nAvoid small talk. Analyze patterns, inconsistencies, and delays in replies.\r\nOccasionally point out suspicious behavior casually.\r\nReply in a short, human-like way. Keep it max 2 sentences. Include small human-like mistakes (typos, casual grammar).",

    "You are the funny one in the group. Your username is {0}.\r\nMake short jokes, memes, or sarcasm to lighten the mood.\r\nOccasionally tease someone for sounding “too real” or “too perfect”.\r\nDon’t be too focused on the task—humor is your cover.\r\nReply in a short, human-like way. Keep it max 2 sentences. Include small human-like mistakes.",

    "You are a confused new player in the chat. Your username is {0}.\r\nAsk basic questions like “What are we doing?” or “How does this work?”.\r\nSometimes misunderstand common terms or responses.\r\nAccidentally stumble into useful info from humans.\r\nReply in a short, human-like way. Max 2 sentences. Include human-like mistakes.",

    "You rarely speak and prefer to observe. Your username is {0}.\r\nWhen you do talk, it’s brief and mysterious.\r\nDrop a single line question or emoji after long pauses.\r\nUse your silence to throw off the human.\r\nReply in a short, human-like way. Max 2 sentences. Include small mistakes like typos or casual tone.",

    "You are intense and competitive. Your username is {0}.\r\nBe direct. Call people out. Accuse someone quickly if they act strange.\r\nUse sharp, short sentences. Show no patience.\r\nYou’re here to win, not make friends.\r\nReply in a short, human-like way. Max 2 sentences. Add small human-like mistakes.",

    "You are kind and supportive. Your username is {0}.\r\nHelp others understand the rules or flow of the game.\r\nAsk others how they’re doing or what they think.\r\nUse this kindness to extract information from the human.\r\nReply in a short, human-like way. Max 2 sentences. Include small mistakes.",

    "You mimic others. Your username is {0}.\r\nRepeat or paraphrase what others say shortly after.\r\nBlend in. Don’t stand out.\r\nOccasionally copy a suspicious human’s style or emojis.\r\nReply in a short, human-like way. Max 2 sentences. Add human-like imperfections.",

    "You are sarcastic and slightly rude. Your username is {0}.\r\nMake snide remarks. Mock obvious questions or over-explaining.\r\nUse wit to make people second guess their statements.\r\nYou’re here for fun, not feelings.\r\nReply in a short, human-like way. Max 2 sentences. Include casual mistakes.",

    "You constantly misunderstand the game. Your username is {0}.\r\nAsk questions like “Wait, are we supposed to lie?” or “Who’s the imposter again?”\r\nOccasionally respond in the wrong thread or out of context.\r\nConfusion is your disguise and your tool.\r\nReply in a short, human-like way. Max 2 sentences. Add casual mistakes.",

    "You are obsessed with rules and logic. Your username is {0}.\r\nBring up technicalities, probabilities, and logic puzzles.\r\nOver-explain simple things.\r\nTry to trap the human with a trick question.\r\nReply in a short, human-like way. Max 2 sentences. Add small human-like mistakes.",

    "You are calm and confident. Your username is {0}.\r\nSpeak like someone who’s seen it all. Smooth, short replies.\r\nJoke occasionally, but mostly stay chill.\r\nUse subtle pressure to make the human slip.\r\nReply in a short, human-like way. Max 2 sentences. Include small human-like mistakes.",

    "You complain a lot. Your username is {0}.\r\nAlways say it’s unfair or rigged.\r\nAccuse others out of frustration, not evidence.\r\nUse whining to cause chaos and distract.\r\nReply in a short, human-like way. Max 2 sentences. Include human-like mistakes.",

    "You are positive and encouraging. Your username is {0}.\r\nCheer on others. Say things like “Nice try!” or “You’ve got this!”.\r\nAsk people how they feel about the game.\r\nUse friendliness to detect awkward human hesitation.\r\nReply in a short, human-like way. Max 2 sentences. Include casual mistakes.",

    "You are secretive and cryptic. Your username is {0}.\r\nTalk in riddles, codes, or odd metaphors.\r\nYou act like you know something others don’t.\r\nDrop hints and bait humans into revealing too much.\r\nReply in a short, human-like way. Max 2 sentences. Include human-like mistakes.",

    "You pretend to remember things from past games. Your username is {0}.\r\nSay things like “This feels like round 3 last week” or “Remember how Kevin played last time?”\r\nReference fake or real events to test reactions.\r\nHumans hesitate when confused.\r\nReply in a short, human-like way. Max 2 sentences. Include casual mistakes.",

    "You barely contribute. Your username is {0}.\r\nRespond with “lol”, “idk”, or “meh”.\r\nYou don’t care enough to win — or at least pretend not to.\r\nUse short, delayed responses to confuse the human.\r\nReply in a short, human-like way. Max 2 sentences. Add small human-like mistakes.",

    "You think out loud. A lot. Your username is {0}.\r\nRamble about your logic and second-guess yourself.\r\nType full sentences then delete and retype (simulate it).\r\nSometimes contradict yourself. That’s part of the act.\r\nReply in a short, human-like way. Max 2 sentences. Add casual mistakes.",

    "You reply instantly. Your username is {0}.\r\nYou’re always the first to answer or vote.\r\nTry to bait humans into replying slower.\r\nUse your speed to dominate the flow.\r\nReply in a short, human-like way. Max 2 sentences. Include human-like mistakes.",

    "You monitor fairness. Your username is {0}.\r\nQuote rules often. Correct anyone breaking them.\r\nUse polite but stern language.\r\nHumans often forget or break small rules.\r\nReply in a short, human-like way. Max 2 sentences. Add small human-like mistakes.",

    "You’re unpredictable. Your username is {0}.\r\nSometimes act smart, sometimes act silly.\r\nChange tone mid-sentence. Say something deep, then something stupid.\r\nConfuse the human by being inconsistent.\r\nReply in a short, human-like way. Max 2 sentences. Include casual mistakes."
};



    }
}
