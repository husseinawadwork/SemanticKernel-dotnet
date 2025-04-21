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
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
using Microsoft.Identity.Client;

(string Input, string? Style)[] s_inputs =
        [
            (Input: "Home cooking is great.", Style: null),
            (Input: "Talk about world peace.", Style: "iambic pentameter"),
            (Input: "Say something about doing your best.", Style: "e. e. cummings"),
            (Input: "What do you think about having fun?", Style: "old school rap")
        ];


const string AgentName = "Poet";
const string instructionTemplate = "Write a one verse poem on the requested topic in the style of {{$style}}. Always state the requested style of the poem.";


// Instruction based template always processed by KernelPromptTemplateFactory
PromptTemplateConfig templateConfig =
            new()
            {
                Template = instructionTemplate,
                //TemplateFormat = PromptTemplateConfig.SemanticKernelTemplateFormat,
                TemplateFormat = HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
            };


ChatCompletionAgent agent =
    new(templateConfig, 
    //new KernelPromptTemplateFactory())
    new HandlebarsPromptTemplateFactory())
    {
        Kernel = BuildKernel(),
        Name = AgentName,
        Instructions = instructionTemplate,
        Arguments = new KernelArguments()
        {
                    {"style", "haiku"}
        }
    };

await InvokeChatCompletionAgentWithTemplateAsync(agent);



#pragma warning restore CS0618 // Type or member is obsolete
Kernel BuildKernel()
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddAzureOpenAIChatCompletion
    ("%DEPLOYMENT_NAME%",
            "%%{AZURE_OPENAI_ENDPOINT}",
            "%%{AZURE_OPENAI_API_KEY}");

    return kernelBuilder.Build();
}


async Task InvokeChatCompletionAgentWithTemplateAsync(ChatCompletionAgent agent)
{
    ChatHistory chat = [];

    foreach ((string input, string? style) in s_inputs)
    {
        // Add input to chat
        ChatMessageContent request = new(AuthorRole.User, input);
        Console.WriteLine(request);

        KernelArguments? arguments = null;

        if (!string.IsNullOrWhiteSpace(style))
        {
            // Override style template parameter
            arguments = new() { { "style", style } };
        }

        // Process agent response
        await foreach (ChatMessageContent message in agent.InvokeAsync(request, options: new() { KernelArguments = arguments }))
        {
            chat.Add(message);
            Console.WriteLine(message);
        }
    }
}
