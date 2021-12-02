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
    public class InboxController : ControllerBase
    {
        [HttpPost("{s}")]
        [ActionName("getinbox")]
        public IActionResult Get([FromRoute] string s)
        {
            int user_id;

            if (!Auth.ValidateSession(s, out user_id, out _)) return StatusCode(403);

            return Ok(Inbox.GetMessages(user_id));
        }

        [HttpPost]
        [ActionName("messageread")]
        public IActionResult Post([FromBody] MsgCheck mc)
        {
            return Ok(Inbox.MarkRead(mc));
        }

    }
}
