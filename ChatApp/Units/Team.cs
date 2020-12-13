using ChatApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatApp.Units
{
    public class Team 
    {
        private readonly Agent[][] _arrayOfAgents;
        private readonly Dictionary<Guid, Agent> _mapSessionAgent = new Dictionary<Guid, Agent>();
        private readonly object _syncObj = new object();
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

        public double Capacity => (_arrayOfAgents.Where(agents => agents != null)
                                                   .Sum(agents => agents.Sum(a => a.Capacity)));

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
            var agent = GetTheLeastBusyAgent(agents);// Real load balancer is here instead of the pure round robin
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
}
