using Microsoft.EntityFrameworkCore;
using ScorerApp.Domain.Models;

namespace ScorerApp.Data;

public static class SeedData
{
    public static async Task SeedSportsAsync(AppDbContext db)
    {
        if (await db.Sports.AnyAsync()) return;

        db.Sports.AddRange(
            new Sport { Name = "Football",   Type = SportType.Football,   MatchType = SportMatchType.HeadToHead,        ParticipantKind = ParticipantKind.Team,       Icon = "bi-dribbble",     ScoringRulesJson = """{"win":3,"draw":1,"loss":0,"winOT":null,"lossOT":null,"hasDraw":true}""" },
            new Sport { Name = "Ice Hockey", Type = SportType.IceHockey,  MatchType = SportMatchType.HeadToHead,        ParticipantKind = ParticipantKind.Team,       Icon = "bi-snow",         ScoringRulesJson = """{"win":3,"draw":null,"loss":0,"winOT":2,"lossOT":1,"hasDraw":false}""" },
            new Sport { Name = "Basketball", Type = SportType.Basketball, MatchType = SportMatchType.HeadToHead,        ParticipantKind = ParticipantKind.Team,       Icon = "bi-circle",       ScoringRulesJson = """{"win":2,"draw":null,"loss":0,"winOT":null,"lossOT":null,"hasDraw":false}""" },
            new Sport { Name = "Tennis",     Type = SportType.Tennis,     MatchType = SportMatchType.HeadToHead,        ParticipantKind = ParticipantKind.Individual, Icon = "bi-award",        ScoringRulesJson = """{"win":2,"draw":null,"loss":0,"winOT":null,"lossOT":null,"hasDraw":false}""" },
            new Sport { Name = "Darts",      Type = SportType.Darts,      MatchType = SportMatchType.HeadToHead,        ParticipantKind = ParticipantKind.Individual, Icon = "bi-bullseye",     ScoringRulesJson = """{"win":2,"draw":null,"loss":0,"winOT":null,"lossOT":null,"hasDraw":false}""" },
            new Sport { Name = "Padel",      Type = SportType.Padel,      MatchType = SportMatchType.HeadToHead,        ParticipantKind = ParticipantKind.Team,       Icon = "bi-grid",         ScoringRulesJson = """{"win":2,"draw":null,"loss":0,"winOT":null,"lossOT":null,"hasDraw":false}""" },
            new Sport { Name = "Cards",      Type = SportType.Cards,      MatchType = SportMatchType.HeadToHead,        ParticipantKind = ParticipantKind.Individual, Icon = "bi-suit-spade",   ScoringRulesJson = """{"win":2,"draw":null,"loss":0,"winOT":null,"lossOT":null,"hasDraw":false}""" },
            new Sport { Name = "Running",    Type = SportType.Running,    MatchType = SportMatchType.MultiParticipant,  ParticipantKind = ParticipantKind.Individual, Icon = "bi-person-walking",ScoringRulesJson = """{"win":0,"draw":null,"loss":0,"winOT":null,"lossOT":null,"hasDraw":false}""" }
        );

        await db.SaveChangesAsync();
    }
}
