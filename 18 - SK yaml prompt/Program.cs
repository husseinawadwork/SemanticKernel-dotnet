
using System.Reflection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

// Create a kernel with OpenAI chat completion
        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                "%Deployment_NAME%",
                "%Azure_OpenAI_Endpoint%",
                "%Azure_OpenAI_Key%")
            .Build();

var resourceStream =
            Path.Combine(Directory.GetCurrentDirectory(), "GenerateStory.yaml");
        if (resourceStream == null)
        {
            throw new FileNotFoundException("Embedded resource 'GenerateStory.yaml' not found.");
        }
        using var reader = new StreamReader(resourceStream);
        var generateStoryYaml = reader.ReadToEnd();

        var function = kernel.CreateFunctionFromPromptYaml(generateStoryYaml);

        // Invoke the prompt function and display the result
        Console.WriteLine(await kernel.InvokeAsync(function, arguments: new()
            {
                { "topic", "Dog" },
                { "length", "3" },
            }));

Console.WriteLine("Handlebars");

    var resourceStreamHandlebar =
            Path.Combine(Directory.GetCurrentDirectory(), "GenerateStoryHandlebars.yaml");
        if (resourceStream == null)
        {
            throw new FileNotFoundException("Embedded resource 'GenerateStoryHandlebars.yaml' not found.");
        }
        using var readerHandlebars = new StreamReader(resourceStream);
        var generateStoryHandlebarsYaml = reader.ReadToEnd();

        var functionHandlebars = kernel.CreateFunctionFromPromptYaml(generateStoryYaml);

        // Invoke the prompt function and display the result
        Console.WriteLine(await kernel.InvokeAsync(functionHandlebars, arguments: new()
            {
                { "topic", "Cat" },
                { "length", "3" },
            }));