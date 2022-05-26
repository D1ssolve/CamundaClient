using Zeebe.Client.Api.Responses;

namespace CamundaClient.Services;

public interface ICamundaService
{
    public Task<IDeployResponse> Deploy(string modelFilename);
    public Task<string> StartWorkflowInstance(string bpmProcessId);
    public void StartWorkers();
    public Task<ITopology> Status();
  
}