using Microsoft.AspNetCore.Mvc;
using EnergyMonitorAPI.Services;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization; // ✅ จำเป็นมากสำหรับแก้ปัญหา Format วันที่

namespace EnergyMonitorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnergyController : ControllerBase
    {
        // ⚠️ Token ของคุณ (ตามที่ส่งมาล่าสุด)
        private const string InfluxToken = "tyHNtVIbuBV_snqusuy4-u5zAooCa6cqbjZcoAXdRx6POZQrJl4tE6R5X6EEnOCQ08t4u8UXlV08EGAkDRJJMw==";
        private const string InfluxUrl = "http://localhost:8086";
        private const string InfluxOrg = "mfec_org";
        private const string InfluxBucket = "energy_bucket";

        [HttpGet("current")]
        public IActionResult GetCurrentEnergy()
        {
            return Ok(MqttWorkerService.LatestData);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string range = "24h",
            [FromQuery] string building = "MFEC Tower",
            [FromQuery] string? anchor = null,
            [FromQuery] string? customStart = null, // ✅ รับวันที่เริ่มต้นเอง
            [FromQuery] string? customEnd = null)   // ✅ รับวันที่สิ้นสุดเอง
        {
            using var client = new InfluxDBClient(InfluxUrl, InfluxToken);
            var queryApi = client.GetQueryApi();

            string startQuery;
            string stopQuery;
            string windowPeriod = "1h"; // ค่า Default

            // --- กรณีเลือกช่วงเวลาเอง (Custom) ---
            if (range.ToLower() == "custom" && !string.IsNullOrEmpty(customStart) && !string.IsNullOrEmpty(customEnd))
            {
                try
                {
                    // ใช้เวลาที่ User เลือกมาตรงๆ
                    DateTime startDt = DateTime.Parse(customStart);
                    DateTime endDt = DateTime.Parse(customEnd);

                    // ✅ บังคับ Format ISO 8601 และใช้ InvariantCulture เพื่อแก้ปัญหา Error parsing time
                    startQuery = $"time(v: \"{startDt.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}\")";
                    stopQuery = $"time(v: \"{endDt.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}\")";

                    // คำนวณ Window Period อัตโนมัติ (Dynamic Downsampling)
                    double totalDays = (endDt - startDt).TotalDays;
                    windowPeriod = totalDays switch {
                        > 365 => "7d", // เกิน 1 ปี -> จุดละ 1 อาทิตย์
                        > 30 => "1d",  // เกิน 1 เดือน -> จุดละ 1 วัน
                        > 7 => "6h",   // เกิน 7 วัน -> จุดละ 6 ชม.
                        _ => "1h"      // น้อยกว่านั้น -> จุดละ 1 ชม.
                    };
                }
                catch (Exception)
                {
                    return BadRequest(new { error = "Invalid custom date format" });
                }
            }
            // --- กรณีเลือกปุ่มลัด (24H, 7D, 30D, 6M, 1Y) ---
            else
            {
                // 1. แปลง Range เป็น TimeSpan
                TimeSpan timeSpan = range.ToLower() switch
                {
                    "7d" => TimeSpan.FromDays(7),
                    "30d" => TimeSpan.FromDays(30),
                    "6m" => TimeSpan.FromDays(180), // ✅ เพิ่ม 6 เดือน
                    "1y" => TimeSpan.FromDays(365), // ✅ เพิ่ม 1 ปี
                    _ => TimeSpan.FromHours(24)
                };

                // 2. กำหนดความละเอียดกราฟ (Downsampling)
                windowPeriod = range.ToLower() switch
                {
                    "7d" => "6h",
                    "30d" => "1d",   // 1 วันต่อจุด
                    "6m" => "1d",    // 1 วันต่อจุด
                    "1y" => "1d",    // 1 วันต่อจุด
                    _ => "1h"
                };

                // 3. คำนวณเวลา Start/Stop จาก Anchor (Time Machine Mode)
                if (!string.IsNullOrEmpty(anchor) && DateTime.TryParse(anchor, out DateTime anchorTime))
                {
                    DateTime startTime = anchorTime.Subtract(timeSpan);

                    // ✅ ใช้ InvariantCulture
                    string startIso = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                    string stopIso = anchorTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

                    startQuery = $"time(v: \"{startIso}\")";
                    stopQuery = $"time(v: \"{stopIso}\")";
                }
                else
                {
                    // Real-time Mode
                    startQuery = $"-{range}";
                    stopQuery = "now()";
                }
            }

            // สร้าง Flux Query
            var fluxQuery = $@"
                from(bucket: ""{InfluxBucket}"")
                |> range(start: {startQuery}, stop: {stopQuery})
                |> filter(fn: (r) => r[""_measurement""] == ""energy_usage"")
                |> filter(fn: (r) => r[""_field""] == ""value"")
                |> filter(fn: (r) => r[""buildingId""] == ""{building}"")
                |> aggregateWindow(every: {windowPeriod}, fn: mean, createEmpty: false)
                |> yield(name: ""mean"")";

            try
            {
                var tables = await queryApi.QueryAsync(fluxQuery, InfluxOrg);
                var result = new List<object>();

                foreach (var record in tables.SelectMany(table => table.Records))
                {
                    var timeVal = record.GetTimeInDateTime();
                    var valueVal = record.GetValue();

                    if (timeVal.HasValue && valueVal != null)
                    {
                        result.Add(new {
                            time = timeVal.Value,
                            value = valueVal
                        });
                    }
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Query Error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
