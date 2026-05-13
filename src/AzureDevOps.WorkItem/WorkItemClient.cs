using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureDevOps.WorkItem;

public class WorkItemClient
{
    private readonly HttpClient _httpClient;
    private readonly ConnectionInfo _connectionInfo;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ApiVersion = "api-version=7.0";

    public WorkItemClient(ConnectionInfo connectionInfo)
    {
        _connectionInfo = connectionInfo;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", connectionInfo.ApiAccessToken);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<WorkItem>> GetFeaturesAsync()
    {
        var wiqlRequest = new WiqlRequest
        {
            Query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{_connectionInfo.Project}' AND [System.WorkItemType] = 'Feature' ORDER BY [System.Id]"
        };

        return await RunWiqlAsync(wiqlRequest);
    }

    public async Task<List<WorkItem>> GetUserStoriesByFeatureAsync(int featureId)
    {
        var wiqlRequest = new WiqlRequest
        {
            Query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{_connectionInfo.Project}' AND [System.WorkItemType] = 'User Story' AND [System.Parent] = {featureId} ORDER BY [System.Id]"
        };

        return await RunWiqlAsync(wiqlRequest);
    }

    private async Task<List<WorkItem>> RunWiqlAsync(WiqlRequest wiqlRequest)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_connectionInfo.BaseUrl}/wiql?{ApiVersion}", wiqlRequest, _jsonOptions);
        await EnsureSuccessAsync(response);
        var body = await response.Content.ReadAsStringAsync();
        var wiqlResponse = JsonSerializer.Deserialize<WiqlResponse>(body, _jsonOptions);

        if (wiqlResponse?.WorkItems == null || wiqlResponse.WorkItems.Count == 0)
            return new List<WorkItem>();

        return await GetWorkItemsByIdsAsync(wiqlResponse.WorkItems.Select(x => x.Id).ToList());
    }

    private async Task<List<WorkItem>> GetWorkItemsByIdsAsync(List<int> ids)
    {
        var fields = string.Join(",", new[]
        {
            "System.Id",
            "System.Title",
            "System.State",
            "System.WorkItemType",
            "System.AssignedTo",
            "System.AreaPath",
            "System.IterationPath",
            "System.Description",
            "System.Parent",
            "Microsoft.VSTS.Common.AcceptanceCriteria",
            "Microsoft.VSTS.Common.StackRank"
        });

        var idList = string.Join(",", ids);
        var response = await _httpClient.GetAsync($"{_connectionInfo.BaseUrl}/workitems?ids={idList}&fields={fields}&{ApiVersion}");
        await EnsureSuccessAsync(response);
        var body = await response.Content.ReadAsStringAsync();

        var workItemsResponse = JsonSerializer.Deserialize<WorkItemsResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new WorkItemFieldsConverter() }
        });

        return workItemsResponse?.Value ?? new List<WorkItem>();
    }

    public async Task<CloneResult> CloneFeatureAsync(int featureId, string targetProject)
    {
        var feature = (await GetWorkItemsByIdsAsync([featureId])).FirstOrDefault()
            ?? throw new InvalidOperationException($"Feature with id {featureId} not found.");

        var userStories = await GetUserStoriesByFeatureAsync(featureId);

        var clonedFeature = await CreateWorkItemAsync(targetProject, feature.Fields.SystemWorkItemType, [
            new WorkItemPatchOperation { Path = "/fields/System.Title", Value = feature.Fields.SystemTitle },
            new WorkItemPatchOperation { Path = "/fields/System.Description", Value = feature.Fields.SystemDescription },
            new WorkItemPatchOperation { Path = "/fields/Microsoft.VSTS.Common.StackRank", Value = feature.Fields.StackRank?.ToString() },
            new WorkItemPatchOperation { Path = "/fields/System.State", Value = "New" }
        ]);

        var clonedStories = new List<WorkItem>();
        foreach (var story in userStories)
        {
            var clonedStory = await CreateWorkItemAsync(targetProject, story.Fields.SystemWorkItemType, [
                new WorkItemPatchOperation { Path = "/fields/System.Title", Value = story.Fields.SystemTitle },
                new WorkItemPatchOperation { Path = "/fields/System.Description", Value = story.Fields.SystemDescription },
                new WorkItemPatchOperation { Path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", Value = story.Fields.AcceptanceCriteria },
                new WorkItemPatchOperation { Path = "/fields/Microsoft.VSTS.Common.StackRank", Value = story.Fields.StackRank?.ToString() },
                new WorkItemPatchOperation { Path = "/fields/System.State", Value = "New" }
            ]);

            await AddParentRelationAsync(targetProject, clonedStory.Id, clonedFeature.Id);

            clonedStories.Add(clonedStory);
        }

        return new CloneResult
        {
            Feature = clonedFeature,
            UserStories = clonedStories
        };
    }

    private async Task<WorkItem> CreateWorkItemAsync(string project, string workItemType, List<WorkItemPatchOperation> operations)
    {
        var encodedType = Uri.EscapeDataString(workItemType);
        var url = $"https://dev.azure.com/{_connectionInfo.Organization}/{Uri.EscapeDataString(project)}/_apis/wit/workitems/${encodedType}?{ApiVersion}";

        var patchBody = JsonSerializer.Serialize(operations, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var content = new StringContent(patchBody, System.Text.Encoding.UTF8, "application/json-patch+json");
        var response = await _httpClient.PatchAsync(url, content);
        await EnsureSuccessAsync(response);
        var body = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<WorkItem>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new WorkItemFieldsConverter() }
        })!;
    }

    private async Task AddParentRelationAsync(string project, int childId, int parentId)
    {
        var url = $"https://dev.azure.com/{_connectionInfo.Organization}/{Uri.EscapeDataString(project)}/_apis/wit/workitems/{childId}?{ApiVersion}";

        var parentUrl = $"https://dev.azure.com/{_connectionInfo.Organization}/{Uri.EscapeDataString(project)}/_apis/wit/workitems/{parentId}";

        var operations = new[]
        {
            new { op = "add", path = "/relations/-", value = new { rel = "System.LinkTypes.Hierarchy-Reverse", url = parentUrl } }
        };

        var patchBody = JsonSerializer.Serialize(operations);
        var content = new StringContent(patchBody, System.Text.Encoding.UTF8, "application/json-patch+json");
        var response = await _httpClient.PatchAsync(url, content);
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}): {body}", null, response.StatusCode);
        }
    }

    public async Task DownloadImagesAsync(WorkItem workItem, string outputFolder)
    {
        var imageLinks = workItem.ImageLinks;
        if (imageLinks.Count == 0)
            return;

        var workItemFolder = Path.Combine(outputFolder, workItem.Id.ToString());
        Directory.CreateDirectory(workItemFolder);

        foreach (var imageUrl in imageLinks)
        {
            var uri = new Uri(imageUrl);
            if (!uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"    Skipped (external): {imageUrl}");
                continue;
            }

            var response = await _httpClient.GetAsync(imageUrl);
            await EnsureSuccessAsync(response);

            var fileName = uri.Query
                .TrimStart('?')
                .Split('&')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2 && p[0].Equals("fileName", StringComparison.OrdinalIgnoreCase))
                .Select(p => Uri.UnescapeDataString(p[1]))
                .FirstOrDefault()
                ?? Path.GetFileName(uri.LocalPath);

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                extension = contentType switch
                {
                    "image/png"     => ".png",
                    "image/jpeg"    => ".jpg",
                    "image/gif"     => ".gif",
                    "image/webp"    => ".webp",
                    "image/bmp"     => ".bmp",
                    "image/svg+xml" => ".svg",
                    _ => string.Empty
                };
            }

            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(Path.GetExtension(fileName)))
                fileName = $"{Guid.NewGuid()}{extension}";

            var filePath = Path.Combine(workItemFolder, fileName);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, imageBytes);
            Console.WriteLine($"    Downloaded: {filePath}");
        }
    }
}

internal class WorkItemFieldsConverter : JsonConverter<WorkItemFields>
{
    public override WorkItemFields Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var fields = new WorkItemFields();

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("System.Title", out var title))
            fields.SystemTitle = title.GetString();
        if (root.TryGetProperty("System.State", out var state))
            fields.SystemState = state.GetString();
        if (root.TryGetProperty("System.WorkItemType", out var type))
            fields.SystemWorkItemType = type.GetString();
        if (root.TryGetProperty("System.AssignedTo", out var assignedTo))
            fields.SystemAssignedTo = assignedTo.ValueKind == JsonValueKind.Object
                ? assignedTo.GetProperty("displayName").GetString()
                : assignedTo.GetString();
        if (root.TryGetProperty("System.AreaPath", out var areaPath))
            fields.SystemAreaPath = areaPath.GetString();
        if (root.TryGetProperty("System.IterationPath", out var iterationPath))
            fields.SystemIterationPath = iterationPath.GetString();
        if (root.TryGetProperty("System.Description", out var description))
            fields.SystemDescription = description.GetString();
        if (root.TryGetProperty("System.Parent", out var parent) && parent.ValueKind == JsonValueKind.Number)
            fields.SystemParent = parent.GetInt32();
        if (root.TryGetProperty("Microsoft.VSTS.Common.AcceptanceCriteria", out var ac))
            fields.AcceptanceCriteria = ac.GetString();
        if (root.TryGetProperty("Microsoft.VSTS.Common.StackRank", out var stackRank) && stackRank.ValueKind == JsonValueKind.Number)
            fields.StackRank = stackRank.GetDouble();

        return fields;
    }

    public override void Write(Utf8JsonWriter writer, WorkItemFields value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}
