namespace BookingPlatform.Server.Modules.Businesses.Domain;

public record BusinessCreated(Guid BusinessId, string BusinessName);

public record BusinessManagerInvited(Guid InvitationId, string ManagerEmail, DateTimeOffset ExpiresAt);

public record BusinessBookabilityChanged(
    string Status,
    string[] Reasons);
