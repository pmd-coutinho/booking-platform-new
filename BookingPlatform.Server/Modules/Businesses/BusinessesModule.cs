using BookingPlatform.Server.Modules.Businesses.Features.AcceptBusinessManagerInvitation;
using BookingPlatform.Server.Modules.Businesses.Features.CompleteBusinessProfile;
using BookingPlatform.Server.Modules.Businesses.Features.CreateRegisteredBusiness;
using FluentValidation;

namespace BookingPlatform.Server;

public static class BusinessesModule
{
    public static IServiceCollection AddBusinessesModule(this IServiceCollection services)
    {
        services.AddScoped<CreateRegisteredBusinessHandler>();
        services.AddScoped<AcceptBusinessManagerInvitationHandler>();
        services.AddScoped<CompleteBusinessProfileHandler>();
        services.AddScoped<IValidator<CreateRegisteredBusinessRequest>, CreateRegisteredBusinessRequestValidator>();
        services.AddScoped<IValidator<AcceptBusinessManagerInvitationRequest>, AcceptBusinessManagerInvitationRequestValidator>();
        services.AddScoped<IValidator<CompleteBusinessProfileRequest>, CompleteBusinessProfileRequestValidator>();

        return services;
    }
}
