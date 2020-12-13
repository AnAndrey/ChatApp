using System;
using ChatApp.Enums;
using ChatApp.Models;
using ChatApp.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Services
{
    public interface ISessionManager
    {
        UserSession CreateSession();
        UserSession CheckSession(Guid sessionId);   
    }

    public class SessionManager : ISessionManager
    {
        ITeamManager _teamManager = null;
        private readonly ILogger<SessionManager> _logger;
        private readonly ITimeProvider _timeProvider;
        private readonly ISessionQueue _sessionQueue;
        private readonly IShiftManager _shiftManager;

        public SessionManager(ISessionQueue sessionQueue, 
            IShiftManager shiftManager, 
            ITeamManager teamManager,
            ILogger<SessionManager> logger, 
            ITimeProvider timeProvider) 
        {
            _sessionQueue = sessionQueue;
            _shiftManager = shiftManager;
            _sessionQueue.OnExpiredSession = OnExpiredSession;
            _teamManager = teamManager;
            _logger = logger;
            _timeProvider = timeProvider;
        }
        private void OnExpiredSession(UserSession session) 
        {
            _teamManager.UnAssignSession(session);
            var awaitingSession = _sessionQueue.GetNextWaitingSession();
            if (awaitingSession != null && _teamManager.TryAssignSessionToAgent(awaitingSession)) 
            {
                awaitingSession.Status = SessionStatus.Working;
            }
        }
        public UserSession CheckSession(Guid sessionId)
        {
            if (_sessionQueue.TryGetSession(sessionId, out var session))
            {
                session.LastUpdated = _timeProvider.CurrentTime;
                return session;
            }
            return null;
        }

        public UserSession CreateSession()
        {
            var newSession = new UserSession() { LastUpdated = _timeProvider.CurrentTime};
            if (_teamManager.TryAssignSessionToAgent(newSession))
            {
                _logger.LogInformation($"Assigned new session '{newSession.SessionId}' to agent.");
                return QueueSession(newSession, SessionStatus.Working);
            }

            var waitingSessionsCount = _sessionQueue.WaitingSessionsCount;
            if (waitingSessionsCount < _teamManager.WaitingCapacity)
            {
                _logger.LogInformation($"Queued new session '{newSession.SessionId}'.");
                return QueueSession(newSession, SessionStatus.Waiting);
            }

            if (_shiftManager.IsOverflowTeamAvailable())
            {
                // - get the oldest awaiting session from the queue
                // - assign the oldest awaiting to the OverFlowTeam
                // - put incoming session in the queue
                var awaitingSession = _sessionQueue.GetNextWaitingSession(); //reduce queue
                if (awaitingSession != null && _teamManager.TryAssignSessionToOverflowAgent(awaitingSession))
                {
                    _logger.LogInformation($"Overflow. Picked and assigned old session '{awaitingSession.SessionId}' to agent.");
                    _logger.LogInformation($"Overflow. Queued new session '{newSession.SessionId}'.");
                    awaitingSession.Status = SessionStatus.Working;
                    return QueueSession(newSession, SessionStatus.Waiting);
                }
            }
            _logger.LogInformation($"Refused new session '{newSession.SessionId}'.");
            newSession.Status = SessionStatus.Refused;
            return newSession;
        }

        private UserSession QueueSession(UserSession session, SessionStatus status) 
        {
            session.Status = status;
            _sessionQueue.Add(session);
            return session;
        }

    }
}
