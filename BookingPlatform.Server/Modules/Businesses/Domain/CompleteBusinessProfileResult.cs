namespace BookingPlatform.Server.Modules.Businesses.Domain;

public enum CompleteBusinessProfileFailureKind
{
    None,
    NotFound,
    Conflict,
    BadRequest
}

public record CompleteBusinessProfileResult
{
    public bool IsSuccess { get; }
    public Guid BusinessId { get; }
    public string BookabilityStatus { get; } = string.Empty;
    public string[] BookabilityReasons { get; } = [];
    public object[] Events { get; } = [];
    public string[] Errors { get; } = [];
    public CompleteBusinessProfileFailureKind FailureKind { get; }

    private CompleteBusinessProfileResult(
        bool isSuccess,
        Guid businessId,
        string bookabilityStatus,
        string[] bookabilityReasons,
        object[] events,
        string[] errors,
        CompleteBusinessProfileFailureKind failureKind)
    {
        IsSuccess = isSuccess;
        BusinessId = businessId;
        BookabilityStatus = bookabilityStatus;
        BookabilityReasons = bookabilityReasons;
        Events = events;
        Errors = errors;
        FailureKind = failureKind;
    }

    public static CompleteBusinessProfileResult Success(
        Guid businessId,
        string bookabilityStatus,
        string[] bookabilityReasons,
        object[] events) =>
        new(true, businessId, bookabilityStatus, bookabilityReasons, events, [], CompleteBusinessProfileFailureKind.None);

    public static CompleteBusinessProfileResult Failure(string[] errors, CompleteBusinessProfileFailureKind failureKind) =>
        new(false, Guid.Empty, string.Empty, [], [], errors, failureKind);
}
