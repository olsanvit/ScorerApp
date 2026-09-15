using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace ScorerApp.Components.Shared;

/// <summary>
/// Základ klubových stránek. Služby klubového modulu ověřují oprávnění samy a potřebují Id účtu
/// a příznak admina — načte se jednou tady místo opakování ve všech stránkách.
/// Potomek, který přepisuje OnInitializedAsync, musí zavolat base.OnInitializedAsync().
/// </summary>
public abstract class ClubPageBase : ComponentBase
{
    [Inject] protected AuthenticationStateProvider Auth { get; set; } = null!;
    [Inject] protected IStringLocalizer<SharedResource> S { get; set; } = null!;
    [Inject] protected SharedServices.ToastService Toast { get; set; } = null!;

    protected string UserId { get; private set; } = "";
    protected bool IsSiteAdmin { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        var user = (await Auth.GetAuthenticationStateAsync()).User;
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        IsSiteAdmin = user.IsInRole("Admin");
    }

    /// <summary>Popisek hodnoty enumu z resx pod klíčem „NázevEnumu_Hodnota“.</summary>
    protected string L(Enum value) => S[$"{value.GetType().Name}_{value}"];

    /// <summary>
    /// Spustí akci a chybu ze služby ukáže jako toast. Služby hlásí oprávnění a validaci výjimkami
    /// se srozumitelnou zprávou; ostatní výjimky se nechytají, aby chyba neskončila potichu.
    /// </summary>
    protected async Task<bool> RunAsync(Func<Task> action, string? successTitle = null, string? successMessage = null)
    {
        try
        {
            await action();
            if (successTitle is not null) Toast.ShowSuccess(successTitle, successMessage ?? "");
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            Toast.ShowError(S["Clubs_Error"], ex.Message);
            return false;
        }
    }
}
