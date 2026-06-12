namespace BookingPlatform.Server.Modules.Businesses.Domain;

public enum AcceptBusinessManagerInvitationFailureKind
{
    None,
    NotFound,
    Conflict
}

public record AcceptBusinessManagerInvitationResult
{
    public bool IsSuccess { get; }
    public Guid BusinessId { get; }
    public Guid InvitationId { get; }
    public string ManagerEmail { get; } = string.Empty;
    public string BookabilityStatus { get; } = string.Empty;
    public string[] BookabilityReasons { get; } = [];
    public object[] Events { get; } = [];
    public string[] Errors { get; } = [];
    public AcceptBusinessManagerInvitationFailureKind FailureKind { get; }

    private AcceptBusinessManagerInvitationResult(
        bool isSuccess,
        Guid businessId,
        Guid invitationId,
        string managerEmail,
        string bookabilityStatus,
        string[] bookabilityReasons,
        object[] events,
        string[] errors,
        AcceptBusinessManagerInvitationFailureKind failureKind)
    {
        IsSuccess = isSuccess;
        BusinessId = businessId;
        InvitationId = invitationId;
        ManagerEmail = managerEmail;
        BookabilityStatus = bookabilityStatus;
        BookabilityReasons = bookabilityReasons;
        Events = events;
        Errors = errors;
        FailureKind = failureKind;
    }

    public static AcceptBusinessManagerInvitationResult Success(
        Guid businessId,
        Guid invitationId,
        string managerEmail,
        string bookabilityStatus,
        string[] bookabilityReasons,
        object[] events) =>
        new(true, businessId, invitationId, managerEmail, bookabilityStatus, bookabilityReasons, events, [], AcceptBusinessManagerInvitationFailureKind.None);

    public static AcceptBusinessManagerInvitationResult Failure(string[] errors, AcceptBusinessManagerInvitationFailureKind failureKind) =>
        new(false, Guid.Empty, Guid.Empty, string.Empty, string.Empty, [], [], errors, failureKind);
}
