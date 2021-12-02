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
    public class CollectionsController : ControllerBase
    {
        // POST: api/Collections
        [HttpPost]
        [ActionName("commitcollection")]
        public IActionResult Post([FromBody] CollectionCommit c)
        {
            return Ok(Collections.AddUpdateCollection(c));
        }
    }
}
