using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

public class MatchGeneratorService
{
    /// <summary>Generates Round Robin or Double Round Robin matches for a season.</summary>
    // AUDIT:FIXED|byl: crash při participants.Count < 2; nyní guard
    // AUDIT:FIXED|byl: každá dvojice dostala vlastní číslo kola (15 "kol" po 1 zápase pro
    //                  6 hráčů); nyní okružní (Berger) rozpis — n-1 kol po n/2 zápasech
    public List<Match> Generate(Season season, List<SeasonParticipant> participants)
    {
        int matchesPerPair = season.Format == SeasonFormat.DoubleRoundRobin ? 2 : 1;
        return GenerateRoundRobin(season, participants, matchesPerPair);
    }

    /// <summary>
    /// Okružní rozpis pro jednu skupinu účastníků. <paramref name="startRound"/> umožňuje navázat
    /// na už existující kola (další fáze soutěže), <paramref name="groupIndex"/> označí skupinu.
    /// </summary>
    public List<Match> GenerateRoundRobin(
        Season season,
        List<SeasonParticipant> participants,
        int matchesPerPair = 1,
        int moduleIndex = 0,
        int startRound = 1,
        MatchStage stage = MatchStage.League,
        int? groupIndex = null)
    {
        if (participants.Count < 2) return [];

        var matches = new List<Match>();
        var rounds  = BuildRounds(participants);
        int round   = startRound;

        // Druhé (a další) kolo se hraje se stejným rozpisem, jen s prohozeným domácím prostředím.
        for (int leg = 0; leg < Math.Max(1, matchesPerPair); leg++)
        {
            bool swap = leg % 2 == 1;
            foreach (var pairs in rounds)
            {
                foreach (var (home, away) in pairs)
                    matches.Add(NewMatch(season, round,
                        swap ? away : home,
                        swap ? home : away,
                        moduleIndex, stage, groupIndex));
                round++;
            }
        }

        return matches;
    }

    /// <summary>
    /// Rozdělí účastníky do skupin „hadem“ (1. do A, 2. do B, 3. do B, 4. do A…) podle pořadí
    /// nasazení a uvnitř každé skupiny vygeneruje okružní rozpis. Had zajistí rovnoměrnou sílu skupin.
    /// </summary>
    public List<Match> GenerateGroupStage(
        Season season,
        List<SeasonParticipant> seededParticipants,
        int groupCount,
        int matchesPerPair = 1,
        int moduleIndex = 0,
        int startRound = 1)
    {
        if (groupCount < 2 || seededParticipants.Count < 2) return [];

        var groups = new List<List<SeasonParticipant>>();
        for (int i = 0; i < groupCount; i++) groups.Add([]);

        for (int i = 0; i < seededParticipants.Count; i++)
        {
            int row = i / groupCount;
            int pos = i % groupCount;
            int target = row % 2 == 0 ? pos : groupCount - 1 - pos;   // had
            groups[target].Add(seededParticipants[i]);
        }

        var matches = new List<Match>();
        for (int g = 0; g < groups.Count; g++)
        {
            // Všechny skupiny hrají svá kola paralelně, proto startRound stejný pro každou.
            matches.AddRange(GenerateRoundRobin(
                season, groups[g], matchesPerPair, moduleIndex, startRound,
                MatchStage.Group, groupIndex: g + 1));
        }
        return matches;
    }

    /// <summary>
    /// Spáruje jedno kolo Swiss systému: účastníci seřazení podle průběžných bodů se párují
    /// odshora dolů a dvojice, které už spolu hrály, se přeskakují (proto hledání dalšího volného).
    /// </summary>
    public List<Match> GenerateSwissRound(
        Season season,
        List<SeasonParticipant> participantsByStanding,
        ISet<(Guid, Guid)> alreadyPlayed,
        int round,
        int moduleIndex = 0)
    {
        var pool    = new List<SeasonParticipant>(participantsByStanding);
        var matches = new List<Match>();

        while (pool.Count >= 2)
        {
            var home = pool[0];
            pool.RemoveAt(0);

            // Nejbližší soupeř, se kterým se ještě nepotkal; když takový není, bereme nejbližšího.
            int idx = pool.FindIndex(p => !alreadyPlayed.Contains(PairKey(home.Guid, p.Guid)));
            if (idx < 0) idx = 0;

            var away = pool[idx];
            pool.RemoveAt(idx);

            matches.Add(NewMatch(season, round, home, away, moduleIndex, MatchStage.Swiss));
            alreadyPlayed.Add(PairKey(home.Guid, away.Guid));
        }

        return matches;   // lichý účastník má v kole volno
    }

    /// <summary>Dvojice nezávislá na pořadí — pro kontrolu „už spolu hráli“.</summary>
    public static (Guid, Guid) PairKey(Guid a, Guid b) => a.CompareTo(b) <= 0 ? (a, b) : (b, a);

    public static Match NewMatch(
        Season season, int round, SeasonParticipant home, SeasonParticipant away,
        int moduleIndex = 0, MatchStage stage = MatchStage.League, int? groupIndex = null) => new()
    {
        SeasonId          = season.Guid,
        Round             = round,
        HomeParticipantId = home.Guid,
        AwayParticipantId = away.Guid,
        Status            = MatchStatus.Scheduled,
        Stage             = stage,
        ModuleIndex       = moduleIndex,
        GroupIndex        = groupIndex
    };

    /// <summary>
    /// Okružní metoda: první účastník stojí, ostatní rotují. Pro lichý počet se přidá
    /// "bye" (null) — účastník, který se s ním potká, má v daném kole volno.
    /// Výsledek: n-1 kol (resp. n při lichém počtu), každá dvojice právě jednou.
    /// </summary>
    private static List<List<(SeasonParticipant Home, SeasonParticipant Away)>> BuildRounds(
        List<SeasonParticipant> participants)
    {
        var slots = new List<SeasonParticipant?>(participants);
        if (slots.Count % 2 != 0) slots.Add(null);   // bye

        int n     = slots.Count;
        int half  = n / 2;
        var order = Enumerable.Range(0, n).ToList();
        var rounds = new List<List<(SeasonParticipant, SeasonParticipant)>>();

        for (int r = 0; r < n - 1; r++)
        {
            var pairs = new List<(SeasonParticipant, SeasonParticipant)>();

            for (int i = 0; i < half; i++)
            {
                var a = slots[order[i]];
                var b = slots[order[n - 1 - i]];
                if (a is null || b is null) continue;   // volno

                // Střídání domácí/hosté, aby nikdo nehrál všechna kola na jedné straně.
                if ((r + i) % 2 == 0) pairs.Add((a, b));
                else                  pairs.Add((b, a));
            }

            rounds.Add(pairs);

            // Rotace: index na pozici 0 zůstává, poslední se přesune na pozici 1.
            var last = order[n - 1];
            order.RemoveAt(n - 1);
            order.Insert(1, last);
        }

        return rounds;
    }
}
