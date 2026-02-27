namespace Atlas.Host.Configuration;

public class AtlasOptions
{
    public string CatalogPath { get; set; } = "/catalog";
    public bool CatalogStrict { get; set; } = true;
    public OidcOptions Oidc { get; set; } = new();
    public PlatformPermissionsOptions PlatformPermissions { get; set; } = new();
    public ExecLimitsOptions ExecLimits { get; set; } = new();
}

public class OidcOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = "api://agent-atlas";
    public string[] AllowedIssuers { get; set; } = [];
}

public class PlatformPermissionsOptions
{
    public string Claim { get; set; } = "scp";
    public string Search { get; set; } = "platform-code-mode:search";
    public string Execute { get; set; } = "platform-code-mode:execute";
    public string ExecuteWrite { get; set; } = "platform-code-mode:execute:write";
}

public class ExecLimitsOptions
{
    public int MaxSteps { get; set; } = 50;
    public int MaxCalls { get; set; } = 50;
    public int MaxSeconds { get; set; } = 30;
    public long MaxBytes { get; set; } = 10_485_760;
    public long MaxResponseBytes { get; set; } = 1_048_576;
    public int MaxConcurrency { get; set; } = 1;
}
