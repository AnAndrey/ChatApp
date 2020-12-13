using ChatApp.Enums;
using ChatApp.Errors;
using ChatApp.Units;

namespace ChatApp.Services
{
    public interface ITeamFactory 
    {
        Team CreateTeam(TeamType teamType);
    }

    //Teams available:
    //Team A: 1x team lead, 2x mid-level, 1x junior
    //Team B: 1x senior, 1x mid-level, 2x junior
    //Team C: 2x mid-level(night shift team)
    //Overflow team: x6 considered Junior.

    //  Seniority Multipliers:
    //  Junior: 0.4
    //  Mid-Level: 0.6
    //  Senior: 0.8
    //  Team Lead: 0.5
    public class TeamFactory:ITeamFactory
    {
        private const int MaximumConcurrencyLevel = 10;

        //private const double JuniorMultiplier = 0.4;
        //private const double MiddleMultiplier = 0.6;
        //private const double SeniorMultiplier = 0.8;
        //private const double TeamLeadMultiplier = 0.5;
        //private const int OwerflowAgentsCount = 6;

        //Debug constants
        private const double JuniorMultiplier = 0.2;
        private const double MiddleMultiplier = 0.2;
        private const double SeniorMultiplier = 0.2;
        private const double TeamLeadMultiplier = 0.2;

        private const int OwerflowAgentsCount = 1;

        private Agent CreateJuniorAgent(string info = null) => new Agent(AgentType.Junior, (int)(JuniorMultiplier * MaximumConcurrencyLevel), info);
        private Agent CreateMiddleAgent() => new Agent(AgentType.Middle, (int)(MiddleMultiplier * MaximumConcurrencyLevel));
        private Agent CreateSeniorAgent() => new Agent(AgentType.Seniour, (int)(SeniorMultiplier * MaximumConcurrencyLevel));
        private Agent CreateTeamLeadAgent() => new Agent(AgentType.TeamLead, (int)(TeamLeadMultiplier * MaximumConcurrencyLevel));

        public Team CreateTeam(TeamType teamType)
        { 
            Agent[] juniors = null;
            Agent[] middles = null;
            Agent[] seniors = null;
            Agent[] teamleads = null;
            switch (teamType) 
            {
                case TeamType.Daily:
                    juniors = new [] { CreateJuniorAgent() };
                    middles = new[] { CreateMiddleAgent(), CreateMiddleAgent() };
                    teamleads = new[] { CreateTeamLeadAgent() };
                    break;
                case TeamType.Evening:
                    juniors = new[] { CreateJuniorAgent(), CreateJuniorAgent() };
                    middles = new[] { CreateMiddleAgent() };
                    seniors = new[] { CreateSeniorAgent() };
                    break;
                case TeamType.Nightly:
                    middles = new[] { CreateMiddleAgent(), CreateMiddleAgent() };
                    break;
                case TeamType.Overflow:
                    juniors = new Agent[OwerflowAgentsCount];
                    var agentInfo = AgentType.Junior.ToString() + TeamType.Overflow.ToString();
                    for (int i = 0; i < OwerflowAgentsCount; i++)
                        juniors[i] = CreateJuniorAgent(agentInfo);
                    break;
                default:
                    throw new ChatException($"Invalid {nameof(TeamType)}: '{teamType}'.");
            }
            return new Team(juniors, middles, seniors, teamleads);
        }
    }
}
