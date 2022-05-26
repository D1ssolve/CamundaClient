using dotenv.net.Interfaces;
using Newtonsoft.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;
using Zeebe.Client.Impl.Builder;

namespace CamundaClient.Services;

public class CamundaService : ICamundaService
{
    private readonly IZeebeClient _client;
    private readonly ILogger<CamundaService> _logger;
    private readonly IHttpClientFactory _httpClient;

    public CamundaService(IEnvReader envReader, ILogger<CamundaService> logger, IHttpClientFactory httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        var authServer = envReader.GetStringValue("ZEEBE_AUTHORIZATION_SERVER_URL"); 
        var clientId = envReader.GetStringValue("ZEEBE_CLIENT_ID");
        var clientSecret = envReader.GetStringValue("ZEEBE_CLIENT_SECRET");
        var camundaUrl = envReader.GetStringValue("ZEEBE_ADDRESS");
        char[] port = {'4', '3', ':'};
        var audience = camundaUrl?.TrimEnd(port);

        _client =
            ZeebeClient.Builder()
                .UseGatewayAddress(camundaUrl)
                .UseTransportEncryption()
                .UseAccessTokenSupplier(
                    CamundaCloudTokenProvider.Builder()
                        .UseAuthServer(authServer)
                        .UseClientId(clientId)
                        .UseClientSecret(clientSecret)
                        .UseAudience(audience)
                        .Build())
                .Build();
    }
    
    public async Task<String> StartWorkflowInstance(string bpmProcessId)
    {
        var instance = await _client.NewCreateProcessInstanceCommand()
            .BpmnProcessId(bpmProcessId)
            .LatestVersion()
            .WithResult()
            .Send();
        return JsonConvert.SerializeObject(instance);
    }
    
    public void StartWorkers()
    {
        CreateWeatherWorker();
    }

    private void CreateWeatherWorker()
    {
        CreateWorker("get-weather", async (client, job) =>
        {
            _logger.LogInformation("Received job: " + job);
            var httpClient = _httpClient.CreateClient();
            httpClient.BaseAddress = new Uri(Config.ApiBaseUrl);
            var response = await httpClient.GetAsync(Config.GetWeather);
            var apiResponse = await response.Content.ReadAsStringAsync();
                    
            await client.NewCompleteJobCommand(job.Key)
                .Variables("{\"Weather\":" + apiResponse + "}")
                .Send();
        });    
    }
    
    private void CreateWorker(string jobType, JobHandler handleJob)
    {
        _client.NewWorker()
            .JobType(jobType)
            .Handler(handleJob)
            .MaxJobsActive(5)
            .Name(jobType)
            .PollInterval(TimeSpan.FromSeconds(50))
            .PollingTimeout(TimeSpan.FromSeconds(50))
            .Timeout(TimeSpan.FromSeconds(10))
            .Open();
    }
    
    public async Task<IDeployResponse> Deploy(string modelFilename)
    {
        var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", modelFilename);
        try
        {
            var deployment = await _client.NewDeployCommand().AddResourceFile(filename).Send();
            var res = deployment.Processes[0];
            _logger.LogInformation("Deployed BPMN Model: " + res?.BpmnProcessId +
                                   " v." + res?.Version);
            return deployment;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            throw;
        }
    }

    public Task<ITopology> Status()
    {
        return _client.TopologyRequest().Send();
    }
}