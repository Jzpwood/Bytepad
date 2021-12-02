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
    public class LoginController : ControllerBase
    {
        [HttpPost]
        [ActionName("trylogin")]
        public IActionResult Get([FromBody] LoginRequest l)
        {
            return Ok(Auth.TryLogin(l.login_username, l.login_password));
        }

        // POST: api/Login
        [HttpPost("{s}")]
        [ActionName("logout")]
        public IActionResult Post([FromRoute] string s)
        {
            Auth.Logout(s);
            return Ok();
        }

        // POST: api/Login
        [HttpGet("{s}")]
        [ActionName("checksession")]
        public IActionResult Get([FromRoute] string s)
        {
            int user_id;

            if (Auth.ValidateSession(s, out user_id, out _))
            {
                Event.Log(ApplicationEvent.UserLogin_Auto, $"session_id:{s},user_id:{user_id}");
                return Ok("valid");
            }
            else
            {
                return Ok("invalid");
            }
        }
    }
}
