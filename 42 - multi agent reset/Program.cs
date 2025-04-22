using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Agent = Azure.AI.Projects.Agent;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents;

#pragma warning disable CS0618 
#pragma warning disable SKEXP0001,SKEXP0110,OPENAI001 // Type or member is obsolete// Suppress warning for evaluation purposes

AgentsClient agentsClient = new AgentsClient("%%AZURE_AI_PROJECTS_ENDPOINT%%",
 new DefaultAzureCredential());

 
const string AgentInstructions =
        """
        The user may either provide information or query on information previously provided.
        If the query does not correspond with information provided, inform the user that their query cannot be answered.
        """;



Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "gpt-4o-mini",
    name: "Analyst",
    instructions: AgentInstructions
    );


// Define the agent
#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
AzureAIAgent agent = new AzureAIAgent(agentClientDefinition, agentsClient);


ChatCompletionAgent chatAgent =
            new()
            {
                Kernel = BuildKernel(),
                Name = "ChatAgent",
                Instructions = AgentInstructions
            };



// Create a chat for agent interaction.
AgentGroupChat chat = new();

// Respond to user input
await InvokeAzureAIAgentAsync(agent, "What is my favorite color?");
await InvokeAgentAsync(chatAgent);

await InvokeAzureAIAgentAsync(agent, "I like green.");
await InvokeAgentAsync(chatAgent);

await InvokeAzureAIAgentAsync(agent, "What is my favorite color?");
await InvokeAgentAsync(chatAgent);

await chat.ResetAsync();

await InvokeAzureAIAgentAsync(agent, "What is my favorite color?");
await InvokeAgentAsync(chatAgent);



#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    ("%%AZURE_OPENAI_API_DEPLOYMENT_NAME%%",
            "%%AZURE_OPENAI_API_ENDPOINT%%",
            "%%AZURE_OPENAI_API_KEY%%");

    return kernelBuilder.Build();
}



// Local function to invoke agent and display the conversation messages.
async Task InvokeAzureAIAgentAsync(AzureAIAgent agent, string? input = null)
{
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        chat.AddChatMessage(new(AuthorRole.User, input));
        Console.WriteLine("agent1: " + message);
    }

    await foreach (ChatMessageContent response in chat.InvokeAsync(agent))
    {
        Console.WriteLine(response.AuthorName + ": " + response);
        //await DownloadResponseContentAsync(response);
    }
}



// Local function to invoke agent and display the conversation messages.
async Task InvokeAgentAsync(ChatCompletionAgent agent, string? input = null)
{
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        chat.AddChatMessage(new(AuthorRole.User, input));
        Console.WriteLine("Agent2: " + message);
    }

    await foreach (ChatMessageContent response in chat.InvokeAsync(agent))
    {
        Console.WriteLine(response.AuthorName + ": " + response);
        //await DownloadResponseContentAsync(response);
    }
}