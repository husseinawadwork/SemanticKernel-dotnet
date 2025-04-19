using Microsoft.SemanticKernel;

// Create a kernel with OpenAI chat completion
        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                "%DEPLOYMENT_NAME%",
                "%AZURE_OPENAI_ENDPOINT%",
                "%AZURE_OPENAI_API_KEY%")
            .Build();

 // Invoke the kernel with a chat prompt and display the result
        string chatPrompt = """
            <message role="user">What is Sydney?</message>
            <message role="system">Respond with JSON.</message>
            """;

Console.WriteLine(await kernel.InvokePromptAsync(chatPrompt));