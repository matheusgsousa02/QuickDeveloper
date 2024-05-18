using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.IO.Compression;
using System.Text;

namespace Services.Services
{
    public class ChatGptServices
    {
        private readonly IConfiguration _configuration;
        private readonly UtilitiesServices _utilities;

        public ChatGptServices(IConfiguration configuration)
        {
            _configuration = configuration;
            _utilities = new UtilitiesServices();
        }

        public async Task<Models.ComplementationResponse> CreateComplementation(Models.ComplementationRequest request)
        {
            string apiKey = _configuration?.GetSection("Api-Key")?.Value ?? throw new Exception("Não foi possível recuperar a API Key da OpenAI na AppSettings. Por favor, verificar!");
            using var openAIClient = new OpenAIClient(apiKey);

            if (Guid.TryParse(request.SessionId, out Guid sessionId) == false)
            {
                sessionId = Guid.NewGuid();
            }

            string history = $"O QuickBot está auxiliando e tirando dúvidas do usuário {request.Name} para a prevenção de doenças infecciosas, respondendo ou o questionando sobre possíveis casos. O QuickBot não pode em qualquer momento fugir desse tema, independente da pergunta do usuário caso ela seja fora do escopo de doenças infecciosas\n";
            if (!string.IsNullOrEmpty(request.History))
            {
                history = request.History;
            }

            string question;
            string prompt = history + $"{request.Name}: " + request.Question + "\nQuickBot: ";

            question = await GenerateQuestion(openAIClient, prompt, sessionId);

            return new Models.ComplementationResponse
            {
                SessionId = sessionId,
                Name = request.Name,
                History = history + $"{request.Name}: " + request.Question + "\n",
                Message = question,
            };
        }

        private async Task<string> GenerateQuestion(OpenAIClient openAIClient, string prompt, Guid sessionId)
        {
            var messages = new List<Message>
        {
            new Message(Role.System, "You are a helpful assistant."),
            new Message(Role.User, prompt)
        };

            var chatRequest = new ChatRequest(messages, Model.GPT3_5_Turbo, maxTokens: 500, user: sessionId.ToString());
            var response = await openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);
            var choice = response.FirstChoice;

            _utilities.GerarLog(sessionId, chatRequest, response, "Chat", false);

            return choice.Message;
        }
    }
}