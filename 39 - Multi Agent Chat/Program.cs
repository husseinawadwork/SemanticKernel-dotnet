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
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel.Agents.Chat;

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



#pragma warning disable CS0618 
#pragma warning disable SKEXP0001,SKEXP0110,OPENAI001 // Type or member is obsolete// Suppress warning for evaluation purposes

AgentsClient agentsClient = new AgentsClient("%AZURE_AI_PROJECT_CONNECTION_STRING%",
 new DefaultAzureCredential());

 
ChatCompletionAgent agentReviewer =
            new()
            {
                Instructions = ReviewerInstructions,
                Name = ReviewerName,
                Kernel = BuildKernel(),
            };


Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "%%DEPLOYMENT_NAME%%",
    name: CopyWriterName,
    tools: null,
    instructions: CopyWriterInstructions);


// Define the agent
#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
AzureAIAgent agentWriter = new AzureAIAgent(agentClientDefinition, agentsClient);

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

await foreach (ChatMessageContent response in chat.InvokeAsync())
{
    Console.WriteLine(response.AuthorName + ": " + response);
    Console.WriteLine();
    Console.WriteLine();
}

Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");


#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    (       "%DEPLOYMENT_NAME%",
            "%%AZURE_OPENAI_ENDPOINT%",
            "%%AZURE_OPENAI_API_KEY%");

    return kernelBuilder.Build();
}


class ApprovalTerminationStrategy : TerminationStrategy
{
    // Terminate when the final message contains the term "approve"

    protected override Task<bool> ShouldAgentTerminateAsync(Microsoft.SemanticKernel.Agents.Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
}