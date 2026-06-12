using BookingPlatform.Server.Modules.Businesses;
using BookingPlatform.Server.Modules.Businesses.Domain;
using Wolverine;
using Xunit;

namespace BookingPlatform.Tests.Unit.Modules.Businesses;

public class CreateBusinessHandlerTests
{
    [Fact]
    public async Task Handle_schedules_expiry_message_at_invitation_expiry()
    {
        var result = Business.Create(
            "Acme Salon",
            "manager@acme.com",
            DateTimeOffset.UtcNow.AddDays(7),
            new ActorContext("PlatformAdmin", "admin-123"));

        var context = new TestMessageContext();
        var (response, _) = await CreateBusinessHandler.Handle(result, context);

        var scheduled = context.ScheduledMessages().ShouldHaveEnvelopeForMessageType<ExpireInvitationMessage>();
        var message = Assert.IsType<ExpireInvitationMessage>(scheduled.Message);
        Assert.Equal(result.BusinessId, message.BusinessId);
        Assert.Equal(result.InvitationId, message.InvitationId);

        var invited = (BusinessManagerInvited)result.Events[1];
        Assert.Equal(invited.ExpiresAt, scheduled.ScheduledTime);
    }
}
