using System.Text.Json;

namespace ThuVienDAU.Circulation;

public sealed class CirculationRules
{
    public Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)[]> OpeningHours { get; } = new();
    public HashSet<DateOnly> Holidays { get; } = new();
    public bool CalendarConfirmed { get; set; }
    public int StopMinutes { get; private set; }
    public static CirculationRules Load()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Cau_hinh_du_an.json")));
        var root = doc.RootElement;
        var rules = new CirculationRules { StopMinutes = root.GetProperty("Muon").GetProperty("SoPhutNgungTruocDongCua").GetInt32(), CalendarConfirmed = root.GetProperty("DaXacNhanLichMienPhat").GetBoolean() };
        var opening = root.GetProperty("ThoiGianMoCua");
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            string key = day == DayOfWeek.Sunday ? "ChuNhat" : day == DayOfWeek.Saturday ? "ThuBay" : "ThuHaiDenThuSau";
            rules.OpeningHours[day] = opening.GetProperty(key).EnumerateArray().Select(x => (TimeSpan.Parse(x[0].GetString()!), TimeSpan.Parse(x[1].GetString()!))).ToArray();
        }
        foreach (var date in root.GetProperty("NgayLeTetMienPhat").EnumerateArray()) rules.Holidays.Add(DateOnly.ParseExact(date.GetString()!, "yyyy-MM-dd"));
        return rules;
    }
    public void RequireOpen(DateTime now, bool borrowing)
    {
        if (CalendarConfirmed && Holidays.Contains(DateOnly.FromDateTime(now))) throw new InvalidOperationException("Thư viện nghỉ lễ/Tết vào ngày này.");
        if (!OpeningHours.TryGetValue(now.DayOfWeek, out var windows)) throw new InvalidOperationException("Thiếu lịch mở cửa.");
        foreach (var (start, end) in windows)
        {
            if (now.TimeOfDay < start || now.TimeOfDay >= end) continue;
            if (borrowing && now.TimeOfDay >= end - TimeSpan.FromMinutes(StopMinutes))
                throw new InvalidOperationException($"Ngừng tiếp nhận mượn trong {StopMinutes} phút cuối buổi (đóng cửa {end:hh\\:mm}).");
            return;
        }
        throw new InvalidOperationException("Ngoài giờ mở cửa: T2–T6 07:30–11:30, 13:30–17:00; T7 07:30–11:30; Chủ nhật nghỉ.");
    }
    public (int Raw, int Excluded, int Net) Overdue(DateTime due, DateTime end)
    {
        if (end <= due) return (0, 0, 0);
        // A due date is the end of its calendar day; each subsequent started day is one overdue day.
        int raw = Math.Max(1, (end.Date - due.Date).Days);
        int excluded = Enumerable.Range(1, raw).Count(i => Holidays.Contains(DateOnly.FromDateTime(due.Date.AddDays(i))));
        return (raw, excluded, raw - excluded);
    }
    public static DateTime EndOfDay(DateTime date) => date.Date.AddDays(1).AddSeconds(-1);
}
