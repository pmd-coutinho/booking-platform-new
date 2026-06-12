using BookingPlatform.Server.Modules.Businesses.Domain;
using Xunit;

namespace BookingPlatform.Tests.Unit.Modules.Businesses;

public class CompleteBusinessProfileTests
{
    [Fact]
    public void Should_complete_profile_and_emit_events_with_remaining_reasons()
    {
        // Arrange: rehydrate a business with an accepted manager
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var events = new object[]
        {
            new BusinessCreated(businessId, "Acme Salon"),
            new BusinessManagerInvited(invitationId, "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7)),
            new BusinessBookabilityChanged("Unbookable", new[] { "ManagerNotAccepted" }),
            new BusinessManagerInvitationAccepted(invitationId, "manager@acme.com", DateTimeOffset.UtcNow),
            new BusinessBookabilityChanged("Unbookable", new[] { "ProfileIncomplete", "NoStaffMembers", "NoAppointmentTypes", "NoStaffCapabilities", "NoBusinessHours", "NoStaffAvailability" })
        };

        var business = Business.Rehydrate(events);

        // Act
        var result = business.CompleteBusinessProfile(
            publicBusinessName: "Acme Salon Public",
            publicBookingSlug: "acme-salon",
            contactPhone: "+1234567890",
            contactEmail: "contact@acme.com",
            street: "123 Main St",
            city: "New York",
            postalCode: "10001",
            country: "US",
            timeZone: "America/New_York",
            currency: "USD",
            slugExists: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(businessId, result.BusinessId);
        Assert.Equal("Unbookable", result.BookabilityStatus);
        Assert.DoesNotContain("ProfileIncomplete", result.BookabilityReasons);
        Assert.Contains("NoStaffMembers", result.BookabilityReasons);
        Assert.Contains("NoAppointmentTypes", result.BookabilityReasons);
        Assert.Contains("NoStaffCapabilities", result.BookabilityReasons);
        Assert.Contains("NoBusinessHours", result.BookabilityReasons);
        Assert.Contains("NoStaffAvailability", result.BookabilityReasons);
        Assert.Equal(2, result.Events.Length);
        Assert.IsType<BusinessProfileCompleted>(result.Events[0]);
        Assert.IsType<BusinessBookabilityChanged>(result.Events[1]);
    }

    [Fact]
    public void Should_reject_when_no_manager_invitation_accepted()
    {
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var events = new object[]
        {
            new BusinessCreated(businessId, "Acme Salon"),
            new BusinessManagerInvited(invitationId, "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7)),
            new BusinessBookabilityChanged("Unbookable", new[] { "ManagerNotAccepted" })
        };

        var business = Business.Rehydrate(events);

        var result = business.CompleteBusinessProfile(
            "Acme Salon Public", "acme-salon", "+1234567890", "contact@acme.com",
            "123 Main St", "New York", "10001", "US",
            "America/New_York", "USD",
            slugExists: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(CompleteBusinessProfileFailureKind.NotFound, result.FailureKind);
        Assert.Contains("Business manager invitation has not been accepted.", result.Errors);
    }

    [Fact]
    public void Should_reject_already_completed_profile()
    {
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var events = new object[]
        {
            new BusinessCreated(businessId, "Acme Salon"),
            new BusinessManagerInvited(invitationId, "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7)),
            new BusinessBookabilityChanged("Unbookable", new[] { "ManagerNotAccepted" }),
            new BusinessManagerInvitationAccepted(invitationId, "manager@acme.com", DateTimeOffset.UtcNow),
            new BusinessBookabilityChanged("Unbookable", new[] { "ProfileIncomplete", "NoStaffMembers" }),
            new BusinessProfileCompleted("Acme Salon Public", "acme-salon", "+1234567890", "contact@acme.com", "123 Main St", "New York", "10001", "US", "America/New_York", "USD"),
            new BusinessBookabilityChanged("Unbookable", new[] { "NoStaffMembers" })
        };

        var business = Business.Rehydrate(events);

        var result = business.CompleteBusinessProfile(
            "Acme Salon Public", "acme-salon", "+1234567890", "contact@acme.com",
            "123 Main St", "New York", "10001", "US",
            "America/New_York", "USD",
            slugExists: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(CompleteBusinessProfileFailureKind.Conflict, result.FailureKind);
        Assert.Contains("Business profile is already completed.", result.Errors);
    }

    [Fact]
    public void Should_reject_invalid_slug_format()
    {
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

        var business = Business.Rehydrate(events);

        var result = business.CompleteBusinessProfile(
            "Acme Salon Public", "Acme Salon", "+1234567890", "contact@acme.com",
            "123 Main St", "New York", "10001", "US",
            "America/New_York", "USD",
            slugExists: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(CompleteBusinessProfileFailureKind.BadRequest, result.FailureKind);
        Assert.Contains("PublicBookingSlug", result.Errors[0]);
    }

    [Fact]
    public void Should_transition_to_bookable_when_only_profile_incomplete_was_missing()
    {
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

        var business = Business.Rehydrate(events);

        var result = business.CompleteBusinessProfile(
            "Acme Salon Public", "acme-salon", "+1234567890", "contact@acme.com",
            "123 Main St", "New York", "10001", "US",
            "America/New_York", "USD",
            slugExists: false);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bookable", result.BookabilityStatus);
        Assert.Empty(result.BookabilityReasons);
        Assert.Equal(2, result.Events.Length);
        var bookabilityChanged = Assert.IsType<BusinessBookabilityChanged>(result.Events[1]);
        Assert.Equal("Bookable", bookabilityChanged.Status);
        Assert.Empty(bookabilityChanged.Reasons);
    }

    [Fact]
    public void Should_reject_duplicate_slug()
    {
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

        var business = Business.Rehydrate(events);

        var result = business.CompleteBusinessProfile(
            "Acme Salon Public", "acme-salon", "+1234567890", "contact@acme.com",
            "123 Main St", "New York", "10001", "US",
            "America/New_York", "USD",
            slugExists: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(CompleteBusinessProfileFailureKind.Conflict, result.FailureKind);
        Assert.Contains("PublicBookingSlug is already taken.", result.Errors);
    }
}
