using Alba;
using BookingPlatform.Server.Modules.Businesses;
using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace BookingPlatform.Tests.Integration.Modules.Businesses.Features.AcceptInvitation;

public class AcceptInvitationTests : IAsyncLifetime
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

    private async Task<CreateBusinessResponse> CreateBusiness(string businessName, string managerEmail, DateTimeOffset expiresAt)
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
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<CreateBusinessResponse>();
        Assert.NotNull(result);
        return result;
    }

    [Fact]
    public async Task Should_accept_invitation_and_return_updated_bookability()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "manager@acme.com" };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<AcceptInvitationResponse>();
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
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
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
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<AcceptInvitationResponse>();
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
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<AcceptInvitationResponse>();
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
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{nonExistentBusinessId}/manager-invitations/{invitationId}/accept");
            _.StatusCodeShouldBe(404);
        });

        var body = response.ReadAsText();
        Assert.Contains("Business not found", body);
    }

    [Fact]
    public async Task Should_return_400_for_missing_invitation_and_append_no_events()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "manager@acme.com" };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{Guid.NewGuid()}/accept");
            _.StatusCodeShouldBe(400);
        });

        var body = response.ReadAsText();
        Assert.Contains("not found", body);

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(3, stream.Count);
    }

    [Fact]
    public async Task Should_return_400_for_wrong_email_and_append_no_events()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var acceptRequest = new { ManagerEmail = "other@acme.com" };
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(400);
        });

        var body = response.ReadAsText();
        Assert.Contains("email", body);

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
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(acceptRequest).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<AcceptInvitationResponse>();
        Assert.NotNull(result);
        Assert.Equal("Unbookable", result.BookabilityStatus);
        Assert.DoesNotContain("ManagerNotAccepted", result.BookabilityReasons);

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
            _.Post.Json(new { ManagerEmail = "manager@acme.com" }).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(200);
        });

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(new { ManagerEmail = "other@acme.com" }).ToUrl($"/api/businesses/{create.BusinessId}/manager-invitations/{create.InvitationId}/accept");
            _.StatusCodeShouldBe(400);
        });

        var body = response.ReadAsText();
        Assert.Contains("email", body);

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(5, stream.Count);
    }
}
