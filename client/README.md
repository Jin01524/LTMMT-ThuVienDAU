# Client WinForms MVC

    dotnet run --project client/ThuVienDAU.WinForms.csproj

Chạy server trước. Mặc định kết nối TCP 127.0.0.1:5080. Sao chép appsettings.example.json
thành appsettings.local.json nếu cần đổi Server.Host, Port, TimeoutSeconds, rồi build lại.
Demo LAN: Host là IP máy server (ví dụ 192.168.1.10), Port 5080. Không dùng URL HTTP.
Công cụ → Kiểm tra kết nối MySQL gửi lệnh DATABASE_HEALTH qua TCP đến server.

Views phát sự kiện; Controllers điều phối; Services gọi TcpCommandClient; Models giữ dữ liệu.
Program.cs nối các thành phần. Không có MySql.Data hoặc mật khẩu MySQL trong client.
Contract JSON và mã đóng khung liên kết từ server/Contracts để hai bên dùng cùng quy ước.
Mỗi lệnh mở một kết nối TCP, gửi yêu cầu, nhận phản hồi khớp RequestId, rồi đóng kết nối.
Chi tiết: [giao thức TCP](../tai_lieu/Giao_thuc_TCP.md).

Menu TV2 · Biên mục có TL-01, TL-02, TL-04 Bản sách và Dãy kệ/ngăn. Chế độ thử cần token loopback do server cấp; xem tai_lieu/TV2_TL04_Ban_sach.md.
