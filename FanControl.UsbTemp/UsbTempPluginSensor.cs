using FanControl.Plugins;
using System;
using System.Diagnostics;
using System.Threading;

namespace FanControl.UsbTemp
{
    /// <summary>
    /// A DS18B20 conversion takes ~800 ms, which used to block FanControl's 1 Hz update loop,
    /// and any serial error escaped as an unhandled exception. The probe is now read on a
    /// background thread; Update() only publishes the latest good reading.
    /// </summary>
    internal class UsbTempPluginSensor : IPluginSensor
    {
        // Report "no value" if no good reading arrived for this long.
        private static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan LogEvery = TimeSpan.FromMinutes(5);

        private readonly UsbTempSensorConfig _config;
        private readonly ITempSensorDriver _thermometer;
        private readonly IPluginLogger _logger;
        private readonly object _lock = new object();

        private Thread _thread;
        private volatile bool _stop;
        private float? _latest;
        private readonly Stopwatch _sinceGood = new Stopwatch();
        private float? _value;

        public string Id => _config.device_type + _config.device_id;

        public string Name => _config.device_id;

        public float? Value => _value;

        public UsbTempPluginSensor(UsbTempSensorConfig config, IPluginLogger logger)
        {
            _config = config;
            _logger = logger;

            switch (_config.device_type)
            {
                case "ds18b20":
                    _thermometer = new Ds18b20Thermometer();
                    break;
                default:
                    throw new Exception("Invalid device type");
            }
        }

        public void Open()
        {
            if (_thread != null) return;
            _stop = false;
            _thread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "UsbTemp " + _config.device_id,
            };
            _thread.Start();
        }

        public void Close()
        {
            _stop = true;
            var t = _thread;
            _thread = null;
            if (t != null && !t.Join(TimeSpan.FromSeconds(3)))
            {
                _logger?.Log(UsbTempPlugin.LOG_PREFIX + $"{_config.device_id}: reader did not stop in time");
            }
            SafeClosePort();
            _value = null;
        }

        public void Update()
        {
            lock (_lock)
            {
                _value = _sinceGood.IsRunning && _sinceGood.Elapsed < StaleAfter ? _latest : null;
            }
        }

        private void ReadLoop()
        {
            var backoff = TimeSpan.FromSeconds(1);
            int failures = 0;
            DateTime lastLog = DateTime.MinValue;

            while (!_stop)
            {
                try
                {
                    if (!_thermometer.IsOpen)
                    {
                        _thermometer.Open(_config.device_id);
                    }

                    float t = _thermometer.Temperature();

                    // 85.0 is the DS18B20 power-on reset value, not a measurement.
                    if (t < -55f || t > 125f || t == 85f)
                    {
                        throw new Ds18b20Thermometer.ThermometerException($"Implausible reading {t}");
                    }

                    lock (_lock)
                    {
                        _latest = t;
                        _sinceGood.Restart();
                    }

                    if (failures > 0)
                    {
                        _logger?.Log(UsbTempPlugin.LOG_PREFIX + $"{_config.device_id}: recovered after {failures} failed reads ({t} °C)");
                    }
                    failures = 0;
                    backoff = TimeSpan.FromSeconds(1);
                    Sleep(TimeSpan.FromMilliseconds(200));
                }
                catch (Exception e)
                {
                    failures++;
                    if (DateTime.UtcNow - lastLog > LogEvery)
                    {
                        _logger?.Log(UsbTempPlugin.LOG_PREFIX + $"{_config.device_id}: read failed ({failures} in a row), retrying: {e.GetType().Name}: {e.Message}");
                        lastLog = DateTime.UtcNow;
                    }

                    // Unplug/replug or a wedged adapter: drop the port and reopen it later.
                    SafeClosePort();
                    Sleep(backoff);
                    backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
                }
            }
        }

        private void SafeClosePort()
        {
            try { _thermometer.Close(); } catch { }
        }

        private void Sleep(TimeSpan duration)
        {
            var until = DateTime.UtcNow + duration;
            while (!_stop && DateTime.UtcNow < until)
            {
                Thread.Sleep(100);
            }
        }
    }
}
