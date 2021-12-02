using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Notebook.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ShareController : ControllerBase
    {
        [HttpPost]
        [ActionName("validate")]
        public IActionResult Post([FromBody] UsrValidate v)
        {
            return Ok(Share.ValidateUser(v));
        }
    }
}
