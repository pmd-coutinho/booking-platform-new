using Wolverine.Http;

namespace BookingPlatform.Server.Modules.Bookings.Features.CreateBooking;

public static class CreateBookingEndpoint
{
    [WolverinePost("/api/bookings")]
    public static IResult Post(CreateBookingRequest request)
    {
        var bookingId = Guid.NewGuid();
        return Results.Ok(new CreateBookingResponse(bookingId, request.CustomerName, request.Service, request.BookingDate));
    }
}

public record CreateBookingRequest(string CustomerName, string Service, DateTime BookingDate);
public record CreateBookingResponse(Guid Id, string CustomerName, string Service, DateTime BookingDate);
