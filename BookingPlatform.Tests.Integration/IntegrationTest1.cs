using Alba;
using Testcontainers.PostgreSql;
using Xunit;

namespace BookingPlatform.Tests.Integration;

public class IntegrationTest1 : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private IAlbaHost? _host;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:bookingdb", _postgres.GetConnectionString());
        });
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Test1()
    {
        Assert.NotNull(_host);
    }
}
