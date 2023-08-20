using FanControl.Plugins;

namespace FanControl.UsbTemp
{
    internal class UsbTempPlugin : IPlugin
    {
        public string Name => "USB Temp";

        Thermometer thermometer_ = new Thermometer();
 
        public void Close()
        {
            thermometer_.Close();
        }

        public void Initialize()
        {
            thermometer_.Open("COM3");
        }

        public void Load(IPluginSensorsContainer container)
        {
            container.TempSensors.Add(new UsbTempPluginSensor(thermometer_));
        }
    }
}
