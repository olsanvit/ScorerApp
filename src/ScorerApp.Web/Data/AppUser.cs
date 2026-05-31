using Microsoft.AspNetCore.Identity;

namespace ScorerApp.Data;

public class AppUser : IdentityUser
{
    public bool IsAdmin { get; set; }
}
