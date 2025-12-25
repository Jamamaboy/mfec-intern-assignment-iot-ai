using System.Text;
using System.Text.Json;
using EnergyMonitorAPI.Models;
using MQTTnet;
using MQTTnet.Client;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnergyMonitorAPI.Services
{
    public class MqttWorkerService : BackgroundService
    {
        private readonly ILogger<MqttWorkerService> _logger;
        private IMqttClient _mqttClient;

        // Static Variable เพื่อให้ Controller ดึงไปใช้ได้ทันที
        public static EnergyData LatestData { get; private set; } = new EnergyData();

        // ⚠️ Config ต้องตรงกับ Controller
        private const string InfluxToken = "tyHNtVIbuBV_snqusuy4-u5zAooCa6cqbjZcoAXdRx6POZQrJl4tE6R5X6EEnOCQ08t4u8UXlV08EGAkDRJJMw=="; // <--- เช็ค Token
        private const string InfluxUrl = "http://localhost:8086";
        private const string InfluxOrg = "mfec_org";
        private const string InfluxBucket = "energy_bucket";

        public MqttWorkerService(ILogger<MqttWorkerService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com", 1883)
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            try
            {
                await _mqttClient.ConnectAsync(options, stoppingToken);
                await _mqttClient.SubscribeAsync("mfec/energy/sensor");
                _logger.LogInformation("✅ MQTT Service Started & Connected!");
            }
            catch(Exception ex)
            {
                _logger.LogError($"❌ MQTT Connection Failed: {ex.Message}");
            }

            // Keep Alive Loop
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_mqttClient.IsConnected)
                {
                    try { await _mqttClient.ConnectAsync(options, stoppingToken); } catch {}
                }
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            try
            {
                var payload = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);

                // ✅ จุดสำคัญ: ตั้งค่าให้อ่าน JSON โดยไม่สนตัวพิมพ์เล็ก/ใหญ่
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // แปลง JSON เป็น Object
                var data = JsonSerializer.Deserialize<EnergyData>(payload, options);

                if (data != null)
                {
                    // 1. อัปเดตค่าล่าสุดใน RAM (ให้หน้าเว็บเห็น Real-time)
                    LatestData = data;
                    _logger.LogInformation($"⚡ New Data: {data.Value} MW at {data.Timestamp}");

                    // 2. บันทึกลง InfluxDB
                    using var client = new InfluxDBClient(InfluxUrl, InfluxToken);
                    var writeApi = client.GetWriteApiAsync();

                    if (DateTime.TryParse(data.Timestamp, out var parsedTime))
                    {
                        var point = PointData
                            .Measurement("energy_usage")
                            .Tag("buildingId", data.BuildingId) // เก็บ Tag ให้ตรงกับตอน Import
                            .Field("value", data.Value)
                            .Timestamp(parsedTime, WritePrecision.Ns);

                        await writeApi.WritePointAsync(point, InfluxBucket, InfluxOrg);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error processing msg: {ex.Message}");
            }
        }
    }
}
