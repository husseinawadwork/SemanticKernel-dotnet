using Microsoft.SemanticKernel;

const string ChatPrompt = """
            <message role="user">What is Seattle?</message>
            <message role="system">Respond with JSON.</message>
            """;


var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion
    ("%%DEPLOYMENT_NAME",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AZURE_OPENAI_APIKEY%").Build();

var chatSemanticFunction = kernel.CreateFunctionFromPrompt(ChatPrompt);
var chatPromptResult = await kernel.InvokeAsync(chatSemanticFunction);


Console.WriteLine("Chat Prompt:");
Console.WriteLine(ChatPrompt);
Console.WriteLine("Chat Prompt Result:");
Console.WriteLine(chatPromptResult);

Console.WriteLine("Chat Prompt Streaming Result:");
string completeMessage = string.Empty;
await foreach (var message in kernel.InvokeStreamingAsync<string>(chatSemanticFunction))
{
    completeMessage += message;
    Console.Write(message);
}

Console.WriteLine("---------- Streamed Content ----------");
Console.WriteLine(completeMessage);
