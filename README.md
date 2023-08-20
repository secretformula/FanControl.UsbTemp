# FanControl.UsbTemp
[![Download](https://img.shields.io/badge/Download-Plugin-green.svg?style=flat&logo=download)](https://github.com/Rem0o/FanControl.HWInfo/releases/)

Plugin for [FanControl](https://github.com/Rem0o/FanControl.Releases) that provides support for USB/serial based temperature probes. Initial support provides for DS18B20 1–wire sensor connected via USB serial bridge as found in [this product](http://usbtemp.com/). Support for other temperature probes may be added in the future.

## To install

Either
* Download the latest [release](https://github.com/secretformula/FanControl.UsbTemp/releases)
* Compile the solution.

And then

1. Copy the FanControl.UsbTemp.dll into FanControl's "Plugins" folder
2. Create new file `<Fan Controller Install Dir>/Configurations/FanControl.UsbTemp.json` and copy content of example configuration.
3. Set COM ports that correspond to your installed temperature sense probe.
4. Open FanControl and enjoy!
