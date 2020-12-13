using ChatApp.Enums;
using ChatApp.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace ChatApp.Services
{
    public interface IShiftManager 
    {
        bool IsOverflowTeamAvailable();
        Team GetCurrentTeam();
        Team GetOverflowTeam();

    }
    public class ShiftManager: IShiftManager
    {
        Dictionary<TeamType, Team> _teams = new Dictionary<TeamType, Team>();

        public ShiftManager(ITeamFactory teamFactory) 
        {
            _teams[TeamType.Daily] = teamFactory.CreateTeam(TeamType.Daily);
            _teams[TeamType.Evening] = teamFactory.CreateTeam(TeamType.Evening);
            _teams[TeamType.Nightly] = teamFactory.CreateTeam(TeamType.Nightly);
            _teams[TeamType.Overflow] = teamFactory.CreateTeam(TeamType.Overflow);
        }

        public bool IsOverflowTeamAvailable()
        {
            return CurrentTeamType == TeamType.Daily;
        }

        public Team GetCurrentTeam() 
        {
            return _teams[CurrentTeamType];
        }
        public Team GetOverflowTeam()
        {
            return _teams[TeamType.Overflow];
        }
        private TeamType CurrentTeamType 
        {
            get 
            {
                return DateTime.UtcNow.Hour switch
                {
                    int hour when hour > 8 && hour <= 16 => TeamType.Daily,
                    int hour when hour > 16 && hour <= 0 => TeamType.Evening,
                    int hour when hour > 0 && hour <= 8 => TeamType.Evening,
                    _ => TeamType.None,
                };
            }
        }
    }
}
