# Project Structure Guidelines

This rule defines the expected directory structure for this project. Always place new files and modify existing ones according to these rules.

## WebUi Project (`WebUi/`)

This is a .NET 8 Blazor Server application.

*   `WebUi/Business/`: Contains all game logic and managers (e.g., `RoomManager`, `BotManager`, `PlayerManager`, `VoteManager`). Business logic should never be placed in UI components.
*   `WebUi/Domains/`: Contains core domain models (e.g., `Player`, `Room`, `Vote`). These should be plain C# classes without UI or infrastructure dependencies.
*   `WebUi/Components/`: Contains all Blazor UI components and pages (`.razor` files) along with any scoped CSS. UI components should call into the `Business` layer for logic.
*   `WebUi/Common/`: Contains shared constants, enums, or helper utilities used across different layers.
*   `WebUi/wwwroot/`: Contains static assets like CSS (e.g., Tailwind), images, and JavaScript files.
*   `WebUi/Program.cs`: The entry point of the application, used for app startup, middleware configuration, and service registration.

## General Principles

*   **Separation of Concerns:** Keep the UI (`Components/`) thin. Complex logic must be delegated to the `Business/` layer.
*   **No Circular Dependencies:** `Domains/` should not depend on `Business/` or `Components/`. `Business/` can depend on `Domains/` but not `Components/`.
