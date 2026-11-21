# Server API thư viện DAU

Server dùng ASP.NET Core .NET 10 và Connector/NET MySql.Data 26.10.0.
Chạy từ gốc repo bằng lệnh sau.

    dotnet run --project server/ThuVienDAU.Server.csproj

Server mặc định dùng http://localhost:5080.
Có thể đặt địa chỉ lắng nghe bằng --urls hoặc biến môi trường ASPNETCORE_URLS.

## Cấu hình MySQL

Sao chép appsettings.example.json thành appsettings.local.json ngay trong server,
điền máy chủ, cổng, CSDL và tài khoản riêng của nhóm, rồi build lại.
Cấu hình mẫu của bản WinForms trước được chuyển sang vị trí này.
File local cũ nếu đã tồn tại trên máy cần chuyển thủ công từ
src/ThuVienDAU.WinForms/appsettings.local.json sang server/appsettings.local.json.

SQL chuẩn ở ../csdl/01_Tao_CSDL.sql. Server đọc file cấu hình cạnh assembly.
Chức năng kiểm tra đọc phiên bản và tên CSDL.

## API đã triển khai

| Phương thức | Đường dẫn | Kết quả |
|---|---|---|
| GET | /api/health | HTTP 200 với status ok khi tiến trình chạy |
| GET | /api/database/health | HTTP 200 với serverVersion và databaseName khi MySQL kết nối được |
| GET | /api/database/health | HTTP 503 với message và errorNumber nếu cấu hình hoặc MySQL gặp lỗi |

Thông báo trả về giữ nội dung hướng dẫn, giới hạn thông tin cấu hình nội bộ.
Contracts định nghĩa DatabaseConnectionInfo và DatabaseHealthError.

## Mô đun và MVC

[TV1](Modules/TV1_VoNgocBinh) phụ trách hệ thống, độc giả và tích hợp.
[TV2](Modules/TV2_TranMyNhung) phụ trách tài liệu và kho.
[TV3](Modules/TV3_HuynhCongThu) phụ trách lưu thông và thu tiền.

Controllers nhận yêu cầu mạng và trả model JSON. Services xử lý nghiệp vụ;
Data/Repository truy cập CSDL. Views của ứng dụng desktop nằm ở client.
HT-01 đã có controller API và dịch vụ đọc MySQL. Các mô đun còn lại có
README quy định phạm vi trước khi thành viên bổ sung code.

Khi triển khai HT-02, xác thực phiên và kiểm quyền tại server trước khi mở API
ghi dữ liệu. Luồng mượn, trả và thu tiền dùng một giao dịch ở server theo kế hoạch nhóm.
