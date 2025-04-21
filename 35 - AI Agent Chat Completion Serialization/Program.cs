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

AgentsClient agentsClient = new AgentsClient("AZURE_AI_PROJECT_CONNECTION_STRING",
 new DefaultAzureCredential());

const string HostName = "Host";
const string HostInstructions = "Answer questions about the menu.";


Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "%DEPLOYMENT_NAME%",
    name: HostName,
    tools: null,
    instructions: HostInstructions);


#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
Kernel kernel = BuildKernel();
ChatCompletionAgent agent = new ChatCompletionAgent
{
    Name = HostName,
    Instructions = HostInstructions,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
};

// Initialize plugin and add to the agent's Kernel (same as direct Kernel usage).
KernelPlugin plugin = KernelPluginFactory.CreateFromType<MenuPlugin>();
agent.Kernel.Plugins.Add(plugin);

AgentGroupChat chat = CreateGroupChat();


// Invoke chat and display messages.
Console.WriteLine("============= Dynamic Agent Chat - Primary (prior to serialization) ==============");
await InvokeAgentAsync(chat, "Hello");
await InvokeAgentAsync(chat, "What is the special soup?");

AgentGroupChat CreateGroupChat() => new(agent);
AgentGroupChat copy = CreateGroupChat();
Console.WriteLine("\n=========== Serialize and restore the Agent Chat into a new instance ============");
await CloneChatAsync(chat, copy);

Console.WriteLine("\n============ Continue with the dynamic Agent Chat (after deserialization) ===============");
await InvokeAgentAsync(copy, "What is the special drink?");
await InvokeAgentAsync(copy, "Thank you");

Console.WriteLine("\n============ The entire Agent Chat (includes messages prior to serialization and those after deserialization) ==============");
await foreach (ChatMessageContent content in copy.GetChatMessagesAsync())
{
    Console.WriteLine(content);
}


#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    ("%DEPLOYMENT_NAME%",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AZURE_OPENAI_API_KEY%");

    return kernelBuilder.Build();
}


// Local function to invoke agent and display the conversation messages.
async Task InvokeAgentAsync(AgentGroupChat chat, string input)
{
    ChatMessageContent message = new(AuthorRole.User, input);
    chat.AddChatMessage(message);

    Console.WriteLine(message);
    try
    {
        await foreach (ChatMessageContent content in chat.InvokeAsync())
        {
            Console.WriteLine(content);
        }
    }
    catch (Exception ex) { }
}


async Task CloneChatAsync(AgentGroupChat source, AgentGroupChat clone)
{
    await using MemoryStream stream = new();
    await AgentChatSerializer.SerializeAsync(source, stream);

    stream.Position = 0;
    using StreamReader reader = new(stream);
    Console.WriteLine(await reader.ReadToEndAsync());

    stream.Position = 0;
    AgentChatSerializer serializer = await AgentChatSerializer.DeserializeAsync(stream);
    await serializer.DeserializeAsync(clone);
}


class MenuPlugin
{
    [KernelFunction, Description("Provides a list of specials from the menu.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
    public string GetSpecials() =>
        """
            Special Soup: Clam Chowder
            Special Salad: Cobb Salad
            Special Drink: Chai Tea
            """;

    [KernelFunction, Description("Provides the price of the requested menu item.")]
    public string GetItemPrice(
        [Description("The name of the menu item.")]
            string menuItem) =>
        "$9.99";
}

