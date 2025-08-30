# Find the Bot - Blazor Game

## Overview
Find the Bot is a multiplayer social deduction game built with Blazor (.NET 8). Players join a chat room and interact to identify which participants are bots controlled by AI. The bots use LLMs (Large Language Models) to mimic human behavior, making the challenge engaging and unpredictable.

## Features
- Real-time multiplayer chat using SignalR
- AI bots powered by LLMs (Groq/OpenAI)
- Dynamic bot personalities and behaviors
- Voting system to eliminate suspected bots
- Game rounds with timers and elimination logic
- Responsive UI with MudBlazor components
- .NET 8 Blazor Server architecture

## How to Play
1. Enter your name and join a game room.
2. Chat with other players and bots to gather clues.
3. Vote for the participant you suspect is a bot by giving them a higher star rating.
4. At the end of each round, the player with the highest rating is eliminated.
5. The game continues until all bots are eliminated or only bots remain.

## Technologies Used
- Blazor Server (.NET 8)
- SignalR for real-time communication
- MudBlazor for UI components
- Groq/OpenAI LLMs for bot intelligence

## Project Structure
- `WebUi/Business/` - Game logic and managers (RoomManager, BotManager, PlayerManager, VoteManager)
- `WebUi/Domains/` - Core domain models (Player, Room, Vote)
- `WebUi/Components/` - Blazor components and pages
- `WebUi/Common/` - Shared constants
- `WebUi/Program.cs` - App startup and service registration

## Getting Started
1. Clone the repository.
2. Ensure you have .NET 8 SDK installed.
3. Restore NuGet packages.
4. Set up your Groq/OpenAI API key in the configuration.
5. Run the project:dotnet run --project WebUi/WebUi.csproj6. Open your browser at `https://localhost:5001` (or the port shown in the console).

## Customization
- Bot personalities and names can be edited in `BotManager.cs`.
- Game rules and round timers are configurable in `Common/Constants.cs`.

## License
This project is open source. See the LICENSE file for details.
