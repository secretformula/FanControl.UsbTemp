using FanControl.Plugins;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;

namespace FanControl.UsbTemp
{
    internal class UsbTempPlugin : IPlugin
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

        public void Initialize()
        {
            string cwd_path = Directory.GetCurrentDirectory();
            var config_directory_path = Path.Combine(cwd_path, "Configurations");
            var config_json_path = Path.Combine(config_directory_path, "FanControl.UsbTemp.json");

            try
            {
                string tempSensorConfigRaw = File.ReadAllText(config_json_path);
                var tempSensorConfig = JsonConvert.DeserializeObject<IList<UsbTempSensorConfig>>(tempSensorConfigRaw);

                foreach (UsbTempSensorConfig sensor_port in tempSensorConfig)
                {
                    _sensors.Add(new UsbTempPluginSensor(sensor_port));
                }
            }
            catch (System.Exception e)
            {
                var error_message = $"Error: Could not parse configuration for the FanControl.UsbTemp plugin at {config_json_path}. Please edit it to be valid and restart FanControl. You can delete {config_json_path} and restart FanControl to get back the default config.";
                _logger.Log(LOG_PREFIX + error_message);
                _logger.Log(LOG_PREFIX + "Not loading any FanControl.UsbTemp sensors.");
                _logger.Log(LOG_PREFIX + $"Parsing exception hint: {e.Message}" + Environment.NewLine + e.StackTrace);
                _dialog.ShowMessageDialog(error_message);
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
                sensor_port.Open();
            }
        }
    }
}
