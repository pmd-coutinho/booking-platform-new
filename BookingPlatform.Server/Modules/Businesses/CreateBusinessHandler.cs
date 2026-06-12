using BookingPlatform.Server.Modules.Businesses.Domain;
using JasperFx.Events;
using Wolverine.Http;
using Wolverine.Marten;
using static Wolverine.Marten.MartenOps;

namespace BookingPlatform.Server.Modules.Businesses;

public static class CreateBusinessHandler
{
    public static (CreateBusinessResponse, IStartStream) Handle(
        CreateBusinessResult result)
    {
        var bookability = (BusinessBookabilityChanged)result.Events[2];

        var events = new IEvent[]
        {
            ((BusinessCreated)result.Events[0]).AsEvent()
                .WithHeader("actor-role", result.Actor.Role)
                .WithHeader("actor-identity", result.Actor.Identity),
            ((BusinessManagerInvited)result.Events[1]).AsEvent()
                .WithHeader("actor-role", result.Actor.Role)
                .WithHeader("actor-identity", result.Actor.Identity),
            ((BusinessBookabilityChanged)result.Events[2]).AsEvent()
                .WithHeader("actor-role", result.Actor.Role)
                .WithHeader("actor-identity", result.Actor.Identity)
        };

        var start = StartStream<Business>(result.BusinessId, events);

        return (
            new CreateBusinessResponse(result.BusinessId, result.InvitationId, bookability.Status, bookability.Reasons),
            start
        );
    }
}

public record CreateBusinessResponse(
    Guid BusinessId,
    Guid InvitationId,
    string BookabilityStatus,
    string[] BookabilityReasons);
