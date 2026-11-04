using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Ai;

/// <summary>
/// Anthropic Claude Messages API 프로바이더. <c>POST {주소}/messages</c>, 키는 <c>x-api-key</c>, API 버전 <see cref="ApiVersion"/>.
/// 이 API 버전부터 온도·Top P·Top K 를 보내면 400 이라 보내지 않습니다. 추론 강도는 <c>output_config.effort</c>, 최대 토큰은 필수라 비우면 기본값을 씁니다.
/// 응답의 text 블록만 이어 붙이고, 거절(<c>stop_reason: refusal</c>)이면 실패로 돌려줍니다.
/// </summary>
public class ClaudeProvider : AiProvider
{
    /// <summary><c>anthropic-version</c> 헤더 값</summary>
    public const string ApiVersion = "2026-10-21";

    /// <inheritdoc/>
    protected override HttpRequestMessage BuildRequest(AiConnection connection, string? apiKey, string? system, IReadOnlyList<AiMessage> messages)
    {
        var list = new JsonArray();
        foreach (var message in messages) list.Add(new JsonObject { ["role"] = message.Role, ["content"] = message.Content });

        var body = new JsonObject { ["model"] = connection.Model, ["max_tokens"] = connection.MaxTokens ?? DefaultMaxTokens, ["messages"] = list };
        if (!string.IsNullOrWhiteSpace(system)) body["system"] = system;
        if (connection.ReasoningEffort.Length > 0) body["output_config"] = new JsonObject { ["effort"] = connection.ReasoningEffort };

        var request = PostJson(UrlOf(connection, "messages"), body);
        request.Headers.Add("anthropic-version", ApiVersion);
        if (!string.IsNullOrEmpty(apiKey)) request.Headers.Add("x-api-key", apiKey);
        return request;
    }

    /// <inheritdoc/>
    protected override string? ReadText(JsonNode? response)
    {
        if (response?["stop_reason"]?.ToString() == "refusal" || response?["content"] is not JsonArray content) return null;
        return string.Concat(content.Where(p => p?["type"]?.ToString() == "text").Select(p => p?["text"]?.ToString()));
    }
}
