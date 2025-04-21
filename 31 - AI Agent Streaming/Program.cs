using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Agent = Azure.AI.Projects.Agent;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents;
using System.ComponentModel;


AgentsClient agentsClient = new AgentsClient("%PROJECT_CONNECTION_STRING%",
 new DefaultAzureCredential());

const string AgentName = "Host";
const string AgentInstructions = "Answer questions about the menu.";

Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "%DEPLOYMENT_NAME%",
    name: AgentName,
    tools: null,
    instructions: AgentInstructions);

#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
AzureAIAgent agent = new AzureAIAgent(agentClientDefinition, agentsClient);

KernelPlugin plugin = KernelPluginFactory.CreateFromType<MenuPlugin>();
agent.Kernel.Plugins.Add(plugin);

#pragma warning restore CS0618 // Type or member is obsolete
AzureAIAgentThread thread = new AzureAIAgentThread(agentsClient);

// Create a thread for the agent conversation.
AzureAIAgentThread agentThread = new(agentsClient);

// Respond to user input
await InvokeAgentAsync(agent, agentThread, "What is the special soup and its price?");
await InvokeAgentAsync(agent, agentThread, "What is the special drink and its price?");

// Output the entire chat history
await DisplayChatHistoryAsync(agentThread);

async Task InvokeAgentAsync(AzureAIAgent agent, Microsoft.SemanticKernel.Agents.AgentThread agentThread, string input)
{
    ChatMessageContent message = new(AuthorRole.User, input);
    Console.WriteLine(message);

    // For this sample, also capture fully formed messages so we can display them later.
    ChatHistory history = [];
    Task OnNewMessage(ChatMessageContent message)
    {
        history.Add(message);
        return Task.CompletedTask;
    }

    bool isFirst = false;
    bool isCode = false;
    await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread, new AgentInvokeOptions() { OnIntermediateMessage = OnNewMessage }))
    {
        if (string.IsNullOrEmpty(response.Content))
        {
            StreamingFunctionCallUpdateContent? functionCall = response.Items.OfType<StreamingFunctionCallUpdateContent>().SingleOrDefault();
            if (functionCall != null)
            {
                Console.WriteLine($"\n# {response.Role} - {response.AuthorName ?? "*"}: FUNCTION CALL - {functionCall.Name}");
            }

            continue;
        }

        // Differentiate between assistant and tool messages
        if (isCode != (response.Metadata?.ContainsKey(AzureAIAgent.CodeInterpreterMetadataKey) ?? false))
        {
            isFirst = false;
            isCode = !isCode;
        }

        if (!isFirst)
        {
            Console.WriteLine($"\n# {response.Role} - {response.AuthorName ?? "*"}:");
            isFirst = true;
        }

        Console.WriteLine($"\t > streamed: '{response.Content}'");
    }

    foreach (ChatMessageContent content in history)
    {
        Console.WriteLine(content);
    }
}

async Task DisplayChatHistoryAsync(AzureAIAgentThread agentThread)
{
    Console.WriteLine("================================");
    Console.WriteLine("CHAT HISTORY");
    Console.WriteLine("================================");

    ChatMessageContent[] messages = await agentThread.GetMessagesAsync().ToArrayAsync();
    for (int index = messages.Length - 1; index >= 0; --index)
    {
        Console.WriteLine(messages[index]);
    }
}

class MenuPlugin
{
    [KernelFunction, Description("Provides a list of specials from the menu.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
    public string GetSpecials()
    {
        return @"
Special Soup: Clam Chowder
Special Salad: Cobb Salad
Special Drink: Chai Tea
";
    }

    [KernelFunction, Description("Provides the price of the requested menu item.")]
    public string GetItemPrice(
        [Description("The name of the menu item.")]
        string menuItem)
    {
        return "$9.99";
    }
}