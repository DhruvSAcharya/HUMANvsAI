# Project Overview

This rule provides a high-level overview of the "Find the Bot" project. Keep this context in mind when suggesting features, fixing bugs, or implementing game logic.

## What is this project?
"Find the Bot" is a multiplayer social deduction game built with Blazor (.NET 8). Players join a chat room and interact to identify which participants are bots controlled by AI. The bots use LLMs (Large Language Models, specifically Groq/OpenAI) to mimic human behavior, making the challenge engaging and unpredictable.

## Core Features
- **Real-time Chat:** Multiplayer communication powered by SignalR.
- **AI Bots:** Powered by LLMs (Groq/OpenAI) with dynamic personalities and behaviors.
- **Voting System:** Players vote to eliminate suspected bots using a star rating system.
- **Game Rounds:** Includes timers and elimination logic.
- **Responsive UI:** Built using MudBlazor components and Tailwind CSS.
- **Architecture:** .NET 8 Blazor Server.

## How the Game Works
1. Players enter their name and join a game room.
2. They chat with other players and bots to gather clues.
3. Players vote for the participant they suspect is a bot by giving them a higher star rating.
4. At the end of each round, the participant with the highest rating is eliminated.
5. The game continues until all bots are eliminated (humans win) or only bots remain (bots win).

## Technology Stack
- **Framework:** Blazor Server (.NET 8)
- **Real-time Communication:** SignalR
- **UI Components:** MudBlazor
- **Styling:** Tailwind CSS (configured in WebUi)
- **AI Integration:** Groq/OpenAI LLMs
