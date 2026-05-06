using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureDevOps.Pipeline;

public class PipelineClient
{
    private readonly HttpClient _httpClient;
    private readonly ConnectionInfo _connectionInfo;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ApiVersion = "api-version=7.0";

    public PipelineClient(ConnectionInfo connectionInfo)
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

    public async Task<List<Pipeline>> ListPipelinesAsync()
    {
        var response = await _httpClient.GetAsync($"{_connectionInfo.BaseUrl}?{ApiVersion}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PipelinesResponse>(body, _jsonOptions)!.Value;
    }

    public async Task<PipelineRun> RunPipelineAsync(int pipelineId, RunPipelineRequest? request = null)
    {
        request ??= new RunPipelineRequest();
        var response = await _httpClient.PostAsJsonAsync($"{_connectionInfo.BaseUrl}/{pipelineId}/runs?{ApiVersion}", request, _jsonOptions);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PipelineRun>(body, _jsonOptions)!;
    }

    public async Task<List<PipelineRun>> ListRunsAsync(int pipelineId)
    {
        var response = await _httpClient.GetAsync($"{_connectionInfo.BaseUrl}/{pipelineId}/runs?{ApiVersion}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RunsResponse>(body, _jsonOptions)!.Value;
    }

    public async Task<PipelineRun> GetRunAsync(int pipelineId, int runId)
    {
        var response = await _httpClient.GetAsync($"{_connectionInfo.BaseUrl}/{pipelineId}/runs/{runId}?{ApiVersion}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PipelineRun>(body, _jsonOptions)!;
    }
}
