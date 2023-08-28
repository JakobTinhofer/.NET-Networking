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
    public static class DebugUtils
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
            writeTolog(tObj, message, args);
        }

        private static void writeTolog(object t, string message, params object?[] args)
        {
            Debug.WriteLine("[ {0} @ {1} | i: {2} | T: {3}] {4}",
                UtcNow.ToString("ss:FFFFFFF"),
                (new System.Diagnostics.StackTrace())?.GetFrame(2)?.GetMethod()?.Name,
                idGen.GetId(t, out _),
                Environment.CurrentManagedThreadId,
                string.Format(message, args));
        }

        private static Dictionary<int, int> counts = new Dictionary<int, int>();
        static Mutex logMutex = new Mutex();
        public static void LogCount(object tObj, string tag, int? expected = null, string? msg = null) {
            logMutex.WaitOne();
            int objId = (int)idGen.GetId(tObj, out _);
            int key = HashCode.Combine(objId, tag);
            if(!counts.ContainsKey(key))
            {
                counts.Add(key, 0);
            }
            counts[key]++;
            logMutex.ReleaseMutex();
            writeTolog(tObj, " COUNT \"{0}\" (object {1}): {2}{3}.{4}", tag, objId, counts[key], expected == null ? "" : " of " + expected, msg ?? "");
        }

        
    }
}
