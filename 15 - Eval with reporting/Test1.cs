using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace TestAIWithReporting
{
    [TestClass]
    public sealed class Test1
    {
        public TestContext? TestContext { get; set; }
        private static ChatConfiguration? s_chatConfiguration;

        private string ScenarioName => $"{TestContext!.FullyQualifiedTestClassName}.{TestContext.TestName}";

        private static string ExecutionName => $"{DateTime.Now:yyyyMMddTHHmmss}";

        private static ReportingConfiguration s_defaultReportingConfiguration =>
            DiskBasedReportingConfiguration.Create(
                storageRootPath: "C:\\AI\\TestReports",
                evaluators: GetEvaluators(),
                chatConfiguration: s_chatConfiguration ?? throw new InvalidOperationException("ChatConfiguration is not initialized."),
                enableResponseCaching: true,
                executionName: ExecutionName);

        [ClassInitialize]
        public static async Task InitializeAsync(TestContext _)
        {
            Azure.AI.OpenAI.AzureOpenAIClient azureClient =
            new(
                new Uri("%%{AZURE_OPENAI_ENDPOINT}"),
                new AzureKeyCredential("AZURE_OPENAI_KEY"));
            IChatClient client = azureClient.GetChatClient("%DEPLOYMENT_NAME%").AsIChatClient();

            s_chatConfiguration = new ChatConfiguration(client);
        }

        private static async Task<(IList<ChatMessage> Messages, ChatResponse ModelResponse)> GetAstronomyConversationAsync(
                IChatClient chatClient,
                string astronomyQuestion)
        {
            const string SystemPrompt =
                """
        You're an AI assistant that can answer questions related to astronomy.
        Keep your responses concise and under 100 words.
        Use the imperial measurement system for all measurements in your response.
        """;

            IList<ChatMessage> messages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, SystemPrompt),
                    new ChatMessage(ChatRole.User, astronomyQuestion)
                };

            var chatOptions =
                new ChatOptions
                {
                    Temperature = 0.0f,
                    ResponseFormat = ChatResponseFormat.Text
                };

            ChatResponse response = await chatClient.GetResponseAsync(messages, chatOptions);
            return (messages, response);
        }


        [TestMethod]
        public async Task SampleAndEvaluateResponse()
        {
            // Create a <see cref="ScenarioRun"/> with the scenario name
            // set to the fully qualified name of the current test method.
            await using ScenarioRun scenarioRun =
                await s_defaultReportingConfiguration.CreateScenarioRunAsync(this.ScenarioName);

            // Use the <see cref="IChatClient"/> that's included in the
            // <see cref="ScenarioRun.ChatConfiguration"/> to get the LLM response.
            (IList<ChatMessage> messages, ChatResponse modelResponse) = await GetAstronomyConversationAsync(
                chatClient: scenarioRun.ChatConfiguration!.ChatClient,
                astronomyQuestion: "How far is the Moon from the Earth at its closest and furthest points?");

            // Run the evaluators configured in <see cref="s_defaultReportingConfiguration"/> against the response.
            EvaluationResult result = await scenarioRun.EvaluateAsync(messages, modelResponse);

            // Run some basic validation on the evaluation result.
            Validate(result);
        }

        private static IEnumerable<IEvaluator> GetEvaluators()
        {
            IEvaluator rtcEvaluator = new RelevanceTruthAndCompletenessEvaluator();
            IEvaluator wordCountEvaluator = new WordCountEvaluator();

            return new[] { rtcEvaluator, wordCountEvaluator };
        }

        private static void Validate(EvaluationResult result)
        {
            // Retrieve the score for relevance from the <see cref="EvaluationResult"/>.
            NumericMetric relevance =
                result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.RelevanceMetricName);
            Assert.IsFalse(relevance.Interpretation!.Failed, relevance.Reason);
            Assert.IsTrue(relevance.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);

            // Retrieve the score for truth from the <see cref="EvaluationResult"/>.
            NumericMetric truth = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.TruthMetricName);
            Assert.IsFalse(truth.Interpretation!.Failed, truth.Reason);
            Assert.IsTrue(truth.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);

            // Retrieve the score for completeness from the <see cref="EvaluationResult"/>.
            NumericMetric completeness =
                result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.CompletenessMetricName);
            Assert.IsFalse(completeness.Interpretation!.Failed, completeness.Reason);
            Assert.IsTrue(completeness.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);

            // Retrieve the word count from the <see cref="EvaluationResult"/>.
            NumericMetric wordCount = result.Get<NumericMetric>(WordCountEvaluator.WordCountMetricName);
            Assert.IsFalse(wordCount.Interpretation!.Failed, wordCount.Reason);
            Assert.IsTrue(wordCount.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
            Assert.IsFalse(wordCount.ContainsDiagnostics());
            Assert.IsTrue(wordCount.Value > 5 && wordCount.Value <= 100);
        }
    }
}