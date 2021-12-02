using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Notebook
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HomepageController : ControllerBase
    {
        [HttpPost("{s}")]
        [ActionName("gethomepage")]
        public IActionResult Get([FromRoute] string s)
        {
            int user_id;
            string username;

            if (!Auth.ValidateSession(s, out user_id, out username)) return StatusCode(403);

            return Ok(Homepage.GetHomepage(user_id, username));
        }

        [HttpPost]
        [ActionName("search")]
        public IActionResult Get([FromBody] searchRequest s)
        {
            return Ok(Search.Get(s));
        }
    }
}
