namespace BookingPlatform.Server.Modules.Businesses.Domain;

public class Business
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BookabilityStatus { get; set; } = string.Empty;
    public string[] BookabilityReasons { get; set; } = [];

    public static (Guid BusinessId, Guid InvitationId, object[] Events) Create(
        string businessName,
        string managerEmail,
        DateTimeOffset expiresAt)
    {
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var status = "Unbookable";
        var reasons = new[] { "ManagerNotAccepted", "OnboardingIncomplete" };

        var events = new object[]
        {
            new BusinessCreated(businessId, businessName),
            new BusinessManagerInvited(invitationId, managerEmail, expiresAt),
            new BusinessBookabilityChanged(status, reasons)
        };

        return (businessId, invitationId, events);
    }
}
