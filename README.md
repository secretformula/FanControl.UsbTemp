# FanControl.UsbTemp
[![Download](https://img.shields.io/badge/Download-Plugin-green.svg?style=flat&logo=download)](https://github.com/secretformula/FanControl.UsbTemp/releases)

Plugin for [FanControl](https://github.com/Rem0o/FanControl.Releases) that provides support for USB/serial based temperature probes. Initial support provides for DS18B20 1–wire sensor connected via USB serial bridge as found in [this product](http://usbtemp.com/). Support for other temperature probes may be added in the future.

## To install

Requires FanControl V2xx or later (.NET 10 build; tested with V285). Version 2.0 of this plugin no longer works with the old .NET Framework builds (v138 and earlier).

Either
* Download the latest [release](https://github.com/secretformula/FanControl.UsbTemp/releases)
* Compile it on Windows with the .NET 10 SDK. The plugin references FanControl's own `FanControl.Plugins.dll`, `Newtonsoft.Json.dll` and `System.IO.Ports.dll`, so FanControl must be installed:

      dotnet build FanControl.UsbTemp.sln -c Release -p:FanControlDir="C:\Program Files (x86)\FanControl"

And then

1. Copy the FanControl.UsbTemp.dll into FanControl's "Plugins" folder
2. Create new file `<Fan Controller Install Dir>/Configurations/FanControl.UsbTemp.json` and copy content of example configuration.
3. Set COM ports that correspond to your installed temperature sense probe.
4. Open FanControl and enjoy! The sensor shows up as `USB Temp/<device_type><device_id>`, e.g. `USB Temp/ds18b20COM3`.

## Changes in 2.0
* Ported to the current plugin API (`IPlugin2`) and an SDK-style .NET 10 project.
* Finds `Configurations/FanControl.UsbTemp.json` next to FanControl.exe, not in the current directory, so it works when FanControl starts from a scheduled task or as a service.
* Reads the probe on a background thread. The ~800 ms DS18B20 conversion no longer blocks FanControl's update loop.
* Serial errors (probe unplugged, adapter wedged) are caught: the port is reopened with a backoff (1 s up to 30 s) and errors are logged at most every 5 minutes. After 15 s with no good reading the sensor reports no value. Before, every error was an unhandled exception that repeated every cycle.
* Rejects implausible readings (outside -55..125 °C, and the 85 °C power-on value).
