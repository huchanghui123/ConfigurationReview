using OpenHardwareMonitor.Hardware;
using QotomReview.model;
using System;
using System.Collections.Generic;

namespace QotomReview.Tools
{
    public class CpuTemperatureReader : IDisposable
    {
        private Computer _computer;

        public CpuTemperatureReader()
        {
            _computer = new Computer
            {
                //MainboardEnabled = true,
                CPUEnabled = true
                //GPUEnabled = true,
                //HDDEnabled = true,
                //RAMEnabled = true
                //FanControllerEnabled = true
            };
            _computer.Open();
        }

        public List<SensorData> GetTemperaturesInCelsius()
        {
            List<SensorData> sensorList = new List<SensorData>();
            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    hardware.Update();
                    if (hardware.HardwareType == HardwareType.CPU)
                    {
                        //获取CPU核心温度
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {

                                sensorList.Add(new SensorData(sensor.Name, 
                                    sensor.Value.Value + " °C",
                                    sensor.Min.Value + " °C", 
                                    sensor.Max.Value + " °C"));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return sensorList;
        }

        public void Dispose()
        {
            try
            {
                _computer.Close();
                Console.WriteLine("CpuTemperatureReader Dispose!!!");
            }
            catch (Exception)
            {
                //ignore closing errors
            }
        }
    }
}
