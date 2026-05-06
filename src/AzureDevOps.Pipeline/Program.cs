using AzureDevOps.Pipeline;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    //.AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>();

var configuration = builder.Build();

var connectionInfo = new ConnectionInfo();

configuration.Bind(connectionInfo);

var client = new PipelineClient(connectionInfo);

// List all pipelines
var pipelines = await client.ListPipelinesAsync();
Console.WriteLine($"Found {pipelines.Count} pipeline(s):");
foreach (var p in pipelines)
{
    Console.WriteLine($"  [{p.Id}] {p.Folder}/{p.Name}");
}

if (pipelines.Count == 0)
{
    Console.WriteLine("\nNo pipelines found. Exiting.");
    Console.WriteLine("\nPress any key to continue ...");
    Console.ReadLine();
    return;
}

// Run the first pipeline
var firstPipeline = pipelines.First(x => x.Name == "Build");
Console.WriteLine($"\nRunning pipeline [{firstPipeline.Id}] {firstPipeline.Name} ...");
var run = await client.RunPipelineAsync(firstPipeline.Id, new RunPipelineRequest
{
    Resources = new RunResources
    {
        Repositories = new RunRepositoryResources
        {
            Self = new RunRepositoryResource { RefName = "refs/heads/main" }
        }
    }
});
Console.WriteLine($"  Run [{run.Id}] started - State: {run.State}");

// List runs for the first pipeline
var runs = await client.ListRunsAsync(firstPipeline.Id);
Console.WriteLine($"\nFound {runs.Count} run(s) for pipeline '{firstPipeline.Name}':");
foreach (var r in runs)
{
    Console.WriteLine($"  [{r.Id}] {r.Name} - State: {r.State}, Result: {r.Result ?? "N/A"}, Created: {r.CreatedDate:u}");
}

// Get status of the triggered run
var runStatus = await client.GetRunAsync(firstPipeline.Id, run.Id);
Console.WriteLine($"\nStatus of run [{runStatus.Id}]: State={runStatus.State}, Result={runStatus.Result ?? "N/A"}");

Console.WriteLine("\nPress any key to continue ...");
Console.ReadLine();
