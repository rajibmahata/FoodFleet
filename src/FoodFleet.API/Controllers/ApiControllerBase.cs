using FoodFleet.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult ToActionResult<T>(Result<T> result) => result.StatusCode switch
    {
        200 => Ok(result.Data),
        201 => StatusCode(201, result.Data),
        404 => NotFound(new { message = result.Error }),
        401 => Unauthorized(new { message = result.Error }),
        403 => Forbid(),
        409 => Conflict(new { message = result.Error }),
        422 => UnprocessableEntity(new { message = result.Error }),
        _ => BadRequest(new { message = result.Error })
    };

    protected IActionResult ToActionResult(Result result) => result.StatusCode switch
    {
        200 or 201 => Ok(),
        404 => NotFound(new { message = result.Error }),
        401 => Unauthorized(new { message = result.Error }),
        403 => Forbid(),
        422 => UnprocessableEntity(new { message = result.Error }),
        _ => BadRequest(new { message = result.Error })
    };
}
