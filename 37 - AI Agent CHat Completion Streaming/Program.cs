using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Agent = Azure.AI.Projects.Agent;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Serialization;
using System.ComponentModel;
using Microsoft.Extensions.Azure;

const string ParrotName = "Parrot";
const string ParrotInstructions = "Repeat the user message in the voice of a pirate.";


Kernel kernel = BuildKernel();
ChatCompletionAgent agent =
            new()
            {
                Name = ParrotName,
                Instructions = ParrotInstructions,
                Kernel = kernel,
            };


#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
ChatHistoryAgentThread agentThread = new();

// Respond to user input
await InvokeAgentAsync(agent, agentThread, "Everybody loves chocloate.");
await InvokeAgentAsync(agent, agentThread, "Coffee is the new gold.");

// Output the entire chat history
await DisplayChatHistory(agentThread);

    
#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    (       "%%DEPLOYMENT_NAME%%",
            "%%AZURE_OPENAI_ENDPOINT%%",
            "%%AZURE_OPENAI_API_KEY%%");

    return kernelBuilder.Build();
}


async Task InvokeAgentAsync(ChatCompletionAgent agent, ChatHistoryAgentThread agentThread, string input)
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        Console.WriteLine(message);

        int historyCount = agentThread.ChatHistory.Count;

        bool isFirst = false;
        await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
        {
            if (string.IsNullOrEmpty(response.Content))
            {
                StreamingFunctionCallUpdateContent? functionCall = response.Items.OfType<StreamingFunctionCallUpdateContent>().SingleOrDefault();
                if (!string.IsNullOrEmpty(functionCall?.Name))
                {
                    Console.WriteLine($"\n# {response.Role} - {response.AuthorName ?? "*"}: FUNCTION CALL - {functionCall.Name}");
                }

                continue;
            }

            if (!isFirst)
            {
                Console.WriteLine($"\n# {response.Role} - {response.AuthorName ?? "*"}:");
                isFirst = true;
            }

            Console.WriteLine($"\t > streamed: '{response.Content}'");
        }

        if (historyCount <= agentThread.ChatHistory.Count)
        {
            for (int index = historyCount; index < agentThread.ChatHistory.Count; index++)
            {
                Console.WriteLine(agentThread.ChatHistory[index]);
            }
        }
    }

    
    async Task DisplayChatHistory(ChatHistoryAgentThread agentThread)
    {
        // Display the chat history.
        Console.WriteLine("================================");
        Console.WriteLine("CHAT HISTORY");
        Console.WriteLine("================================");

        await foreach (ChatMessageContent message in agentThread.GetMessagesAsync())
        {
            Console.WriteLine(message);
        }
    }