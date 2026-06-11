namespace BookingPlatform.Server.Modules.Businesses.Domain;

public record CreateBusinessResult
{
    public bool IsSuccess { get; }
    public Guid BusinessId { get; }
    public Guid InvitationId { get; }
    public object[] Events { get; } = [];
    public string[] Errors { get; } = [];

    private CreateBusinessResult(bool isSuccess, Guid businessId, Guid invitationId, object[] events, string[] errors)
    {
        IsSuccess = isSuccess;
        BusinessId = businessId;
        InvitationId = invitationId;
        Events = events;
        Errors = errors;
    }

    public static CreateBusinessResult Success(Guid businessId, Guid invitationId, object[] events) =>
        new(true, businessId, invitationId, events, []);

    public static CreateBusinessResult Failure(string[] errors) =>
        new(false, Guid.Empty, Guid.Empty, [], errors);
}
