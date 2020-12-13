using ChatApp.Enums;
using ChatApp.Models;
using System;
using System.Linq;

namespace ChatApp.Units
{
    public class Agent
    {
        public AgentType AgentType { get;}
        public double Capacity { get; }
        private UserSession[] _sessions;
        private object _syncObj = new object();
        private readonly string _info;

        public Agent(AgentType type, int capacity, string info = null) 
        {
            AgentType = type;
            Capacity = capacity;
            _info = info;
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
                        session.AgentInfo = _info??AgentType.ToString();
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
        }

        internal void FinishAllProccessing()
        {
            lock (_syncObj)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    if (_sessions[i] == null)
                        continue;

                    _sessions[i].AgentInfo = string.Empty;
                    _sessions[i] = null;
                }
                ProcessingSessionsCount = 0;
            }
        }
    }
}
