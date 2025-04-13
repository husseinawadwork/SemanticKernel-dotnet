using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;


IChatClient client = new ChatCompletionsClient(
        endpoint: new Uri("%MODEL INFERENCE ENDPOINT%"),
        new AzureKeyCredential("%AZURE_OPENAI_API_KEY%") ??
         throw new InvalidOperationException("Missing API Key."))
        .AsChatClient("%DEPLOYMENT_NAME%");   

//IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434/"), "phi4-mini");

var response = await client.GetResponseAsync("What is AI?");

Console.WriteLine(response.Text);