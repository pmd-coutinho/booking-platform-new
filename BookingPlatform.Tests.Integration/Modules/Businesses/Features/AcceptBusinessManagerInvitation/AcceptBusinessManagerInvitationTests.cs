using Alba;
using BookingPlatform.Server.Modules.Businesses.Domain;
using BookingPlatform.Server.Modules.Businesses.Features.AcceptBusinessManagerInvitation;
using BookingPlatform.Server.Modules.Businesses.Features.CreateRegisteredBusiness;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace BookingPlatform.Tests.Integration.Modules.Businesses.Features.AcceptBusinessManagerInvitation;

public class AcceptBusinessManagerInvitationTests : IAsyncLifetime
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

    private async Task<CreateRegisteredBusinessResponse> CreateBusiness(string businessName, string managerEmail, DateTimeOffset expiresAt)
    {
        Assert.NotNull(_host);

        var request = new
        {
            BusinessName = businessName,
            ManagerEmail = managerEmail,
            InvitationExpiresAt = expiresAt
        };

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl("/api/businesses");
            _.StatusCodeShouldBe(201);
        });

        var result = await response.ReadAsJsonAsync<CreateRegisteredBusinessResponse>();
        Assert.NotNull(result);
        return result;
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

    [Fact]
    public async Task Should_accept_invitation_and_return_updated_bookability()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "manager@acme.com" };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<AcceptBusinessManagerInvitationResponse>();
        Assert.NotNull(result);
        Assert.Equal(create.BusinessId, result.BusinessId);
        Assert.Equal(create.InvitationId, result.InvitationId);
        Assert.Equal("manager@acme.com", result.ManagerEmail);
        Assert.Equal("Unbookable", result.BookabilityStatus);
        Assert.DoesNotContain("ManagerNotAccepted", result.BookabilityReasons);
        Assert.Contains("OnboardingIncomplete", result.BookabilityReasons);
    }

    [Fact]
    public async Task Should_persist_accepted_and_bookability_changed_events_in_stream()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "manager@acme.com" };
        await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(5, stream.Count);

        var created = Assert.IsType<BusinessCreated>(stream[0].Data);
        Assert.Equal(create.BusinessId, created.BusinessId);

        var invited = Assert.IsType<BusinessManagerInvited>(stream[1].Data);
        Assert.Equal(create.InvitationId, invited.InvitationId);

        var initialBookability = Assert.IsType<BusinessBookabilityChanged>(stream[2].Data);
        Assert.Contains("ManagerNotAccepted", initialBookability.Reasons);

        var accepted = Assert.IsType<BusinessManagerInvitationAccepted>(stream[3].Data);
        Assert.Equal(create.InvitationId, accepted.InvitationId);
        Assert.Equal("manager@acme.com", accepted.ManagerEmail);

        var updatedBookability = Assert.IsType<BusinessBookabilityChanged>(stream[4].Data);
        Assert.Equal("Unbookable", updatedBookability.Status);
        Assert.DoesNotContain("ManagerNotAccepted", updatedBookability.Reasons);
        Assert.Contains("OnboardingIncomplete", updatedBookability.Reasons);
    }

    [Fact]
    public async Task Should_normalize_email_by_trimming_whitespace()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "  manager@acme.com  " };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<AcceptBusinessManagerInvitationResponse>();
        Assert.NotNull(result);
        Assert.Equal("manager@acme.com", result.ManagerEmail);
    }

    [Fact]
    public async Task Should_match_email_case_insensitively()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "MANAGER@ACME.COM" };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<AcceptBusinessManagerInvitationResponse>();
        Assert.NotNull(result);
        Assert.Equal("Unbookable", result.BookabilityStatus);
    }

    [Fact]
    public async Task Should_return_404_for_missing_business()
    {
        Assert.NotNull(_host);

        var nonExistentBusinessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var acceptRequest = new { ManagerEmail = "manager@acme.com" };

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{nonExistentBusinessId}/business-manager-invitations/{invitationId}/accept");
            _.StatusCodeShouldBe(404);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("Business manager invitation was not found", body);
    }

    [Fact]
    public async Task Should_return_400_for_invalid_manager_email()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        await _host!.Scenario(_ =>
        {
            _.Post.Json(new { ManagerEmail = "not-an-email" }).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(400);
            _.ContentTypeShouldBe("application/problem+json");
        });
    }

    [Fact]
    public async Task Should_return_409_for_expired_invitation_and_append_no_events()
    {
        Assert.NotNull(_host);

        var (businessId, invitationId) = await CreateBusinessWithPastExpiry();

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(new { ManagerEmail = "manager@acme.com" }).ToUrl($"/api/businesses/{businessId}/business-manager-invitations/{invitationId}/accept");
            _.StatusCodeShouldBe(409);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("Business manager invitation cannot be accepted", body);
        Assert.Contains("expired", body);

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(businessId);

        Assert.Equal(3, stream.Count);
    }

    [Fact]
    public async Task Should_return_404_for_missing_invitation_and_append_no_events()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "manager@acme.com" };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{Guid.NewGuid()}/accept");
            _.StatusCodeShouldBe(404);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("not found", body);

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(3, stream.Count);
    }

    [Fact]
    public async Task Should_return_404_for_wrong_email_and_append_no_events()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "other@acme.com" };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(404);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("Business manager invitation was not found", body);

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(3, stream.Count);
    }

    [Fact]
    public async Task Should_be_idempotent_on_same_email_retry_and_append_no_duplicate_events()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "manager@acme.com" };
        await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(204);
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(5, stream.Count);
    }

    [Fact]
    public async Task Should_reject_different_email_after_acceptance()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        await _host!.Scenario(_ =>
        {
            _.Post.Json(new { ManagerEmail = "manager@acme.com" }).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(new { ManagerEmail = "other@acme.com" }).ToUrl($"/api/businesses/{create.BusinessId}/business-manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(404);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("Business manager invitation was not found", body);

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(5, stream.Count);
    }
}
