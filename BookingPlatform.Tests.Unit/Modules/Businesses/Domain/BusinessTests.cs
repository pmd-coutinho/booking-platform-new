using BookingPlatform.Server.Modules.Businesses.Domain;
using Xunit;

namespace BookingPlatform.Tests.Unit.Modules.Businesses.Domain;

public class BusinessTests
{
    [Fact]
    public void Create_produces_BusinessCreated_BusinessManagerInvited_and_BusinessBookabilityChanged()
    {
        var businessName = "Acme Salon";
        var managerEmail = "manager@acme.com";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var actor = new ActorContext("PlatformAdmin", "admin-123");

        var result = Business.Create(businessName, managerEmail, expiresAt, actor);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.BusinessId);
        Assert.NotEqual(Guid.Empty, result.InvitationId);
        Assert.Equal(3, result.Events.Length);

        var created = Assert.IsType<BusinessCreated>(result.Events[0]);
        Assert.Equal(result.BusinessId, created.BusinessId);
        Assert.Equal(businessName, created.BusinessName);

        var invited = Assert.IsType<BusinessManagerInvited>(result.Events[1]);
        Assert.Equal(result.InvitationId, invited.InvitationId);
        Assert.Equal(managerEmail, invited.ManagerEmail);
        Assert.Equal(expiresAt, invited.ExpiresAt);

        var bookability = Assert.IsType<BusinessBookabilityChanged>(result.Events[2]);
        Assert.Equal("Unbookable", bookability.Status);
        Assert.Contains("ManagerNotAccepted", bookability.Reasons);
    }

    [Fact]
    public void Create_includes_actor_context_in_result()
    {
        var actor = new ActorContext("PlatformAdmin", "admin-123");
        var result = Business.Create("Acme Salon", "manager@acme.com", DateTimeOffset.UtcNow.AddDays(7), actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(actor, result.Actor);
    }

    [Fact]
    public void Create_allows_duplicate_business_names()
    {
        var businessName = "Acme Salon";
        var managerEmail = "manager@acme.com";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var actor = new ActorContext("PlatformAdmin", "admin-123");

        var result1 = Business.Create(businessName, managerEmail, expiresAt, actor);
        var result2 = Business.Create(businessName, managerEmail, expiresAt, actor);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.BusinessId, result2.BusinessId);
        Assert.Equal(3, result1.Events.Length);
        Assert.Equal(3, result2.Events.Length);
    }

    [Fact]
    public void Create_rejects_blank_business_name()
    {
        var managerEmail = "manager@acme.com";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var actor = new ActorContext("PlatformAdmin", "admin-123");

        var result = Business.Create("   ", managerEmail, expiresAt, actor);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("BusinessName", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@acme.com")]
    [InlineData("manager@")]
    public void Create_rejects_invalid_manager_email(string invalidEmail)
    {
        var businessName = "Acme Salon";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var actor = new ActorContext("PlatformAdmin", "admin-123");

        var result = Business.Create(businessName, invalidEmail, expiresAt, actor);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("ManagerEmail", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_rejects_past_invitation_expiry()
    {
        var businessName = "Acme Salon";
        var managerEmail = "manager@acme.com";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        var actor = new ActorContext("PlatformAdmin", "admin-123");

        var result = Business.Create(businessName, managerEmail, expiresAt, actor);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("InvitationExpiresAt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_rejects_invitation_expiry_beyond_platform_maximum()
    {
        var businessName = "Acme Salon";
        var managerEmail = "manager@acme.com";
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(31);
        var actor = new ActorContext("PlatformAdmin", "admin-123");

        var result = Business.Create(businessName, managerEmail, expiresAt, actor, now);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("InvitationExpiresAt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rehydrate_reconstructs_business_state_from_events()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);

        Assert.Equal(createResult.BusinessId, business.Id);
        Assert.Equal("Acme Salon", business.BusinessName);
        Assert.Equal("Unbookable", business.BookabilityStatus);
        Assert.Contains("ManagerNotAccepted", business.BookabilityReasons);
        Assert.True(business.Invitations.ContainsKey(createResult.InvitationId));
        Assert.Equal(InvitationState.Pending, business.Invitations[createResult.InvitationId].State);
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_produces_accepted_and_bookability_changed_events()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var now = DateTimeOffset.UtcNow;
        var result = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "manager@acme.com", now);

        Assert.True(result.IsSuccess);
        Assert.Equal(createResult.BusinessId, result.BusinessId);
        Assert.Equal(createResult.InvitationId, result.InvitationId);
        Assert.Equal("manager@acme.com", result.ManagerEmail);
        Assert.Equal("Unbookable", result.BookabilityStatus);
        Assert.DoesNotContain("ManagerNotAccepted", result.BookabilityReasons);
        Assert.Contains("ProfileIncomplete", result.BookabilityReasons);
        Assert.Contains("NoStaffMembers", result.BookabilityReasons);
        Assert.Contains("NoAppointmentTypes", result.BookabilityReasons);
        Assert.Contains("NoStaffCapabilities", result.BookabilityReasons);
        Assert.Contains("NoBusinessHours", result.BookabilityReasons);
        Assert.Contains("NoStaffAvailability", result.BookabilityReasons);
        Assert.Equal(2, result.Events.Length);

        var accepted = Assert.IsType<BusinessManagerInvitationAccepted>(result.Events[0]);
        Assert.Equal(createResult.InvitationId, accepted.InvitationId);
        Assert.Equal("manager@acme.com", accepted.ManagerEmail);
        Assert.Equal(now, accepted.AcceptedAt);

        var bookability = Assert.IsType<BusinessBookabilityChanged>(result.Events[1]);
        Assert.Equal("Unbookable", bookability.Status);
        Assert.DoesNotContain("ManagerNotAccepted", bookability.Reasons);
        Assert.Contains("ProfileIncomplete", bookability.Reasons);
        Assert.Contains("NoStaffMembers", bookability.Reasons);
        Assert.Contains("NoAppointmentTypes", bookability.Reasons);
        Assert.Contains("NoStaffCapabilities", bookability.Reasons);
        Assert.Contains("NoBusinessHours", bookability.Reasons);
        Assert.Contains("NoStaffAvailability", bookability.Reasons);
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_normalizes_email_by_trimming_whitespace()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var result = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "  manager@acme.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Events.Length);
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_matches_email_case_insensitively()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var result = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "MANAGER@ACME.COM");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Events.Length);
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_is_idempotent_when_already_accepted_by_same_email()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);

        var first = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "manager@acme.com");

        Assert.True(first.IsSuccess);
        Assert.Equal(2, first.Events.Length);

        business = Business.Rehydrate(
            [.. createResult.Events, .. first.Events]);

        var second = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "manager@acme.com");

        Assert.True(second.IsSuccess);
        Assert.Empty(second.Events);
        Assert.Equal("Unbookable", second.BookabilityStatus);
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_rejects_wrong_email()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var result = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "other@acme.com");

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Events);
        Assert.Equal(AcceptBusinessManagerInvitationFailureKind.NotFound, result.FailureKind);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_rejects_missing_invitation()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var result = business.AcceptBusinessManagerInvitation(Guid.NewGuid(), "manager@acme.com");

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Events);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_rejects_expired_invitation()
    {
        var now = DateTimeOffset.UtcNow;
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            now.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"),
            now);

        var business = Business.Rehydrate(createResult.Events);

        var expiredNow = now.AddDays(8);
        var result = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "manager@acme.com", expiredNow);

        Assert.False(result.IsSuccess);
        Assert.Equal(AcceptBusinessManagerInvitationFailureKind.Conflict, result.FailureKind);
        Assert.Empty(result.Events);
        Assert.Contains(result.Errors, e => e.Contains("expired", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_rejects_different_email_after_prior_acceptance()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);

        var acceptResult = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "manager@acme.com");
        Assert.True(acceptResult.IsSuccess);
        Assert.Equal(2, acceptResult.Events.Length);

        business = Business.Rehydrate(
            [.. createResult.Events, .. acceptResult.Events]);

        var second = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "other@acme.com");

        Assert.False(second.IsSuccess);
        Assert.Empty(second.Events);
        Assert.Equal(AcceptBusinessManagerInvitationFailureKind.NotFound, second.FailureKind);
        Assert.Contains(second.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AcceptBusinessManagerInvitation_does_not_opportunistically_append_expired_event()
    {
        var now = DateTimeOffset.UtcNow;
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            now.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"),
            now);

        var business = Business.Rehydrate(createResult.Events);

        var expiredNow = now.AddDays(8);
        var result = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "manager@acme.com", expiredNow);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void ExpireInvitation_produces_expired_event_for_pending_due_invitation()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var invitation = business.Invitations[createResult.InvitationId];
        var now = invitation.ExpiresAt.AddMinutes(1);

        var result = business.ExpireInvitation(createResult.InvitationId, now);

        Assert.NotEmpty(result.Events);
        Assert.Single(result.Events);

        var expired = Assert.IsType<BusinessManagerInvitationExpired>(result.Events[0]);
        Assert.Equal(createResult.InvitationId, expired.InvitationId);
        Assert.Equal(invitation.ExpiresAt, expired.ExpiredAt);
    }

    [Fact]
    public void ExpireInvitation_is_noop_when_already_accepted()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);

        var acceptResult = business.AcceptBusinessManagerInvitation(createResult.InvitationId, "manager@acme.com");
        Assert.True(acceptResult.IsSuccess);

        business = Business.Rehydrate(
            [.. createResult.Events, .. acceptResult.Events]);

        var invitation = business.Invitations[createResult.InvitationId];
        var now = invitation.ExpiresAt.AddMinutes(1);

        var result = business.ExpireInvitation(createResult.InvitationId, now);

        Assert.Empty(result.Events);
    }

    [Fact]
    public void ExpireInvitation_is_noop_when_already_expired()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var invitation = business.Invitations[createResult.InvitationId];
        var now = invitation.ExpiresAt.AddMinutes(1);

        var expireResult = business.ExpireInvitation(createResult.InvitationId, now);
        Assert.NotEmpty(expireResult.Events);

        business = Business.Rehydrate(
            [.. createResult.Events, .. expireResult.Events]);

        var secondResult = business.ExpireInvitation(createResult.InvitationId, now.AddMinutes(1));

        Assert.Empty(secondResult.Events);
    }

    [Fact]
    public void ExpireInvitation_is_noop_when_not_due_yet()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);
        var invitation = business.Invitations[createResult.InvitationId];
        var now = invitation.ExpiresAt.AddMinutes(-1);

        var result = business.ExpireInvitation(createResult.InvitationId, now);

        Assert.Empty(result.Events);
    }

    [Fact]
    public void ExpireInvitation_is_noop_when_invitation_not_found()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var business = Business.Rehydrate(createResult.Events);

        var result = business.ExpireInvitation(Guid.NewGuid());

        Assert.Empty(result.Events);
    }

    [Fact]
    public void Rehydrate_applies_expired_event_to_set_state()
    {
        var createResult = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var invitation = ((BusinessManagerInvited)createResult.Events[1]);
        var expiredEvent = new BusinessManagerInvitationExpired(
            invitation.InvitationId,
            invitation.ExpiresAt);

        var business = Business.Rehydrate(
            [.. createResult.Events, expiredEvent]);

        Assert.Equal(InvitationState.Expired, business.Invitations[invitation.InvitationId].State);
    }
}
