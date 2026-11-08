using FanControl.Plugins;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;

namespace FanControl.UsbTemp
{
    public class UsbTempPlugin : IPlugin2
    {
        public static readonly string LOG_PREFIX = "[FanControl.UsbTemp] ";
        public string Name => "USB Temp";

        private readonly IPluginLogger _logger;
        private readonly IPluginDialog _dialog;
        private readonly List<UsbTempPluginSensor> _sensors;

        public UsbTempPlugin(IPluginLogger logger, IPluginDialog dialog)
        {
            _logger = logger;
            _dialog = dialog;
            _sensors = new List<UsbTempPluginSensor>();
        }

        // FanControl's working directory is not its install folder when it is started by a
        // scheduled task, at boot, or as a service. Look next to FanControl.exe first.
        private static string FindConfigPath()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Configurations", "FanControl.UsbTemp.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Configurations", "FanControl.UsbTemp.json"),
            };
            foreach (var path in candidates)
            {
                if (File.Exists(path)) return path;
            }
            return candidates[0];
        }

        public void Initialize()
        {
            // FanControl can call Initialize again after a refresh; start from a clean list.
            Close();
            _sensors.Clear();

            var config_json_path = FindConfigPath();

            try
            {
                string tempSensorConfigRaw = File.ReadAllText(config_json_path);
                var tempSensorConfig = JsonConvert.DeserializeObject<IList<UsbTempSensorConfig>>(tempSensorConfigRaw);

                foreach (UsbTempSensorConfig sensor_port in tempSensorConfig)
                {
                    _sensors.Add(new UsbTempPluginSensor(sensor_port, _logger));
                }
            }
            catch (Exception e)
            {
                var error_message = $"Error: Could not parse configuration for the FanControl.UsbTemp plugin at {config_json_path}. Please edit it to be valid and restart FanControl.";
                _logger.Log(LOG_PREFIX + error_message);
                _logger.Log(LOG_PREFIX + "Not loading any FanControl.UsbTemp sensors.");
                _logger.Log(LOG_PREFIX + $"Parsing exception hint: {e.Message}" + Environment.NewLine + e.StackTrace);
                _dialog?.ShowMessageDialog(error_message);
            }
        }

        public void Close()
        {
            foreach (UsbTempPluginSensor sensor_port in _sensors)
            {
                sensor_port.Close();
            }
        }

        public void Load(IPluginSensorsContainer container)
        {
            foreach (UsbTempPluginSensor sensor_port in _sensors)
            {
                // Starts a background reader; never throws, so one bad probe can't stop FanControl.
                sensor_port.Open();
                container.TempSensors.Add(sensor_port);
            }
        }

        // Called once per FanControl update cycle. Sensors read on their own threads,
        // so there is nothing to do here.
        public void Update()
        {
        }
    }
}
