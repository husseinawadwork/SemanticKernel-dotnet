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

AgentsClient agentsClient = new AgentsClient("%%AZURE_OPENAI_PROJECT_CONNECTION_STRING%%",
 new DefaultAzureCredential());

 
const string TranslatorName = "Translator";
const string TranslatorInstructions =
    """
        Spell the last number in chat as a word in english and spanish on a single line without any line breaks.
        """;

const string CounterName = "Counter";
const string CounterInstructions =
    """
        Increment the last number from your most recent response.
        Never repeat the same number.
        
        Only respond with a single number that is the result of your calculation without explanation.
        """;


Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "%%DEPLOYMENT_NAME%%",
    name: TranslatorName,
    instructions: TranslatorInstructions
    );


// Define the agent
#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
AzureAIAgent agentTranslator = new AzureAIAgent(agentClientDefinition, agentsClient);


ChatCompletionAgent agentCounter =
            new()
            {
                Kernel = BuildKernel(),
                Name = CounterName,
                Instructions = CounterInstructions
            };


// Create a chat for agent interaction.
AgentGroupChat chat = CreateGroupChat();
// Invoke chat and display messages.
ChatMessageContent input = new(AuthorRole.User, "1");
chat.AddChatMessage(input);
Console.WriteLine(input);

Console.WriteLine("============= Dynamic Agent Chat - Primary (prior to serialization) ==============");
await InvokeAgents(chat);

AgentGroupChat copy = CreateGroupChat();
Console.WriteLine("\n=========== Serialize and restore the Agent Chat into a new instance ============");
await CloneChatAsync(chat, copy);

Console.WriteLine("\n============ Continue with the dynamic Agent Chat (after deserialization) ===============");
await InvokeAgents(copy);

Console.WriteLine("\n============ The entire Agent Chat (includes messages prior to serialization and those after deserialization) ==============");
await foreach (ChatMessageContent content in copy.GetChatMessagesAsync())
{
    Console.WriteLine(content);
}



async Task InvokeAgents(AgentGroupChat chat)
{
    await foreach (ChatMessageContent content in chat.InvokeAsync())
    {
        Console.WriteLine(content.AuthorName+": " + content);
    }
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


AgentGroupChat CreateGroupChat() =>
            new(agentTranslator, agentCounter)
            {
                ExecutionSettings =
                    new()
                    {
                        TerminationStrategy =
                            new CountingTerminationStrategy(5)
                            {
                                // Only the art-director may approve.
                                Agents = [agentTranslator],
                                // Limit total number of turns
                                MaximumIterations = 20,
                            }
                    }
            };




#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    (       "%%AZURE_OPENAI_CHAT_DEPLOYMENT_NAME%%",
            "%%AZURE_OPENAI_ENDPOINT%%",
            "%%AZURE_OPENAI_API_KEY%%");

    return kernelBuilder.Build();
}



class CountingTerminationStrategy(int maxTurns) : TerminationStrategy
{
    private int _count = 0;

    protected override Task<bool> ShouldAgentTerminateAsync(Microsoft.SemanticKernel.Agents.Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        ++this._count;

        bool shouldTerminate = this._count >= maxTurns;

        return Task.FromResult(shouldTerminate);
    }
}