using Wolverine.Http;
using Wolverine.Marten;
using BookingPlatform.Server.Modules.Businesses.Domain;
using static Wolverine.Marten.MartenOps;

namespace BookingPlatform.Server.Modules.Businesses.Features.CreateBusiness;

public static class CreateBusinessEndpoint
{
    [WolverinePost("/api/businesses")]
    public static (CreateBusinessResponse, IStartStream) Post(CreateBusinessRequest request)
    {
        var (businessId, invitationId, events) = Business.Create(
            request.BusinessName,
            request.ManagerEmail,
            request.InvitationExpiresAt);

        var bookability = (BusinessBookabilityChanged)events[2];

        var start = StartStream<Business>(businessId, events);

        return (new CreateBusinessResponse(businessId, invitationId, bookability.Status, bookability.Reasons), start);
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
