using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;


var deploymentName = "%AZURE_OPENAI_DEPLOYMENT_NAME%"; // e.g., "gpt-4o-mini" or "gpt-4o" for the full model
var endpoint = "%AZURE_OPENAI_ENDPOINT%";
var apiKey = "%AZURE_OPENAI_API_KEY%";
var modelId = "%AZURE_OPENAI_MODEL_ID%"; // e.g., "gpt-4o-mini" or "gpt-4o" for the full model


//// Initialize the OpenAI chat completion service with the gpt-4o-mini model.
var chatService = new AzureOpenAIChatCompletionService(
    deploymentName: deploymentName, 
    endpoint: endpoint,
    apiKey: apiKey,
    modelId: modelId
);


// Create a new chat history and add a user message to prompt the model.
ChatHistory chatHistory = [];
chatHistory.AddUserMessage("Why is the sky blue in one sentence?");

#pragma warning disable SKEXP0010 // Reasoning effort is still in preview for OpenAI SDK.
// Configure reasoning effort for the chat completion request.
var settings = new OpenAIPromptExecutionSettings { ReasoningEffort = "High" };


// Send the chat completion request to o3-mini
var reply = await chatService.GetChatMessageContentAsync(chatHistory, settings);
Console.WriteLine("o3-mini reply: " + reply);