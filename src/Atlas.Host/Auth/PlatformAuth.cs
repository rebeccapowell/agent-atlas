namespace Atlas.Host.Auth;

public static class PlatformAuth
{
    public static bool HasPermission(HttpContext ctx, string permission, string claimName)
    {
        var perms = GetPermissions(ctx, claimName);
        return perms.Contains(permission);
    }

    public static HashSet<string> GetPermissions(HttpContext ctx, string claimName)
    {
        var user = ctx.User;
        if (user.Identity?.IsAuthenticated != true)
            return [];

        var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Try space-delimited claim (scp)
        var scpClaim = user.FindFirst(claimName)?.Value;
        if (!string.IsNullOrWhiteSpace(scpClaim))
        {
            foreach (var p in scpClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                perms.Add(p);
        }

        // Also check array-style claims
        foreach (var claim in user.FindAll(claimName))
        {
            if (!string.IsNullOrWhiteSpace(claim.Value))
            {
                foreach (var p in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    perms.Add(p);
            }
        }

        return perms;
    }
}
