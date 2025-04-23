using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.Core;
using System.Globalization;


var systemPromptTemplate =
    @"You are an AI assistant that helps people find information.
    The chat started at: {{ $startTime }}
    The current time is: {{ time.now }}
    Text selected:
    {{ $selectedText }}";

var selectedText =
    @"The central Sahara is hyperarid, with sparse vegetation. 
    The northern and southern reaches of the desert, along with the highlands, 
    have areas of sparse grassland and desert shrub, with trees and taller shrubs in wadis, 
    where moisture collects. In the central, hyperarid region, there are many subdivisions 
    of the great desert: Tanezrouft, the Ténéré, the Libyan Desert, the Eastern Desert, 
    the Nubian Desert and others. These extremely arid areas often receive no rain for years.";

var userPromptTemplate =
    "{{ time.now }}: {{ $userMessage }}";


var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion
    ("%DEPLOYMENT_NAME%",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AZURE_OPENAI_API_KEY%").Build();

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
kernel.ImportPluginFromType<TimePlugin>("time");

// Adding required arguments referenced by the prompt templates.
var arguments = new KernelArguments
{
    // Put the selected document into the variable used by the system prompt (see 30-system-prompt.txt).
    ["selectedText"] = selectedText,

    // Demo another variable, e.g. when the chat started, used by the system prompt (see 30-system-prompt.txt).
    ["startTime"] = DateTimeOffset.Now.ToString("hh:mm:ss tt zz", CultureInfo.CurrentCulture),

    // This is the user message, store it in the variable used by 30-user-prompt.txt
    ["userMessage"] = "extract locations as a bullet point list"
};

var promptTemplateFactory = new KernelPromptTemplateFactory();

string systemMessage = await promptTemplateFactory.Create(
    new PromptTemplateConfig(systemPromptTemplate)).RenderAsync(kernel, arguments);
Console.WriteLine($"------------------------------------\n{systemMessage}");

// Render the user prompt. This string is the query sent by the user
// This contains the user request, ie "extract locations as a bullet point list"
string userMessage = await promptTemplateFactory.Create(
    new PromptTemplateConfig(userPromptTemplate)).RenderAsync(kernel, arguments);
Console.WriteLine($"------------------------------------\n{userMessage}");


// Client used to request answers
var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

// The full chat history. Depending on your scenario, you can pass the full chat if useful,
// or create a new one every time, assuming that the "system message" contains all the
// information needed.
var chatHistory = new ChatHistory(systemMessage);

// Add the user query to the chat history
chatHistory.AddUserMessage(userMessage);

// Finally, get the response from AI
var answer = await chatCompletion.GetChatMessageContentAsync(chatHistory);
Console.WriteLine($"------------------------------------\n{answer}");