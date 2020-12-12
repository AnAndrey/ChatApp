using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Services.Interfaces
{
    public interface ISessionManager
    {
        Guid CreateSession();
    }

    public class SessionManager : ISessionManager
    {
        public Guid CreateSession()
        {
            return Guid.NewGuid();
        }
    }
}
