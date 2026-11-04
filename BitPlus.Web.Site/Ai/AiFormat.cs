namespace BitPlus.Web.Site.Ai;

/// <summary>
/// AI API 요청 형식 (<see cref="AiConnection.Format"/>).
/// </summary>
public enum AiFormat
{
    /// <summary>OpenAI Chat Completions 호환 (OpenAI, Ollama, vLLM, LM Studio 등)</summary>
    OpenAI,

    /// <summary>Anthropic Claude Messages API</summary>
    Claude,

    /// <summary>Google Gemini generateContent API</summary>
    Gemini
}
