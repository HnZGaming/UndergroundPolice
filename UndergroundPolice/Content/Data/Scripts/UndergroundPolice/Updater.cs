using System;
using Sandbox.ModAPI;
using VRage.Utils;

namespace UndergroundPolice
{
    public class Updater
    {
        readonly TimeSpan _updateSpan;
        readonly Action _update;
        DateTime? _lastUpdateTime;
        bool _processing;

        public Updater(TimeSpan updateSpan, Action update)
        {
            _updateSpan = updateSpan;
            _update = update;
        }

        public void Update()
        {
            var nowTime = DateTime.UtcNow;
            if (_lastUpdateTime != null)
            {
                var pastTime = nowTime - _lastUpdateTime.Value;
                if (pastTime < _updateSpan) return;
            }

            if (_processing) return;

            _lastUpdateTime = nowTime;

            MyAPIGateway.Parallel.Start(() =>
            {
                try
                {
                    _processing = true;
                    _update?.Invoke();
                }
                catch (Exception e)
                {
                    MyLog.Default.Error($"{e.Message}\n{e.StackTrace}");
                }
                finally
                {
                    _processing = false;
                }
            });
        }
    }
}