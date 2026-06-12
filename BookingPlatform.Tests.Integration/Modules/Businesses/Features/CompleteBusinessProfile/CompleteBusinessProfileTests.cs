using Alba;
using BookingPlatform.Server.Modules.Businesses.Domain;
using BookingPlatform.Server.Modules.Businesses.Features.CompleteBusinessProfile;
using BookingPlatform.Server.Modules.Businesses.Features.CreateRegisteredBusiness;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace BookingPlatform.Tests.Integration.Modules.Businesses.Features.CompleteBusinessProfile;

public class CompleteBusinessProfileTests : IAsyncLifetime
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

    private async Task AcceptInvitation(Guid businessId, Guid invitationId, string managerEmail)
    {
        Assert.NotNull(_host);

        await _host!.Scenario(_ =>
        {
            _.Post.Json(new { ManagerEmail = managerEmail }).ToUrl($"/api/businesses/{businessId}/business-manager-invitations/{invitationId}/accept");
            _.StatusCodeShouldBe(200);
        });
    }

    private static object BuildProfileRequest(string slug) => new
    {
        PublicBusinessName = "Acme Salon Public",
        PublicBookingSlug = slug,
        ContactPhone = "+1234567890",
        ContactEmail = "contact@acme.com",
        Street = "123 Main St",
        City = "New York",
        PostalCode = "10001",
        Country = "US",
        TimeZone = "America/New_York",
        Currency = "USD"
    };

    [Fact]
    public async Task Should_complete_profile_and_return_updated_bookability()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));
        await AcceptInvitation(create.BusinessId, create.InvitationId, "manager@acme.com");

        var request = BuildProfileRequest("acme-salon");
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create.BusinessId}/profile");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<CompleteBusinessProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal(create.BusinessId, result.BusinessId);
        Assert.Equal("acme-salon", result.PublicBookingSlug);
        Assert.Equal("Unbookable", result.BookabilityStatus);
        Assert.DoesNotContain("ProfileIncomplete", result.BookabilityReasons);
        Assert.Contains("NoStaffMembers", result.BookabilityReasons);
    }

    [Fact]
    public async Task Should_persist_profile_completed_and_bookability_changed_events()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));
        await AcceptInvitation(create.BusinessId, create.InvitationId, "manager@acme.com");

        var request = BuildProfileRequest("acme-salon");
        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create.BusinessId}/profile");
            _.StatusCodeShouldBe(200);
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(create.BusinessId);

        Assert.Equal(7, stream.Count);

        var profileCompleted = Assert.IsType<BusinessProfileCompleted>(stream[5].Data);
        Assert.Equal("Acme Salon Public", profileCompleted.PublicBusinessName);
        Assert.Equal("acme-salon", profileCompleted.PublicBookingSlug);

        var bookabilityChanged = Assert.IsType<BusinessBookabilityChanged>(stream[6].Data);
        Assert.Equal("Unbookable", bookabilityChanged.Status);
        Assert.DoesNotContain("ProfileIncomplete", bookabilityChanged.Reasons);
    }

    [Fact]
    public async Task Should_reserve_slug_in_database()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));
        await AcceptInvitation(create.BusinessId, create.InvitationId, "manager@acme.com");

        var request = BuildProfileRequest("acme-salon");
        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create.BusinessId}/profile");
            _.StatusCodeShouldBe(200);
        });

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var reservation = await session.Query<SlugReservation>().FirstOrDefaultAsync(x => x.Id == "acme-salon");
        Assert.NotNull(reservation);
        Assert.Equal(create.BusinessId, reservation.BusinessId);
    }

    [Fact]
    public async Task Should_reject_duplicate_slug_with_409()
    {
        Assert.NotNull(_host);

        var create1 = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));
        await AcceptInvitation(create1.BusinessId, create1.InvitationId, "manager@acme.com");

        var request = BuildProfileRequest("shared-slug");
        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create1.BusinessId}/profile");
            _.StatusCodeShouldBe(200);
        });

        var create2 = await CreateBusiness("Another Salon", "other@acme.com", DateTimeOffset.UtcNow.AddDays(7));
        await AcceptInvitation(create2.BusinessId, create2.InvitationId, "other@acme.com");

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create2.BusinessId}/profile");
            _.StatusCodeShouldBe(409);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("PublicBookingSlug is already taken", body);
    }

    [Fact]
    public async Task Should_return_404_for_missing_business()
    {
        Assert.NotNull(_host);

        var nonExistentBusinessId = Guid.NewGuid();
        var request = BuildProfileRequest("acme-salon");

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{nonExistentBusinessId}/profile");
            _.StatusCodeShouldBe(404);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("Business not found", body);
    }

    [Fact]
    public async Task Should_return_404_when_manager_not_accepted()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));

        var request = BuildProfileRequest("acme-salon");
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create.BusinessId}/profile");
            _.StatusCodeShouldBe(404);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("Business manager invitation has not been accepted", body);
    }

    [Fact]
    public async Task Should_return_409_for_already_completed_profile()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));
        await AcceptInvitation(create.BusinessId, create.InvitationId, "manager@acme.com");

        var request = BuildProfileRequest("acme-salon");
        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create.BusinessId}/profile");
            _.StatusCodeShouldBe(200);
        });

        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create.BusinessId}/profile");
            _.StatusCodeShouldBe(409);
            _.ContentTypeShouldBe("application/problem+json");
        });

        var body = response.ReadAsText();
        Assert.Contains("Business profile is already completed", body);
    }

    [Fact]
    public async Task Should_return_400_for_invalid_slug()
    {
        Assert.NotNull(_host);

        var create = await CreateBusiness("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7));
        await AcceptInvitation(create.BusinessId, create.InvitationId, "manager@acme.com");

        var request = new
        {
            PublicBusinessName = "Acme Salon Public",
            PublicBookingSlug = "Acme Salon",
            ContactPhone = "+1234567890",
            ContactEmail = "contact@acme.com",
            Street = "123 Main St",
            City = "New York",
            PostalCode = "10001",
            Country = "US",
            TimeZone = "America/New_York",
            Currency = "USD"
        };

        await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{create.BusinessId}/profile");
            _.StatusCodeShouldBe(400);
            _.ContentTypeShouldBe("application/problem+json");
        });
    }

    [Fact]
    public async Task Should_transition_to_bookable_when_only_profile_incomplete_was_missing()
    {
        Assert.NotNull(_host);

        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var events = new object[]
        {
            new BusinessCreated(businessId, "Acme Salon"),
            new BusinessManagerInvited(invitationId, "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7)),
            new BusinessBookabilityChanged("Unbookable", new[] { "ManagerNotAccepted" }),
            new BusinessManagerInvitationAccepted(invitationId, "manager@acme.com", DateTimeOffset.UtcNow),
            new BusinessBookabilityChanged("Unbookable", new[] { "ProfileIncomplete" })
        };

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var seedSession = store.LightweightSession();
        seedSession.Events.StartStream<Business>(businessId, events);
        await seedSession.SaveChangesAsync();

        var request = BuildProfileRequest("acme-salon");
        var response = await _host!.Scenario(_ =>
        {
            _.Post.Json(request).ToUrl($"/api/businesses/{businessId}/profile");
            _.StatusCodeShouldBe(200);
        });

        var result = await response.ReadAsJsonAsync<CompleteBusinessProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal("Bookable", result.BookabilityStatus);
        Assert.Empty(result.BookabilityReasons);
    }
}
