using ApiService.Domain.UseCases.Permissions.Commands;
using ApiService.Domain.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiService.Domain.Utils;

public static class Extensions
{
    public static IServiceCollection AddDomain(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreatePermissionCommand>());

        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<CreatePermissionCommandValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateBodyPermissionCommandValidator>();

        return services;
    }
}

