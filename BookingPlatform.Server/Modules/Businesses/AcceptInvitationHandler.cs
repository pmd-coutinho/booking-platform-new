using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;

namespace BookingPlatform.Server.Modules.Businesses;

public static class AcceptInvitationHandler
{
    public static async Task<AcceptInvitationHandlerResult> Handle(
        Guid businessId,
        Guid invitationId,
        string managerEmail,
        IDocumentSession session)
    {
        var stream = await session.Events.FetchStreamAsync(businessId);

        if (!stream.Any())
        {
            return AcceptInvitationHandlerResult.Failure(["Business not found."], businessNotFound: true);
        }

        var events = stream.Select(e => e.Data).ToArray();
        var business = Business.Rehydrate(events);

        var result = business.AcceptInvitation(invitationId, managerEmail);

        if (!result.IsSuccess)
        {
            return AcceptInvitationHandlerResult.Failure(result.Errors);
        }

        if (result.Events.Length > 0)
        {
            session.Events.Append(businessId, result.Events);
        }

        return AcceptInvitationHandlerResult.Success(new AcceptInvitationResponse(
            result.BusinessId,
            result.InvitationId,
            result.ManagerEmail,
            result.BookabilityStatus,
            result.BookabilityReasons));
    }
}

public record AcceptInvitationHandlerResult(
    bool IsSuccess,
    AcceptInvitationResponse? Response,
    string[] Errors,
    bool BusinessNotFound)
{
    public static AcceptInvitationHandlerResult Success(AcceptInvitationResponse response) =>
        new(true, response, [], false);

    public static AcceptInvitationHandlerResult Failure(string[] errors, bool businessNotFound = false) =>
        new(false, null, errors, businessNotFound);
}

public record AcceptInvitationResponse(
    Guid BusinessId,
    Guid InvitationId,
    string ManagerEmail,
    string BookabilityStatus,
    string[] BookabilityReasons);
