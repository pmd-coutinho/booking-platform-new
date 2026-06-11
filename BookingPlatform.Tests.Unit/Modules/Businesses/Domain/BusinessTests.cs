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

        var (businessId, invitationId, events) = Business.Create(businessName, managerEmail, expiresAt);

        Assert.NotEqual(Guid.Empty, businessId);
        Assert.NotEqual(Guid.Empty, invitationId);
        Assert.Equal(3, events.Length);
        
        var created = Assert.IsType<BusinessCreated>(events[0]);
        Assert.Equal(businessId, created.BusinessId);
        Assert.Equal(businessName, created.BusinessName);

        var invited = Assert.IsType<BusinessManagerInvited>(events[1]);
        Assert.Equal(invitationId, invited.InvitationId);
        Assert.Equal(managerEmail, invited.ManagerEmail);
        Assert.Equal(expiresAt, invited.ExpiresAt);

        var bookability = Assert.IsType<BusinessBookabilityChanged>(events[2]);
        Assert.Equal("Unbookable", bookability.Status);
        Assert.Contains("ManagerNotAccepted", bookability.Reasons);
        Assert.Contains("OnboardingIncomplete", bookability.Reasons);
    }

    [Fact]
    public void Create_allows_duplicate_business_names()
    {
        var businessName = "Acme Salon";
        var managerEmail = "manager@acme.com";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var (id1, _, events1) = Business.Create(businessName, managerEmail, expiresAt);
        var (id2, _, events2) = Business.Create(businessName, managerEmail, expiresAt);

        Assert.NotEqual(id1, id2);
        Assert.Equal(3, events1.Length);
        Assert.Equal(3, events2.Length);
    }
}
