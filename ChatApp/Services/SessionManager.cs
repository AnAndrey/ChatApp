using System;
using System.Threading;
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
            _shiftManager.OnTransition = OnTransition;
            _sessionQueue.OnExpiredSession = OnExpiredSession;
            _teamManager = teamManager;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        private void OnTransition() 
        {
            _lock.EnterWriteLock();
            try
            {
                // - unassign all sessions
                // - get all sessions from the queue
                // - assing session to the new team and keep the sessions order the same
                _teamManager.UnAssignAllSession();
                var sessions =_sessionQueue.DequeueAll();
                foreach (var session in sessions)
                {
                    session.Status = SessionStatus.None;
                    session.AgentInfo = null;
                    StoreSession(session);
                }
            }
            finally 
            {
                _lock.ExitWriteLock();
            }
        }
        private void OnExpiredSession(UserSession session) 
        {
            _lock.EnterReadLock();
            try
            {
                _teamManager.UnAssignSession(session);
                var awaitingSession = _sessionQueue.GetNextWaitingSession();
                if (awaitingSession != null && _teamManager.TryAssignSessionToAgent(awaitingSession))
                {
                    awaitingSession.Status = SessionStatus.Working;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        public UserSession CheckSession(Guid sessionId)
        {
            _lock.EnterReadLock();
            try
            {
                if (_sessionQueue.TryGetSession(sessionId, out var session))
                {
                    session.LastUpdated = _timeProvider.CurrentTime;
                    return session;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        public UserSession CreateSession()
        {
            _lock.EnterReadLock();
            try
            {
                var newSession = new UserSession() { LastUpdated = _timeProvider.CurrentTime };
                return StoreSession(newSession);
            }
            finally
            {
                _lock.ExitReadLock();
            }

        }
        private UserSession StoreSession(UserSession newSession) 
        {
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
