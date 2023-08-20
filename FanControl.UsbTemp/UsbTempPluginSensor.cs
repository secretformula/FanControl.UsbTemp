using FanControl.Plugins;

namespace FanControl.UsbTemp
{
    internal class UsbTempPluginSensor : IPluginSensor
    {
        private Thermometer thermometer_;
        private float temp_measurement_;

        public UsbTempPluginSensor(Thermometer thermometer)
        {
            thermometer_ = thermometer;
        }

        public string Id => "UsbTemp" + thermometer_.name;

        public string Name => thermometer_.name;

        public float? Value => temp_measurement_;

        public void Update()
        {
            temp_measurement_ = thermometer_.Temperature();
        }
    }
}
