namespace EnergyMonitorAPI.Models
{
    public class EnergyData
    {
        // ใส่ ? เพื่อบอกว่าเป็น Nullable หรือใส่ = string.Empty;
        public string? Timestamp { get; set; }
        public double Value { get; set; }
        public string? BuildingId { get; set; }
    }
}
