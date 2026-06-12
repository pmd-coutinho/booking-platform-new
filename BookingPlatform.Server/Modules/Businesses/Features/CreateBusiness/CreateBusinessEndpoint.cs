using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;
using BookingPlatform.Server.Modules.Businesses.Domain;
using BookingPlatform.Server.Modules.Businesses;

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
    public static async Task<(CreateBusinessResponse, IStartStream)> Post(
        CreateBusinessRequest request,
        HttpContext httpContext,
        IMessageBus bus)
    {
        var actor = new ActorContext(
            httpContext.Request.Headers.TryGetValue("X-Actor-Role", out var role) ? role.ToString() : "Unknown",
            httpContext.Request.Headers.TryGetValue("X-Actor-Identity", out var identity) ? identity.ToString() : "Unknown");

        var result = Business.Create(
            request.BusinessName,
            request.ManagerEmail,
            request.InvitationExpiresAt,
            actor);

        return await CreateBusinessHandler.Handle(result, bus);
    }
}

public record CreateBusinessRequest(
    string BusinessName,
    string ManagerEmail,
    DateTimeOffset InvitationExpiresAt);
