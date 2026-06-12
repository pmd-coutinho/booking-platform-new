using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;

namespace BookingPlatform.Server.Modules.Businesses.Features.CompleteBusinessProfile;

public class CompleteBusinessProfileHandler(IDocumentSession session)
{
    public async Task<CompleteBusinessProfileHandlerResult> Handle(
        Guid businessId,
        CompleteBusinessProfileRequest request)
    {
        var stream = await session.Events.FetchStreamAsync(businessId);

        if (!stream.Any())
        {
            return CompleteBusinessProfileHandlerResult.Failure(
                ["Business not found."],
                CompleteBusinessProfileFailureKind.NotFound);
        }

        var events = stream.Select(e => e.Data).ToArray();
        var business = Business.Rehydrate(events);

        var slugExists = await session.Query<SlugReservation>().AnyAsync(x => x.Id == request.PublicBookingSlug);

        var result = business.CompleteBusinessProfile(
            request.PublicBusinessName,
            request.PublicBookingSlug,
            request.ContactPhone,
            request.ContactEmail,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.TimeZone,
            request.Currency,
            slugExists);

        if (!result.IsSuccess)
        {
            return CompleteBusinessProfileHandlerResult.Failure(result.Errors, result.FailureKind);
        }

        if (result.Events.Length > 0)
        {
            session.Events.Append(businessId, result.Events);
            session.Store(new SlugReservation
            {
                Id = request.PublicBookingSlug,
                BusinessId = businessId
            });
        }

        return CompleteBusinessProfileHandlerResult.Success(
            new CompleteBusinessProfileResponse(
                businessId,
                request.PublicBookingSlug,
                result.BookabilityStatus,
                result.BookabilityReasons));
    }
}

public record CompleteBusinessProfileHandlerResult(
    bool IsSuccess,
    CompleteBusinessProfileResponse? Response,
    string[] Errors,
    CompleteBusinessProfileFailureKind FailureKind)
{
    public static CompleteBusinessProfileHandlerResult Success(CompleteBusinessProfileResponse response) =>
        new(true, response, [], CompleteBusinessProfileFailureKind.None);

    public static CompleteBusinessProfileHandlerResult Failure(string[] errors, CompleteBusinessProfileFailureKind failureKind) =>
        new(false, null, errors, failureKind);
}
