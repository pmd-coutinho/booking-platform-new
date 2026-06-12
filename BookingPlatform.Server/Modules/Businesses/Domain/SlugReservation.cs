namespace BookingPlatform.Server.Modules.Businesses.Domain;

public class SlugReservation
{
    public string Id { get; set; } = string.Empty; // slug
    public Guid BusinessId { get; set; }
}
