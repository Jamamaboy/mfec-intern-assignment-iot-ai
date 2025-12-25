using EnergyMonitorAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers();

// 🔥 Register MQTT Background Worker
builder.Services.AddHostedService<MqttWorkerService>();

var app = builder.Build();

app.UseRouting();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();
