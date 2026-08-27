using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using WebUi.Domains;

namespace WebUi.Business
{
    /// <summary>
    /// Validates an AI API key by making a minimal live call to the provider's endpoint.
    /// Uses Microsoft.Extensions.AI abstraction via OpenAI compatible endpoints.
    /// </summary>
    public class AiKeyValidatorService(ILogger<AiKeyValidatorService> logger)
    {
        public async Task<bool> ValidateAsync(AIProviderConfig config)
        {
            var maskedKey = MaskKey(config.ApiKey);
            logger.LogInformation(
                "[KeyValidator] Starting validation (MEAI) — Provider: {Provider} | Model: {Model} | Key: {Key}",
                config.Provider, config.Model, maskedKey);

            string endpointUrl = config.Provider switch
            {
                AIProvider.Groq => "https://api.groq.com/openai/v1",
                AIProvider.GoogleAIStudio => "https://generativelanguage.googleapis.com/v1beta/openai/",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(endpointUrl))
            {
                return LogAndReturn(false, $"[KeyValidator] Unknown provider: {config.Provider}");
            }

            var result = await ValidateWithChatClientAsync(config.ApiKey, config.Model, endpointUrl, config.Provider.ToString());

            logger.LogInformation(
                "[KeyValidator] Result — Provider: {Provider} | Model: {Model} | Key: {Key} | Valid: {Valid}",
                config.Provider, config.Model, maskedKey, result);

            return result;
        }

        private async Task<bool> ValidateWithChatClientAsync(string apiKey, string model, string endpointUrl, string providerName)
        {
            try
            {
                var chatClient = new OpenAI.Chat.ChatClient(model, new ApiKeyCredential(apiKey), new OpenAIClientOptions
                {
                    Endpoint = new Uri(endpointUrl)
                }).AsIChatClient();

                logger.LogDebug("[KeyValidator][{Provider}] MEAI GetResponseAsync starting — Model: {Model} | Endpoint: {EndpointUrl}", providerName, model, endpointUrl);

                var response = await chatClient.GetResponseAsync("hi", new ChatOptions { MaxOutputTokens = 1 });
                
                logger.LogInformation("[KeyValidator][{Provider}] ✅ Valid key — HTTP 200 (MEAI)", providerName);
                return true;
            }
            catch (ClientResultException ex) when (ex.Status is 400 or 401 or 403 or 404)
            {
                logger.LogWarning("[KeyValidator][{Provider}] ❌ Auth/Endpoint rejected — HTTP {Status} | Message: {Message}", providerName, ex.Status, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[KeyValidator][{Provider}] ❌ Validation Exception — {Message}", providerName, ex.Message);
                return false;
            }
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length < 12)
                return "***";
            return $"{key[..6]}...{key[^4..]}";
        }

        private bool LogAndReturn(bool value, string message)
        {
            logger.LogWarning(message);
            return value;
        }
    }
}
