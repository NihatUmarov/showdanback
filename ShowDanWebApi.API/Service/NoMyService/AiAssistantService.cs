using ShowDanWebApi.Core.DTO;
using System.Text.Json;
using System.Text;


namespace ShowDanWebApi.API.Service;

public interface IAiAssistantService
{
    Task<string> ProcessChatAsync(string userMessage, UserAiState currentState);
}

public class AiAssistantService : IAiAssistantService
{
    }

    private string BuildPrompt(string userMessage, UserAiState currentState)
    {
        var historyToShow = currentState.History.Skip(Math.Max(0, currentState.History.Count - 6));
        var formattedHistory = new StringBuilder();

        foreach (var m in historyToShow)
        {
            formattedHistory.AppendLine(m.Role == "user" ? $"U:{m.Content}" : $"A:{m.Content}");
        }

        return "English";
    }
}