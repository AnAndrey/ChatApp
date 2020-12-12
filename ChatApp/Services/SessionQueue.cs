using ChatApp.Enums;
using ChatApp.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ChatApp.Services
{
    public interface ISessionQueue: IDisposable
    {
        int TotalSessionsCount { get; }
        Action<UserSession> OnExpiredSession { get; set; }
        bool TryGetSession(Guid sessionId, out UserSession session);
        UserSession GetNextWaitingSession();
        public void Add(UserSession newSession);
    }
    public class SessionQueue: ISessionQueue
    {
        
        private Dictionary<Guid, DoubleLinkedListNode<UserSession>> _mappedSessions= new Dictionary<Guid, DoubleLinkedListNode<UserSession>>(); //for fast search
        private DoubleLinkedList<UserSession> _userSessions = new DoubleLinkedList<UserSession>(); // as a FIFO collection
        object _syncObj = new object();
        Timer _monitorTimer = null;
        bool _monitorStarted = false;
        private static readonly TimeSpan _monitoringInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan _oldSessionTreshold = TimeSpan.FromSeconds(3);
        private readonly ILogger<SessionQueue> _logger;

        public SessionQueue(ILogger<SessionQueue> logger) 
        {
            _monitorTimer = new Timer(ExpiredSessionMonitor);
            _logger = logger;
        }
        public int TotalSessionsCount 
        { 
            get 
            {
                lock (_syncObj) {
                    return _mappedSessions.Count;
                }
            }
        }

        public Action<UserSession> OnExpiredSession { get; set; }
        public bool TryGetSession(Guid sessionId, out UserSession session) 
        {
            lock (_syncObj)
            {
                var res = _mappedSessions.TryGetValue(sessionId, out var node);
                session = node.Value;
                return res;
            }
        }
        private void ExpiredSessionMonitor(object _)
        {
            _monitorTimer.Change(-1, -1); //stop monitoring to avoid monitoring threads duplication
            try
            {
                lock (_syncObj)
                {
                    var oldSessions = _userSessions.Where(x => (DateTime.UtcNow - x.LastUpdated) > _oldSessionTreshold);
                    foreach (var session in oldSessions)
                    {
                        session.Status = SessionStatus.Refused;
                        if (_mappedSessions.TryGetValue(session.SessionId, out var node))
                        {
                            _userSessions.Remove(node);
                            _mappedSessions.Remove(session.SessionId);
                            OnExpiredSession?.Invoke(session);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to check expired user session.");
            }
            _monitorTimer.Change(_monitoringInterval, _monitoringInterval);//start monitor again
        }
        public UserSession GetNextWaitingSession()
        {
            lock (_syncObj) 
            {
                return _userSessions.FirstOrDefault(x => x.Status == SessionStatus.Waiting);
            }
        }

        public void Add(UserSession newSession)
        {
            lock (_syncObj) 
            {
                if (!_monitorStarted)
                    _monitorTimer.Change(_monitoringInterval, _monitoringInterval);
                var node = _userSessions.AddLast(newSession);
                _mappedSessions.Add(node.Value.SessionId, node);
            }
        }

        public void Dispose()
        {
            if (_monitorTimer != null)
                _monitorTimer.Dispose();
            _monitorTimer = null;
        }
    }
}
