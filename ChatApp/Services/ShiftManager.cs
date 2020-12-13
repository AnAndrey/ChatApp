using ChatApp.Enums;
using ChatApp.Errors;
using ChatApp.Services;
using ChatApp.Units;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ChatApp.Services
{
    public interface IShiftManager : IDisposable
    {
        IEnumerable<Team> Teams { get; }

        bool IsOverflowTeamAvailable();
        Team GetCurrentTeam();
        Team GetOverflowTeam();

        Action OnTransition { get; set; }
    }
    public class ShiftManager: IShiftManager
    {
        Dictionary<TeamType, Team> _teams = new Dictionary<TeamType, Team>();// TODO: change to array
        private readonly ITimeProvider _timeProvider;
        private readonly ILogger<ShiftManager> _logger;
        private Timer _shiftTimer;
        private int[] _shiftHours = new [] { 8, 16, 24 };
        public Action OnTransition { get; set; }

        public ShiftManager(ITeamFactory teamFactory, ITimeProvider timeProvider, ILogger<ShiftManager> logger) 
        {
            _teams[TeamType.Daily] = teamFactory.CreateTeam(TeamType.Daily);
            _teams[TeamType.Evening] = teamFactory.CreateTeam(TeamType.Evening);
            _teams[TeamType.Nightly] = teamFactory.CreateTeam(TeamType.Nightly);
            _teams[TeamType.Overflow] = teamFactory.CreateTeam(TeamType.Overflow);
            _timeProvider = timeProvider;
            _logger = logger;
            _shiftTimer = new Timer(ShiftTeams);

            SetShiftTimer();
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
                return _timeProvider.CurrentTime.Hour switch
                {
                    int hour when hour >= 8 && hour < 16 => TeamType.Daily,
                    int hour when hour >= 16 && hour < 24 => TeamType.Evening,
                    int hour when hour >= 0 && hour < 8 => TeamType.Nightly,
                    _ => TeamType.None,
                };
            }
        }

        public IEnumerable<Team> Teams
        {
            get
            {
                return _teams.Values;
            }
        }

        private void ShiftTeams(object _) 
        {
            _shiftTimer.Change(-1, -1); //stop timer
            OnTransition?.Invoke();
            SetShiftTimer(); //start timer
        }

        private void SetShiftTimer()
        {
            var delta = CalculateTimeBeforeNextShift();
            _shiftTimer.Change(delta, delta);
            _logger.LogInformation("The shift transition has been completed. ");
        }
        private TimeSpan CalculateTimeBeforeNextShift() 
        {
            var currentTime = _timeProvider.CurrentTime;
            foreach (var shiftHour in _shiftHours)
            {
                DateTime shiftTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0)
                                         .AddHours(shiftHour);
                var delta = shiftTime - currentTime;
                if (delta > TimeSpan.Zero && delta < TimeSpan.FromHours(8))
                {
                    return delta;
                }
            }
            throw new ChatException("Failed to calculate time before next shift.");
        }

        public void Dispose()
        {
            if (_shiftTimer != null)
                _shiftTimer.Dispose();
            _shiftTimer = null;
        }
    }
}
