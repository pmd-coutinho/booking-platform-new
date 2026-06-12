using BookingPlatform.Server.Modules.Businesses.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace BookingPlatform.Server.Modules.Businesses.Features.AcceptBusinessManagerInvitation;

public static class AcceptBusinessManagerInvitationEndpoint
{
    [Tags("Business Manager Invitations")]
    [EndpointName("AcceptBusinessManagerInvitation")]
    [EndpointSummary("Accept a business manager invitation.")]
    [EndpointDescription("Accepts a pending business manager invitation for the matching manager email and updates the business's bookability reasons.")]
    [ProducesResponseType<AcceptBusinessManagerInvitationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [WolverinePost("/api/businesses/{businessId}/business-manager-invitations/{invitationId}/accept")]
    public static async Task<IResult> Post(
        Guid businessId,
        Guid invitationId,
        AcceptBusinessManagerInvitationRequest request,
        AcceptBusinessManagerInvitationHandler handler)
    {
        var result = await handler.Handle(businessId, invitationId, request.ManagerEmail);

        if (result.IsSuccess)
        {
            return result.AlreadyAccepted
                ? Results.NoContent()
                : Results.Ok(result.Response);
        }

        return result.FailureKind switch
        {
            AcceptBusinessManagerInvitationFailureKind.NotFound => Results.Problem(
                title: "Business manager invitation was not found.",
                detail: "Business manager invitation was not found.",
                statusCode: StatusCodes.Status404NotFound),
            AcceptBusinessManagerInvitationFailureKind.Conflict => Results.Problem(
                title: "Business manager invitation cannot be accepted.",
                detail: result.Errors.FirstOrDefault(),
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status400BadRequest)
        };
    }
}

public record AcceptBusinessManagerInvitationRequest(string ManagerEmail);

public record AcceptBusinessManagerInvitationResponse(
    Guid BusinessId,
    Guid InvitationId,
    string ManagerEmail,
    string BookabilityStatus,
    string[] BookabilityReasons);

public class AcceptBusinessManagerInvitationRequestValidator : AbstractValidator<AcceptBusinessManagerInvitationRequest>
{
    public AcceptBusinessManagerInvitationRequestValidator()
    {
        RuleFor(x => x.ManagerEmail)
            .Cascade(CascadeMode.Stop)
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("Manager email is required.")
            .Must(email => IsValidEmail(email.Trim()))
            .WithMessage("Manager email must be a valid email address.");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return address.Address == email && address.Host.Contains('.');
        }
        catch
        {
            return false;
        }
    }
}
