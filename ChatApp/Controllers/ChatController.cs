using System;
using ChatApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ITimeProvider _timeProvider;

        private ISessionManager SessionManager { get; }

        public ChatController(ISessionManager sessionManager, ITimeProvider timeProvider)
        {
            SessionManager = sessionManager;
            _timeProvider = timeProvider;
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
                return BadRequest($"The '{nameof(sessionId)}' is invalid."); //TODO: Use error model
            }
            var session = SessionManager.CheckSession(sessionId.Value);
            if (session == null)
                return NotFound($"{_timeProvider.CurrentTime}: '{sessionId}' not found. Probably, session is expired.");

            return Ok(session);

        }

        
    }
}
