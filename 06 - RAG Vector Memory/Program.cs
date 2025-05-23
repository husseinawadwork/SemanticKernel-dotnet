﻿using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

var vectorStore = new InMemoryVectorStore();

// get movie list
var movies = vectorStore.GetCollection<int, MovieVector<int>>("movies");
await movies.CreateCollectionIfNotExistsAsync();
var movieData = MovieFactory<int>.GetMovieVectorList();

// get embeddings generator and generate embeddings for movies
IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "phi4-mini");
foreach (var movie in movieData)
{
    movie.Vector = await generator.GenerateEmbeddingVectorAsync(movie.Description);
    await movies.UpsertAsync(movie);
}


// perform the search
var query = "A family friendly movie that includes AI and robots";
var queryEmbedding = await generator.GenerateEmbeddingVectorAsync(query);
var searchOptions = new VectorSearchOptions<MovieVector<int>>()
{  
    Top = 5,
    VectorPropertyName = nameof(MovieVector<int>.Vector)
};

var results = await movies.VectorizedSearchAsync(queryEmbedding, searchOptions);


await foreach (var result in results.Results)
{
    Console.WriteLine($"Title: {result.Record.Title}");
    Console.WriteLine($"Description: {result.Record.Description}");
    Console.WriteLine($"Score: {result.Score}");
    Console.WriteLine();
}