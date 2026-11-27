using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

internal static class ReaderImportFileReader
{
    private const long MaximumFileBytes = 10 * 1024 * 1024;
    private const int MaximumRows = 500;
    private static readonly XNamespace MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    internal static IReadOnlyList<ReaderImportRow> Read(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length is <= 0 or > MaximumFileBytes)
            throw new InvalidDataException("Tệp không tồn tại, rỗng hoặc vượt quá 10 MiB.");
        var rows = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".csv" => ReadCsv(File.ReadAllText(path, Encoding.UTF8)),
            ".xlsx" => ReadXlsx(path),
            _ => throw new InvalidDataException("Chỉ hỗ trợ tệp .xlsx hoặc .csv.")
        };
        if (rows.Count is < 1 or > MaximumRows)
            throw new InvalidDataException($"Tệp phải có từ 1 đến {MaximumRows} dòng dữ liệu.");
        return rows;
    }

    private static IReadOnlyList<ReaderImportRow> ReadCsv(string content)
    {
        var table = ParseCsv(content);
        return MapTable(table);
    }

    private static IReadOnlyList<ReaderImportRow> ReadXlsx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count > 64 || archive.Entries.Sum(entry => entry.Length) > 50L * 1024 * 1024)
            throw new InvalidDataException("Workbook Excel có cấu trúc hoặc kích thước giải nén vượt giới hạn.");
        var workbook = LoadXml(archive, "xl/workbook.xml");
        var firstSheet = workbook.Root?.Element(MainNs + "sheets")?.Elements(MainNs + "sheet").FirstOrDefault()
            ?? throw new InvalidDataException("Workbook không có sheet dữ liệu.");
        var relationshipId = (string?)firstSheet.Attribute(RelationshipNs + "id")
            ?? throw new InvalidDataException("Không xác định được sheet đầu tiên.");
        var relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
        var target = relationships.Root?.Elements(PackageRelationshipNs + "Relationship")
            .FirstOrDefault(element => (string?)element.Attribute("Id") == relationshipId)?
            .Attribute("Target")?.Value
            ?? throw new InvalidDataException("Không tìm thấy nội dung sheet đầu tiên.");
        var worksheetPath = target.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : "xl/" + target.Replace("\\", "/", StringComparison.Ordinal);
        var worksheet = LoadXml(archive, worksheetPath);
        var sharedStrings = ReadSharedStrings(archive);
        var zeroPadWidths = ReadZeroPadWidths(archive);
        var table = new List<string[]>();
        foreach (var row in worksheet.Descendants(MainNs + "row"))
        {
            var cells = new List<string>();
            foreach (var cell in row.Elements(MainNs + "c"))
            {
                var index = ColumnIndex((string?)cell.Attribute("r") ?? "A1");
                while (cells.Count <= index) cells.Add("");
                var type = (string?)cell.Attribute("t");
                var value = cell.Element(MainNs + "v")?.Value ?? "";
                cells[index] = type switch
                {
                    "s" => SharedString(sharedStrings, value),
                    "inlineStr" => string.Concat(cell.Descendants(MainNs + "t").Select(text => text.Value)),
                    "b" => value == "1" ? "TRUE" : "FALSE",
                    _ => FormatNumericCell(value, (string?)cell.Attribute("s"), zeroPadWidths)
                };
            }
            table.Add(cells.ToArray());
            if (table.Count > MaximumRows + 20)
                throw new InvalidDataException($"Sheet vượt giới hạn {MaximumRows} dòng dữ liệu.");
        }
        return MapTable(table);
    }

    private static XDocument LoadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidDataException("Workbook Excel không đúng cấu trúc.");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return Array.Empty<string>();
        var document = LoadXml(archive, "xl/sharedStrings.xml");
        return document.Root?.Elements(MainNs + "si")
            .Select(item => string.Concat(item.Descendants(MainNs + "t").Select(text => text.Value))).ToArray()
            ?? Array.Empty<string>();
    }

    private static IReadOnlyList<int> ReadZeroPadWidths(ZipArchive archive)
    {
        if (archive.GetEntry("xl/styles.xml") is null) return Array.Empty<int>();
        var document = LoadXml(archive, "xl/styles.xml");
        var formats = document.Root?.Element(MainNs + "numFmts")?.Elements(MainNs + "numFmt")
            .Select(element => (Id: (string?)element.Attribute("numFmtId"), Code: (string?)element.Attribute("formatCode")))
            .Where(item => int.TryParse(item.Id, NumberStyles.None, CultureInfo.InvariantCulture, out _)
                && item.Code is not null && Regex.IsMatch(item.Code, "\\A0+\\z", RegexOptions.CultureInvariant))
            .ToDictionary(item => int.Parse(item.Id!, CultureInfo.InvariantCulture), item => item.Code!.Length)
            ?? new Dictionary<int, int>();
        return document.Root?.Element(MainNs + "cellXfs")?.Elements(MainNs + "xf")
            .Select(element => int.TryParse((string?)element.Attribute("numFmtId"), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                && formats.TryGetValue(id, out var width) ? width : 0)
            .ToArray() ?? Array.Empty<int>();
    }

    private static string FormatNumericCell(string value, string? styleValue, IReadOnlyList<int> zeroPadWidths)
    {
        if (int.TryParse(styleValue, NumberStyles.None, CultureInfo.InvariantCulture, out var styleIndex)
            && styleIndex >= 0 && styleIndex < zeroPadWidths.Count && zeroPadWidths[styleIndex] > 0
            && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number >= 0)
            return number.ToString("D" + zeroPadWidths[styleIndex], CultureInfo.InvariantCulture);
        return value;
    }

    private static string SharedString(IReadOnlyList<string> sharedStrings, string index)
        => int.TryParse(index, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 0 && value < sharedStrings.Count
            ? sharedStrings[value]
            : throw new InvalidDataException("Workbook tham chiếu shared string không hợp lệ.");

    private static int ColumnIndex(string cellReference)
    {
        var index = 0;
        foreach (var character in cellReference.TakeWhile(char.IsLetter))
            index = checked(index * 26 + char.ToUpperInvariant(character) - 'A' + 1);
        return index - 1;
    }

    private static IReadOnlyList<ReaderImportRow> MapTable(IReadOnlyList<string[]> table)
    {
        var headerRowIndex = -1;
        Dictionary<string, int>? columns = null;
        for (var index = 0; index < Math.Min(table.Count, 20); index++)
        {
            var candidate = BuildColumnMap(table[index]);
            if ((candidate.ContainsKey("masinhvien") || candidate.ContainsKey("magiangvien") || candidate.ContainsKey("madinhdanhtruong"))
                && candidate.ContainsKey("hoten"))
            {
                headerRowIndex = index;
                columns = candidate;
                break;
            }
        }
        if (columns is null || headerRowIndex < 0)
            throw new InvalidDataException("Không tìm thấy hàng tiêu đề có cột mã sinh viên/mã cán bộ và họ tên trong 20 dòng đầu.");

        var result = new List<ReaderImportRow>();
        for (var index = headerRowIndex + 1; index < table.Count; index++)
        {
            var values = table[index];
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            if (result.Count >= MaximumRows)
                throw new InvalidDataException($"Tệp vượt giới hạn {MaximumRows} dòng dữ liệu.");
            string Get(string key) => columns.TryGetValue(key, out var column) && column < values.Length ? values[column].Trim() : "";
            var type = CanonicalType(Get("loaidocgia"));
            var genericCode = Get("madinhdanhtruong");
            var studentId = Get("masinhvien");
            var staffId = Get("magiangvien");
            if (genericCode.Length > 0)
            {
                if (type == "SINH_VIEN") studentId = genericCode;
                else staffId = genericCode;
            }
            result.Add(new ReaderImportRow(index + 1, studentId, staffId, Get("hoten"), type,
                Get("makhoa"), Get("manganh"), Get("khoahoc"), Get("trangthaihoctap"), Get("thudientu")));
        }
        return result;
    }

    private static Dictionary<string, int> BuildColumnMap(string[] headers)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < headers.Length; index++)
        {
            var normalized = NormalizeHeader(headers[index]);
            var key = normalized switch
            {
                "masinhvien" or "masosinhvien" or "mssv" or "masv" or "mathesinhvien" or "studentid" => "masinhvien",
                "magiangvien" or "macanbo" or "macanbogiangvien" or "masocanbo" or "masocanbogiangvien" or "mathecanbo" or "staffid" => "magiangvien",
                "madinhdanhtruong" or "mathetruong" or "madocgia" => "madinhdanhtruong",
                "hoten" or "hovaten" or "fullname" => "hoten",
                "loaidocgia" or "doituong" or "loaidoituong" => "loaidocgia",
                "makhoa" or "khoaphongban" or "facultycode" => "makhoa",
                "manganh" or "majorcode" => "manganh",
                "khoahoc" or "nienkhoa" or "khoalop" => "khoahoc",
                "trangthaihoctap" or "trangthaicongtac" or "tinhtranghoctap" or "trangthai" => "trangthaihoctap",
                "thudientu" or "email" or "emailtruong" => "thudientu",
                _ => ""
            };
            if (key.Length > 0 && !result.TryAdd(key, index))
                throw new InvalidDataException($"Tệp có nhiều cột cùng ánh xạ tới {key}.");
        }
        if (result.ContainsKey("madinhdanhtruong") && (result.ContainsKey("masinhvien") || result.ContainsKey("magiangvien")))
            throw new InvalidDataException("Dùng cột mã tổng quát hoặc cột mã riêng, không dùng đồng thời cả hai dạng.");
        return result;
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = (value ?? "").Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
                builder.Append(character);
        return builder.ToString().Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();
    }

    private static string CanonicalType(string value)
    {
        var normalized = NormalizeHeader(value);
        return normalized switch
        {
            "sinhvien" or "sv" => "SINH_VIEN",
            "canbo" or "nhanvien" => "CAN_BO",
            "giangvien" or "gv" => "GIANG_VIEN",
            "trogiang" => "TRO_GIANG",
            "cnv" => "CNV",
            _ => value.Trim().ToUpperInvariant()
        };
    }

    private static List<string[]> ParseCsv(string content)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < content.Length && content[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"') quoted = false;
                else field.Append(character);
            }
            else if (character == '"' && field.Length == 0) quoted = true;
            else if (character == ',')
            {
                row.Add(field.ToString()); field.Clear();
            }
            else if (character is '\r' or '\n')
            {
                row.Add(field.ToString()); field.Clear();
                if (row.Any(value => value.Length > 0)) rows.Add(row.ToArray());
                row.Clear();
                if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n') index++;
                if (rows.Count > MaximumRows + 20) throw new InvalidDataException($"Tệp vượt giới hạn {MaximumRows} dòng dữ liệu.");
            }
            else field.Append(character);
        }
        if (quoted) throw new InvalidDataException("CSV có dấu ngoặc kép chưa được đóng.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            if (row.Any(value => value.Length > 0)) rows.Add(row.ToArray());
        }
        return rows;
    }
}
