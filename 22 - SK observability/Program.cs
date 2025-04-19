using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;


IKernelBuilder kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                "%DEPLOYMENT_NAME%",
                "%AZURE_OPENAI_ENDPOINT%",
                "%AZURE_OPENAI_API_KEY%");

kernelBuilder.Plugins.AddFromType<TimeInformation>();

// Add filter using DI
kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter, MyFunctionFilter>();

Kernel kernel = kernelBuilder.Build();

// Add filter without DI
kernel.PromptRenderFilters.Add(new MyPromptFilter());


// Invoke the kernel with a prompt and allow the AI to automatically invoke functions
OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
Console.WriteLine(await kernel.InvokePromptAsync("How many days until Christmas? Explain your thinking.", new(settings)));

class TimeInformation
{
    [KernelFunction]
    [Description("Retrieves the current time in UTC.")]
    public string GetCurrentUtcTime() => DateTime.UtcNow.ToString("R");
}

class MyFunctionFilter() : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"Invoking {context.Function.Name}");

        await next(context);

        var metadata = context.Result?.Metadata;

        if (metadata is not null && metadata.ContainsKey("Usage"))
        {
            Console.WriteLine($"Token usage: {metadata["Usage"]}");
        }
    }
}


class MyPromptFilter() : IPromptRenderFilter
{

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        Console.WriteLine($"Rendering prompt for {context.Function.Name}");

        await next(context);

        Console.WriteLine($"Rendered prompt: {context.RenderedPrompt}");
    }
}