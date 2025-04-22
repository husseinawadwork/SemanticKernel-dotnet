using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Agent = Azure.AI.Projects.Agent;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;



#pragma warning disable CS0618 
#pragma warning disable SKEXP0001,SKEXP0110,OPENAI001 // Type or member is obsolete// Suppress warning for evaluation purposes

AgentsClient agentsClient = new AgentsClient("%AZURE_OPENAI_PROJECT_CONNECTION_STRING%",
 new DefaultAzureCredential());

 
const string ReviewerName = "ArtDirector";
const string ReviewerInstructions =
        """
        You are an art director who has opinions about copywriting born of a love for David Ogilvy.
        The goal is to determine is the given copy is acceptable to print.
        If so, state that it is approved.
        If not, provide insight on how to refine suggested copy without example.
        """;
const string CopyWriterName = "CopyWriter";
const string CopyWriterInstructions =
        """
        You are a copywriter with ten years of experience and are known for brevity and a dry humor.
        The goal is to refine and decide on the single best copy as an expert in the field.
        Only provide a single proposal per response.
        You're laser focused on the goal at hand.
        Don't waste time with chit chat.
        Consider suggestions when refining an idea.
        """;


Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "%DEPLOYMENT_NAME%",
    name: ReviewerName,
    instructions: ReviewerInstructions
    );


// Define the agent
#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
AzureAIAgent agentReviewer = new AzureAIAgent(agentClientDefinition, agentsClient);


ChatCompletionAgent agentWriter =
            new()
            {
                Kernel = BuildKernel(),
                Name = CopyWriterName,
                Instructions = CopyWriterInstructions
            };



// Create a chat for agent interaction.
AgentGroupChat chat =
    new(agentWriter, agentReviewer)
    {
        ExecutionSettings =
            new()
            {
                // Here a TerminationStrategy subclass is used that will terminate when
                // an assistant message contains the term "approve".
                TerminationStrategy =
                    new ApprovalTerminationStrategy()
                    {
                        // Only the art-director may approve.
                        Agents = [agentReviewer],
                        // Limit total number of turns
                        MaximumIterations = 10,
                    }
            }
    };


// Invoke chat and display messages.
ChatMessageContent input = new(AuthorRole.User, "concept: maps made out of egg cartons.");
chat.AddChatMessage(input);
Console.WriteLine(input);

string lastAgent = string.Empty;
await foreach (StreamingChatMessageContent response in chat.InvokeStreamingAsync())
{
    if (string.IsNullOrEmpty(response.Content))
    {
        continue;
    }

    if (!lastAgent.Equals(response.AuthorName, StringComparison.Ordinal))
    {
        Console.WriteLine($"\n# {response.Role} - {response.AuthorName ?? "*"}:");
        lastAgent = response.AuthorName ?? string.Empty;
    }

    Console.WriteLine($"\t > streamed: '{response.Content}'");
}



// Display the chat history.
Console.WriteLine("================================");
Console.WriteLine("CHAT HISTORY");
Console.WriteLine("================================");

ChatMessageContent[] history = await chat.GetChatMessagesAsync().Reverse().ToArrayAsync();

for (int index = 0; index < history.Length; index++)
{
    Console.WriteLine(history[index].AuthorName + ": " + history[index]);
}

Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");



#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    (       "%%{AZURE_OPENAI_DEPLOYMENT_NAME}",
            "%%{AZURE_OPENAI_ENDPOINT}",
            "%%{AZURE_OPENAI_API_KEY}");

    return kernelBuilder.Build();
}


class ApprovalTerminationStrategy : TerminationStrategy
{
    // Terminate when the final message contains the term "approve"
    protected override Task<bool> ShouldAgentTerminateAsync(Microsoft.SemanticKernel.Agents.Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
}
