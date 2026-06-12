using Alba;
using BookingPlatform.Server.Modules.Businesses;
using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace BookingPlatform.Tests.Integration.Modules.Businesses.Features.ExpireInvitation;

public class ExpireInvitationTests : IAsyncLifetime
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

    private async Task<(Guid BusinessId, Guid InvitationId)> CreateBusinessWithPastExpiry()
    {
        Assert.NotNull(_host);

        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var pastExpiry = DateTimeOffset.UtcNow.AddHours(-1);

        var events = new object[]
        {
            new BusinessCreated(businessId, "Acme Salon"),
            new BusinessManagerInvited(invitationId, "manager@acme.com", pastExpiry),
            new BusinessBookabilityChanged("Unbookable", new[] { "ManagerNotAccepted", "OnboardingIncomplete" })
        };

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        session.Events.StartStream<Business>(businessId, events);
        await session.SaveChangesAsync();

        return (businessId, invitationId);
    }

    private async Task<(Guid BusinessId, Guid InvitationId)> CreateBusinessWithFutureExpiry()
    {
        Assert.NotNull(_host);

        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var futureExpiry = DateTimeOffset.UtcNow.AddDays(7);

        var events = new object[]
        {
            new BusinessCreated(businessId, "Acme Salon"),
            new BusinessManagerInvited(invitationId, "manager@acme.com", futureExpiry),
            new BusinessBookabilityChanged("Unbookable", new[] { "ManagerNotAccepted", "OnboardingIncomplete" })
        };

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        session.Events.StartStream<Business>(businessId, events);
        await session.SaveChangesAsync();

        return (businessId, invitationId);
    }

    [Fact]
    public async Task Should_append_expired_event_for_pending_due_invitation()
    {
        Assert.NotNull(_host);

        var (businessId, invitationId) = await CreateBusinessWithPastExpiry();

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();

        var message = new ExpireInvitationMessage(businessId, invitationId);
        await ExpireInvitationHandler.Handle(message, session);
        await session.SaveChangesAsync();

        await using var verifySession = store.LightweightSession();
        var stream = await verifySession.Events.FetchStreamAsync(businessId);
        Assert.Equal(4, stream.Count);

        var expired = Assert.IsType<BusinessManagerInvitationExpired>(stream[3].Data);
        Assert.Equal(invitationId, expired.InvitationId);
    }

    [Fact]
    public async Task Should_be_noop_when_invitation_already_expired()
    {
        Assert.NotNull(_host);

        var (businessId, invitationId) = await CreateBusinessWithPastExpiry();

        var store = _host.Services.GetRequiredService<IDocumentStore>();

        await using var session1 = store.LightweightSession();
        var message = new ExpireInvitationMessage(businessId, invitationId);
        await ExpireInvitationHandler.Handle(message, session1);
        await session1.SaveChangesAsync();

        await using var session2 = store.LightweightSession();
        await ExpireInvitationHandler.Handle(message, session2);
        await session2.SaveChangesAsync();

        await using var verifySession = store.LightweightSession();
        var finalStream = await verifySession.Events.FetchStreamAsync(businessId);
        Assert.Equal(4, finalStream.Count);
    }

    [Fact]
    public async Task Should_be_noop_when_invitation_already_accepted()
    {
        Assert.NotNull(_host);

        var (businessId, invitationId) = await CreateBusinessWithFutureExpiry();

        var store = _host.Services.GetRequiredService<IDocumentStore>();

        await using var acceptSession = store.LightweightSession();
        var stream = await acceptSession.Events.FetchStreamAsync(businessId);
        var events = stream.Select(e => e.Data).ToArray();
        var business = Business.Rehydrate(events);

        var acceptResult = business.AcceptBusinessManagerInvitation(invitationId, "manager@acme.com");
        Assert.True(acceptResult.IsSuccess);
        acceptSession.Events.Append(businessId, acceptResult.Events);
        await acceptSession.SaveChangesAsync();

        await using var expireSession = store.LightweightSession();
        var message = new ExpireInvitationMessage(businessId, invitationId);
        await ExpireInvitationHandler.Handle(message, expireSession);
        await expireSession.SaveChangesAsync();

        await using var verifySession = store.LightweightSession();
        var finalStream = await verifySession.Events.FetchStreamAsync(businessId);
        Assert.Equal(5, finalStream.Count);

        var third = Assert.IsType<BusinessManagerInvitationAccepted>(finalStream[3].Data);
        Assert.Equal(invitationId, third.InvitationId);

        var fourth = Assert.IsType<BusinessBookabilityChanged>(finalStream[4].Data);
        Assert.DoesNotContain("ManagerNotAccepted", fourth.Reasons);
    }

    [Fact]
    public async Task Should_be_noop_when_not_due_yet()
    {
        Assert.NotNull(_host);

        var (businessId, invitationId) = await CreateBusinessWithFutureExpiry();

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();

        var message = new ExpireInvitationMessage(businessId, invitationId);
        await ExpireInvitationHandler.Handle(message, session);
        await session.SaveChangesAsync();

        await using var verifySession = store.LightweightSession();
        var stream = await verifySession.Events.FetchStreamAsync(businessId);
        Assert.Equal(3, stream.Count);
    }

    [Fact]
    public async Task Should_be_noop_when_business_not_found()
    {
        Assert.NotNull(_host);

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();

        var message = new ExpireInvitationMessage(Guid.NewGuid(), Guid.NewGuid());
        await ExpireInvitationHandler.Handle(message, session);
        await session.SaveChangesAsync();

        var allEvents = await session.Events.QueryAllRawEvents().ToListAsync();
        Assert.Empty(allEvents);
    }
}
