using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Services;

namespace ThuVienDAU.Server.Modules.TV1_HuynhCongThu.HT;

public sealed class DatabaseBackupService(DatabaseOptions options, AuthenticationService authentication)
{
    private const long MaximumArchiveBytes = 2L * 1024 * 1024 * 1024;
    private readonly MySqlConnectionFactory connectionFactory = new(options);
    private static string BackupRoot => Path.Combine(AppContext.BaseDirectory, "data", "backups");
    private static string AttachmentRoot => Path.Combine(AppContext.BaseDirectory, "data", "tep-do-an");

    public async Task<BackupListResult> ListAsync(string sessionToken, CancellationToken ct)
    {
        await RequireManager(sessionToken, ct);
        Directory.CreateDirectory(BackupRoot);
        var backups = new List<BackupInfo>();
        foreach (var path in Directory.EnumerateFiles(BackupRoot, "*.zip").OrderByDescending(File.GetLastWriteTimeUtc).Take(100))
        {
            var manifest = ReadManifest(path);
            if (!string.Equals(manifest.Database, options.Database, StringComparison.Ordinal)) continue;
            backups.Add(new(manifest.BackupId, manifest.CreatedAtUtc, new FileInfo(path).Length, manifest.AttachmentCount, manifest.Database));
        }
        return new(options.Database, backups);
    }

    public async Task<BackupInfo> CreateAsync(string sessionToken, CancellationToken ct)
    {
        var actor = await RequireManager(sessionToken, ct);
        Directory.CreateDirectory(BackupRoot);
        var id = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'") + "-" + Guid.NewGuid().ToString("N");
        var path = Path.Combine(BackupRoot, id + ".zip");
        var pendingPath = Path.Combine(BackupRoot, id + ".pending");
        var createdAt = DateTime.UtcNow;
        var complete = false;
        try
        {
            using (var archive = ZipFile.Open(pendingPath, ZipArchiveMode.Create))
            {
                await DumpDatabaseAsync(archive, ct);
                var attachments = Directory.Exists(AttachmentRoot)
                    ? Directory.EnumerateFiles(AttachmentRoot, "*.pdf", SearchOption.TopDirectoryOnly)
                        .Where(file => Regex.IsMatch(Path.GetFileName(file), "\\A[a-f0-9]{32}\\.pdf\\z", RegexOptions.CultureInvariant))
                        .ToArray()
                    : Array.Empty<string>();
                foreach (var file in attachments)
                {
                    var entry = archive.CreateEntry("attachments/" + Path.GetFileName(file), CompressionLevel.Optimal);
                    await using var target = entry.Open();
                    await using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                    await source.CopyToAsync(target, ct);
                }
                var backupManifest = new BackupManifest(id, options.Database, createdAt, attachments.Length);
                var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
                await using var manifestStream = manifestEntry.Open();
                await JsonSerializer.SerializeAsync(manifestStream, backupManifest, cancellationToken: ct);
            }
            var validatedManifest = ReadManifest(pendingPath);
            File.Move(pendingPath, path);
            complete = true;
            await WriteAuditAsync(actor.AccountId, "HT04_BACKUP_THANH_CONG", id,
                new { database = options.Database, attachments = validatedManifest.AttachmentCount }, ct);
            return new(id, createdAt, new FileInfo(path).Length, validatedManifest.AttachmentCount, options.Database);
        }
        catch (Exception exception)
        {
            if (File.Exists(pendingPath)) File.Delete(pendingPath);
            if (!complete)
            {
                try
                {
                    await WriteAuditAsync(actor.AccountId, "HT04_BACKUP_THAT_BAI", id,
                        new { database = options.Database, error = exception.GetType().Name }, CancellationToken.None);
                }
                catch (Exception auditException)
                {
                    throw new AggregateException("Sao lưu thất bại và không thể ghi audit kết quả.", exception, auditException);
                }
            }
            throw;
        }
    }

    public async Task<RestoreResult> RestoreAsync(string sessionToken, RestoreBackupRequest request, CancellationToken ct)
    {
        var actor = await RequireManager(sessionToken, ct);
        if (request.BackupId is null || !Regex.IsMatch(request.BackupId,
                "\\A[0-9]{8}T[0-9]{9}Z-[a-f0-9]{32}\\z", RegexOptions.CultureInvariant))
            throw new ArgumentException("Mã bản sao lưu không hợp lệ.");
        if (!string.Equals(request.Confirmation, "PHUC_HOI " + options.Database, StringComparison.Ordinal))
            throw new ArgumentException("Xác nhận phục hồi không khớp tên CSDL cấu hình.");

        Directory.CreateDirectory(BackupRoot);
        var selectedPath = Path.Combine(BackupRoot, request.BackupId + ".zip");
        if (!File.Exists(selectedPath)) throw new FileNotFoundException("Không tìm thấy bản sao lưu đã chọn.");
        var selectedManifest = ReadManifest(selectedPath);
        if (!string.Equals(selectedManifest.Database, options.Database, StringComparison.Ordinal))
            throw new InvalidOperationException("Bản sao lưu thuộc CSDL khác, không thể phục hồi vào CSDL hiện tại.");

        var safetyBackup = await CreateAsync(sessionToken, ct);
        var safetyPath = Path.Combine(BackupRoot, safetyBackup.BackupId + ".zip");
        var stageRoot = Path.Combine(BackupRoot, ".restore-" + Guid.NewGuid().ToString("N"));
        var liveRoot = AttachmentRoot;
        var oldRoot = liveRoot + ".before-restore-" + Guid.NewGuid().ToString("N");
        var movedOld = false;
        var movedNew = false;
        try
        {
            ExtractAttachments(selectedPath, stageRoot);
            await WriteAuditAsync(actor.AccountId, "HT04_PHUC_HOI_BAT_DAU", request.BackupId,
                new { database = options.Database, safetyBackup = safetyBackup.BackupId }, ct);
            await ImportDatabaseAsync(selectedPath, ct);
            if (Directory.Exists(liveRoot))
            {
                Directory.Move(liveRoot, oldRoot);
                movedOld = true;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(liveRoot)!);
            Directory.Move(stageRoot, liveRoot);
            movedNew = true;
        }
        catch (Exception restoreException)
        {
            try
            {
                await ImportDatabaseAsync(safetyPath, CancellationToken.None);
                if (movedNew && Directory.Exists(liveRoot)) Directory.Delete(liveRoot, recursive: true);
                if (movedOld && Directory.Exists(oldRoot)) Directory.Move(oldRoot, liveRoot);
            }
            catch (Exception rollbackException)
            {
                authentication.InvalidateAllSessions();
                throw new InvalidOperationException(
                    "Phục hồi thất bại và không thể hoàn tác hoàn toàn từ bản sao lưu an toàn " + safetyBackup.BackupId + ". Cần kiểm tra CSDL và thư mục tệp ngay.",
                    new AggregateException(restoreException, rollbackException));
            }

            try
            {
                await WriteAuditAsync(null, "HT04_PHUC_HOI_THAT_BAI", request.BackupId,
                    new
                    {
                        database = options.Database,
                        error = restoreException.GetType().Name,
                        safetyBackup = safetyBackup.BackupId,
                        sourceActor = new { actor.AccountId, actor.Username, actor.Role }
                    }, CancellationToken.None);
            }
            catch (Exception auditException)
            {
                throw new AggregateException("Phục hồi thất bại; đã khôi phục từ backup an toàn nhưng không ghi được audit kết quả.",
                    restoreException, auditException);
            }
            throw;
        }
        finally
        {
            if (Directory.Exists(stageRoot)) Directory.Delete(stageRoot, recursive: true);
        }

        authentication.InvalidateAllSessions();
        try
        {
            await WriteAuditAsync(null, "HT04_PHUC_HOI_THANH_CONG", request.BackupId,
                new
                {
                    database = options.Database,
                    safetyBackup = safetyBackup.BackupId,
                    sourceActor = new { actor.AccountId, actor.Username, actor.Role }
                }, CancellationToken.None);
        }
        catch (Exception auditException)
        {
            throw new InvalidOperationException(
                "Phục hồi đã hoàn tất nhưng không thể ghi audit kết quả; dữ liệu không bị hoàn tác. Kiểm tra CSDL và audit ngay.",
                auditException);
        }

        return new(request.BackupId, safetyBackup.BackupId, options.Database, true);
    }

    private async Task<SessionInfo> RequireManager(string sessionToken, CancellationToken ct)
    {
        var actor = await authentication.ValidateSessionAsync(sessionToken, ct);
        if (actor.Role is not ("QUAN_TRI" or "QUAN_LY_THU_VIEN"))
            throw new UnauthorizedAccessException("Chỉ Quản trị hoặc Quản lý thư viện được sao lưu/phục hồi CSDL.");
        return actor;
    }

    private async Task DumpDatabaseAsync(ZipArchive archive, CancellationToken ct)
    {
        var entry = archive.CreateEntry("database.sql", CompressionLevel.Optimal);
        await using var target = entry.Open();
        using var process = StartTool("mysqldump.exe", [
            "--no-defaults", "--protocol=TCP", "--host=" + options.Server, "--port=" + options.Port,
            "--user=" + options.User, "--ssl-mode=" + options.SslMode, "--default-character-set=utf8mb4",
            "--single-transaction", "--routines", "--triggers", "--events", "--hex-blob", "--no-tablespaces",
            "--set-gtid-purged=OFF", options.Database
        ], redirectInput: false);
        process.Start();
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        try
        {
            await process.StandardOutput.BaseStream.CopyToAsync(target, ct);
            await process.WaitForExitAsync(ct);
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException("mysqldump không thể tạo bản sao lưu. Kiểm tra MySQL client và quyền đọc cấu trúc/dữ liệu.");
        _ = stderr;
    }

    private async Task ImportDatabaseAsync(string archivePath, CancellationToken ct)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.GetEntry("database.sql") ?? throw new InvalidDataException("Bản sao lưu thiếu database.sql.");
        using var process = StartTool("mysql.exe", [
            "--no-defaults", "--protocol=TCP", "--host=" + options.Server, "--port=" + options.Port,
            "--user=" + options.User, "--ssl-mode=" + options.SslMode, "--default-character-set=utf8mb4",
            "--binary-mode=1", options.Database
        ], redirectInput: true);
        process.Start();
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        try
        {
            await using var source = entry.Open();
            await source.CopyToAsync(process.StandardInput.BaseStream, ct);
            process.StandardInput.Close();
            await process.WaitForExitAsync(ct);
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
        var stderr = await stderrTask;
        _ = await stdoutTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException("mysql không thể phục hồi bản sao lưu. Đã thử hoàn tác tự động nếu thao tác đích thất bại.");
        _ = stderr;
    }

    private Process StartTool(string executable, IEnumerable<string> arguments, bool redirectInput)
    {
        var path = FindTool(executable);
        if (path is null)
            throw new InvalidOperationException($"Không tìm thấy {executable}; cài MySQL Server/client trên máy server và thêm thư mục bin vào PATH.");
        var startInfo = new ProcessStartInfo(path)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = redirectInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(Convert.ToString(argument, System.Globalization.CultureInfo.InvariantCulture)!);
        if (!string.IsNullOrEmpty(options.Password)) startInfo.Environment["MYSQL_PWD"] = options.Password;
        return new Process { StartInfo = startInfo };
    }

    private static string? FindTool(string executable)
    {
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), executable);
            if (File.Exists(candidate)) return candidate;
        }
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var mysqlRoot = Path.Combine(programFiles, "MySQL");
        if (Directory.Exists(mysqlRoot))
            foreach (var directory in Directory.EnumerateDirectories(mysqlRoot, "MySQL Server *").OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var candidate = Path.Combine(directory, "bin", executable);
                if (File.Exists(candidate)) return candidate;
            }
        return null;
    }

    private static BackupManifest ReadManifest(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count > 5002 || archive.Entries.Sum(entry => entry.Length) > MaximumArchiveBytes)
            throw new InvalidDataException("Bản sao lưu vượt giới hạn số tệp hoặc kích thước giải nén.");
        var manifestEntry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException("Bản sao lưu thiếu manifest.");
        var databaseEntry = archive.GetEntry("database.sql");
        if (databaseEntry is null || databaseEntry.Length == 0)
            throw new InvalidDataException("Bản sao lưu thiếu dữ liệu SQL.");
        var attachmentCount = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName is "manifest.json" or "database.sql") continue;
            if (!entry.FullName.StartsWith("attachments/", StringComparison.Ordinal)
                || !Regex.IsMatch(entry.FullName["attachments/".Length..], "\\A[a-f0-9]{32}\\.pdf\\z", RegexOptions.CultureInvariant))
                throw new InvalidDataException("Bản sao lưu chứa mục hoặc đường dẫn không hợp lệ.");
            attachmentCount++;
        }
        using var stream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<BackupManifest>(stream)
            ?? throw new InvalidDataException("Manifest bản sao lưu không hợp lệ.");
        if (Path.GetFileNameWithoutExtension(path) != manifest.BackupId || manifest.AttachmentCount != attachmentCount)
            throw new InvalidDataException("Tên/mã bản sao lưu hoặc số lượng tệp đính kèm không khớp.");
        return manifest;
    }

    private static void ExtractAttachments(string archivePath, string destination)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        if (archive.Entries.Count > 5000 || archive.Entries.Sum(entry => entry.Length) > MaximumArchiveBytes)
            throw new InvalidDataException("Bản sao lưu vượt giới hạn số tệp hoặc kích thước giải nén.");
        Directory.CreateDirectory(destination);
        foreach (var entry in archive.Entries.Where(item => item.FullName.StartsWith("attachments/", StringComparison.Ordinal)))
        {
            var fileName = entry.FullName["attachments/".Length..];
            if (!Regex.IsMatch(fileName, "\\A[a-f0-9]{32}\\.pdf\\z", RegexOptions.CultureInvariant))
                throw new InvalidDataException("Bản sao lưu chứa đường dẫn tệp đính kèm không hợp lệ.");
            var path = Path.Combine(destination, fileName);
            using var source = entry.Open();
            using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(target);
        }
    }

    private async Task WriteAuditAsync(int? actorId, string action, string key, object details, CancellationToken ct)
    {
        await using var connection = await connectionFactory.OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet) VALUES(@actor,@action,'CSDL',@key,@details)";
        command.Parameters.AddWithValue("@actor", (object?)actorId ?? DBNull.Value);
        command.Parameters.AddWithValue("@action", action);
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@details", JsonSerializer.Serialize(details));
        await command.ExecuteNonQueryAsync(ct);
    }

    private sealed record BackupManifest(string BackupId, string Database, DateTime CreatedAtUtc, int AttachmentCount);
}
