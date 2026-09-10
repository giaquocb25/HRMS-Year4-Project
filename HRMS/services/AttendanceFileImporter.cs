using HRMS.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Security.Cryptography;

namespace HRMS.services
{
    internal sealed class AttendanceFileImporter
    {
        private const long MaximumFileBytes = 20L * 1024L * 1024L;
        private const int MaximumRows = 50000;
        private readonly AttendanceService attendanceService = new AttendanceService();

        public AttendanceImportSummary Import(string path)
        {
            if (!AuthorizationService.Can(Permission.ImportAttendance))
                return new AttendanceImportSummary { Failed = 1, Details = "Tài khoản không có quyền nhập dữ liệu chấm công." };
            if (String.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return new AttendanceImportSummary { Failed = 1, Details = "Không tìm thấy file." };
            if (new FileInfo(path).Length > MaximumFileBytes)
                return new AttendanceImportSummary
                {
                    Failed = 1,
                    Details = "File vượt quá giới hạn 20 MB. Hãy chia thành nhiều file nhỏ hơn."
                };

            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".csv" && extension != ".tsv" && extension != ".txt")
                return new AttendanceImportSummary
                {
                    Failed = 1,
                    Details = "Định dạng không hỗ trợ. Hãy dùng .xlsx, .csv, .tsv hoặc .txt."
                };
            var rows = extension == ".xlsx" ? ReadXlsx(path) : ReadDelimited(path);
            if (rows.Count == 0)
                return new AttendanceImportSummary
                {
                    Failed = 1,
                    Details = "File không có dòng dữ liệu sau tiêu đề."
                };
            if (rows.Count > MaximumRows)
                return new AttendanceImportSummary
                {
                    Total = rows.Count,
                    Failed = rows.Count,
                    Details = "File vượt quá 50.000 dòng. Hãy chia thành nhiều file nhỏ hơn."
                };
            return ImportRows(rows, Path.GetFileName(path));
        }

        private AttendanceImportSummary ImportRows(IList<IDictionary<string, string>> rows, string deviceName)
        {
            var summary = new AttendanceImportSummary { Total = rows.Count };
            var errors = new List<string>();
            for (var index = 0; index < rows.Count; index++)
            {
                try
                {
                    var row = rows[index];
                    int employeeId;
                    DateTime eventTime;
                    var employeeText = Get(row, "employeeid", "employee_id", "id");
                    var employeeCode = Get(row, "employeecode", "employee_code", "manhanvien", "mãnhânviên");
                    var cardUid = Get(row, "carduid", "card_uid", "mathe", "mãthẻ");
                    var timeText = Get(row, "eventtime", "event_time", "timestamp", "datetime", "thoigian", "thờigian");

                    if (!TryParseDate(timeText, out eventTime))
                        throw new FormatException("thời gian không hợp lệ");
                    if (String.IsNullOrWhiteSpace(cardUid) && String.IsNullOrWhiteSpace(employeeCode)
                        && !Int32.TryParse(employeeText, out employeeId))
                        throw new FormatException("thiếu ID nội bộ, mã nhân viên hoặc mã thẻ");

                    var result = attendanceService.Record(new AttendanceInput
                    {
                        EmployeeId = Int32.TryParse(employeeText, out employeeId) ? (int?)employeeId : null,
                        EmployeeCode = employeeCode,
                        CardUid = cardUid,
                        EventTime = eventTime,
                        Source = "file",
                        ExternalEventId = CreateExternalId(
                            Get(row, "eventid", "event_id", "externalid", "external_event_id"),
                            employeeText, employeeCode, cardUid, eventTime),
                        DeviceName = deviceName,
                        RawData = String.Join(";", row.Select(item => item.Key + "=" + item.Value))
                    });

                    if (!result.Success)
                    {
                        summary.Failed++;
                        errors.Add("Dòng " + (index + 2) + ": " + result.Message);
                    }
                    else if (result.Duplicate)
                        summary.Duplicates++;
                    else
                        summary.Imported++;
                }
                catch (Exception ex)
                {
                    summary.Failed++;
                    errors.Add("Dòng " + (index + 2) + ": " + ex.Message);
                }
            }

            summary.Details = errors.Count == 0 ? "Không có lỗi." : String.Join(Environment.NewLine, errors.Take(20));
            AuditService.Log("ATTENDANCE_IMPORT_FILE", "AttendanceImport", deviceName,
                "Total=" + summary.Total + "; Imported=" + summary.Imported
                + "; Duplicates=" + summary.Duplicates + "; Failed=" + summary.Failed);
            return summary;
        }

        private static IList<IDictionary<string, string>> ReadDelimited(string path)
        {
            var lines = File.ReadAllLines(path, DetectEncoding(path)).Where(line => !String.IsNullOrWhiteSpace(line)).ToList();
            if (lines.Count < 2)
                return new List<IDictionary<string, string>>();

            var delimiter = new[] { ',', ';', '\t' }
                .OrderByDescending(candidate => lines[0].Count(ch => ch == candidate))
                .First();
            var headers = ParseDelimitedLine(lines[0], delimiter).Select(NormalizeHeader).ToList();
            return lines.Skip(1).Select(line =>
            {
                var values = ParseDelimitedLine(line, delimiter);
                return (IDictionary<string, string>)headers.Select((header, i) => new { header, value = i < values.Count ? values[i] : String.Empty })
                    .Where(item => !String.IsNullOrWhiteSpace(item.header))
                    .GroupBy(item => item.header)
                    .ToDictionary(group => group.Key, group => group.First().value, StringComparer.OrdinalIgnoreCase);
            }).ToList();
        }

        private static IList<IDictionary<string, string>> ReadXlsx(string path)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var sheetEntry = archive.GetEntry(GetFirstWorksheetPath(archive));
                if (sheetEntry == null)
                    throw new InvalidDataException("Không tìm thấy sheet đầu tiên trong file Excel.");
                if (sheetEntry.Length > MaximumFileBytes * 5)
                    throw new InvalidDataException("Sheet Excel sau giải nén vượt quá giới hạn an toàn.");

                XDocument document;
                using (var stream = sheetEntry.Open())
                    document = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var rawRows = document.Descendants(ns + "row")
                    .Select(row => ReadXlsxRow(row, ns, sharedStrings))
                    .ToList();
                if (rawRows.Count < 2)
                    return new List<IDictionary<string, string>>();

                var headers = rawRows[0].Select(NormalizeHeader).ToList();
                return rawRows.Skip(1).Select(values =>
                    (IDictionary<string, string>)headers.Select((header, i) => new { header, value = i < values.Count ? values[i] : String.Empty })
                        .Where(item => !String.IsNullOrWhiteSpace(item.header))
                        .GroupBy(item => item.header)
                        .ToDictionary(group => group.Key, group => group.First().value, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        private static IList<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();
            using (var stream = entry.Open())
            {
                var document = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                return document.Descendants(ns + "si")
                    .Select(item => String.Concat(item.Descendants(ns + "t").Select(t => t.Value)))
                    .ToList();
            }
        }

        private static string GetFirstWorksheetPath(ZipArchive archive)
        {
            var workbookEntry = archive.GetEntry("xl/workbook.xml");
            var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (workbookEntry == null || relationshipsEntry == null)
                return "xl/worksheets/sheet1.xml";

            XDocument workbook;
            XDocument relationships;
            using (var stream = workbookEntry.Open())
                workbook = XDocument.Load(stream);
            using (var stream = relationshipsEntry.Open())
                relationships = XDocument.Load(stream);

            XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace officeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace packageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
            var firstSheet = workbook.Descendants(spreadsheet + "sheet").FirstOrDefault();
            var relationId = firstSheet == null ? null : (string)firstSheet.Attribute(officeRelationships + "id");
            var relationship = relationships.Descendants(packageRelationships + "Relationship")
                .FirstOrDefault(item => String.Equals((string)item.Attribute("Id"), relationId, StringComparison.Ordinal));
            var target = relationship == null ? null : (string)relationship.Attribute("Target");
            if (String.IsNullOrWhiteSpace(target))
                return "xl/worksheets/sheet1.xml";

            target = target.Replace('\\', '/').TrimStart('/');
            return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target : "xl/" + target;
        }

        private static IList<string> ReadXlsxRow(XElement row, XNamespace ns, IList<string> sharedStrings)
        {
            var result = new List<string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = (string)cell.Attribute("r") ?? String.Empty;
                var column = ColumnIndex(reference);
                while (result.Count <= column)
                    result.Add(String.Empty);

                var type = (string)cell.Attribute("t");
                var value = (string)cell.Element(ns + "v") ?? String.Empty;
                int sharedIndex;
                if (type == "s" && Int32.TryParse(value, out sharedIndex) && sharedIndex < sharedStrings.Count)
                    value = sharedStrings[sharedIndex];
                else if (type == "inlineStr")
                    value = String.Concat(cell.Descendants(ns + "t").Select(item => item.Value));
                result[column] = value;
            }
            return result;
        }

        private static int ColumnIndex(string reference)
        {
            var index = 0;
            foreach (var ch in reference.TakeWhile(Char.IsLetter))
                index = index * 26 + (Char.ToUpperInvariant(ch) - 'A' + 1);
            return Math.Max(0, index - 1);
        }

        private static IList<string> ParseDelimitedLine(string line, char delimiter)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                        quoted = !quoted;
                }
                else if (ch == delimiter && !quoted)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                    current.Append(ch);
            }
            result.Add(current.ToString().Trim());
            return result;
        }

        private static bool TryParseDate(string value, out DateTime result)
        {
            double serial;
            if (Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out serial) && serial > 1)
            {
                result = DateTime.FromOADate(serial);
                return true;
            }
            var formats = new[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" };
            return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)
                || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out result);
        }

        private static string Get(IDictionary<string, string> row, params string[] names)
        {
            string value;
            foreach (var name in names)
                if (row.TryGetValue(NormalizeHeader(name), out value) && !String.IsNullOrWhiteSpace(value))
                    return value.Trim();
            return null;
        }

        private static string NormalizeHeader(string value)
        {
            if (value == null)
                return String.Empty;
            return new string(value.Trim().ToLowerInvariant().Where(ch => Char.IsLetterOrDigit(ch) || ch == '_').ToArray());
        }

        private static Encoding DetectEncoding(string path)
        {
            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                reader.Peek();
                return reader.CurrentEncoding;
            }
        }

        private static string CreateExternalId(string eventId, string employeeId, string employeeCode,
            string cardUid, DateTime eventTime)
        {
            if (String.IsNullOrWhiteSpace(eventId))
                return null;
            var value = String.Join("|", eventId.Trim(), employeeId, employeeCode, cardUid,
                eventTime.ToUniversalTime().Ticks);
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", String.Empty);
        }
    }
}
