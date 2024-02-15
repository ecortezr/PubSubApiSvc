using System.Net;
using ApiService.ActionFilters;
using ApiService.Domain.Messages;
using ApiService.Domain.UseCases.Permissions.Commands;
using ApiService.Domain.UseCases.Permissions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers;

[ApiController]
[Route("Api/[controller]")]
[Produces("application/json")]
[ServiceFilter(typeof(LogActionFilter))]
public class PermissionController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PermissionRecord>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var result = await _mediator.Send(new GetPermissionsQuery());

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PermissionRecord), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> RequestPermission([FromBody] CreatePermissionCommand command)
    {
        var newPermission = await _mediator.Send(command);

        return Created("Permission", newPermission);
    }

    [HttpPut]
    [ProducesResponseType(typeof(PermissionRecord), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    [Route("{Id:int}")]
    public async Task<IActionResult> UpdatePermission([FromRoute] int Id, [FromBody] UpdateBodyPermissionCommand command)
    {
        var fullCommand = new UpdatePermissionCommand()
        {
            Id = Id,
            Name = command.Name
        };

        var updatedPermission = await _mediator.Send(fullCommand);

        return (updatedPermission is null)
            ? NotFound()
            : Ok(updatedPermission);
    }
}

