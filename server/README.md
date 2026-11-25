# Server TCP thư viện DAU

Console .NET 10 dùng TcpListener, xử lý lệnh JSON và truy cập MySQL.

    dotnet run --project server/ThuVienDAU.Server.csproj

Mặc định 127.0.0.1:5080. Demo LAN:

    dotnet run --project server/ThuVienDAU.Server.csproj -- --host 0.0.0.0 --port 5080

Client cấu hình IP server và cổng 5080. Chạy server trước client; Ctrl+C để dừng.
MySQL chỉ đặt trên server: sao chép appsettings.example.json thành appsettings.local.json,
điền thông tin DB rồi build lại. File cấu hình được đọc cạnh assembly.
Không chạy lại SQL tạo schema trên CSDL hiện hữu.

Lệnh PING trả trạng thái server; DATABASE_HEALTH trả phiên bản/tên CSDL hoặc lỗi cấu hình/MySQL.
Không còn endpoint HTTP. Giao thức đóng khung và ví dụ: [Giao_thuc_TCP.md](../tai_lieu/Giao_thuc_TCP.md).
Mỗi kết nối xử lý một yêu cầu và một phản hồi rồi đóng, tối đa 32 kết nối đang xử lý,
timeout 15 giây và giới hạn bản tin 1 MiB. Lỗi một client không dừng server.

Modules chia theo TV1/TV2/TV3. Controller nhận lệnh TCP, Service xử lý nghiệp vụ,
Data truy cập MySQL. View WinForms ở client. DTO và mã đóng khung dùng chung nằm trong Contracts.
TL-01/TL-02/TL-04 đã có bản đầu; HT-02 và các nghiệp vụ còn lại vẫn cần lập trình; xác thực, kiểm quyền và giao dịch phải đặt ở server.
Khung hiện tại chưa có xác thực phiên hoặc mã hóa kênh TCP; chỉ dùng demo nội bộ.
