using AzureDevOps.NuGetFeed;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

var feedInfo = new FeedInfo
{
    Organization = "xxx",
    Project = "xxx",
    FeedId = "xxx",
    PersonalAccessToken = "xxx"
};

var client = new HttpClient();
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", feedInfo.ApiAccessToken);

var jsonOptions = new JsonSerializerOptions()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

HttpResponseMessage response = await client.GetAsync(feedInfo.FeedUrl + "/packages?api-version=7.0");
response.EnsureSuccessStatusCode();
string responseBody = await response.Content.ReadAsStringAsync();

var packages = JsonSerializer.Deserialize<PackagesResponse>(responseBody, jsonOptions).Value;

foreach (var package in packages)
{
    responseBody = await (await client.GetAsync(package.Url + "/versions?api-version=7.0&isDeleted=false")).Content.ReadAsStringAsync();
    package.Versions = JsonSerializer.Deserialize<PackageVersionsResponse>(responseBody, jsonOptions).Value;

    responseBody = await (await client.PostAsJsonAsync(package.Url + "/versionmetricsbatch?api-version=7.0", new
    {
        PackageVersionIds = package.Versions.Where(x => x.IsListed).Select(x => x.Id).ToArray()
    })).Content.ReadAsStringAsync();
    package.VersionMetrics = JsonSerializer.Deserialize<PackageVersionMetricsResponse>(responseBody, jsonOptions).Value;

    var deletedPackageVersions = package.Versions.Where(x => !x.IsLatest && !x.IsDeleted && x.IsListed && !package.VersionMetrics.Any(y => y.PackageVersionId == x.Id)).ToArray();
    deletedPackageVersions = deletedPackageVersions.Where(x => x.PublishDate <= DateTime.Now.AddDays(-14)).ToArray();

    Console.WriteLine($"Package: {package.Name} - {package.Versions.Count} packages - Needs to delete: {deletedPackageVersions.Length} packages");

    foreach (var deletedPackageVersion in deletedPackageVersions)
    {
        var packageUrl = feedInfo.NuGetUrl + $"/packages/{package.Name}/versions/{deletedPackageVersion.Version}?api-version=7.0";

        responseBody = await (await client.DeleteAsync(packageUrl)).Content.ReadAsStringAsync();

        Console.WriteLine($"Package: {package.Name} - Deleted: {deletedPackageVersion.Version}");
    }

    var sprints = package.Versions.GroupBy(x => x.GetSprint());
    foreach (var sprint in sprints)
    {
        var versions = sprint.Where(x => !x.IsLatest && !x.IsDeleted && x.IsListed && package.VersionMetrics.Any(y => y.PackageVersionId == x.Id))
            .OrderByDescending(x => x.Version)
            .Skip(5)
            .ToList();

        Console.WriteLine($"Package: {package.Name} - Sprint: {sprint.Key} - {sprint.Count()} packages - Needs to delete: {versions.Count} packages");

        foreach (var deletedPackageVersion in versions)
        {
            var packageUrl = feedInfo.NuGetUrl + $"/packages/{package.Name}/versions/{deletedPackageVersion.Version}?api-version=7.0";

            responseBody = await (await client.DeleteAsync(packageUrl)).Content.ReadAsStringAsync();

            Console.WriteLine($"Package: {package.Name} - Deleted: {deletedPackageVersion.Version}");
        }
    }

}

Console.WriteLine("Press any key to continue ...");
Console.ReadLine();