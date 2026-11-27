# Ghép nhánh login TV1 với main — 25/11/2026

Nhánh `feature/TV1-HT02-login-session`, TV1 = **Huỳnh Công Thủ**. Merge main f8c6ade
vào lịch sử TV1 hiện có, giữ các commit tác giả và toàn bộ phần tra cứu độc giả,
quyền mượn, cấu hình/nhật ký và báo cáo của TV1. Không ghép PR TV2 còn chờ vào PR này.

## Xung đột đã xử lý

- Dùng thư mục `TV1_HuynhCongThu` ở cả client/server; chuyển các service/controller/
  form mới từ tên cũ, sửa namespace cùng phần ghi tên người phụ trách.
- Giữ WinForms client + Console TCP server của main; bỏ WebApplication/ASP.NET và
  HttpClient của nhánh cũ. Client đọc Host/Port/TimeoutSeconds; cấu hình BaseUrl cũ
  được báo rõ cần chuyển đổi.
- Giữ các menu mới của TV1 và menu biên mục TV2; truyền phiên đăng nhập thật vào
  CatalogWorkspaceForm. Người dùng không phải nhập token TV2 bằng tay sau login.
- Server dùng cùng instance phiên cho đăng nhập, TV1 và TV2. Phiên ngẫu nhiên,
  hết hạn 30 phút, thu hồi khi đăng xuất hoặc khóa đăng nhập độc giả. Khởi động
  lại server yêu cầu đăng nhập lại.
- Các lệnh TV1 kiểm trạng thái/vai trò hiện tại trong DB; không lấy SessionId hoặc
  vai trò từ payload để thay token trên frame. Các ghi dữ liệu vẫn dùng giao dịch
  và thủ tục/trigger hiện hành. Quyền khóa đăng nhập và quyền mượn độc lập.
- Giữ copy `Cau_hinh_du_an.json` cạnh assembly của Console server cho HT-03.
- Phản hồi TV1 quá 1 MiB được trả lỗi yêu cầu thu hẹp bộ lọc, giữ nguyên giới hạn
  frame. Không âm thầm bỏ bớt kết quả để xuất CSV không đầy đủ.

## Chạy

Chuẩn bị schema/cấu hình theo README. Không chạy lại SQL tạo mới lên DB có dữ liệu.
Cần tài khoản MySQL có quyền phù hợp với thao tác; SELECT đơn thuần không đủ để
cấp tài khoản, khóa/mở hoặc ghi nhật ký. Nhánh này không tự cấp quyền trên DB thật.

```powershell
dotnet restore ThuVienDAU.sln --locked-mode
dotnet build ThuVienDAU.sln -c Release --no-restore
dotnet run --project server
```

Mở terminal khác, chạy `dotnet run --project client`, đăng nhập rồi sử dụng menu.
Client mặc định nối TCP 127.0.0.1:5080. Không còn URL `/api/...` trong transport.
Không cần dùng `--tv2-demo-account` khi đã đăng nhập. Chế độ demo loopback chỉ giữ
để bộ test TV2 cũ chạy, không phải phiên người dùng LAN.

## Hợp đồng TCP bổ sung

Payload/SessionToken dùng cùng frame 1 MiB hiện hành.

| Lệnh | Payload | Quyền |
| --- | --- | --- |
| AUTH_LOGIN | LoginRequest | Xác thực tài khoản hoạt động |
| AUTH_LOGOUT | rỗng | Thu hồi token frame, idempotent |
| AUTH_ACCOUNTS / AUTH_PROVISION | rỗng / ProvisionAccountRequest | QUAN_TRI |
| AUTH_READER_ACCOUNTS / AUTH_READER_STATUS | rỗng / ReaderAccountStatusRequest | BIEN_MUC |
| TV1_READER_SEARCH | ReaderQuery | QUAN_TRI, QUAN_LY_THU_VIEN, BIEN_MUC, THU_THU |
| TV1_BORROW_PERMISSION | BorrowPermissionCommand | Chỉ QUAN_LY_THU_VIEN |
| TV1_CONFIGURATION | rỗng | Vai trò cán bộ thư viện ở trên |
| TV1_AUDIT | AuditQuery, khoảng [From,To) | QUAN_TRI, QUAN_LY_THU_VIEN |
| TV1_READER_REPORT | ReaderReportQuery | QUAN_TRI, QUAN_LY_THU_VIEN |
| TV1_READER_IMPORT_PREVIEW / TV1_READER_IMPORT_COMMIT | ReaderImportCommand | QUAN_LY_THU_VIEN |
| TV1_BACKUP_LIST / TV1_BACKUP_CREATE / TV1_BACKUP_RESTORE | RestoreBackupRequest hoặc rỗng | QUAN_TRI, QUAN_LY_THU_VIEN |

Các DTO cũ được giữ, bổ sung `TV1CommandContracts.cs`. Client service giữ chữ ký
được các form TV1 gọi; thay phần truyền nhận bên trong bằng TcpCommandClient.
BC-01 vẫn đếm hồ sơ hiện tại; khoảng ngày chỉ lọc nhật ký. Không có cột ngày tạo
hồ sơ trong schema, nên không suy diễn thành số hồ sơ theo kỳ.

## Kiểm thử

```powershell
dotnet run --project cong_cu/kiem_tra_tcp -- .
python cong_cu/kiem_tra_tv1/integration.py
python cong_cu/kiem_tra_tv2/integration.py
```

TV1 fixture tự tạo MySQL/data directory và server trên cổng trống trong `.tooling/`,
dùng tài khoản giả ngẫu nhiên và dừng tiến trình trong finally. Không ghi đè cấu
hình local hoặc DB đang dùng. Cần MySQL bin/Python như fixture TV2. Suite client
`cong_cu/kiem_tra_tv1_client` được fixture gọi với cấu hình thử, dùng service client
thật, rồi khôi phục/xóa file cấu hình thử cạnh assembly.

Log: `kiem_tra/TV1_Merge_TCP.txt`, `TV1_Client_TCP.txt` và hồi quy TV2. Parser DG-01
đã chạy test CSV/XLSX giả. Chưa chạy fixture MySQL/TCP và backup/restore vì không có
Python interpreter (chỉ có Windows app alias), dù MySQL binaries có trên máy. Chưa
nghiệm thu UI/LAN vì không có máy đích. Các bước này vẫn cần làm trước TC-02.

Các file cũ vẫn ghi trạng thái thiết kế tại thời điểm tạo; bản này mô tả code TCP
đã ghép. DG-01, HT-04 và đóng gói thử đã được bổ sung nhưng không đánh dấu TC-02,
nghiệm thu LAN/UI hoặc mọi module TV1 là hoàn tất.
