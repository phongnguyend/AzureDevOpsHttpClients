using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureDevOps.VariableGroup;

public class VariableGroupClient
{
    private readonly HttpClient _httpClient;
    private readonly ConnectionInfo _connectionInfo;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ApiVersion = "api-version=7.0";

    public VariableGroupClient(ConnectionInfo connectionInfo)
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

    // Variable Group CRUD

    public async Task<List<VariableGroup>> ListVariableGroupsAsync()
    {
        var response = await _httpClient.GetAsync($"{_connectionInfo.BaseUrl}?{ApiVersion}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<VariableGroupsResponse>(body, _jsonOptions).Value;
    }

    public async Task<VariableGroup> GetVariableGroupAsync(int groupId)
    {
        var response = await _httpClient.GetAsync($"{_connectionInfo.BaseUrl}/{groupId}?{ApiVersion}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<VariableGroup>(body, _jsonOptions);
    }

    public async Task<VariableGroup> CreateVariableGroupAsync(CreateVariableGroupRequest request)
    {
        if (request.VariableGroupProjectReferences.Count == 0)
        {
            request.VariableGroupProjectReferences.Add(new VariableGroupProjectReference
            {
                Name = request.Name,
                Description = request.Description,
                ProjectReference = new ProjectReference
                {
                    Name = _connectionInfo.Project
                }
            });
        }

        var response = await _httpClient.PostAsJsonAsync($"{_connectionInfo.BaseUrl}?{ApiVersion}", request, _jsonOptions);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<VariableGroup>(body, _jsonOptions);
    }

    public async Task<VariableGroup> UpdateVariableGroupAsync(int groupId, UpdateVariableGroupRequest request)
    {
        if (request.VariableGroupProjectReferences.Count == 0)
        {
            request.VariableGroupProjectReferences.Add(new VariableGroupProjectReference
            {
                Name = request.Name,
                Description = request.Description,
                ProjectReference = new ProjectReference
                {
                    Name = _connectionInfo.Project
                }
            });
        }

        var response = await _httpClient.PutAsJsonAsync($"{_connectionInfo.BaseUrl}/{groupId}?{ApiVersion}", request, _jsonOptions);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<VariableGroup>(body, _jsonOptions);
    }

    public async Task DeleteVariableGroupAsync(int groupId, string projectId)
    {
        var response = await _httpClient.DeleteAsync($"{_connectionInfo.DeleteBaseUrl}/{groupId}?projectIds={projectId}&{ApiVersion}");
        response.EnsureSuccessStatusCode();
    }

    // Variable CRUD within a specific group

    public async Task<Dictionary<string, VariableValue>> ListVariablesAsync(int groupId)
    {
        var group = await GetVariableGroupAsync(groupId);
        return group.Variables ?? new Dictionary<string, VariableValue>();
    }

    public async Task<VariableGroup> CreateVariableAsync(int groupId, string variableName, VariableValue variableValue)
    {
        var group = await GetVariableGroupAsync(groupId);
        group.Variables ??= new Dictionary<string, VariableValue>();

        if (group.Variables.ContainsKey(variableName))
            throw new InvalidOperationException($"Variable '{variableName}' already exists in group '{group.Name}'.");

        group.Variables[variableName] = variableValue;

        var updateRequest = new UpdateVariableGroupRequest
        {
            Name = group.Name,
            Description = group.Description,
            Type = group.Type,
            Variables = group.Variables
        };

        return await UpdateVariableGroupAsync(groupId, updateRequest);
    }

    public async Task<VariableGroup> UpdateVariableAsync(int groupId, string variableName, VariableValue variableValue)
    {
        var group = await GetVariableGroupAsync(groupId);
        group.Variables ??= new Dictionary<string, VariableValue>();

        if (!group.Variables.ContainsKey(variableName))
            throw new KeyNotFoundException($"Variable '{variableName}' not found in group '{group.Name}'.");

        group.Variables[variableName] = variableValue;

        var updateRequest = new UpdateVariableGroupRequest
        {
            Name = group.Name,
            Description = group.Description,
            Type = group.Type,
            Variables = group.Variables
        };

        return await UpdateVariableGroupAsync(groupId, updateRequest);
    }

    public async Task<VariableGroup> DeleteVariableAsync(int groupId, string variableName)
    {
        var group = await GetVariableGroupAsync(groupId);
        group.Variables ??= new Dictionary<string, VariableValue>();

        if (!group.Variables.Remove(variableName))
            throw new KeyNotFoundException($"Variable '{variableName}' not found in group '{group.Name}'.");

        var updateRequest = new UpdateVariableGroupRequest
        {
            Name = group.Name,
            Description = group.Description,
            Type = group.Type,
            Variables = group.Variables
        };

        return await UpdateVariableGroupAsync(groupId, updateRequest);
    }
}
