using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Sport : BaseGuid
{
    public string Name { get; set; } = "";
    public SportType Type { get; set; }
    public MatchType MatchType { get; set; }
    public ParticipantKind ParticipantKind { get; set; }
    public string Icon { get; set; } = "bi-trophy";
    public string ScoringRulesJson { get; set; } = """{"win":3,"draw":1,"loss":0,"winOT":null,"lossOT":null,"hasDraw":true}""";

    public List<League> Leagues { get; set; } = new();
}
