
using System;
using ADAS_System;

class Program
{
    static void Main(string[] args)
    {
        var alarmSystem = new AlarmSystem();
        var consoleLogger = new ConsoleLogger();
        var sensorMonitor = new SensorMonitor("Main Monitor", alarmSystem, consoleLogger);

        var sensorFactory = new SensorFactory();
        var camera = sensorFactory.Create("camera", "Front Camera");
        var radar = sensorFactory.Create("radar", "Front Radar");
        var lidar = sensorFactory.Create("lidar", "Front Lidar");

        sensorMonitor.AddSensor(camera);
        sensorMonitor.AddSensor(radar);
        sensorMonitor.AddSensor(lidar);

        //sensorMonitor.Poll();
        sensorMonitor.PollAsync2().Wait();
        sensorMonitor.PollAsync2().Wait();
        sensorMonitor.PollAsync2().Wait();

        var average = sensorMonitor.GetAverage("Front Lidar");
        Console.WriteLine($"Average value for Front Lidar: {average}");
    }
}   
