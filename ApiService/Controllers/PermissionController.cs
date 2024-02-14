using System.Net;
using ApiService.Domain.Messages;
using ApiService.Domain.UseCases.Permissions.Commands;
using ApiService.Domain.UseCases.Permissions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers;

[ApiController]
[Route("Api/[controller]")]
[Produces("application/json")]
public class PermissionController : ControllerBase
{
    private readonly ILogger<PermissionController> _logger;
    private readonly IMediator _mediator;

    public PermissionController(ILogger<PermissionController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PermissionRecord>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetAllPermissions()
    {
        _logger.LogInformation("Starting 'GetAllPermissions' on 'PermissionController'");

        var result = await _mediator.Send(new GetPermissionsQuery());

        _logger.LogInformation("Ending GetAllPermissions on Permission Controller");

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PermissionRecord), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> RequestPermission([FromBody] CreatePermissionCommand command)
    {
        _logger.LogInformation("Starting 'RequestPermission' on 'PermissionController'");

        var newPermission = await _mediator.Send(command);

        _logger.LogInformation("Ending 'RequestPermission' on 'PermissionController'");

        return Created("Permission", newPermission);
    }

    [HttpPut]
    [ProducesResponseType(typeof(PermissionRecord), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    [Route("{Id:int}")]
    public async Task<IActionResult> UpdatePermission([FromRoute] int Id, [FromBody] UpdateBodyPermissionCommand command)
    {
        _logger.LogInformation("Starting 'UpdatePermission' on 'PermissionController'");

        var fullCommand = new UpdatePermissionCommand()
        {
            Id = Id,
            Name = command.Name
        };

        var updatedPermission = await _mediator.Send(fullCommand);

        _logger.LogInformation("Ending 'UpdatePermission' on 'PermissionController'");

        return (updatedPermission is null)
            ? NotFound()
            : Ok(updatedPermission);
    }
}

