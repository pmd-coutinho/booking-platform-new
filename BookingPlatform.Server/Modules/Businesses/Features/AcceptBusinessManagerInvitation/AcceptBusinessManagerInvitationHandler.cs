using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;

namespace BookingPlatform.Server.Modules.Businesses.Features.AcceptBusinessManagerInvitation;

public class AcceptBusinessManagerInvitationHandler(IDocumentSession session)
{
    public async Task<AcceptBusinessManagerInvitationHandlerResult> Handle(
        Guid businessId,
        Guid invitationId,
        string managerEmail)
    {
        var stream = await session.Events.FetchStreamAsync(businessId);

        if (!stream.Any())
        {
            return AcceptBusinessManagerInvitationHandlerResult.Failure(
                ["Business manager invitation was not found."],
                AcceptBusinessManagerInvitationFailureKind.NotFound);
        }

        var events = stream.Select(e => e.Data).ToArray();
        var business = Business.Rehydrate(events);

        var result = business.AcceptBusinessManagerInvitation(invitationId, managerEmail);

        if (!result.IsSuccess)
        {
            return AcceptBusinessManagerInvitationHandlerResult.Failure(result.Errors, result.FailureKind);
        }

        if (result.Events.Length > 0)
        {
            session.Events.Append(businessId, result.Events);
        }

        return AcceptBusinessManagerInvitationHandlerResult.Success(
            new AcceptBusinessManagerInvitationResponse(
                result.BusinessId,
                result.InvitationId,
                result.ManagerEmail,
                result.BookabilityStatus,
                result.BookabilityReasons),
            alreadyAccepted: result.Events.Length == 0);
    }
}

public record AcceptBusinessManagerInvitationHandlerResult(
    bool IsSuccess,
    AcceptBusinessManagerInvitationResponse? Response,
    string[] Errors,
    AcceptBusinessManagerInvitationFailureKind FailureKind,
    bool AlreadyAccepted)
{
    public static AcceptBusinessManagerInvitationHandlerResult Success(
        AcceptBusinessManagerInvitationResponse response,
        bool alreadyAccepted) =>
        new(true, response, [], AcceptBusinessManagerInvitationFailureKind.None, alreadyAccepted);

    public static AcceptBusinessManagerInvitationHandlerResult Failure(
        string[] errors,
        AcceptBusinessManagerInvitationFailureKind failureKind) =>
        new(false, null, errors, failureKind, false);
}
