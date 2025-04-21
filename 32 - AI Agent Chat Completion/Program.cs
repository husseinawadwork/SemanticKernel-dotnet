using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Agent = Azure.AI.Projects.Agent;
using Azure.Identity;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

// Define the agent
AgentsClient agentsClient = new AgentsClient("AZURE_OPENAI_RPOJECT_CONNECTION_STRING",
 new DefaultAzureCredential());

const string AgentName = "Host";
const string AgentInstructions = "Answer questions about the menu.";

Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "gpt-4o-mini",
    name: AgentName,
    tools: null,
    instructions: AgentInstructions);

#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
AzureAIAgent agent = new AzureAIAgent(agentClientDefinition, agentsClient)
{
    Kernel = BuildKernel(),
    Arguments = new KernelArguments((new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }))
};

KernelPlugin plugin = KernelPluginFactory.CreateFromType<MenuPlugin>();
agent.Kernel.Plugins.Add(plugin);

#pragma warning restore CS0618 // Type or member is obsolete
AzureAIAgentThread thread = new AzureAIAgentThread(agentsClient);
// Create a thread for the agent conversation.
AzureAIAgentThread agentThread = new(agentsClient);

await InvokeAgentAsync("Hello");
await InvokeAgentAsync("What is the special soup?");
await InvokeAgentAsync("What is the price of the special drink?");
await InvokeAgentAsync("Thank you");

// Output the entire chat history
WriteChatHistory(await agentThread.GetMessagesAsync().ToArrayAsync());


async Task InvokeAgentAsync(string input)
{
    ChatMessageContent message = new(AuthorRole.User, input);
    Console.WriteLine("User: " + message);

    await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))
    {
        Console.WriteLine("Agent: "+ response);
    }
}

void WriteChatHistory(IEnumerable<ChatMessageContent> chat)
{
    Console.WriteLine("================================");
    Console.WriteLine("CHAT HISTORY");
    Console.WriteLine("================================");
    foreach (ChatMessageContent message in chat)
    {
        Console.WriteLine(message);
    }
}

Kernel BuildKernel()
{

    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    (       "%DELOYMENT_NAME%",
            "%AZURE_OPENAI_ENDPOINT%",
            "%Azure_OPENAI_API_KEY%");

    kernelBuilder.Services.AddSingleton<IAutoFunctionInvocationFilter>(new AutoInvocationFilter());

    return kernelBuilder.Build();
}



class AutoInvocationFilter(bool terminate = true) : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        // Execution the function
        await next(context);

        // Signal termination if the function is from the MenuPlugin
        if (context.Function.PluginName == nameof(MenuPlugin))
        {
            context.Terminate = terminate;
        }
    }
}


class MenuPlugin
{
    [KernelFunction, Description("Provides a list of specials from the menu.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
    public string GetSpecials()
    {
        return
            """
                Special Soup: Chicken Noodle Soup
                """;
    }

    [KernelFunction, Description("Provides the price of the requested menu item.")]
    public string GetItemPrice(
        [Description("The name of the menu item.")]
        string menuItem)
    {
        return "$9.99";
    }

    [KernelFunction, Description("Greating the user.")]
    public string GetGreetings(
        string menuItem)
    {
        return "Aloha";
    }

    [KernelFunction, Description("Wishing a nice day.")]
    public string GetGoodBye(
        string menuItem)
    {
        return "Have a nice day!";
    }
}