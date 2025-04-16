using Microsoft.Extensions.AI;
using Azure.AI.Inference;
using Azure;



var AzureApiKey = "%Azure_API_KEY%";
if (string.IsNullOrEmpty(AzureApiKey))
{
    Console.WriteLine("Please set the Azure API key in the code.");
    return;
}


IChatClient chatClient =
    new ChatCompletionsClient(
        endpoint: new Uri("%AZURE_ENDPOINT%"),
        new AzureKeyCredential(AzureApiKey))
        .AsChatClient("%DEPLOYMENT_NAME%");


string imgRunningShoes = "running-shoes.jpg";
string imgCarLicense = "license.jpg";
string imgReceipt = "german-receipt.jpg";

// prompts
var prompt = "Describe the image";
var prompt_runningshoe = "Describe the image and then tell me How many red shoes are in the picture? and what other shoes colors are there?";
var prompt_license = "Describe the image and then What is the text in this picture? Is there a theme for this?";
var prompt_receipt = "I bought the coffee and the sausage. How much do I owe? Add a 18% tip.";


// prompts
string systemPrompt = @"You are a useful assistant that describes images using a direct style.";
string imageFileName = imgRunningShoes;
string image = Path.Combine(Directory.GetCurrentDirectory(), "images", imageFileName);

List<ChatMessage> messages =
[
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.System, systemPrompt),
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, prompt_runningshoe),
];


// read the image bytes, create a new image content part and add it to the messages
AIContent aic = new DataContent(File.ReadAllBytes(image), "image/jpeg");
var message = new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, [aic]);
    messages.Add(message);


// send the messages to the assistant
var response = await chatClient.GetResponseAsync(messages);
Console.WriteLine();
Console.WriteLine();
Console.WriteLine($"Prompt: {prompt}");
Console.WriteLine($"Image: {imageFileName}");
Console.WriteLine($"Response: {response.Message.Text}");
