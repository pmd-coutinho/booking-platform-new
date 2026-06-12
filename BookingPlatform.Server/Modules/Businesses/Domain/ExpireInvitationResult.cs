namespace BookingPlatform.Server.Modules.Businesses.Domain;

public record ExpireInvitationResult
{
    public object[] Events { get; } = [];

    private ExpireInvitationResult(object[] events)
    {
        Events = events;
    }

    public static ExpireInvitationResult Success(object[] events) =>
        new(events);

    public static ExpireInvitationResult NoOp() =>
        new([]);
}
