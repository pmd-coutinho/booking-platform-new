namespace BookingPlatform.Server.Modules.Businesses.Domain;

public class Business
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BookabilityStatus { get; set; } = string.Empty;
    public string[] BookabilityReasons { get; set; } = [];
}
