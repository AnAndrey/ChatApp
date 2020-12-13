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
        TeamManager _teamManager = null;
        private readonly ISessionQueue _sessionQueue;
        private readonly IShiftManager _shiftManager;

        public SessionManager(ISessionQueue sessionQueue, IShiftManager shiftManager) 
        {
            _sessionQueue = sessionQueue;
            _shiftManager = shiftManager;
            _sessionQueue.OnExpiredSession = OnExpiredSession;
            _teamManager = new TeamManager(shiftManager);
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
#warning !!!!
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
        private readonly IShiftManager _shiftManager;

        public int TotalCapacity { get; internal set; } = 5;

        public TeamManager(IShiftManager shiftManager) 
        {
            _shiftManager = shiftManager;
        }
        internal bool TryAssignSessionToAgent(UserSession newSession)
        {
            var team = _shiftManager.GetCurrentTeam();
            if (team == null) 
                return false;

            var res = team.TryAssignSessionToAgent(newSession); 
            if (!res && _shiftManager.IsOverflowTeamAvailable())
            {
                team = _shiftManager.GetOverflowTeam();
                res = team.TryAssignSessionToAgent(newSession);
            }
            return res;
        }

        internal bool TryAssignSessionToOverflowAgent(UserSession awaitingSession)
        {
            throw new NotImplementedException();
        }

        internal void UnAssignSession(UserSession session)
        {
            var team = _shiftManager.GetCurrentTeam();
            if (team == null) 
                return;

            var res = team.TryUnAssignSession(session);
            if (!res) 
            {
                //TODO: check Overflow
            }
        }
    }
    public class Team 
    {
        private readonly Agent[][] _arrayOfAgents;
        private readonly Dictionary<Guid, Agent> _mapSessionAgent = new Dictionary<Guid, Agent>();
        private readonly Object _syncObj = new object();
        //private Agent[] _juniors;
        //private Agent[] _middles;
        //private Agent[] _seniors;
        //private Agent[] _teamleads;

        public Team(Agent[] juniors, Agent[] middles, Agent[] seniors, Agent[] teamleads)
        {
            // according to requirements
            // assign the junior first, then mid, then senior etc
            _arrayOfAgents = new []{ juniors, middles, seniors, teamleads};
            //_juniors = juniors;
            //_middles = middles;
            //_seniors = seniors;
            //_teamleads = teamleads;
        }

        public bool TryAssignSessionToAgent(UserSession newSession) 
        {
            lock (_syncObj)
            {
                for (int i = 0; i < _arrayOfAgents.Count(); i++)
                {
                    if (_arrayOfAgents[i] == null)
                        continue;

                    if (AssignSessionToAgent(newSession, _arrayOfAgents[i]))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool TryUnAssignSession(UserSession newSession)
        {
            lock (_syncObj)
            {
                if (_mapSessionAgent.TryGetValue(newSession.SessionId, out var agent))
                {
                    agent.FinishProccessing(newSession);
                    _mapSessionAgent.Remove(newSession.SessionId);
                    return true;
                }
                return false;
            }
        }

        private bool AssignSessionToAgent(UserSession newSession, IEnumerable<Agent> agents) 
        {
            if (agents == null || !agents.Any())
                return false;
            var agent = GetTheLeastBusyAgent(agents);// Real load balancer here instead of the pure round robin
            if (agent != null)
            {
                if (agent.Proccess(newSession))
                {
                    _mapSessionAgent[newSession.SessionId] = agent;
                    return true;
                }
            }
            return false;
        }

        private Agent GetTheLeastBusyAgent(IEnumerable<Agent> agents)
        {
            var minSessionsCount = int.MaxValue;
            Agent theLeastBusyAgent = agents.FirstOrDefault();
            foreach (var agent in agents) 
            {
                if (agent.ProcessingSessionsCount < minSessionsCount)
                {
                    minSessionsCount = agent.ProcessingSessionsCount;
                    theLeastBusyAgent = agent;
                }
            }
            //TODO: implement Agent.IsFull property for the better code readability
            if (theLeastBusyAgent ==null || minSessionsCount >= theLeastBusyAgent.Capacity)//if the Least Busy Agent is full
                return null;

            return theLeastBusyAgent;
        }
    }
    public class Agent
    {
        public AgentType AgentType { get;}
        public double Capacity { get; }
        private UserSession[] _sessions;
        private Object _syncObj = new object();
        public Agent(AgentType type, int capacity) 
        {
            AgentType = type;
            Capacity = capacity;
            _sessions = new UserSession[capacity];
        }

        public int ProcessingSessionsCount { get; private set; }

        public bool Proccess(UserSession session) 
        {
            lock (_syncObj)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    if (_sessions[i] == null)
                    {
                        _sessions[i] = session;
                        session.AgentInfo = AgentType.ToString();
                        ProcessingSessionsCount++;
                        return true;
                    }
                }
            }
            return false;
        }

        public void FinishProccessing(UserSession session)
        {
            lock (_syncObj)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    if (_sessions[i] == null)
                        continue;

                    if (_sessions[i].SessionId == session.SessionId)
                    {
                        _sessions[i].AgentInfo = string.Empty;
                        _sessions[i] = null;
                        ProcessingSessionsCount--;
                        break;
                    }
                }
            }
            return;
        }
    }
}
