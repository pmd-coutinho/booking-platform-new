using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;

namespace BookingPlatform.Server.Modules.Businesses;

public static class ExpireInvitationHandler
{
    public static async Task Handle(
        ExpireInvitationMessage message,
        IDocumentSession session)
    {
        var stream = await session.Events.FetchStreamAsync(message.BusinessId);

        if (!stream.Any())
        {
            return;
        }

        var events = stream.Select(e => e.Data).ToArray();
        var business = Business.Rehydrate(events);

        var result = business.ExpireInvitation(message.InvitationId);

        if (result.Events.Length > 0)
        {
            session.Events.Append(message.BusinessId, result.Events);
        }
    }
}
