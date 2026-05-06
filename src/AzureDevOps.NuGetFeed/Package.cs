namespace AzureDevOps.NuGetFeed;

public class FeedInfo
{
    public string Organization { get; set; }

    public string Project { get; set; }

    public string FeedId { get; set; }

    public string PersonalAccessToken { get; set; }

    public string ApiAccessToken
    {
        get
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", "", PersonalAccessToken)));
        }
    }

    public string FeedUrl
    {
        get
        {
            return $"https://feeds.dev.azure.com/{Organization}/{Project}/_apis/packaging/feeds/{FeedId}";
        }
    }

    public string NuGetUrl
    {
        get
        {
          return $"https://pkgs.dev.azure.com/{Organization}/{Project}/_apis/packaging/feeds/{FeedId}/nuget";
        }
    }
}

public class PackagesResponse
{
    public int Count { get; set; }

    public List<Package> Value { get; set; }
}

public class PackageVersionsResponse
{
    public int Count { get; set; }

    public List<PackageVersion> Value { get; set; }
}

public class PackageVersionMetricsResponse
{
    public int Count { get; set; }

    public List<PackageVersionMetric> Value { get; set; }
}

public class Package
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public List<PackageVersion> Versions { get; set; }

    public List<PackageVersionMetric> VersionMetrics { get; set; }
}

public class PackageVersion
{
    public string Id { get; set; }

    public bool IsLatest { get; set; }

    public bool IsListed { get; set; }

    public bool IsDeleted { get; set; }

    public string Version { get; set; }

    public DateTime PublishDate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string GetSprint()
    {
        return Version.Split('.')[0];
    }
}

public class PackageVersionMetric
{
    public string PackageId { get; set; }

    public string PackageVersionId { get; set; }

    public decimal DownloadCount { get; set; }

    public decimal DownloadUniqueUsers { get; set; }

    public DateTime LastDownloaded { get; set; }
}
