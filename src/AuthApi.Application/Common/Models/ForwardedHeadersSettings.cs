namespace AuthApi.Application.Common.Models;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; set; } = true;
    public int? ForwardLimit { get; set; } = 1;
    public List<string> KnownProxies { get; set; } = ["127.0.0.1", "::1"];
    public List<string> KnownNetworks { get; set; } = [];
}

