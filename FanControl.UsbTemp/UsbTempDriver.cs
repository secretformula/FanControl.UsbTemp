using System;
using System.IO.Ports;
using System.Xml.Linq;

namespace FanControl.UsbTemp
{
    internal class Ds18b20Thermometer : ITempSensorDriver
    {
        internal class ThermometerException : Exception
        {
            public ThermometerException(string message) : base(message)
            {
            }
        }

        const int ok = 0;
        const int fail = -1;

        const byte generator = 0x8c;
        const int sp_size = 9;

        private SerialPort serial_port;

        protected byte lsb_crc8(byte[] buf, int buflen, byte generator)
        {
            int i, bit_counter;
            byte crc = 0;

            for (i = 0; i < buflen; i++)
            {
                crc ^= buf[i];
                bit_counter = 8;
                do
                {
                    crc = (crc & 0x01) == 0x01 ? (byte)(((crc >> 1) & 0x7f) ^ generator) : (byte)((crc >> 1) & 0x7f);
                    bit_counter--;
                } while (bit_counter > 0);
            }
            return crc;
        }

        protected int owReset()
        {
            int rv;
            byte wbuff;
            byte rbuff;
            byte[] buf;

            serial_port.BaudRate = 9600;
            serial_port.DataBits = 8;

            buf = new byte[1];

            wbuff = 0xf0;
            buf[0] = wbuff;
            serial_port.Write(buf, 0, 1);

            serial_port.Read(buf, 0, 1);
            rbuff = buf[0];
            rv = (rbuff == 0x00 || rbuff == 0xf0) ? fail : ok;

            serial_port.BaudRate = 115200;
            serial_port.DataBits = 6;

            return rv;
        }

        protected byte owWriteByte(byte wbuff)
        {
            int rbytes, remaining, i;
            byte[] buf;
            byte rbuff;

            buf = new byte[8];
            for (i = 0; i < 8; i++)
            {
                buf[i] = Convert.ToBoolean(wbuff & (1 << (i & 0x7))) ? (byte)0xff : (byte)0x00;
            }
            serial_port.Write(buf, 0, 8);

            rbuff = 0;
            remaining = 8;
            while (remaining > 0)
            {
                rbytes = serial_port.Read(buf, 0, remaining);
                for (i = 0; i < rbytes; i++)
                {
                    rbuff >>= 1;
                    rbuff |= (buf[i] & 0x01) == 0x01 ? (byte)0x80 : (byte)0x00;
                    remaining--;
                }
            }
            return rbuff;
        }

        protected byte owRead()
        {
            return owWriteByte(0xff);
        }

        protected int owWrite(byte wbuff)
        {
            if (owWriteByte(wbuff) != wbuff)
            {
                return fail;
            }
            return ok;
        }

        public void Open(string device_id)
        {
            serial_port = new SerialPort(device_id);

            serial_port.BaudRate = 115200;
            serial_port.Parity = Parity.None;
            serial_port.StopBits = StopBits.One;
            serial_port.DataBits = 6;
            serial_port.Handshake = Handshake.None;
            serial_port.RtsEnable = false;

            serial_port.ReadTimeout = 500;
            serial_port.WriteTimeout = 500;

            serial_port.Open();
        }

        public void Close()
        {
            serial_port.Close();
        }

        public float Temperature()
        {
            byte crc;
            byte[] sp_sensor;
            short T;
            int i;

            if (owReset() == fail || owWrite(0xcc) == fail || owWrite(0x44) == fail)
            {
                throw new ThermometerException("Cannot start the conversion process!");
            }

            System.Threading.Thread.Sleep(800);

            if (owReset() == fail || owWrite(0xcc) == fail || owWrite(0xbe) == fail)
            {
                throw new ThermometerException("Cannot read the temperature!");
            }

            sp_sensor = new byte[sp_size];
            for (i = 0; i < sp_size; i++)
            {
                sp_sensor[i] = owRead();
            }

            if ((sp_sensor[4] & 0x9f) != 0x1f)
            {
                throw new ThermometerException("Invalid device!");
            }

            crc = lsb_crc8(sp_sensor, sp_size - 1, generator);
            if (sp_sensor[sp_size - 1] != crc)
            {
                throw new ThermometerException("CRC Error!");
            }

            T = BitConverter.ToInt16(sp_sensor, 0);
            return (float)T / 16;
        }
    }
}
