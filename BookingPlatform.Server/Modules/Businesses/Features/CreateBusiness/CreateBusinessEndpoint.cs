using Wolverine.Http;
using Wolverine.Marten;
using BookingPlatform.Server.Modules.Businesses.Domain;
using static Wolverine.Marten.MartenOps;

namespace BookingPlatform.Server.Modules.Businesses.Features.CreateBusiness;

public static class CreateBusinessEndpoint
{
    public static string[] Validate(CreateBusinessRequest request)
    {
        var result = Business.Create(
            request.BusinessName,
            request.ManagerEmail,
            request.InvitationExpiresAt);

        return result.IsSuccess ? [] : result.Errors;
    }

    [WolverinePost("/api/businesses")]
    public static (CreateBusinessResponse, IStartStream) Post(CreateBusinessRequest request)
    {
        var result = Business.Create(
            request.BusinessName,
            request.ManagerEmail,
            request.InvitationExpiresAt);

        var bookability = (BusinessBookabilityChanged)result.Events[2];

        var start = StartStream<Business>(result.BusinessId, result.Events);

        return (new CreateBusinessResponse(result.BusinessId, result.InvitationId, bookability.Status, bookability.Reasons), start);
    }
}

public record CreateBusinessRequest(
    string BusinessName,
    string ManagerEmail,
    DateTimeOffset InvitationExpiresAt);

public record CreateBusinessResponse(
    Guid BusinessId,
    Guid InvitationId,
    string BookabilityStatus,
    string[] BookabilityReasons);
