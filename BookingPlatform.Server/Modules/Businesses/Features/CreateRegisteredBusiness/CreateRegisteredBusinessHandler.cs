using BookingPlatform.Server.Modules.Businesses.Domain;
using JasperFx.Events;
using Wolverine;
using Wolverine.Marten;
using static Wolverine.Marten.MartenOps;

namespace BookingPlatform.Server.Modules.Businesses.Features.CreateRegisteredBusiness;

public class CreateRegisteredBusinessHandler(IMessageBus bus)
{
    public async Task<(CreateRegisteredBusinessResponse, IStartStream)> Handle(CreateRegisteredBusinessResult result)
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

        var invited = (BusinessManagerInvited)result.Events[1];
        await bus.ScheduleAsync(
            new ExpireInvitationMessage(result.BusinessId, invited.InvitationId),
            invited.ExpiresAt);

        return (
            new CreateRegisteredBusinessResponse(result.BusinessId, result.InvitationId, bookability.Status, bookability.Reasons),
            start
        );
    }
}
