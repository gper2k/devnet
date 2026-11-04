using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Ai;

/// <summary>
/// Google Gemini generateContent 프로바이더. <c>POST {주소}/models/{모델}:generateContent</c>, 키는 <c>x-goog-api-key</c>.
/// 역할은 user/model 로 바꾸고, 시스템 프롬프트는 <c>systemInstruction</c>, 옵션은 <c>generationConfig</c>(추론 강도는 <c>thinkingConfig.thinkingLevel</c>)로 보냅니다.
/// 응답의 생각(thought) 부분은 빼고 글만 이어 붙입니다.
/// </summary>
public class GeminiProvider : AiProvider
{
    /// <inheritdoc/>
    protected override HttpRequestMessage BuildRequest(AiConnection connection, string? apiKey, string? system, IReadOnlyList<AiMessage> messages)
    {
        var contents = new JsonArray();
        foreach (var message in messages)
            contents.Add(new JsonObject
            {
                ["role"] = message.Role == AiMessage.AssistantRole ? "model" : "user",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = message.Content })
            });

        var config = new JsonObject();
        if (connection.Temperature is { } temperature) config["temperature"] = temperature;
        if (connection.TopP is { } topP) config["topP"] = topP;
        if (connection.TopK is { } topK) config["topK"] = topK;
        if (connection.MaxTokens is { } maxTokens) config["maxOutputTokens"] = maxTokens;
        if (connection.ReasoningEffort.Length > 0) config["thinkingConfig"] = new JsonObject { ["thinkingLevel"] = connection.ReasoningEffort };

        var body = new JsonObject { ["contents"] = contents };
        if (!string.IsNullOrWhiteSpace(system)) body["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = system }) };
        if (config.Count > 0) body["generationConfig"] = config;

        var request = PostJson(UrlOf(connection, $"models/{Uri.EscapeDataString(connection.Model)}:generateContent"), body);
        if (!string.IsNullOrEmpty(apiKey)) request.Headers.Add("x-goog-api-key", apiKey);
        return request;
    }

    /// <inheritdoc/>
    protected override string? ReadText(JsonNode? response)
    {
        if (response?["candidates"]?[0]?["content"]?["parts"] is not JsonArray parts) return null;
        return string.Concat(parts.Where(p => p?["thought"]?.GetValue<bool>() != true).Select(p => p?["text"]?.ToString()));
    }
}
