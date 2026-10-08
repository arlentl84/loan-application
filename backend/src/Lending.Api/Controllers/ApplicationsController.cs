using Lending.Api.Contracts;
using Lending.Application;
using Microsoft.AspNetCore.Mvc;

namespace Lending.Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController(SubmitApplicationHandler handler) : ControllerBase
{
    /// <summary>Submits the loan form. Responds 200 with status Approved or Denied; 400 for invalid input.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubmitApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitApplicationResponse>> Submit(
        SubmitApplicationRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return Ok(SubmitApplicationResponse.From(result));
    }
}
