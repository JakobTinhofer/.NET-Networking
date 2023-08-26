using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.Util
{
    internal static class DebugUtils
    {
        private static DateTime _startTime = DateTime.UtcNow;
        private static Stopwatch _stopWatch = Stopwatch.StartNew();
        private static TimeSpan _maxIdle =
            TimeSpan.FromSeconds(10);

        public static DateTime UtcNow
        {
            get
            {
                if (_startTime.Add(_maxIdle) < DateTime.UtcNow)
                {
                    Reset();
                }
                return _startTime.AddTicks(_stopWatch.Elapsed.Ticks);
            }
        }

        private static void Reset()
        {
            _startTime = DateTime.UtcNow;
            _stopWatch = Stopwatch.StartNew();
        }

        private static ObjectIDGenerator idGen = new ObjectIDGenerator();
        public static void Log(object tObj, string message, params object?[] args)
        {
            var ddd = false;
            Debug.WriteLine("[ {0} @ {1} | i: {2} | T: {3}] {4}",
                UtcNow.ToString("ss:FFFFFFF"),
                (new System.Diagnostics.StackTrace())?.GetFrame(1)?.GetMethod()?.Name,
                idGen.GetId(tObj, out ddd),
                Environment.CurrentManagedThreadId,
                string.Format(message, args));
        }

        


        
    }
}
