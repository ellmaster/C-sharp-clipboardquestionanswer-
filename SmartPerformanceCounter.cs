﻿namespace it
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class SmartPerformanceCounter
    {
        private readonly Func<PerformanceCounter> _factory;

        private readonly TimeSpan _time;

        private long _cpuCounterLastAccessedTimestamp;

        private PerformanceCounter _value;

        private readonly object _lock = new object();

        public bool IsValueCreated { get; private set; }

        public PerformanceCounter Value
        {
            get
            {
                lock (_lock)
                {
                    if (!IsValueCreated)
                    {
                        _value = _factory();
                        IsValueCreated = true;
                    }
                }
                _cpuCounterLastAccessedTimestamp = Stopwatch.GetTimestamp();
                return _value;
            }
        }
        public SmartPerformanceCounter(Func<PerformanceCounter> factory, TimeSpan time)
        {
            _factory = factory;
            _time = time;
        }

        private void DoCleaningCheck()
        {
            long now = Stopwatch.GetTimestamp();
            if (now - _cpuCounterLastAccessedTimestamp <= _time.Ticks) return;
            lock (_lock)
            {
                IsValueCreated = false;
                _value.Close();
                _value.Dispose();
                _value = null;
            }
        }

        public async Task Function()
        {
            await Task.Delay(_time).ConfigureAwait(false);
            DoCleaningCheck();
        }
    }
}