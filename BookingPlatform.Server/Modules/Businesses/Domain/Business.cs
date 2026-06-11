namespace BookingPlatform.Server.Modules.Businesses.Domain;

public class Business
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BookabilityStatus { get; set; } = string.Empty;
    public string[] BookabilityReasons { get; set; } = [];

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return address.Address == email && address.Host.Contains('.');
        }
        catch
        {
            return false;
        }
    }

    public static CreateBusinessResult Create(
        string businessName,
        string managerEmail,
        DateTimeOffset expiresAt,
        DateTimeOffset? now = null)
    {
        var currentTime = now ?? DateTimeOffset.UtcNow;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(businessName))
        {
            errors.Add("BusinessName is required.");
        }

        if (string.IsNullOrWhiteSpace(managerEmail) || !IsValidEmail(managerEmail))
        {
            errors.Add("ManagerEmail is invalid.");
        }

        if (expiresAt <= currentTime)
        {
            errors.Add("InvitationExpiresAt must be in the future.");
        }

        if (expiresAt > currentTime.AddDays(30))
        {
            errors.Add("InvitationExpiresAt cannot exceed 30 days.");
        }

        if (errors.Count > 0)
        {
            return CreateBusinessResult.Failure(errors.ToArray());
        }

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

        return CreateBusinessResult.Success(businessId, invitationId, events);
    }
}
