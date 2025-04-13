using Azure.AI.Inference;
using Azure;
using Microsoft.Extensions.AI;
using System.ComponentModel;


[Description("Get the weather")]
static string GetTheWeather()
{
    var temperature = Random.Shared.Next(5, 20);
    var conditions = Random.Shared.Next(0, 1) == 0 ? "sunny" : "rainy";
    var weatherInfo = $"The weather is {temperature} degrees C and {conditions}.";
    Console.WriteLine($"\tFunction Call - Returning weather info: {weatherInfo}");
    return weatherInfo;
}


var AzureApiKey = "%AZURE_OPENAI_API_KEY%";
if (string.IsNullOrEmpty(AzureApiKey))
{
    Console.WriteLine("Please set the Azure API key in the code.");
    return;
}


ChatOptions options = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(GetTheWeather)
    ]
};

/*IChatClient client = new ChatCompletionsClient(
    endpoint: new Uri("%AZURE_OPENAI_ENDPOINT%"),
    new AzureKeyCredential(AzureApiKey))
    .AsChatClient("%DEPLOYMENT_NAME%")
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();
*/



IChatClient client = new OllamaChatClient(
    endpoint: "http://localhost:11434/",
    modelId: "phi4-mini")
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var question = "Do I need an umbrella today?";
Console.WriteLine($"question: {question}");
var response = await client.GetResponseAsync(question, options);
Console.WriteLine($"response: {response}");