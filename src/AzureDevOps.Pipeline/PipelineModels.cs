namespace AzureDevOps.Pipeline;

public class ConnectionInfo
{
    public string Organization { get; set; }

    public string Project { get; set; }

    public string PersonalAccessToken { get; set; }

    public string ApiAccessToken
    {
        get
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", "", PersonalAccessToken)));
        }
    }

    public string BaseUrl => $"https://dev.azure.com/{Organization}/{Project}/_apis/pipelines";
}

public class PipelinesResponse
{
    public int Count { get; set; }

    public List<Pipeline> Value { get; set; }
}

public class Pipeline
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Folder { get; set; }

    public int Revision { get; set; }

    public PipelineLinks Links { get; set; }
}

public class PipelineLinks
{
    public LinkHref Self { get; set; }

    public LinkHref Web { get; set; }
}

public class LinkHref
{
    public string Href { get; set; }
}

public class RunPipelineRequest
{
    public RunResources? Resources { get; set; }

    public Dictionary<string, string>? Variables { get; set; }

    public Dictionary<string, string>? TemplateParameters { get; set; }

    public string? StagesToSkip { get; set; }
}

public class RunResources
{
    public RunRepositoryResources? Repositories { get; set; }
}

public class RunRepositoryResources
{
    public RunRepositoryResource? Self { get; set; }
}

public class RunRepositoryResource
{
    public string? RefName { get; set; }
}

public class RunsResponse
{
    public int Count { get; set; }

    public List<PipelineRun> Value { get; set; }
}

public class PipelineRun
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string State { get; set; }

    public string? Result { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? FinishedDate { get; set; }

    public PipelineRef Pipeline { get; set; }

    public PipelineLinks Links { get; set; }
}

public class PipelineRef
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Folder { get; set; }
}
