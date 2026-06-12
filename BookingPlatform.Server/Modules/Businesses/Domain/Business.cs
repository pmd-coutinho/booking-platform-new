namespace BookingPlatform.Server.Modules.Businesses.Domain;

public class Business
{
    public Guid Id { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string BookabilityStatus { get; private set; } = string.Empty;
    public string[] BookabilityReasons { get; private set; } = [];
    public Dictionary<Guid, Invitation> Invitations { get; private set; } = [];

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

    public static CreateRegisteredBusinessResult Create(
        string businessName,
        string managerEmail,
        DateTimeOffset expiresAt,
        ActorContext actor,
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
            return CreateRegisteredBusinessResult.Failure(errors.ToArray());
        }

        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var status = "Unbookable";
        var reasons = new[] { "ManagerNotAccepted" };

        var events = new object[]
        {
            new BusinessCreated(businessId, businessName),
            new BusinessManagerInvited(invitationId, managerEmail, expiresAt),
            new BusinessBookabilityChanged(status, reasons)
        };

        return CreateRegisteredBusinessResult.Success(businessId, invitationId, actor, events);
    }

    public static Business Rehydrate(object[] events)
    {
        var business = new Business();
        foreach (var e in events)
        {
            business.Apply(e);
        }

        return business;
    }

    private void Apply(object e)
    {
        switch (e)
        {
            case BusinessCreated created:
                Id = created.BusinessId;
                BusinessName = created.BusinessName;
                break;

            case BusinessManagerInvited invited:
                Invitations[invited.InvitationId] = new Invitation(
                    invited.ManagerEmail,
                    invited.ExpiresAt,
                    InvitationState.Pending);
                break;

            case BusinessBookabilityChanged changed:
                BookabilityStatus = changed.Status;
                BookabilityReasons = changed.Reasons;
                break;

            case BusinessManagerInvitationAccepted accepted:
                if (Invitations.TryGetValue(accepted.InvitationId, out var invitation))
                {
                    Invitations[accepted.InvitationId] = invitation with { State = InvitationState.Accepted };
                }

                break;

            case BusinessManagerInvitationExpired expired:
                if (Invitations.TryGetValue(expired.InvitationId, out var inv))
                {
                    Invitations[expired.InvitationId] = inv with { State = InvitationState.Expired };
                }

                break;
        }
    }

    public AcceptBusinessManagerInvitationResult AcceptBusinessManagerInvitation(
        Guid invitationId,
        string managerEmail,
        DateTimeOffset? now = null)
    {
        var acceptedAt = now ?? DateTimeOffset.UtcNow;
        var normalizedEmail = managerEmail.Trim();

        if (!Invitations.TryGetValue(invitationId, out var invitation))
        {
            return AcceptBusinessManagerInvitationResult.Failure(
                ["Business manager invitation was not found."],
                AcceptBusinessManagerInvitationFailureKind.NotFound);
        }

        if (!string.Equals(invitation.ManagerEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return AcceptBusinessManagerInvitationResult.Failure(
                ["Business manager invitation was not found."],
                AcceptBusinessManagerInvitationFailureKind.NotFound);
        }

        if (invitation.State == InvitationState.Accepted)
        {
            return AcceptBusinessManagerInvitationResult.Success(
                Id,
                invitationId,
                invitation.ManagerEmail,
                BookabilityStatus,
                BookabilityReasons,
                []);
        }

        if (invitation.State == InvitationState.Expired || acceptedAt > invitation.ExpiresAt)
        {
            return AcceptBusinessManagerInvitationResult.Failure(
                ["The business manager invitation has expired."],
                AcceptBusinessManagerInvitationFailureKind.Conflict);
        }

        var reasons = new[] { "ProfileIncomplete", "NoStaffMembers", "NoAppointmentTypes", "NoStaffCapabilities", "NoBusinessHours", "NoStaffAvailability" };

        var events = new object[]
        {
            new BusinessManagerInvitationAccepted(invitationId, invitation.ManagerEmail, acceptedAt),
            new BusinessBookabilityChanged("Unbookable", reasons)
        };

        return AcceptBusinessManagerInvitationResult.Success(
            Id,
            invitationId,
            invitation.ManagerEmail,
            "Unbookable",
            reasons,
            events);
    }

    public ExpireInvitationResult ExpireInvitation(
        Guid invitationId,
        DateTimeOffset? now = null)
    {
        var currentTime = now ?? DateTimeOffset.UtcNow;

        if (!Invitations.TryGetValue(invitationId, out var invitation))
        {
            return ExpireInvitationResult.NoOp();
        }

        if (invitation.State != InvitationState.Pending)
        {
            return ExpireInvitationResult.NoOp();
        }

        if (currentTime < invitation.ExpiresAt)
        {
            return ExpireInvitationResult.NoOp();
        }

        var events = new object[]
        {
            new BusinessManagerInvitationExpired(invitationId, invitation.ExpiresAt)
        };

        return ExpireInvitationResult.Success(events);
    }
}

public record Invitation(
    string ManagerEmail,
    DateTimeOffset ExpiresAt,
    InvitationState State);
