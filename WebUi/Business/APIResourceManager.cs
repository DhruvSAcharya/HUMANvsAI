using WebUi.Domains;

namespace WebUi.Business
{
    /// <summary>
    /// Singleton that manages a unified round-robin pool of AI provider configs.
    ///
    /// Pool composition:
    ///   - Server-configured keys (from IConfiguration["AI_API_KEYS"]), always present.
    ///   - User-submitted keys, added on login and removed on logout / browser close.
    ///
    /// Key lifecycle rule: a user's key is removed ONLY when the player is fully gone
    /// (Logout or tab close). Leaving/rejoining a room does NOT evict the key.
    /// </summary>
    public class APIResourceManager
    {
        // Server-configured keys loaded at startup (immutable after construction).
        private readonly List<AIProviderConfig> _serverConfigs;

        // User-submitted keys: username -> config. Protected by _lock.
        private readonly Dictionary<string, AIProviderConfig> _userConfigs = new();

        // Unified pool rebuilt whenever the user pool changes.
        private List<AIProviderConfig> _pool = new();
        private int _currentIndex = -1;
        private readonly object _lock = new();

        // Default server-side model used when reading from IConfiguration.
        // Keep in sync with the Groq model list in PlayerInputModel.razor.
        private const string DefaultGroqModel = "groq/compound";

        public APIResourceManager(IConfiguration configuration)
        {
            var apiKeys = configuration["AI_API_KEYS"];

            _serverConfigs = string.IsNullOrWhiteSpace(apiKeys)
                ? new List<AIProviderConfig>()
                : apiKeys
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(k => new AIProviderConfig(AIProvider.Groq, k, DefaultGroqModel))
                    .ToList();

            RebuildPool();
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the next AIProviderConfig in round-robin order across the full pool.
        /// Falls back to server configs if no user keys are registered.
        /// </summary>
        public AIProviderConfig FetchAPI()
        {
            lock (_lock)
            {
                if (_pool.Count == 0)
                    throw new InvalidOperationException("No AI API resources available. Please ask the server admin to configure AI_API_KEYS, or have a player join with their own key.");

                _currentIndex = (_currentIndex + 1) % _pool.Count;
                return _pool[_currentIndex];
            }
        }

        /// <summary>
        /// Adds or updates the API config for a connected user.
        /// Call this after successful key validation, just before the player joins.
        /// </summary>
        public void AddUserKey(string username, AIProviderConfig config)
        {
            lock (_lock)
            {
                _userConfigs[username] = config;
                RebuildPool();
            }
        }

        /// <summary>
        /// Removes the API config for a user who has fully disconnected.
        /// Safe to call even if the user never registered a key.
        /// </summary>
        public void RemoveUserKey(string username)
        {
            lock (_lock)
            {
                if (_userConfigs.Remove(username))
                    RebuildPool();
            }
        }

        /// <summary>
        /// Returns true if the exact API key value is already registered by any user.
        /// Used to reject duplicate key submissions.
        /// </summary>
        public bool IsKeyAlreadyRegistered(string apiKey)
        {
            lock (_lock)
            {
                return _userConfigs.Values.Any(c => c.ApiKey == apiKey)
                    || _serverConfigs.Any(c => c.ApiKey == apiKey);
            }
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Rebuilds the unified pool = server configs + all current user configs.
        /// Must be called inside _lock.
        /// </summary>
        private void RebuildPool()
        {
            _pool = _serverConfigs
                .Concat(_userConfigs.Values)
                .ToList();

            // Keep index in bounds after the pool shrinks.
            if (_pool.Count > 0 && _currentIndex >= _pool.Count)
                _currentIndex = _pool.Count - 1;
        }
    }
}
