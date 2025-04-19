using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;


IKernelBuilder builder = Kernel.CreateBuilder();
// Create a kernel with OpenAI chat completion
builder.AddAzureOpenAIChatCompletion(
        "%DEPLOYMENT_NAME%",
        "%%AZURE_OPENAI_ENDPOINT%%",
        "%%AZURE_OPENAI_API_KEY%%");

builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
Kernel kernel = builder.Build();

KernelFunction parseDouble = 
    KernelFunctionFactory.CreateFromMethod((string s) => double.Parse(s, CultureInfo.InvariantCulture), "parseDouble");
KernelFunction multiplyByN = 
    KernelFunctionFactory.CreateFromMethod((double i, double n) => i * n, "multiplyByN");
KernelFunction truncate = 
    KernelFunctionFactory.CreateFromMethod((double d) => (int)d, "truncate");
KernelFunction humanize = KernelFunctionFactory.CreateFromPrompt(new PromptTemplateConfig()
    {
        Template = "Spell out this number in English: {{$number}}",
        InputVariables = [new() { Name = "number" }],
    });

KernelFunction pipeline = KernelFunctionCombinators.Pipeline([parseDouble, multiplyByN, truncate, humanize]);

KernelArguments arg = new()
    {
        ["s"] = "123.456",
        ["n"] = (double)78.90,
    };

Console.WriteLine(await pipeline.InvokeAsync(kernel, arg));


class KernelFunctionCombinators
{
    public static KernelFunction Pipeline(
        IEnumerable<KernelFunction> functions)
    {
        ArgumentNullException.ThrowIfNull(functions);

        KernelFunction[] funcs = functions.ToArray();
        Array.ForEach(funcs, f => ArgumentNullException.ThrowIfNull(f));

        var funcsAndVars = new (KernelFunction Function, string OutputVariable)[funcs.Length];
        for (int i = 0; i < funcs.Length; i++)
        {
            string p = "";
            if (i < funcs.Length - 1)
            {
                var parameters = funcs[i + 1].Metadata.Parameters;
                if (parameters.Count > 0)
                {
                    p = parameters[0].Name;
                }
            }
            Trace.WriteLine($"Function {i}: {funcs[i].Name} -> {p}");
            funcsAndVars[i] = (funcs[i], p);
        }

        return Pipe(funcsAndVars);
    }

    public static KernelFunction Pipe(
        IEnumerable<(KernelFunction Function, string OutputVariable)> functions)
    {
        ArgumentNullException.ThrowIfNull(functions);

        (KernelFunction Function, string OutputVariable)[] arr = functions.ToArray();
        Array.ForEach(arr, f =>
        {
            ArgumentNullException.ThrowIfNull(f.Function);
            ArgumentNullException.ThrowIfNull(f.OutputVariable);
        });

        return KernelFunctionFactory.CreateFromMethod(async (Kernel kernel, KernelArguments arguments) =>
        {
            FunctionResult? result = null;
            for (int i = 0; i < arr.Length; i++)
            {
                result = await arr[i].Function.InvokeAsync(kernel, arguments).ConfigureAwait(false);
                if (i < arr.Length - 1)
                {
                    arguments[arr[i].OutputVariable] = result.GetValue<object>();
                }
            }
            return result;
        });
    }
}