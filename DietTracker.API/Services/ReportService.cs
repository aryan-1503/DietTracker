using ClosedXML.Excel;
using DietTracker.API.Data;
using DietTracker.API.DTOs;
using DietTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Services;

public interface IReportService
{
    Task<MonthlyReportMetaDto> GetMonthlyMetaAsync(int userId, int year, int month, CancellationToken ct = default);
    Task<byte[]> GenerateMonthlyExcelAsync(int userId, int year, int month, string userDisplayName, CancellationToken ct = default);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    // ── Meta ──────────────────────────────────────────────────────────────────

    public async Task<MonthlyReportMetaDto> GetMonthlyMetaAsync(
        int userId, int year, int month, CancellationToken ct = default)
    {
        var firstDay = new DateOnly(year, month, 1);
        var lastDay  = firstDay.AddMonths(1).AddDays(-1);
        var totalDays = lastDay.Day;

        var primaryPlan = await _db.DietPlans
            .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        var daysWithData = await _db.DailyEntries
            .Where(de => de.UserId == userId
                      && de.EntryDate >= firstDay
                      && de.EntryDate <= lastDay)
            .Select(de => de.EntryDate)
            .Distinct()
            .CountAsync(ct);

        var monthName = firstDay.ToString("MMMM");

        return new MonthlyReportMetaDto
        {
            Year         = year,
            Month        = month,
            MonthName    = monthName,
            DietPlanName = primaryPlan?.Name,
            TotalDays    = totalDays,
            DaysWithData = daysWithData,
            HasAnyData   = daysWithData > 0,
        };
    }

    // ── Excel Generation ──────────────────────────────────────────────────────

    public async Task<byte[]> GenerateMonthlyExcelAsync(
        int userId, int year, int month, string userDisplayName, CancellationToken ct = default)
    {
        var firstDay = new DateOnly(year, month, 1);
        var lastDay  = firstDay.AddMonths(1).AddDays(-1);
        var monthName = firstDay.ToString("MMMM");

        // Load primary plan with slots + food options
        var primaryPlan = await _db.DietPlans
            .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.IsActive)
            .Include(dp => dp.MealSlots.OrderBy(ms => ms.SortOrder))
                .ThenInclude(ms => ms.FoodOptions)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        var slots = primaryPlan?.MealSlots.OrderBy(ms => ms.SortOrder).ToList()
                    ?? new List<MealSlot>();

        // Load daily entries for month (with FoodOption nav)
        var entries = await _db.DailyEntries
            .Where(de => de.UserId == userId
                      && de.EntryDate >= firstDay
                      && de.EntryDate <= lastDay)
            .Include(de => de.FoodOption)
            .AsNoTracking()
            .ToListAsync(ct);

        // Load daily notes for month
        var notes = await _db.DailyNotes
            .Where(dn => dn.UserId == userId
                      && dn.EntryDate >= firstDay
                      && dn.EntryDate <= lastDay)
            .AsNoTracking()
            .ToDictionaryAsync(dn => dn.EntryDate, ct);

        // Build lookup: (date, slotId) -> entry
        var entryLookup = entries
            .GroupBy(e => e.EntryDate)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(e => e.MealSlotId));

        // Column count: Date + slots + DailyNote
        var totalCols = 1 + slots.Count + 1;
        var noteColIndex = 1 + slots.Count + 1; // 1-based

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"{monthName} {year}");

        // ── Metadata rows (1–4) ───────────────────────────────────────────────

        ws.Cell(1, 1).Value = $"DietTracker Monthly Report — {monthName} {year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, totalCols).Merge();

        ws.Cell(2, 1).Value = $"Diet Plan: {primaryPlan?.Name ?? "No primary plan"}";
        ws.Cell(2, 2).Value = $"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC";

        ws.Cell(3, 1).Value = $"User: {userDisplayName}";

        // Row 4 is blank separator

        // ── Header row (row 5) ────────────────────────────────────────────────

        const int headerRow = 5;
        var headerRange = ws.Range(headerRow, 1, headerRow, totalCols);

        ws.Cell(headerRow, 1).Value = "Date";

        for (int i = 0; i < slots.Count; i++)
        {
            ws.Cell(headerRow, 2 + i).Value = slots[i].MealCategory;
        }

        ws.Cell(headerRow, noteColIndex).Value = "Daily Intake Note";

        // Style header row
        var purple = XLColor.FromHtml("#4F46E5");
        foreach (var cell in headerRange.Cells())
        {
            cell.Style.Font.Bold              = true;
            cell.Style.Fill.BackgroundColor   = purple;
            cell.Style.Font.FontColor         = XLColor.White;
            cell.Style.Alignment.WrapText     = true;
            cell.Style.Alignment.Vertical     = XLAlignmentVerticalValues.Center;
        }

        // Freeze header row and first column
        ws.SheetView.FreezeRows(headerRow);
        ws.SheetView.FreezeColumns(1);

        // ── Data rows (row 6+) ────────────────────────────────────────────────

        for (int d = 1; d <= lastDay.Day; d++)
        {
            var date     = new DateOnly(year, month, d);
            var dataRow  = headerRow + d;     // row 6 = day 1, row 7 = day 2, …

            // Date column
            ws.Cell(dataRow, 1).Value = date.ToString("dd MMM");

            // Meal slot columns
            entryLookup.TryGetValue(date, out var dayEntries);

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var col  = 2 + i;

                if (dayEntries is not null && dayEntries.TryGetValue(slot.Id, out var entry))
                {
                    var cellText = FormatEntryCell(entry);
                    ws.Cell(dataRow, col).Value = cellText;
                    ws.Cell(dataRow, col).Style.Alignment.WrapText = true;
                }
                // else: leave blank
            }

            // Daily note column
            if (notes.TryGetValue(date, out var note) && !string.IsNullOrWhiteSpace(note.NoteText))
            {
                ws.Cell(dataRow, noteColIndex).Value = note.NoteText;
                ws.Cell(dataRow, noteColIndex).Style.Alignment.WrapText = true;
            }
        }

        // ── Column widths ─────────────────────────────────────────────────────

        ws.Columns().AdjustToContents();

        // Date col: min 10
        if (ws.Column(1).Width < 10) ws.Column(1).Width = 10;

        // Meal slot cols: max 30
        for (int i = 0; i < slots.Count; i++)
        {
            var col = 2 + i;
            if (ws.Column(col).Width > 30) ws.Column(col).Width = 30;
            if (ws.Column(col).Width < 14) ws.Column(col).Width = 14;
        }

        // Note col: max 50
        if (ws.Column(noteColIndex).Width > 50) ws.Column(noteColIndex).Width = 50;
        if (ws.Column(noteColIndex).Width < 20) ws.Column(noteColIndex).Width = 20;

        // ── Serialize ─────────────────────────────────────────────────────────

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatEntryCell(DailyEntry entry)
    {
        string foodName;
        if (entry.FoodOptionId.HasValue && entry.FoodOption is not null)
        {
            foodName = entry.FoodOption.Name;
        }
        else if (!string.IsNullOrWhiteSpace(entry.OtherText))
        {
            foodName = $"{entry.OtherText} — Other";
        }
        else
        {
            foodName = string.Empty;
        }

        var followed = entry.FollowedPlan ? "[x]" : "[ ]";
        var time     = entry.ActualTime is not null
            ? $" ({FormatTime12h(entry.ActualTime)})"
            : string.Empty;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(foodName)) parts.Add(foodName);
        parts.Add(followed);

        var result = string.Join(" ", parts) + time;
        return result.Trim();
    }

    /// <summary>Converts "HH:mm" 24h string to "hh:mm AM/PM" 12h string.</summary>
    private static string FormatTime12h(string hhmm)
    {
        if (!TimeOnly.TryParseExact(hhmm, "HH:mm", out var t)) return hhmm;
        return t.ToString("hh:mm tt");
    }
}
