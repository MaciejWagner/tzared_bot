using TzarBot.Dashboard.Hubs;
using TzarBot.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

// Configure training service based on settings
var useMockService = builder.Configuration.GetValue<bool>("Training:UseMockService", true);

if (useMockService)
{
    builder.Services.AddSingleton<ITrainingStateService, MockTrainingService>();
}
else
{
    builder.Services.AddSingleton<ITrainingStateService, TrainingStateService>();
}

// Register the SignalR broadcasting service
builder.Services.AddSingleton<TrainingBroadcaster>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TrainingBroadcaster>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<TrainingHub>("/traininghub");
app.MapFallbackToPage("/_Host");

app.Run();
