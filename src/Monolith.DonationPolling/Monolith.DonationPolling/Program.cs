using Monolith.DonationPolling.PollDonations;
using Serilog;
using Serilog.Events;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);


// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .WriteTo.Console(
            outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            "logs/log-.txt",
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] ({SourceContext}) {Message:lj} {Properties:j}{NewLine}{Exception}",            rollingInterval: RollingInterval.Day,
            restrictedToMinimumLevel: LogEventLevel.Information);
});



builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddJsonFile("secrets.json", optional: true)
    .AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpLogging();
builder.Services.AddHttpClient();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<DonationPlatformClientFactory>();
builder.Services.AddTransient<PollDonationService>();
builder.Services.AddHostedService<PollDonationsBackgroundService>();

var app = builder.Build();


// --- STARTUP LOGGING ---
var env = app.Environment;
var assembly = Assembly.GetExecutingAssembly();
var version = assembly.GetName().Version?.ToString() ?? "Unknown";

Log.Information("------------------------------------------------------------");
Log.Information("Starting application {AppName}", assembly.GetName().Name);
Log.Information("Version: {Version}", version);
Log.Information("Environment: {Environment}", env.EnvironmentName);
Log.Information("ContentRootPath: {ContentRoot}", env.ContentRootPath);
Log.Information("WebRootPath: {WebRoot}", env.WebRootPath);
Log.Information("OS: {OS}", Environment.OSVersion);
Log.Information(".NET Version: {DotNetVersion}", Environment.Version);
Log.Information("------------------------------------------------------------");

// Request logging middleware (VERY IMPORTANT)
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


