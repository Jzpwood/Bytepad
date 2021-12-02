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
    public class LockController : ControllerBase
    {
        [HttpPost]
        [ActionName("request")]
        public IActionResult Post([FromBody] LockRequestSession r)
        {
            return Ok(Lock.RequestSession(r));
        }
    }
}
