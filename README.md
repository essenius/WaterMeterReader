# WaterMeterReader

This is an initial C# version of an analog water meter reader via a magnetometer.
Water meters in homes usually work with a turning magnet that can be detected via a magnetometer.
This application takes a set of samples from a file (log from the actual sensor) and detects the time that there is flow.
Complicating factors are that the signal is quite noisy and the sensor cannot support sample times faster than 10ms. 
The sensor is sensitive to moving as well. 
Finally, serial writing is quite slow (max 115200 baud) so we need to limit the amount of data over the line.

FlowMeter does the signal processing, MeasurementReader grabs the input signals.
MeasurementWiter logs the measurements in batches. This is done because we don't want to overburden the receiving end
a Raspberry Pi). SummaryWriter logs a summary of the analysis result. We make sure they never write at the same time.

Intention is to translate the code to C++ to be used on an Arduino, but for experimenting, debugging and unit testing I prefer C#.