using Azure.AI.Projects;
using System.Text.Json;

public static class CityInfo
{
    // This function is used to get the user's favorite city.
    public static string GetUserFavoriteCity() => "Sydney";
    public static FunctionToolDefinition getUserFavoriteCityTool = new("getUserFavoriteCity", "Gets the user's favorite city.");

    // This function is used to get the nickname of a city.
    public static  string GetWeatherAtLocation(string cityName) => cityName switch
    {
        "Sydney" => "20c",
        "London" => "-5c",
        _ => throw new NotImplementedException()
    };
    public static FunctionToolDefinition getWeatherAtLocationTool = new(
    name: "getWeatherAtLocation",
    description: "Gets the weather for a city, e.g. 'Sydney' or 'London'.",
    parameters: BinaryData.FromObjectAsJson(
        new
        {
            Type = "object",
            Properties = new
            {
                CityName = new
                {
                    Type = "string",
                    Description = "The city name",
                },
            },
            Required = new[] { "cityName" },
        },
        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

    // this function is used to get the parks of a city
    public static string GetParksAtLocation(string location) => location switch
    {
        "Sydney" => "Centennial Park in Sydney is a large urban park offering a variety of recreational activities and historical significance. It covers 189 hectares and includes formal gardens, ponds, sports fields, and historic buildings. It's known for its tree plantings, including Norfolk Island pines and Port Jackson figs",
        "London" => "Hyde Park in London offers a variety of attractions, including the iconic Serpentine Lake, where you can swim, paddle boat, or enjoy lakeside views",
        _ => throw new NotImplementedException()
    };
    public static FunctionToolDefinition getParksAtLocationTool = new(
name: "getParksAtLocation",
description: "Gets informations about parks for a city, e.g. 'Sydney' or 'London'.",
parameters: BinaryData.FromObjectAsJson(
    new
    {
        Type = "object",
        Properties = new
        {
            CityName = new
            {
                Type = "string",
                Description = "The city name",
            },
        },
        Required = new[] { "cityName" },
    },
    new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
