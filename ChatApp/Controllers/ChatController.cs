using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private ISessionManager SessionManager { get; }

        public ChatController(ISessionManager sessionManager)
        {
            SessionManager = sessionManager;
        }
        [HttpGet("session")]
        public IActionResult CreateSession() 
        {
            var session = SessionManager.CreateSession();
            return Ok(session);
        }

        [HttpGet("poll")]
        public IActionResult CheckSession(Guid? sessionId)
        {
            if (!sessionId.HasValue)
            {
                return BadRequest($"The {nameof(sessionId)} is invalid."); //TODO: Use error model
            }
            return Ok($"{DateTime.UtcNow}: {sessionId} is ok");
        }
    }
}
