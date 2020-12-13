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
        int WaitingSessionsCount { get; }

        Action<UserSession> OnExpiredSession { get; set; }
        bool TryGetSession(Guid sessionId, out UserSession session);
        UserSession GetNextWaitingSession();
        public void Add(UserSession newSession);
        IEnumerable<UserSession> DequeueAll();
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
        private readonly ITimeProvider _timeProvider;
        public Action<UserSession> OnExpiredSession { get; set; }

        public SessionQueue(ILogger<SessionQueue> logger, ITimeProvider timeProvider) 
        {
            _monitorTimer = new Timer(ExpiredSessionMonitor);
            _logger = logger;
            _timeProvider = timeProvider;
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

        public int WaitingSessionsCount
        {
            get
            {
                lock (_syncObj)
                {
                    return _mappedSessions.Values
                        .Where(x => x.Value.Status == SessionStatus.Waiting)
                        .Count();
                }
            }
        }
        public bool TryGetSession(Guid sessionId, out UserSession session) 
        {
            session = null;
            lock (_syncObj)
            {
                if (_mappedSessions.TryGetValue(sessionId, out var node))
                {
                    session = node.Value;
                    return true;
                }
                return false;
            }
        }
        private void ExpiredSessionMonitor(object _)
        {
            _monitorTimer.Change(-1, -1); //stop monitoring to avoid monitoring threads duplication
            try
            {
                lock (_syncObj)
                {
                    var oldSessions = _userSessions.Where(x => (_timeProvider.CurrentTime - x.LastUpdated) > _oldSessionTreshold)
                        .ToArray();
                    foreach (var session in oldSessions)
                    {
                        _logger.LogInformation($"The session '{session}' is expired. Current time: {_timeProvider.CurrentTime}.");
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
                //According to FIFO get first Waiting session
                return _userSessions.FirstOrDefault(x => x.Status == SessionStatus.Waiting);
            }
        }

        public void Add(UserSession newSession)
        {
            lock (_syncObj) 
            {
                if (!_monitorStarted)
                {
                    _monitorTimer.Change(_monitoringInterval, _monitoringInterval);
                    _monitorStarted = true;
                }
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

        public IEnumerable<UserSession> DequeueAll()
        {
            lock (_syncObj)
            {
                var queue = new Queue<UserSession>();
                _mappedSessions.Clear();
                foreach (var session in _userSessions) 
                {
                    queue.Enqueue(session);
                }
                _userSessions.Clear();
                return queue;
            }
        }
    }
}
