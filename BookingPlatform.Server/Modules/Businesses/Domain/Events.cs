namespace BookingPlatform.Server.Modules.Businesses.Domain;

public record BusinessCreated(Guid BusinessId, string BusinessName);

public record BusinessManagerInvited(Guid InvitationId, string ManagerEmail, DateTimeOffset ExpiresAt);

public record BusinessManagerInvitationAccepted(
    Guid InvitationId,
    string ManagerEmail,
    DateTimeOffset AcceptedAt);

public record BusinessManagerInvitationExpired(
    Guid InvitationId,
    DateTimeOffset ExpiredAt);

public record BusinessBookabilityChanged(
    string Status,
    string[] Reasons);

public record BusinessProfileCompleted(
    string PublicBusinessName,
    string PublicBookingSlug,
    string ContactPhone,
    string ContactEmail,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string TimeZone,
    string Currency);
