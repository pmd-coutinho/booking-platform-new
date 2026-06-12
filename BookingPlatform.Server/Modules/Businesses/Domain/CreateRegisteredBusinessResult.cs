namespace BookingPlatform.Server.Modules.Businesses.Domain;

public record CreateRegisteredBusinessResult
{
    public bool IsSuccess { get; }
    public Guid BusinessId { get; }
    public Guid InvitationId { get; }
    public ActorContext Actor { get; }
    public object[] Events { get; } = [];
    public string[] Errors { get; } = [];

    private CreateRegisteredBusinessResult(bool isSuccess, Guid businessId, Guid invitationId, ActorContext actor, object[] events, string[] errors)
    {
        IsSuccess = isSuccess;
        BusinessId = businessId;
        InvitationId = invitationId;
        Actor = actor;
        Events = events;
        Errors = errors;
    }

    public static CreateRegisteredBusinessResult Success(Guid businessId, Guid invitationId, ActorContext actor, object[] events) =>
        new(true, businessId, invitationId, actor, events, []);

    public static CreateRegisteredBusinessResult Failure(string[] errors) =>
        new(false, Guid.Empty, Guid.Empty, new ActorContext("Unknown", "Unknown"), [], errors);
}
