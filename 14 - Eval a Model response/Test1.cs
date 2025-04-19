using Azure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

[TestClass]
public sealed class MyTest
{

    private static ChatConfiguration? s_chatConfiguration;
    private static IList<ChatMessage> s_messages = [
    new ChatMessage(
        ChatRole.System,
        """
        You're an AI assistant that can answer questions related to astronomy.
        Keep your responses concise and try to stay under 100 words.
        Use the imperial measurement system for all measurements in your response.
        """),
    new ChatMessage(
        ChatRole.User,
        "How far is the planet Venus from Earth at its closest and furthest points?")];
    private static ChatResponse s_response = new();

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        Azure.AI.OpenAI.AzureOpenAIClient azureClient =
            new(
                new Uri("%AzureOpenAIEndpoint%"),
                new AzureKeyCredential("%AzureOpenAIKey%"));
        IChatClient client = azureClient.GetChatClient("%DEPLOYMENT_NAME").AsIChatClient();

        s_chatConfiguration = new ChatConfiguration(client);

        var chatOptions =
            new ChatOptions
            {
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Text
            };

        // Fetch the response to be evaluated
        s_response = await s_chatConfiguration.ChatClient.GetResponseAsync(s_messages, chatOptions);
    }

    [TestMethod]
    public async Task TestCoherence()
    {
        IEvaluator coherenceEvaluator = new CoherenceEvaluator();
        EvaluationResult result = await coherenceEvaluator.EvaluateAsync(
            s_messages,
            s_response,
            s_chatConfiguration);

        /// Retrieve the score for coherence from the <see cref="EvaluationResult"/>.
        NumericMetric coherence = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);

        // Validate the default interpretation
        // for the returned coherence metric.
        Assert.IsFalse(coherence.Interpretation!.Failed);
        Assert.IsTrue(coherence.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
        
        Assert.IsFalse(coherence.ContainsDiagnostics());
    }

}
