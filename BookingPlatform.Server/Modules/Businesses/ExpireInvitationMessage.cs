namespace BookingPlatform.Server.Modules.Businesses;

public record ExpireInvitationMessage(Guid BusinessId, Guid InvitationId);
