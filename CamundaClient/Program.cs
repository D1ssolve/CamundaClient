using System.Text;
using CamundaClient.Services;
using dotenv.net.DependencyInjection.Microsoft;
using NLog.Web;

var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();

try
{
    logger.Debug("init main");
    
    var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
    builder.Services.AddSingleton<ICamundaService, CamundaService>();
    builder.Services.AddHttpClient();

    builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
//EnvFile
    builder.Services.AddEnv(optionsBuilder => {
        optionsBuilder.AddEnvFile("CamundaCloud.env")
            .AddThrowOnError(false)
            .AddEncoding(Encoding.ASCII);
    });
    builder.Services.AddEnvReader();
// Logging
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(LogLevel.Trace);
    builder.Host.UseNLog();

    var app = builder.Build();

// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    var camundaService = app.Services.GetService<ICamundaService>();
    if (camundaService != null)
    {
        camundaService.Deploy("diagram_1.bpmn");
        camundaService.StartWorkers();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}