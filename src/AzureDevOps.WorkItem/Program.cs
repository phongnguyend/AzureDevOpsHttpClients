using AzureDevOps.WorkItem;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    //.AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>();

var configuration = builder.Build();

var connectionInfo = new ConnectionInfo();

configuration.Bind(connectionInfo);

var client = new WorkItemClient(connectionInfo);

var outputFolder = Path.Combine(AppContext.BaseDirectory, "Images");

// List all Features
var features = await client.GetFeaturesAsync();
Console.WriteLine($"Found {features.Count} feature(s):");
foreach (var feature in features)
    Console.WriteLine($"  [{feature.Id}] {feature.Fields.SystemTitle} - {feature.Fields.SystemState}");

// List User Stories for each Feature
foreach (var feature in features)
{
    var userStories = await client.GetUserStoriesByFeatureAsync(feature.Id);
    Console.WriteLine($"\nUser Stories for Feature [{feature.Id}] '{feature.Fields.SystemTitle}': {userStories.Count} item(s)");
    foreach (var story in userStories)
        Console.WriteLine($"  [{story.Id}] {story.Fields.SystemTitle} - {story.Fields.SystemState} (Assigned: {story.Fields.SystemAssignedTo ?? "Unassigned"})");

    // Download images from the feature and its user stories
    //Console.WriteLine($"\nDownloading images for Feature [{feature.Id}]...");
    //await client.DownloadImagesAsync(feature, outputFolder);

    //foreach (var story in userStories)
    //{
    //    Console.WriteLine($"  Downloading images for User Story [{story.Id}]...");
    //    await client.DownloadImagesAsync(story, outputFolder);
    //}
}

// Clone a Feature and its User Stories to another project
//if (features.Count > 0)
//{
//    foreach (var feature in features)
//    {
//        var sourceFeature = feature;
//        var targetProject = "TargetProject";

//        Console.WriteLine($"\nCloning Feature [{sourceFeature.Id}] '{sourceFeature.Fields.SystemTitle}' to project '{targetProject}'...");
//        var cloneResult = await client.CloneFeatureAsync(sourceFeature.Id, targetProject);

//        Console.WriteLine($"  Cloned Feature: [{cloneResult.Feature.Id}] {cloneResult.Feature.Fields.SystemTitle}");
//        Console.WriteLine($"  Cloned {cloneResult.UserStories.Count} User Story/Stories:");
//        foreach (var story in cloneResult.UserStories)
//            Console.WriteLine($"    [{story.Id}] {story.Fields.SystemTitle}");
//    }
//}

Console.WriteLine("\nPress any key to continue ...");
Console.ReadLine();
