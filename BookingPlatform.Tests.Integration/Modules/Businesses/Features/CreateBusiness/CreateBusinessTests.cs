using Alba;
using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace BookingPlatform.Tests.Integration.Modules.Businesses.Features.CreateBusiness;

public class CreateBusinessTests : IAsyncLifetime
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
    public async Task Should_create_business_with_generated_ids_and_unbookable_status()
    {
        Assert.NotNull(_host);

        var request = new
        {
            BusinessName = "Acme Salon",
            ManagerEmail = "manager@acme.com",
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl("/api/businesses");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<CreateBusinessResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.BusinessId);
        Assert.NotEqual(Guid.Empty, result.InvitationId);
        Assert.Equal("Unbookable", result.BookabilityStatus);
        Assert.Contains("ManagerNotAccepted", result.BookabilityReasons);
        Assert.Contains("OnboardingIncomplete", result.BookabilityReasons);
    }

    [Fact]
    public async Task Should_persist_expected_business_stream_events()
    {
        Assert.NotNull(_host);

        var request = new
        {
            BusinessName = "Acme Salon",
            ManagerEmail = "manager@acme.com",
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl("/api/businesses");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<CreateBusinessResponse>();
        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(result!.BusinessId);

        Assert.Equal(3, stream.Count);

        var created = Assert.IsType<BusinessCreated>(stream[0].Data);
        Assert.Equal(result.BusinessId, created.BusinessId);
        Assert.Equal("Acme Salon", created.BusinessName);

        var invited = Assert.IsType<BusinessManagerInvited>(stream[1].Data);
        Assert.Equal(result.InvitationId, invited.InvitationId);
        Assert.Equal("manager@acme.com", invited.ManagerEmail);

        var bookability = Assert.IsType<BusinessBookabilityChanged>(stream[2].Data);
        Assert.Equal("Unbookable", bookability.Status);
        Assert.Contains("ManagerNotAccepted", bookability.Reasons);
        Assert.Contains("OnboardingIncomplete", bookability.Reasons);
    }

    [Fact]
    public async Task Should_reject_blank_business_name_and_not_persist_events()
    {
        Assert.NotNull(_host);

        var request = new
        {
            BusinessName = "   ",
            ManagerEmail = "manager@acme.com",
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl("/api/businesses");
            _.StatusCodeShouldBe(400);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var allEvents = await session.Events.QueryAllRawEvents().ToListAsync();
        Assert.Empty(allEvents);
    }

    [Fact]
    public async Task Should_reject_invalid_email_and_not_persist_events()
    {
        Assert.NotNull(_host);

        var request = new
        {
            BusinessName = "Acme Salon",
            ManagerEmail = "not-an-email",
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl("/api/businesses");
            _.StatusCodeShouldBe(400);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var allEvents = await session.Events.QueryAllRawEvents().ToListAsync();
        Assert.Empty(allEvents);
    }

    [Fact]
    public async Task Should_reject_past_expiry_and_not_persist_events()
    {
        Assert.NotNull(_host);

        var request = new
        {
            BusinessName = "Acme Salon",
            ManagerEmail = "manager@acme.com",
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl("/api/businesses");
            _.StatusCodeShouldBe(400);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var allEvents = await session.Events.QueryAllRawEvents().ToListAsync();
        Assert.Empty(allEvents);
    }

    [Fact]
    public async Task Should_reject_expiry_beyond_maximum_and_not_persist_events()
    {
        Assert.NotNull(_host);

        var request = new
        {
            BusinessName = "Acme Salon",
            ManagerEmail = "manager@acme.com",
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(31)
        };

        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl("/api/businesses");
            _.StatusCodeShouldBe(400);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var allEvents = await session.Events.QueryAllRawEvents().ToListAsync();
        Assert.Empty(allEvents);
    }
}

public record CreateBusinessResponse(
    Guid BusinessId,
    Guid InvitationId,
    string BookabilityStatus,
    string[] BookabilityReasons);
