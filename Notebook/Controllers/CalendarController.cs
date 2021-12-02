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
    public class CalendarController : ControllerBase
    {
        [HttpPost]
        [ActionName("getcalendar")]
        public IActionResult Post([FromBody] CalendarRequest c)
        {
            int user_id;
            string username;

            if (!Auth.ValidateSession(c.session_id, out user_id, out username)) return StatusCode(403);

            return Ok(Calendar.getCalendar(user_id, c));
        }
    }
}
