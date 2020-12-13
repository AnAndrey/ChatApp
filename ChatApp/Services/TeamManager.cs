using ChatApp.Models;
using System;
using System.Collections.Generic;

namespace ChatApp.Services.Interfaces
{
    public interface ITeamManager
    {
        int WaitingCapacity { get; }

        bool TryAssignSessionToAgent(UserSession newSession);
        bool TryAssignSessionToOverflowAgent(UserSession awaitingSession);

        void UnAssignSession(UserSession session);
        void UnAssignAllSession();
    }
    public class TeamManager: ITeamManager
    {
        private readonly IShiftManager _shiftManager;
        private const double CapacityMultiplier = 1.5;

        //public int TotalCapacity { get; internal set; };
        public int WaitingCapacity
        {
            get
            {
                var team = _shiftManager.GetCurrentTeam();
                if (team == null)
                    return 0;
                var workingCapacity = team.Capacity;
                var totalCapacity = workingCapacity * CapacityMultiplier;
                var waitingCapacity = totalCapacity - workingCapacity;
                return (int)(waitingCapacity);
            }
        }


        public TeamManager(IShiftManager shiftManager) 
        {
            _shiftManager = shiftManager;
        }
        public bool TryAssignSessionToAgent(UserSession newSession)
        {
            var team = _shiftManager.GetCurrentTeam();
            if (team == null) 
                return false;

            var res = team.TryAssignSessionToAgent(newSession); 
            return res;
        }

        public bool TryAssignSessionToOverflowAgent(UserSession awaitingSession)
        {
            if (!_shiftManager.IsOverflowTeamAvailable())
                return false;

            var overTeam = _shiftManager.GetOverflowTeam();
            if (overTeam == null)
                return false;
            var res = overTeam.TryAssignSessionToAgent(awaitingSession);
            return res;
        }

        public void UnAssignSession(UserSession session)
        {
            var team = _shiftManager.GetCurrentTeam();
            if (team == null) 
                return;

            var res = team.TryUnAssignSession(session);
            if (!res && _shiftManager.IsOverflowTeamAvailable())
            {
                team = _shiftManager.GetOverflowTeam();
                team.TryUnAssignSession(session);
            }
        }

        public void UnAssignAllSession()
        {
            foreach (var team in _shiftManager.Teams)
            {
                team.FreeAgents();
            }
        }
    }
}
