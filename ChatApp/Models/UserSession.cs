using ChatApp.Enums;
using System;
using Newtonsoft.Json;
using System.Runtime.Serialization;
namespace ChatApp.Models
{
    
    public class UserSession 
    {
        public UserSession() 
        {
            SessionId = Guid.NewGuid();
        }

        public Guid SessionId { get; }
        [JsonIgnore]
        public SessionStatus Status { get; set; }
        public string StatusAsString => Status.ToString();         
        public string AgentInfo { get; set; }
        public DateTime LastUpdated { get; set; }

        public override string ToString()
        {
            return $"'{SessionId}' has '{Status}' status. Agent info '{AgentInfo ?? "Not available"}'";
        }
    }
}
