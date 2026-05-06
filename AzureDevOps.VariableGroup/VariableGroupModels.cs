namespace AzureDevOps.VariableGroup;

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

    public string BaseUrl => $"https://dev.azure.com/{Organization}/{Project}/_apis/distributedtask/variablegroups";

    public string DeleteBaseUrl => $"https://dev.azure.com/{Organization}/_apis/distributedtask/variablegroups";
}

public class VariableGroupsResponse
{
    public int Count { get; set; }

    public List<VariableGroup> Value { get; set; }
}

public class VariableGroup
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Type { get; set; }

    public Dictionary<string, VariableValue> Variables { get; set; }

    public List<VariableGroupProjectReference> VariableGroupProjectReferences { get; set; }
}

public class VariableValue
{
    public string Value { get; set; }

    public bool IsSecret { get; set; }
}

public class VariableGroupProjectReference
{
    public string Name { get; set; }

    public string Description { get; set; }

    public ProjectReference ProjectReference { get; set; }
}

public class ProjectReference
{
    public string Id { get; set; }

    public string Name { get; set; }
}

public class CreateVariableGroupRequest
{
    public string Name { get; set; }

    public string Description { get; set; }

    public string Type { get; set; } = "Vsts";

    public Dictionary<string, VariableValue> Variables { get; set; } = new();

    public List<VariableGroupProjectReference> VariableGroupProjectReferences { get; set; } = new();
}

public class UpdateVariableGroupRequest
{
    public string Name { get; set; }

    public string Description { get; set; }

    public string Type { get; set; } = "Vsts";

    public Dictionary<string, VariableValue> Variables { get; set; } = new();

    public List<VariableGroupProjectReference> VariableGroupProjectReferences { get; set; } = new();
}
