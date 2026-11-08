using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanControl.UsbTemp
{
    internal interface ITempSensorDriver
    {
        bool IsOpen { get; }

        void Open(string device_id);

        void Close();
        float Temperature();
    }
}
