using CamundaClient.Services;
using Microsoft.AspNetCore.Mvc;

namespace CamundaClient.Controllers;

public class CamundaController : Controller
{
    private readonly ICamundaService _camundaService;

    public CamundaController(ICamundaService camundaService)
    {
        _camundaService = camundaService;
    }

    [Route("/status")]
    [HttpGet]   
    public async Task<string> Get()
    {
        return (await _camundaService.Status()).ToString();
    }
        
    [Route("/start")]
    [HttpGet]
    public async Task<string> StartWorkflowInstance()
    {
        var instance = await _camundaService.StartWorkflowInstance("Process_16zj8xx");
        return instance;
    }
}