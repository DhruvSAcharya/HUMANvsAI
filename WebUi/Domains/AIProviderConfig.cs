namespace WebUi.Domains
{
    /// <summary>
    /// Supported AI providers. Add a new value here to introduce a new provider.
    /// </summary>
    public enum AIProvider
    {
        Groq,
        GoogleAIStudio
    }

    /// <summary>
    /// Immutable configuration representing a single AI provider API key + chosen model.
    /// Stored per-user in APIResourceManager and used by BotManager for all AI calls.
    /// </summary>
    /// <param name="Provider">Which AI provider this config targets.</param>
    /// <param name="ApiKey">The user-supplied (or server-configured) API key.</param>
    /// <param name="Model">The model identifier to use (provider-specific string).</param>
    public record AIProviderConfig(AIProvider Provider, string ApiKey, string Model);
}
