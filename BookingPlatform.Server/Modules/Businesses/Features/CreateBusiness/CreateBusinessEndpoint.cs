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
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var status = "Unbookable";
        var reasons = new[] { "ManagerNotAccepted", "OnboardingIncomplete" };

        var events = new object[]
        {
            new BusinessCreated(businessId, request.BusinessName),
            new BusinessManagerInvited(invitationId, request.ManagerEmail, request.InvitationExpiresAt),
            new BusinessBookabilityChanged(status, reasons)
        };

        var start = StartStream<Business>(businessId, events);

        return (new CreateBusinessResponse(businessId, invitationId, status, reasons), start);
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
