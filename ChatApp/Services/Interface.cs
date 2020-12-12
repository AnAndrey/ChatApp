using ChatApp.Enums;
using ChatApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Services.Interfaces
{
    public interface ISessionManager
    {
        UserSession CreateSession();
        UserSession CheckSession(Guid sessionId);   
    }

    public class SessionManager : ISessionManager
    {
        TeamManager _teamManager = new TeamManager();
        private readonly ISessionQueue _sessionQueue;
        private readonly IShiftManager _shiftManager;

        public SessionManager(ISessionQueue sessionQueue, IShiftManager shiftManager) 
        {
            _sessionQueue = sessionQueue;
            _shiftManager = shiftManager;
            _sessionQueue.OnExpiredSession = OnExpiredSession;
            //Get awaiting session and assign agent
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
                session.LastUpdated = DateTime.UtcNow;
                return session;
            }
            return null;
        }

        public UserSession CreateSession()
        {
            var newSession = new UserSession() { LastUpdated = DateTime.UtcNow};
            if (_teamManager.TryAssignSessionToAgent(newSession))
            {
                newSession.Status = SessionStatus.Working;
                _sessionQueue.Add(newSession);
                return newSession;
            }

            var waitingSessions = _sessionQueue.TotalSessionsCount; // ??? WaitingSessionsCount or Available slots or TotalItems?
            if (waitingSessions >= _teamManager.TotalCapacity)
            {
                if (_shiftManager.IsOverflowTeamAvailable())
                {
                    var awaitingSession = _sessionQueue.GetNextWaitingSession(); //reduce queue
                    if (awaitingSession != null && _teamManager.TryAssignSessionToOverflowAgent(awaitingSession))
                    {
                        awaitingSession.Status = SessionStatus.Working;
                        //_queue.Queue(awaitingSession);                        
                        newSession.Status = SessionStatus.Waiting;
                        _sessionQueue.Add(newSession);
                        return newSession;
                    }

                }
            }
            else 
            {
                newSession.Status = SessionStatus.Waiting;
                _sessionQueue.Add(newSession);
                return newSession;
            }
            newSession.Status = SessionStatus.Refused;
            return newSession;
        }

    }

    public class TeamManager
    {
        private Dictionary<Guid, Agent> dd = new Dictionary<Guid, Agent>();

        public int TotalCapacity { get; internal set; } = 5;

        internal bool TryAssignSessionToAgent(UserSession newSession)
        {
            dd.Add(newSession.SessionId, new Agent(AgentType.Junior, 1));
            return dd.Count < 2 ? true : false;
        }

        internal bool TryAssignSessionToOverflowAgent(UserSession awaitingSession)
        {
            throw new NotImplementedException();
        }

        internal void UnAssignSession(UserSession session)
        {
            //throw new NotImplementedException();
        }
    }
    public class Team 
    {
        private Agent[] _juniors;
        private Agent[] _middles;
        private Agent[] _seniors;
        private Agent[] _teamleads;

        public Team(Agent[] juniors, Agent[] middles, Agent[] seniors, Agent[] teamleads)
        {
            _juniors = juniors;
            _middles = middles;
            _seniors = seniors;
            _teamleads = teamleads;
        }

    }

    public enum AgentType 
    {
        None,
        Junior,
        Middle,
        Seniour,
        TeamLEad
    }
    public class Agent
    {
        public AgentType AgentType { get;}
        public double Capacity { get; }
        private UserSession[] _sessions;
        public Agent(AgentType type, int capacity) 
        {
            AgentType = type;
            Capacity = capacity;
            _sessions = new UserSession[capacity];
        }

        public bool TryProccess(UserSession session) 
        {
            for (int i = 0; i < Capacity; i++) 
            {
                if (_sessions[i] == null)
                {
                    _sessions[i] = session;
                    return true;
                }
            }
            return false;
        }

        public void FinishProccess(UserSession session)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (_sessions[i].SessionId == session.SessionId)
                {
                    _sessions[i] = null;
                    break;
                }
            }
            return;
        }
    }
}
