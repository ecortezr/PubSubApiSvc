using ApiService.ActionFilters;
using ApiService.Domain.Utils;
using ApiService.Infrastructure.Utils;
using Serilog;
using Serilog.Events;

namespace ApiService;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Create Serilog logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File("logs/response-time-log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        // Add services to the container.

        builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.PropertyNamingPolicy = null
        );

        // Adding Serilog
        builder.Host.UseSerilog();

        // Add filter to log controller actions
        builder.Services.AddScoped<LogActionFilter>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add domain elements
        builder.Services.AddDomain(configuration);

        // Add infraestructure services
        switch (builder.Environment.EnvironmentName)
        {
            case "UnitTesting":
                builder.Services.AddInfrastructureForUnitTests(configuration);
                break;

            case "IntegrationTesting":
                builder.Services.AddInfrastructureForIntegrationTests(configuration);
                break;

            default:
                builder.Services.AddInfrastructure(configuration);
                break;
        }

        // Configure Kafka from config
        builder.Services.Configure<KafkaOptions>(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}