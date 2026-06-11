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
        Assert.Contains("OnboardingIncomplete", bookability.Reasons);
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
}
