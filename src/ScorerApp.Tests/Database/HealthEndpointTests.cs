using System.Net;

namespace ScorerApp.Tests.Database;

/// <summary>
/// /health kontroluje spojení s databází (SharedServices.AddSharedHealthChecks). Deploy skript na něm
/// pozná, že se kontejner rozběhl, takže musí být „Healthy“ právě tehdy, když je DB dostupná.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class HealthEndpointTests(DatabaseTestFactory factory)
{
    [Fact]
    public async Task Health_IsHealthy_WhenDatabaseIsReachable()
    {
        using var client = factory.ClientAs(null);
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
