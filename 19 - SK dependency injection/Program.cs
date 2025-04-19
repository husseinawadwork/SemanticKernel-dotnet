using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;



        // If an application follows DI guidelines, the following line is unnecessary because DI will inject an instance of the KernelClient class to a class that references it.
        // DI container guidelines - https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#recommendations
        var serviceProvider = BuildServiceProvider();
        var kernel = serviceProvider.GetRequiredService<Kernel>();

        // Invoke the kernel with a templated prompt and stream the results to the display
        KernelArguments arguments = new() { { "topic", "earth when viewed from space" } };
        await foreach (var update in
                       kernel.InvokePromptStreamingAsync("What color is the {{$topic}}? Provide a detailed explanation.", arguments))
        {
            Console.Write(update);
        }
   


 ServiceProvider BuildServiceProvider()
    {
        var collection = new ServiceCollection();
        collection.AddSingleton<IUserService>(new FakeUserService());

        var kernelBuilder = collection.AddKernel();
        kernelBuilder.Services.AddAzureOpenAIChatCompletion
        (       "%DEPLOYMENT_NAME%",
                "%AZURE_OPENAI_ENDPOINT%",
                "%AzURE_OPENAI_API_KEY%");

        kernelBuilder.Plugins.AddFromType<TimeInformation>();
        kernelBuilder.Plugins.AddFromType<UserInformation>();

        return collection.BuildServiceProvider();
    }


    public class TimeInformation()
    {
        [KernelFunction]
        [Description("Retrieves the current time in UTC.")]
        public string GetCurrentUtcTime()
        {
            var utcNow = DateTime.UtcNow.ToString("R");
            Console.WriteLine("Returning current time {0}", utcNow);
            return utcNow;
        }
    }

    public class UserInformation(IUserService userService)
    {
        [KernelFunction]
        [Description("Retrieves the current users name.")]
        public string GetUsername()
        {
            return userService.GetCurrentUsername();
        }
    }

     public interface IUserService
    {
        /// <summary>
        /// Return the user id for the current user.
        /// </summary>
        string GetCurrentUsername();
    }

    public class FakeUserService : IUserService
    {
        /// <inheritdoc/>
        public string GetCurrentUsername() => "Bob";
    }