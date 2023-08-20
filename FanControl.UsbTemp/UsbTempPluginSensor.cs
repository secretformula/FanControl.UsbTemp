using FanControl.Plugins;
using System;

namespace FanControl.UsbTemp
{
    internal class UsbTempPluginSensor : IPluginSensor
    {
        private readonly UsbTempSensorConfig _config;
        private ITempSensorDriver _thermometer;
        private float _temp_measurement;

        public string Id => _config.device_type + " - " + _config.device_id;

        public string Name => _config.device_id;

        public float? Value => _temp_measurement;

        public UsbTempPluginSensor(UsbTempSensorConfig config)
        {
            _config = config;

            switch(_config.device_type)
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
            _thermometer.Open(_config.device_id);
        }

        public void Close()
        {
            _thermometer.Close();
        }
        public void Update()
        {
            _temp_measurement = _thermometer.Temperature();
        }
    }
}
