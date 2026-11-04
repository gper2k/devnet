using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Ai;

/// <summary>
/// OpenAI Chat Completions 호환 프로바이더 (OpenAI, Ollama, vLLM, LM Studio 등). <c>POST {주소}/chat/completions</c>, 키는 <c>Authorization: Bearer</c>.
/// Top K 는 OpenAI 공식 API 가 받지 않으므로 값을 넣었을 때만 보냅니다(Ollama 등 지원 서버용).
/// 추론 강도를 쓰면 추론 모델 규칙대로 최대 토큰을 <c>max_completion_tokens</c> 로 보냅니다.
/// </summary>
public class OpenAiProvider : AiProvider
{
    /// <inheritdoc/>
    protected override HttpRequestMessage BuildRequest(AiConnection connection, string? apiKey, string? system, IReadOnlyList<AiMessage> messages)
    {
        var list = new JsonArray();
        if (!string.IsNullOrWhiteSpace(system)) list.Add(new JsonObject { ["role"] = "system", ["content"] = system });
        foreach (var message in messages) list.Add(new JsonObject { ["role"] = message.Role, ["content"] = message.Content });

        var body = new JsonObject { ["model"] = connection.Model, ["messages"] = list };
        if (connection.Temperature is { } temperature) body["temperature"] = temperature;
        if (connection.TopP is { } topP) body["top_p"] = topP;
        if (connection.TopK is { } topK) body["top_k"] = topK;
        if (connection.ReasoningEffort.Length > 0) body["reasoning_effort"] = connection.ReasoningEffort;
        if (connection.MaxTokens is { } maxTokens) body[connection.ReasoningEffort.Length > 0 ? "max_completion_tokens" : "max_tokens"] = maxTokens;

        var request = PostJson(UrlOf(connection, "chat/completions"), body);
        if (!string.IsNullOrEmpty(apiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return request;
    }

    /// <inheritdoc/>
    protected override string? ReadText(JsonNode? response) => response?["choices"]?[0]?["message"]?["content"]?.ToString();
}
