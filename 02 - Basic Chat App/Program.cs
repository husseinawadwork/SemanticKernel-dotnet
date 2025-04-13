using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.Text;

var AzureApiKey = "%AZURE_API_KEY%";
if(string.IsNullOrEmpty(AzureApiKey))
{
    Console.WriteLine("Please set the Azure API key in the code.");
    return;
}

/*IChatClient client = new ChatCompletionsClient(
        endpoint: new Uri("%AZURE_OPENAI_ENDPOINT%"),
        new AzureKeyCredential(AzureApiKey) ??
         throw new InvalidOperationException("Missing API Key."))
        .AsChatClient("%DEPLOYMENT_NAME%");
*/

IChatClient client =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "phi4-mini");

// here we're building the prompt
StringBuilder prompt = new StringBuilder();
prompt.AppendLine("You will analyze the sentiment of the following product reviews. Each line is its own review. Output the sentiment of each review in a bulleted list and then provide a generate sentiment of all reviews. ");
prompt.AppendLine("I bought this jacket and it's amazing. I love it!");
prompt.AppendLine("This phone is terrible. I hate it.");
prompt.AppendLine("I'm not sure about this shoes. It's okay.");
prompt.AppendLine("I found this headset based on the other reviews on Amazon. It worked for some time, and then it didn't.");

// send the prompt to the model and wait for the text completion
var response = await client.GetResponseAsync(prompt.ToString());

// display the response
Console.WriteLine(response.Text);
