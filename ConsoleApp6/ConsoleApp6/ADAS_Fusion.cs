using ADAS_System;
using System;
using System.Collections.Generic;
using System.Linq;      
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ADAS_Fusion
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


    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
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

        public void LogError(string message)
        {
            Console.WriteLine($"Error: {message}");
        }
    }


    class SensorFactory
    {

        public SensorFactory() { }

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
        private static SemaphoreSlim? semaphore;
        public delegate void MyEvent4Handler(ISensor sens);
        public event MyEvent4Handler? ValueChanged;

        AlarmSystem alarmSystem;
        ConsoleLogger consoleLogger;

        List<ISensor> sensors = new List<ISensor>();

        public SensorMonitor(string name, AlarmSystem alarmSystem, ConsoleLogger consoleLogger)
        {
            Name = name;
            this.alarmSystem = alarmSystem;
            this.consoleLogger = consoleLogger;
            semaphore = new SemaphoreSlim(1, 2);
            ValueChanged += (sensor) => {
                consoleLogger.Log($"Sensor {sensor.Name} value: {(int)sensor.History.Last()}");
                if (sensor.History.Last() > sensor.CheckValue)
                {
                    alarmSystem.Update(new SensorData
                    {
                        SensorName = sensor.Name,
                        Value = sensor.History.Last(),
                        Timestamp = DateTime.UtcNow
                    });
                }

                GetAverage();
            };
        }

        public void AddSensor(ISensor sensor)
        {
            sensors.Add(sensor);
        }

        public async Task PollAll(CancellationToken externalCt = default)
        {
            foreach (var sensor in sensors)
            {
                int count = 0;
                if (await Poll(sensor, externalCt))
                {
                    ValueChanged?.Invoke(sensor);
                }
                else 
                {   
                    while (!await Poll(sensor, externalCt) && count++ <= 2)
                    {
                        Console.WriteLine($"Retry {count} for sensor {sensor.Name}");
                    }
                }
            }
        }

        public async Task<bool> Poll(ISensor sensor, CancellationToken externalCt = default)
        {
            bool isCompleted = true;
            bool acquired = false; // ← セマフォ取得フラグ

            // タイムアウト用トークン（3秒で自動キャンセル）
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            // 外部キャンセル + タイムアウトを1つに結合
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

            try
            {
                if (semaphore != null)
                {
                    await semaphore.WaitAsync(linkedCts.Token);
                    acquired = true; // ← セマフォ取得成功
                }

                await Pollcheck(sensor, linkedCts.Token);
                Console.WriteLine($"{sensor.Name}: Get Data");
                isCompleted = true;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // ← タイムアウトが原因の場合
                Console.WriteLine($"{sensor.Name}: Result1 timeout");
                isCompleted = false;
            }
            catch (OperationCanceledException)
            {
                // ← 外部キャンセルが原因の場合
                Console.WriteLine($"{sensor.Name}: operation canceled");
                isCompleted = false;
            }
            finally
            {
                // 例外が起きても必ずリリース
                if (semaphore != null && acquired) semaphore.Release(1);
            }

            return isCompleted;
        }

        public async Task Pollcheck(ISensor sensor, CancellationToken ct = default)
        {
            await Task.Delay(4000, ct); // 外部トークンで止められる
            var data = sensor.Read();
        }

        public void GetAverage()
        {

            foreach (var sensor in sensors)
            {
                var sensordata = sensors.FirstOrDefault(s => s.Name == sensor.Name);
                if (sensordata == null || sensordata.History.Count == 0)
                {
                    continue;
                }
                consoleLogger.Log($"Sensor {sensordata.Name} average value: {(int)sensordata.History.Average()}");
            }
        }
    }


}
