using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Agent = Azure.AI.Projects.Agent;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents;
using System.ComponentModel;
using Microsoft.Extensions.Azure;


AgentsClient agentsClient = new AgentsClient("%%AZURE_AI_PROJECTS_ENDPOINT%%",
 new DefaultAzureCredential());

const string TranslatorName = "Numeric Counter";
const string TranslatorInstructions = "Add one to latest user number and spell it in English.";

Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "gpt-4o-mini",
    name: TranslatorName,
    tools: null,
    instructions: TranslatorInstructions);


#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
Kernel kernel = BuildKernel();
ChatCompletionAgent agent = new ChatCompletionAgent
{
    Name = TranslatorName,
    Instructions = TranslatorInstructions,
    Kernel = kernel,
    HistoryReducer = new ChatHistorySummarizationReducer(kernel.GetRequiredService<IChatCompletionService>(), 10),
};

await InvokeAgentAsync(agent, 50);


#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{

    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    ("%DEPLOYMENT_NAME%",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AzURE_OPENAI_API_KEY%");

    return kernelBuilder.Build();
}



async Task InvokeAgentAsync(ChatCompletionAgent agent, int messageCount)
{
    ChatHistoryAgentThread agentThread = new();

    int index = 1;
    while (index <= messageCount)
    {
        // Provide user input
        Console.WriteLine($"# {AuthorRole.User}: '{index}'");

        // Reduce prior to invoking the agent
        bool isReduced = await agent.ReduceAsync(agentThread.ChatHistory);

        try
        {
            var messages = agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, $"# {AuthorRole.User}: '{index}'"), agentThread);

            // Invoke and display assistant response
            await foreach (ChatMessageContent message in messages)
            {
                Console.WriteLine($"# {message.Role} - {message.AuthorName ?? "*"}: '{message.Content}'");
            }
        }        catch (HttpOperationException ex){}
        


        index += 2;

        // Display the message count of the chat-history for visibility into reduction
        Console.WriteLine($"@ Message Count: {agentThread.ChatHistory.Count}\n");

        // Display summary messages (if present) if reduction has occurred
        if (isReduced)
        {
            int summaryIndex = 0;
            while (agentThread.ChatHistory[summaryIndex].Metadata?.ContainsKey(ChatHistorySummarizationReducer.SummaryMetadataKey) ?? false)
            {
                Console.WriteLine($"\tSummary: {agentThread.ChatHistory[summaryIndex].Content}");
                ++summaryIndex;
            }
        }
    }
    // Output the entire chat history
    WriteChatHistory(await agentThread.GetMessagesAsync().ToArrayAsync());

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