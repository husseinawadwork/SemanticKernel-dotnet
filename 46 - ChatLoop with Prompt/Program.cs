
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion
    ("%DEPLOYMENT_NAME%",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AZURE_OPENAI_APIKEY%").Build();


var chatHistory = new ChatHistory();
KernelArguments arguments = new() { { "chatHistory", chatHistory } };
string[] userMessages = [
           "What is Seattle?",
            "What is the population of Seattle?",
            "What is the area of Seattle?",
            "What is the weather in Seattle?",
            "What is the zip code of Seattle?",
            "What is the elevation of Seattle?",
            "What is the latitude of Seattle?",
            "What is the longitude of Seattle?",
            "What is the mayor of Seattle?"
       ];


foreach (var userMessage in userMessages)
{
    chatHistory.AddUserMessage(userMessage);
    Console.WriteLine(chatHistory.Last().Role + ": " + chatHistory.Last().Content);

    var function = kernel.CreateFunctionFromPrompt(
        new()
        {
            Template =
            """
                    {{#each (chatHistory)}}
                    <message role="{{Role}}">{{Content}}</message>
                    {{/each}}
                    """,
            TemplateFormat = "handlebars"
        },
        new HandlebarsPromptTemplateFactory()
    );

    var response = await kernel.InvokeAsync(function, arguments);

    chatHistory.AddAssistantMessage(response.ToString());
    Console.WriteLine(chatHistory.Last().Role + ": " + chatHistory.Last().Content);
}
