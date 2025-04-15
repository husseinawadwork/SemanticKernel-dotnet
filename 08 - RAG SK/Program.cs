#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052, SKEXP0070


using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.SemanticKernel;


var ollamaEndpoint = "http://localhost:11434";
var modelIdChat = "phi4-mini";
var modelIdEmbeddings = "phi4-mini";


var configOllamaKernelMemory = new OllamaConfig
{
    Endpoint = ollamaEndpoint,
    TextModel = new OllamaModelConfig(modelIdChat),
    EmbeddingModel = new OllamaModelConfig(modelIdEmbeddings, 2048)
};


// Create a kernel with Ollama chat completion service
var builder = Kernel.CreateBuilder().AddOllamaChatCompletion(
    modelId: modelIdChat, 
    endpoint: new Uri(ollamaEndpoint));



var question = "What is Donald Duck's favourite moive?";

Console.WriteLine(question);
Console.WriteLine("Answer:");
Console.WriteLine($"");
Console.WriteLine($"");

Kernel kernel = builder.Build();
var response = kernel.InvokePromptStreamingAsync(question);
await foreach (var result in response)
{
    Console.Write(result.ToString());
}



// separator
Console.WriteLine($"");
Console.WriteLine($"");
Console.WriteLine($"");
Console.WriteLine($"");
Console.WriteLine("*******************");

var memory = new KernelMemoryBuilder()
    .WithOllamaTextGeneration(configOllamaKernelMemory)
    .WithOllamaTextEmbeddingGeneration(configOllamaKernelMemory)
    .Build();


Console.WriteLine($"Adding information to the memory.");
var facts = new List<string>
{
    "Mickey Mouse's favourite movie is Star Wars",
    "The last movie watched by Mickey Mouse was The incredibles",
    "Donald Duck's favourite movie is The Amazing Spider Man",
    "The last movie watched by Donald Duck was The Incredibles",
    "Mickey Mouse and Minnie watched the movies The Incredibles",
    "Mickey Mouse, Donald Duck and Minnie are Disney characters",
};



int docId = 1;
foreach (var fact in facts)
{
    Console.WriteLine($"Adding docId: {docId} - fact: {fact}", true);
    await memory.ImportTextAsync(fact, docId.ToString());
    docId++;
}


var question2 = "How many total characters watched The Incredibles in general?";

Console.WriteLine($"Asking question with memory: {question2}");
var answer = memory.AskStreamingAsync(question2);


await foreach (var result in answer)
{
    Console.WriteLine($"{result.Result}");
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine($"Token Usage", true);
    foreach (var token in result.TokenUsage)
    {
        Console.WriteLine($"\t>> Tokens IN: {token.TokenizerTokensIn}", true);
        Console.WriteLine($"\t>> Tokens OUT: {token.TokenizerTokensOut}", true);
    }

    Console.WriteLine();
    Console.WriteLine($"Sources", true);
    foreach (var source in result.RelevantSources)
    {
        Console.WriteLine($"\t>> Content Type: {source.SourceContentType}", true);
        Console.WriteLine($"\t>> Document Id: {source.DocumentId}", true);
        Console.WriteLine($"\t>> 1st Partition Text: {source.Partitions.FirstOrDefault().Text}", true);
        Console.WriteLine($"\t>> 1st Partition Relevance: {source.Partitions.FirstOrDefault().Relevance}", true);
        Console.WriteLine();
    }


}
