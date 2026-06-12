using BookingPlatform.Server.Modules.Businesses.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace BookingPlatform.Server.Modules.Businesses.Features.CompleteBusinessProfile;

public static class CompleteBusinessProfileEndpoint
{
    [Tags("Businesses")]
    [EndpointName("CompleteBusinessProfile")]
    [EndpointSummary("Complete the business profile.")]
    [EndpointDescription("Completes the business profile with public identity, contact, address, timezone, and currency. Reserves the public booking slug and updates bookability.")]
    [ProducesResponseType<CompleteBusinessProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [WolverinePost("/api/businesses/{businessId}/profile")]
    public static async Task<IResult> Post(
        Guid businessId,
        CompleteBusinessProfileRequest request,
        CompleteBusinessProfileHandler handler)
    {
        var result = await handler.Handle(businessId, request);

        if (result.IsSuccess)
        {
            return Results.Ok(result.Response);
        }

        return result.FailureKind switch
        {
            CompleteBusinessProfileFailureKind.NotFound => Results.Problem(
                title: "Business not found.",
                detail: result.Errors.FirstOrDefault(),
                statusCode: StatusCodes.Status404NotFound),
            CompleteBusinessProfileFailureKind.Conflict => Results.Problem(
                title: "Business profile cannot be completed.",
                detail: result.Errors.FirstOrDefault(),
                statusCode: StatusCodes.Status409Conflict),
            CompleteBusinessProfileFailureKind.BadRequest => Results.Problem(
                title: "Business profile cannot be completed.",
                detail: result.Errors.FirstOrDefault(),
                statusCode: StatusCodes.Status400BadRequest),
            _ => Results.Problem(statusCode: StatusCodes.Status400BadRequest)
        };
    }
}

public record CompleteBusinessProfileRequest(
    string PublicBusinessName,
    string PublicBookingSlug,
    string ContactPhone,
    string ContactEmail,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string TimeZone,
    string Currency);

public record CompleteBusinessProfileResponse(
    Guid BusinessId,
    string PublicBookingSlug,
    string BookabilityStatus,
    string[] BookabilityReasons);

public class CompleteBusinessProfileRequestValidator : AbstractValidator<CompleteBusinessProfileRequest>
{
    public CompleteBusinessProfileRequestValidator()
    {
        RuleFor(x => x.PublicBusinessName)
            .NotEmpty()
            .WithMessage("Public business name is required.");

        RuleFor(x => x.PublicBookingSlug)
            .NotEmpty()
            .WithMessage("Public booking slug is required.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .WithMessage("Contact phone is required.");

        RuleFor(x => x.ContactEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Contact email is required.")
            .EmailAddress()
            .WithMessage("Contact email must be a valid email address.");

        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("Street is required.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required.");

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .WithMessage("Postal code is required.");

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country is required.");

        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .WithMessage("Time zone is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.");
    }
}
