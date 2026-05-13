using System.Text.RegularExpressions;

namespace AzureDevOps.WorkItem;

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

    public string BaseUrl => $"https://dev.azure.com/{Organization}/{Project}/_apis/wit";
}

public class WiqlRequest
{
    public string Query { get; set; }
}

public class WiqlResponse
{
    public List<WorkItemReference> WorkItems { get; set; } = new();
}

public class WorkItemReference
{
    public int Id { get; set; }

    public string Url { get; set; }
}

public class WorkItemsResponse
{
    public int Count { get; set; }

    public List<WorkItem> Value { get; set; } = new();
}

public class WorkItem
{
    public int Id { get; set; }

    public string Url { get; set; }

    public WorkItemFields Fields { get; set; }

    public List<WorkItemRelation> Relations { get; set; } = new();

    public List<string> ImageLinks
    {
        get
        {
            var links = new List<string>();
            var htmlFields = new[] { Fields?.SystemDescription, Fields?.AcceptanceCriteria };
            foreach (var html in htmlFields)
            {
                if (string.IsNullOrEmpty(html))
                    continue;
                foreach (Match match in Regex.Matches(html, @"<img[^>]+src\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase))
                    links.Add(match.Groups[1].Value);
            }
            return links;
        }
    }
}

public class WorkItemFields
{
    public string SystemTitle { get; set; }

    public string SystemState { get; set; }

    public string SystemWorkItemType { get; set; }

    public string SystemAssignedTo { get; set; }

    public string SystemAreaPath { get; set; }

    public string SystemIterationPath { get; set; }

    public string SystemDescription { get; set; }

    public int? SystemParent { get; set; }

    public string AcceptanceCriteria { get; set; }

    public double? StackRank { get; set; }
}

public class WorkItemPatchOperation
{
    public string Op { get; set; } = "add";

    public string Path { get; set; }

    public string Value { get; set; }
}

public class CloneResult
{
    public WorkItem Feature { get; set; }

    public List<WorkItem> UserStories { get; set; } = new();
}

public class WorkItemRelation
{
    public string Rel { get; set; }
    public string Url { get; set; }
    public WorkItemRelationAttributes Attributes { get; set; }
}

public class WorkItemRelationAttributes
{
    public string Name { get; set; }
    public string Comment { get; set; }
}

public class Attachment
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Comment { get; set; }
}
