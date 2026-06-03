using Microsoft.AspNetCore.Identity;

namespace ScorerApp.Data;

public class AppUser : IdentityUser
{
    public bool IsAdmin { get; set; }

    /// <summary>For whitelist-mode apps: user needs explicit access grant. Unused in Public mode.</summary>
    public bool IsWhitelisted { get; set; }
}
