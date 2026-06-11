using Wolverine.Http;
using Wolverine.Marten;
using BookingPlatform.Server.Modules.Businesses.Domain;
using static Wolverine.Marten.MartenOps;
using JasperFx.Events;

namespace BookingPlatform.Server.Modules.Businesses.Features.CreateBusiness;

public static class CreateBusinessEndpoint
{
    public static string[] Validate(CreateBusinessRequest request)
    {
        var actor = new ActorContext("Unknown", "Unknown");
        var result = Business.Create(
            request.BusinessName,
            request.ManagerEmail,
            request.InvitationExpiresAt,
            actor);

        return result.IsSuccess ? [] : result.Errors;
    }

    [WolverinePost("/api/businesses")]
    public static (CreateBusinessResponse, IStartStream) Post(CreateBusinessRequest request, HttpContext httpContext)
    {
        var actor = new ActorContext(
            httpContext.Request.Headers.TryGetValue("X-Actor-Role", out var role) ? role.ToString() : "Unknown",
            httpContext.Request.Headers.TryGetValue("X-Actor-Identity", out var identity) ? identity.ToString() : "Unknown");

        var result = Business.Create(
            request.BusinessName,
            request.ManagerEmail,
            request.InvitationExpiresAt,
            actor);

        var bookability = (BusinessBookabilityChanged)result.Events[2];

        var events = new IEvent[]
        {
            ((BusinessCreated)result.Events[0]).AsEvent()
                .WithHeader("actor-role", actor.Role)
                .WithHeader("actor-identity", actor.Identity),
            ((BusinessManagerInvited)result.Events[1]).AsEvent()
                .WithHeader("actor-role", actor.Role)
                .WithHeader("actor-identity", actor.Identity),
            ((BusinessBookabilityChanged)result.Events[2]).AsEvent()
                .WithHeader("actor-role", actor.Role)
                .WithHeader("actor-identity", actor.Identity)
        };

        var start = StartStream<Business>(result.BusinessId, events);

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
