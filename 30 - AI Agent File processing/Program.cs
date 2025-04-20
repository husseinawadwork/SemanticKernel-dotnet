using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Agent = Azure.AI.Projects.Agent;
using Azure.Identity;


AgentsClient agentsClient = new AgentsClient("%AZURE_AI_PROJECT_CONNECTION_STRING%",
 new DefaultAzureCredential());

var resourceStream =
            Path.Combine(Directory.GetCurrentDirectory(), "sales.csv");
if (resourceStream == null)
{
    throw new FileNotFoundException("Embedded resource 'sales.csv' not found.");
}

AgentFile fileInfo = await agentsClient.UploadFileAsync(resourceStream, AgentFilePurpose.Agents);


Agent agentClientDefinition = await agentsClient.CreateAgentAsync(
    model: "%DEPLOYMENT_NAME%",
    name: "Sales Report Generator",
    tools: [new CodeInterpreterToolDefinition()],
    toolResources:
        new()
        {
            CodeInterpreter = new()
            {
                FileIds = { fileInfo.Id },
            }
        });

//Agent agentClientDefinition = await agentsClient.GetAgentAsync("asst_q1gfombzElyyVr6xbUGKY8IM");
#pragma warning disable SKEXP0110 // Type or member is obsolete
AzureAIAgent agent = new AzureAIAgent(agentClientDefinition, agentsClient);
#pragma warning restore CS0618 // Type or member is obsolete
AzureAIAgentThread thread = new AzureAIAgentThread(agentsClient);

// Respond to user input
//ChatMessageContent message = new ChatMessageContent(AuthorRole.User, "What is the total sales amount?");
ChatMessageContent message = new ChatMessageContent(AuthorRole.User,"List the top 5 countries that generated the most profit.");


Console.WriteLine(message);

await foreach (ChatMessageContent response in agent.InvokeAsync(message, thread))
{
    Console.WriteLine(response);
}


