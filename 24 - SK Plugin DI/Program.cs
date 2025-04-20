using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;


// Create a kernel with OpenAI chat completion
var serviceProvider = BuildServiceProvider();
var kernel = serviceProvider.GetRequiredService<Kernel>();


var plugin = await kernel.CreatePluginFromOpenApiAsync("RepairService",
Path.Combine(Directory.GetCurrentDirectory(), "repair-service.json"));

kernel.Plugins.Add(plugin);
//kernel.Plugins.Add(TransformPlugin(plugin));

PromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
Console.WriteLine(await kernel.InvokePromptAsync("List all of the repairs .", new(settings)));
//Console.WriteLine(await kernel.InvokePromptAsync("Book an appointment to drain the old engine oil and replace it with fresh oil.", new(settings)));


ServiceProvider BuildServiceProvider()
{
    var collection = new ServiceCollection();
    collection.AddSingleton<IMechanicService>(new FakeMechanicService());

    var kernelBuilder = collection.AddKernel();
    kernelBuilder.AddAzureOpenAIChatCompletion(
        "%DEPLOYMENT_NAME%",
        "%AZURE_OPENAI_ENDPOINT%",
        "%AZURE_OPENAI_API_KEY%");

    return collection.BuildServiceProvider();
}


static KernelPlugin TransformPlugin(KernelPlugin plugin)
{
    List<KernelFunction>? functions = [];

    foreach (KernelFunction function in plugin)
    {
        if (function.Name == "createRepair")
        {
            functions.Add(CreateRepairFunction(function));
        }
        else
        {
            functions.Add(function);
        }
    }

    return KernelPluginFactory.CreateFromFunctions(plugin.Name, plugin.Description, functions);
}

static KernelFunction CreateRepairFunction(KernelFunction function)
{

    var method = (
        Kernel kernel,
        KernelFunction currentFunction,
        KernelArguments arguments,
        [FromKernelServices] IMechanicService mechanicService,
        CancellationToken cancellationToken) =>
    {
        arguments.Add("assignedTo", mechanicService.GetMechanic());
        arguments.Add("date", DateTime.UtcNow.ToString("R"));
        return function.InvokeAsync(kernel, arguments, cancellationToken);
    };

    var options = new KernelFunctionFromMethodOptions()
    {
        FunctionName = function.Name,
        Description = function.Description,
        Parameters = function.Metadata.Parameters.ToList(),
        ReturnParameter = function.Metadata.ReturnParameter,
    };


    return KernelFunctionFactory.CreateFromMethod(method, options);
}

public interface IMechanicService
{
    /// <summary>
    /// Return the name of the mechanic to assign the next job to.
    /// </summary>
    string GetMechanic();
}

/// <summary>
/// Fake implementation of <see cref="IMechanicService"/>
/// </summary>
public class FakeMechanicService : IMechanicService
{
    /// <inheritdoc/>
    public string GetMechanic() => "%%MECHANIC_NAME%%";
}
