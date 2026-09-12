using System.Text.Json;
using System.Text.Json.Serialization;
using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

/// <summary>
/// Čte a zapisuje modulární formát sezóny (Season.FormatJson). Sezóny založené před zavedením
/// modulů mají FormatJson prázdný — pro ně se formát odvodí ze starého enumu, aby fungovaly dál
/// bez datové migrace.
/// </summary>
public class SeasonFormatService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        Converters                  = { new JsonStringEnumConverter() }
    };

    public SeasonFormatDefinition Resolve(Season season)
    {
        if (!string.IsNullOrWhiteSpace(season.FormatJson))
        {
            try
            {
                var def = JsonSerializer.Deserialize<SeasonFormatDefinition>(season.FormatJson, JsonOpts);
                if (def is not null && def.Modules.Count > 0) return def;
            }
            catch (JsonException)
            {
                // Poškozený JSON nesmí shodit stránku sezóny — spadneme zpět na starý enum.
            }
        }
        return FromLegacy(season.Format);
    }

    public string Serialize(SeasonFormatDefinition definition) =>
        JsonSerializer.Serialize(definition, JsonOpts);

    public static SeasonFormatDefinition FromLegacy(SeasonFormat format) => format switch
    {
        SeasonFormat.DoubleRoundRobin => Single(new SeasonFormatModule
            { Type = SeasonModuleType.RoundRobin, MatchesPerPair = 2 }),
        SeasonFormat.Playoff => Single(new SeasonFormatModule
            { Type = SeasonModuleType.Playoff }),
        SeasonFormat.GroupsAndKnockout => new SeasonFormatDefinition
        {
            Modules =
            [
                new SeasonFormatModule { Type = SeasonModuleType.GroupStage, GroupCount = 2, AdvanceCount = 2 },
                new SeasonFormatModule { Type = SeasonModuleType.Playoff }
            ]
        },
        _ => Single(new SeasonFormatModule { Type = SeasonModuleType.RoundRobin, MatchesPerPair = 1 })
    };

    private static SeasonFormatDefinition Single(SeasonFormatModule module) =>
        new() { Modules = [module] };

    /// <summary>Předdefinované šablony pro rychlý výběr při zakládání sezóny.</summary>
    public static IReadOnlyList<SeasonFormatTemplate> Templates =>
    [
        new("Jen tabulka", "bi-table", Single(new SeasonFormatModule
            { Type = SeasonModuleType.RoundRobin, MatchesPerPair = 1 })),
        new("Tabulka doma i venku", "bi-arrow-left-right", Single(new SeasonFormatModule
            { Type = SeasonModuleType.RoundRobin, MatchesPerPair = 2 })),
        new("Jen pavouk", "bi-diagram-3", Single(new SeasonFormatModule
            { Type = SeasonModuleType.Playoff })),
        new("Tabulka + playoff", "bi-trophy", new SeasonFormatDefinition
        {
            Modules =
            [
                new SeasonFormatModule { Type = SeasonModuleType.RoundRobin, MatchesPerPair = 1 },
                new SeasonFormatModule { Type = SeasonModuleType.Playoff, BracketSize = 4 }
            ]
        }),
        new("Skupiny + playoff", "bi-grid-3x3", new SeasonFormatDefinition
        {
            Modules =
            [
                new SeasonFormatModule { Type = SeasonModuleType.GroupStage, GroupCount = 2, AdvanceCount = 2 },
                new SeasonFormatModule { Type = SeasonModuleType.Playoff, BracketSize = 4 }
            ]
        }),
        new("Swiss + playoff", "bi-shuffle", new SeasonFormatDefinition
        {
            Modules =
            [
                new SeasonFormatModule { Type = SeasonModuleType.Swiss, Rounds = 5 },
                new SeasonFormatModule { Type = SeasonModuleType.Playoff, BracketSize = 4 }
            ]
        })
    ];

    /// <summary>Popis formátu pro náhled („round-robin → playoff top 8“).</summary>
    public string Describe(SeasonFormatDefinition definition) =>
        string.Join(" → ", definition.Modules.Select(Describe));

    public static string Describe(SeasonFormatModule m) => m.Type switch
    {
        SeasonModuleType.RoundRobin => (m.MatchesPerPair ?? 1) >= 2
            ? "tabulka (každý s každým doma i venku)"
            : "tabulka (každý s každým)",
        SeasonModuleType.GroupStage =>
            $"{m.GroupCount ?? 2} skupiny, postupuje {m.AdvanceCount ?? 2} z každé",
        SeasonModuleType.Swiss     => $"Swiss na {m.Rounds ?? 5} kol",
        SeasonModuleType.Playoff   => m.BracketSize is int size
            ? $"playoff top {size}"
            : "playoff (všichni účastníci)",
        _ => m.Type.ToString()
    };
}

public record SeasonFormatTemplate(string Name, string Icon, SeasonFormatDefinition Definition);
