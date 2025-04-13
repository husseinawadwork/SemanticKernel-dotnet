using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ClientModel;
using System.Text;
using OllamaSharp;


var AzureApiKey = "%AZURE_API_KEY%";	
if (string.IsNullOrEmpty(AzureApiKey))
{
    Console.WriteLine("Please set the Azure API key in the code.");
    return;
}


var modelId = "%DEPLOYMENT_NAME%";
var uri = "%AZURE_OPENAI_ENDPOINT%";

// create client
var client = new OpenAIClient(new ApiKeyCredential(AzureApiKey), 
    new OpenAIClientOptions { Endpoint = new Uri(uri) });

// Create a chat completion service
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(modelId, client);

// Get the chat completion service
Kernel kernel = builder.Build();
var chat = kernel.GetRequiredService<IChatCompletionService>();

/*
#pragma warning disable SKEXP0001, SKEXP0070  

// create client
var chat = new OllamaApiClient("http://localhost:11434/", "phi4-mini")
    .AsChatCompletionService();*/

var history = new ChatHistory();
history.AddSystemMessage("You are a useful chatbot. If you don't know an answer, say 'I don't know!'. Always reply in a funny way. Use emojis if possible.");

while (true)
{
    Console.Write("Q: ");
    var userQ = Console.ReadLine();
    if (string.IsNullOrEmpty(userQ))
    {
        break;
    }
    history.AddUserMessage(userQ);

    var sb = new StringBuilder();
    var result = chat.GetStreamingChatMessageContentsAsync(history);
    Console.Write("AI: ");
    await foreach (var item in result)
    {
        sb.Append(item);
        Console.Write(item.Content);
    }
    Console.WriteLine();

    history.AddAssistantMessage(sb.ToString());
}
