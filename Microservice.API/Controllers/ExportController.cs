using System.Linq;
using Microservice.Business.Business;
using Microservice.Business.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microservice.API.Controllers
{
    [Route("api")]
    public class ExportController : ControllerBase
    {
        private readonly IExportManagement _exportManagement;

        public ExportController(IExportManagement exportManagement)
        {
            _exportManagement = exportManagement;
        }

        [HttpPost]
        [Route("request/add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ApiExplorerSettings(GroupName = "Request")]
        public IActionResult AddRequest([FromBody] RequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(
                    ModelState.Values.SelectMany(
                        v => v.Errors.Select(
                            b => b.ErrorMessage)));

            _exportManagement.AddRequest(model, out string errorMessage);

            if (errorMessage is not null)
                return BadRequest(errorMessage);

            return Ok();
        }

    }
}
