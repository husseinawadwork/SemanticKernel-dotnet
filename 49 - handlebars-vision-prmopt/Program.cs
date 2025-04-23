using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

const string HandlebarsTemplate = """
            <message role="system">You are an AI assistant designed to help with image recognition tasks.</message>
            <message role="user">
               <text>{{request}}</text>
               <image>{{imageData}}</image>
            </message>
            """;


var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion
    ("%DEPLOYMENT_NAME",
            "%AZURE_OPENAI_ENDPOINT%",
            "%AZURE_OPENAI_API_KEY%").Build();


var templateFactory = new HandlebarsPromptTemplateFactory();
var promptTemplateConfig = new PromptTemplateConfig()
{
    Template = HandlebarsTemplate,
    TemplateFormat = "handlebars",
    Name = "Vision_Chat_Prompt",
};
var function = kernel.CreateFunctionFromPrompt(promptTemplateConfig, templateFactory);


var arguments = new KernelArguments(new Dictionary<string, object?>
        {
            {"request","Describe this image:"},
            {"imageData", "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAAXNSR0IArs4c6QAAACVJREFUKFNj/KTO/J+BCMA4iBUyQX1A0I10VAizCj1oMdyISyEAFoQbHwTcuS8AAAAASUVORK5CYII="}
        });

var response = await kernel.InvokeAsync(function, arguments);
Console.WriteLine(response);