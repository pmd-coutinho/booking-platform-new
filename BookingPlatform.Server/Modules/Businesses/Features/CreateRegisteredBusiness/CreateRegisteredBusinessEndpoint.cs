using BookingPlatform.Server.Modules.Businesses.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Wolverine.Marten;

namespace BookingPlatform.Server.Modules.Businesses.Features.CreateRegisteredBusiness;

public static class CreateRegisteredBusinessEndpoint
{
    [Tags("Businesses")]
    [EndpointName("CreateRegisteredBusiness")]
    [EndpointSummary("Create a registered business and invite its first manager.")]
    [EndpointDescription("Creates a registered business, creates a business manager invitation, and leaves the business unbookable until onboarding is completed.")]
    [ProducesResponseType<CreateRegisteredBusinessResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [WolverinePost("/api/businesses")]
    public static async Task<(CreateRegisteredBusinessResponse, IStartStream)> Post(
        CreateRegisteredBusinessRequest request,
        HttpContext httpContext,
        CreateRegisteredBusinessHandler handler)
    {
        var actor = new ActorContext(
            httpContext.Request.Headers.TryGetValue("X-Actor-Role", out var role) ? role.ToString() : "Unknown",
            httpContext.Request.Headers.TryGetValue("X-Actor-Identity", out var identity) ? identity.ToString() : "Unknown");

        var result = Business.Create(
            request.BusinessName,
            request.ManagerEmail,
            request.InvitationExpiresAt,
            actor);

        return await handler.Handle(result);
    }
}

public record CreateRegisteredBusinessRequest(
    string BusinessName,
    string ManagerEmail,
    DateTimeOffset InvitationExpiresAt);

public record CreateRegisteredBusinessResponse(
    Guid BusinessId,
    Guid InvitationId,
    string BookabilityStatus,
    string[] BookabilityReasons) : CreationResponse($"/api/businesses/{BusinessId}");

public class CreateRegisteredBusinessRequestValidator : AbstractValidator<CreateRegisteredBusinessRequest>
{
    public CreateRegisteredBusinessRequestValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .WithMessage("Business name is required.");

        RuleFor(x => x.ManagerEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Manager email is required.")
            .EmailAddress()
            .WithMessage("Manager email must be a valid email address.");

        RuleFor(x => x.InvitationExpiresAt)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("Invitation expiration must be in the future.")
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow.AddDays(30))
            .WithMessage("Invitation expiration cannot be more than 30 days from now.");
    }
}
