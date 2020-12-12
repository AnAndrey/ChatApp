using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Errors
{
    public class ChatException : Exception
    {
        public ChatException(string errorMessage) :base(errorMessage)
        { 
        
        }
    }
}
