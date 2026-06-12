using Wolverine.Http;
using BookingPlatform.Server.Modules.Businesses;
using Marten;

namespace BookingPlatform.Server.Modules.Businesses.Features.AcceptInvitation;

public static class AcceptInvitationEndpoint
{
    [WolverinePost("/api/businesses/{businessId}/manager-invitations/{invitationId}/accept")]
    public static async Task<IResult> Post(
        Guid businessId,
        Guid invitationId,
        AcceptInvitationRequest request,
        IDocumentSession session)
    {
        var result = await AcceptInvitationHandler.Handle(
            businessId,
            invitationId,
            request.ManagerEmail,
            session);

        if (!result.IsSuccess)
        {
            return result.BusinessNotFound
                ? Microsoft.AspNetCore.Http.Results.NotFound(result.Errors[0])
                : Microsoft.AspNetCore.Http.Results.BadRequest(new { errors = result.Errors });
        }

        return Microsoft.AspNetCore.Http.Results.Ok(result.Response);
    }
}

public record AcceptInvitationRequest(string ManagerEmail);
