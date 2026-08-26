namespace WebUi.Business
{
    public class APIResourceManager
    {
        private readonly List<string> _resources;
        private int _currentIndex = -1;

        public APIResourceManager(IConfiguration configuration)
        {
            var apiKeys = configuration["AI_API_KEYS"];

            _resources = string.IsNullOrWhiteSpace(apiKeys)
                ? new List<string>()
                : apiKeys
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
        }

        public string FetchAPI()
        {
            if (_resources.Count == 0)
                throw new InvalidOperationException("No resources available");

            // Move to next index (round robin)
            _currentIndex = (_currentIndex + 1) % _resources.Count;
            return _resources[_currentIndex];
        }
    }
}
