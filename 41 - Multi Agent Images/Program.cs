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


#pragma warning disable CS0618 
#pragma warning disable SKEXP0001,SKEXP0110,OPENAI001 // Type or member is obsolete// Suppress warning for evaluation purposes

AgentsClient agentsClient = new AgentsClient("%%AZURE_OPENAI_ENDPOINT",
 new DefaultAzureCredential());


const string AnalystName = "Analyst";
const string AnalystInstructions = "Create charts as requested without explanation.";

const string SummarizerName = "Summarizer";
const string SummarizerInstructions = "Summarize the entire conversation for the user in natural language.";


Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "%%{AZURE_OPENAI_DEPLOYMENT_NAME}",
    name: AnalystName,
    instructions: AnalystInstructions
    );


// Define the agent
#pragma warning disable SKEXP0001,SKEXP0110 // Type or member is obsolete
AzureAIAgent agent = new AzureAIAgent(agentClientDefinition, agentsClient);


ChatCompletionAgent summaryAgent =
            new()
            {
                Name = SummarizerName,
                Instructions = SummarizerInstructions,
                Kernel = BuildKernel(),
            };


// Create a chat for agent interaction.
AgentGroupChat chat = new();


// Respond to user input
await InvokeAzureAIAgentAsync(
               agent,
               """
                Graph the percentage of storm events by state using a pie chart:

                State, StormCount
                TEXAS, 4701
                KANSAS, 3166
                IOWA, 2337
                ILLINOIS, 2022
                MISSOURI, 2016
                GEORGIA, 1983
                MINNESOTA, 1881
                WISCONSIN, 1850
                NEBRASKA, 1766
                NEW YORK, 1750
                """);

Console.WriteLine();
Console.WriteLine();

await InvokeAgentAsync(summaryAgent);


// Local function to invoke agent and display the conversation messages.
async Task InvokeAzureAIAgentAsync(AzureAIAgent agent, string? input = null)
{
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        chat.AddChatMessage(new(AuthorRole.User, input));
        Console.WriteLine("agent: " + message);
    }

    await foreach (ChatMessageContent response in chat.InvokeAsync(agent))
    {
        Console.WriteLine(response.AuthorName +": " + response);
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
        Console.WriteLine("Summary Agent: "+message);
    }

    await foreach (ChatMessageContent response in chat.InvokeAsync(agent))
    {
        Console.WriteLine(response.AuthorName +": " + response);
        //await DownloadResponseContentAsync(response);
    }
}


#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    ("%%{AZURE_OPENAI_DEPLOYMENT_NAME}",
            "%%{AZURE_OPENAI_ENDPOINT}",
            "%%{AZURE_OPENAI_API_KEY}");

    return kernelBuilder.Build();
}