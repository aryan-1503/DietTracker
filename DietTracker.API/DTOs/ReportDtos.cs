namespace DietTracker.API.DTOs;

public class MonthlyReportMetaDto
{
    public int    Year        { get; set; }
    public int    Month       { get; set; }
    public string MonthName   { get; set; } = string.Empty;
    public string? DietPlanName { get; set; }
    public int    TotalDays   { get; set; }
    public int    DaysWithData { get; set; }
    public bool   HasAnyData  { get; set; }
}
