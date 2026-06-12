namespace BookingPlatform.Server.Modules.Businesses.Domain;

public record AcceptInvitationResult
{
    public bool IsSuccess { get; }
    public Guid BusinessId { get; }
    public Guid InvitationId { get; }
    public string ManagerEmail { get; } = string.Empty;
    public string BookabilityStatus { get; } = string.Empty;
    public string[] BookabilityReasons { get; } = [];
    public object[] Events { get; } = [];
    public string[] Errors { get; } = [];

    private AcceptInvitationResult(
        bool isSuccess,
        Guid businessId,
        Guid invitationId,
        string managerEmail,
        string bookabilityStatus,
        string[] bookabilityReasons,
        object[] events,
        string[] errors)
    {
        IsSuccess = isSuccess;
        BusinessId = businessId;
        InvitationId = invitationId;
        ManagerEmail = managerEmail;
        BookabilityStatus = bookabilityStatus;
        BookabilityReasons = bookabilityReasons;
        Events = events;
        Errors = errors;
    }

    public static AcceptInvitationResult Success(
        Guid businessId,
        Guid invitationId,
        string managerEmail,
        string bookabilityStatus,
        string[] bookabilityReasons,
        object[] events) =>
        new(true, businessId, invitationId, managerEmail, bookabilityStatus, bookabilityReasons, events, []);

    public static AcceptInvitationResult Failure(string[] errors) =>
        new(false, Guid.Empty, Guid.Empty, string.Empty, string.Empty, [], [], errors);
}
