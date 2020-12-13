using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Services
{
    public interface ITimeProvider 
    {
        DateTime CurrentTime { get; }
    }
    public class TimeProvider: ITimeProvider
    {
        private readonly TimeSpan _timeShift = TimeSpan.Zero;

        public DateTime CurrentTime => DateTime.Now + _timeShift;
    }
}
