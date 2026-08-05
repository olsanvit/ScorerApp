using System.Text.Json;
using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

public class ScoringRules
{
    public int Win { get; set; } = 3;
    public int? Draw { get; set; } = 1;
    public int Loss { get; set; } = 0;
    public int? WinOT { get; set; }
    public int? LossOT { get; set; }
    public bool HasDraw { get; set; } = true;
}

public class ScoringRulesService
{
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    // AUDIT:FIXED|byl: bez try-catch + bez null-guard; poškozený JSON crashoval stránku
    public ScoringRules Resolve(Sport sport, League? league = null)
    {
        var json = !string.IsNullOrWhiteSpace(league?.ScoringRulesOverrideJson)
            ? league.ScoringRulesOverrideJson
            : sport.ScoringRulesJson;

        if (string.IsNullOrWhiteSpace(json))
            return new ScoringRules();

        try
        {
            return JsonSerializer.Deserialize<ScoringRules>(json, _opts) ?? new ScoringRules();
        }
        catch (JsonException)
        {
            return new ScoringRules();
        }
    }
}
