using System;
using System.Collections.Generic;
using System.Text;

namespace ADAS_System
{
    public interface ISensor
    {
        string Name { get; }
        int CheckValue { get; }
        List<int> History { get; }
        SensorData Read();
    }   

    public class SensorData
    {
        public string? SensorName { get; set; }

        public double Value { get; set; }

        public DateTime Timestamp { get; set; }

        
    }

    public class Camera : ISensor
    {
        public string Name { get; private set; }
        public int CheckValue { get; private set; } = 80;
        public List<int> History { get; }
        public Camera(string name)
        {
            Name = name;
            History = new List<int>();
        }
        public SensorData Read()
        {
            int value = Random.Shared.Next(0, 100);
            History.Add(value);
            // Simulate reading sensor data
            return new SensorData
            {
                SensorName = Name,
                Value = value, // Simulated value
                Timestamp = DateTime.UtcNow,
            };
        }
    }

    public class Radar : ISensor
    {
        public string Name { get; private set; }
        public int CheckValue { get; private set; } = 150;

        public List<int> History { get; }
        public Radar(string name)
        {
            Name = name;
            History = new List<int>();
        }
        public SensorData Read()
        {
            int value = Random.Shared.Next(0, 200);
            History.Add(value);
            // Simulate reading sensor data
            return new SensorData
            {
                SensorName = Name,
                Value = value, // Simulated value
                Timestamp = DateTime.UtcNow,
            };
        }
    }

    public class Lidar : ISensor
    {
        public string Name { get; private set; }
        public int CheckValue { get; private set; } = 250;

        public List<int> History { get; }
        public Lidar(string name)
        {
            Name = name;
            History = new List<int>();
        }
        public SensorData Read()
        {
            int value = Random.Shared.Next(0, 300);
            History.Add(value);
            // Simulate reading sensor data
            return new SensorData
            {
                SensorName = Name,
                Value = value, // Simulated value
                Timestamp = DateTime.UtcNow,
            };
        }
    }

    class SensorFactory
    {

        public SensorFactory(){}

        public ISensor Create(string type, string name)
        {
            switch (type.ToLowerInvariant())
            {
                case "camera":
                    return new Camera(name);
                case "radar":
                    return new Radar(name);
                case "lidar":
                    return new Lidar(name);
                default:
                    throw new ArgumentException("Invalid sensor type");
            }
        }
    }


    public class SensorMonitor
    {

        private string Name { get; set; }

        AlarmSystem alarmSystem;
        ConsoleLogger consoleLogger;

        List<ISensor> sensors = new List<ISensor>();

        public SensorMonitor(string name, AlarmSystem alarmSystem, ConsoleLogger consoleLogger)
        {
            Name = name;
            this.alarmSystem = alarmSystem;
            this.consoleLogger = consoleLogger;
        }

        public void AddSensor(ISensor sensor)
        {
            sensors.Add(sensor);
        }

        public void Poll()
        {
            foreach (var sensor in sensors)
            {
                var data = sensor.Read();
                consoleLogger.Log($"Sensor {data.SensorName} value: {(int)data.Value}");
                if (data.Value > sensor.CheckValue)
                {
                    alarmSystem.Update(data);
                }
            }
        }

        public async Task PollAsync()
        {
            var tasks = sensors.Select(async sensor =>
            {

                var data = sensor.Read();

                if (data.SensorName == "Front Radar")
                {
                    await Task.Delay(1000); // Simulate a delay for the second sensor
                }

                consoleLogger.Log($"Sensor {data.SensorName} value: {(int)data.Value}");

                if (data.Value > sensor.CheckValue)
                {
                    alarmSystem.Update(data);
                }
            });


            await Task.WhenAll(tasks);
        }

        public void Pollcheck(ISensor sensor)
        {
            var data = sensor.Read();
            consoleLogger.Log($"Sensor {data.SensorName} value: {(int)data.Value}");

            if (data.Value > sensor.CheckValue)
            {
                alarmSystem.Update(data);
            }
        }

        public async Task PollAsync2()
        {
            var cameraTask = Task.Run(() =>
            {
                Pollcheck(sensors[0]);
            });

            var radarTask = Task.Run(async () =>
            {
                Pollcheck(sensors[1]);
                await Task.Delay(1000);

            });

            var lidarTask = Task.Run(() =>
            {
                Pollcheck(sensors[2]);
            });

            await Task.WhenAll(cameraTask, radarTask, lidarTask);
        }

        public double GetAverage(string sensorName)
        {
            var sensor = sensors.FirstOrDefault(s => s.Name == sensorName);
            if (sensor == null || sensor.History.Count == 0)
            {
                return 0;
            }
            return sensor.History.Average();
        }

    }

    public interface ILogger
    {
        void Log(string message);
    }


    public interface IAlarmObserver
    {
        void Update(SensorData data);
    }

    public class AlarmSystem : IAlarmObserver
    {
        public void Update(SensorData data)
        {
            // Implement alarm logic based on sensor data
            if (data.Value > 80) // Example threshold
            {
                Console.WriteLine($"Alarm triggered by {data.SensorName} at {data.Timestamp}: Value = {(int)data.Value}");
            }
        }
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            // Implement logging logic (e.g., write to a file or database)
            Console.WriteLine($"Log: {message}");
        }
    }

}
