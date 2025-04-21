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

AgentsClient agentsClient = new AgentsClient("%AZURE_AI_PROJECT_CONNECTION_STRING%",
 new DefaultAzureCredential());

const string ServiceKeyGood = "chat-good";
const string ServiceKeyBad = "chat-bad";

Kernel kernel = BuildKernel();

ChatCompletionAgent agentGood =
            new()
            {
                Kernel = kernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { ServiceId = ServiceKeyGood }),
            };

ChatCompletionAgent agentBad =
    new()
    {
        Kernel = kernel,
        Arguments = new KernelArguments(new PromptExecutionSettings() { ServiceId = ServiceKeyBad }),
    };

ChatCompletionAgent agentDefault = new() { Kernel = kernel };


// Invoke agent as initialized with ServiceId = ServiceKeyGood: Expect agent response
Console.WriteLine("\n[Agent With Good ServiceId]");
await InvokeAgentAsync(agentGood);

// Invoke agent as initialized with ServiceId = ServiceKeyBad: Expect failure due to invalid service key
Console.WriteLine("\n[Agent With Bad ServiceId]");
await InvokeAgentAsync(agentBad);

// Invoke agent as initialized with no explicit ServiceId: Expect agent response
Console.WriteLine("\n[Agent With No ServiceId]");
await InvokeAgentAsync(agentDefault);

// Invoke agent with override arguments where ServiceId = ServiceKeyGood: Expect agent response
Console.WriteLine("\n[Bad Agent: Good ServiceId Override]");
await InvokeAgentAsync(agentBad, new(new PromptExecutionSettings() { ServiceId = ServiceKeyGood }));

// Invoke agent with override arguments where ServiceId = ServiceKeyBad: Expect failure due to invalid service key
Console.WriteLine("\n[Good Agent: Bad ServiceId Override]");
await InvokeAgentAsync(agentGood, new(new PromptExecutionSettings() { ServiceId = ServiceKeyBad }));
Console.WriteLine("\n[Default Agent: Bad ServiceId Override]");
await InvokeAgentAsync(agentDefault, new(new PromptExecutionSettings() { ServiceId = ServiceKeyBad }));

// Invoke agent with override arguments with no explicit ServiceId: Expect agent response
Console.WriteLine("\n[Good Agent: No ServiceId Override]");
await InvokeAgentAsync(agentGood, new(new PromptExecutionSettings()));
Console.WriteLine("\n[Bad Agent: No ServiceId Override]");
await InvokeAgentAsync(agentBad, new(new PromptExecutionSettings()));
Console.WriteLine("\n[Default Agent: No ServiceId Override]");
await InvokeAgentAsync(agentDefault, new(new PromptExecutionSettings()));

#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddAzureOpenAIChatCompletion
    (       "DEPLOYMENT_NAME",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AZURE_OPENAI_API_KEY%", 
            ServiceKeyBad);

    kernelBuilder.AddAzureOpenAIChatCompletion
    (       "%DEPLOYMENT_NAME%",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AZURE_OPENAI_API_KEY%", 
            ServiceKeyGood);

    return kernelBuilder.Build();
}


async Task InvokeAgentAsync(ChatCompletionAgent agent, KernelArguments? arguments = null)
{
    try
    {
        await foreach (ChatMessageContent response in agent.InvokeAsync(
            new ChatMessageContent(AuthorRole.User, "Hello"),
            options: new() { KernelArguments = arguments }))
        {
            Console.WriteLine(response.Content);
        }
    }
    catch (HttpOperationException exception)
    {
        Console.WriteLine($"Status: {exception.StatusCode}");
    }
}