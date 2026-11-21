# Kiểm tra khung server client và MVC

Ngày **21/11/2026**. Nền mã nguồn trước thay đổi là commit 21856d8.
Môi trường kiểm tra Linux, SDK .NET 10.0.404. Client WinForms được biên dịch
với EnableWindowsTargeting=true. Chương trình desktop cần Windows để chạy giao diện.

## Restore và build

    dotnet restore ThuVienDAU.sln --locked-mode -p:EnableWindowsTargeting=true -m:1
    dotnet build ThuVienDAU.sln -c Release --no-restore -p:EnableWindowsTargeting=true -m:1

Kết quả hai project **0 lỗi, 0 cảnh báo**.
Server và client có packages.lock.json riêng.
Client dùng HTTP và DTO liên kết từ server/Contracts.

## Chạy server thật trên địa chỉ loopback tạm

| Tình huống | Kết quả |
|---|---|
| GET /api/health | HTTP 200, status ok |
| GET /api/database/health khi chưa có cấu hình | HTTP 503 và hướng dẫn cấu hình |
| appsettings.local.json sai JSON | HTTP 503 và thông báo cấu hình |
| MySQL tại cổng thử chưa mở | HTTP 503 và mã lỗi MySQL |

Các lần kiểm tra dùng cấu hình tổng hợp và đã dọn sau khi chạy.
Server được dừng sau thử nghiệm.

## Kiểm tra logic client qua điểm vào Controller

Dùng mã Controller, IMainView, Service, Models và ServerOptions thực tế
với View kiểm tra và HTTP handler giả lập trong chương trình thử tạm.

| Tình huống | Kết quả |
|---|---|
| JSON thành công | Đọc đúng phiên bản MySQL và tên CSDL |
| HTTP 503 lỗi MySQL | Hiện thông báo và mã MySQL |
| HTTP 503 cấu hình server | Hiện thông báo server trả về |
| Mất kết nối HTTP | Hiện hướng dẫn kiểm tra server và mạng |
| Hết thời gian chờ | Hiện thông báo thử lại |
| JSON phản hồi sai | Hiện lỗi dữ liệu JSON |
| URL server thiếu dấu / cuối | Tạo đúng đường dẫn /api/database/health |
| URL khác HTTP/HTTPS | Từ chối trước khi gọi mạng |
| Cấu hình client sai JSON | Hiện lỗi cấu hình |
| Thử lại sau lỗi cấu hình | Thực hiện thành công |

Mỗi nhánh đều kiểm tra hành động đã được mở lại sau khi xử lý.

## Cấu trúc và giới hạn kiểm tra

Đối chiếu danh sách 29 mô đun với bảng Excel, phân công 10/9/10 và đường dẫn
ở hai phía. Kiểm tra patch áp dụng được lên cây mã nguồn gốc.

Kết quả này xác nhận khung mạng và luồng kiểm tra lỗi.
Kết nối MySQL thành công bằng tài khoản thật, giao diện Windows,
thiết bị và nhiều máy LAN cần được nhóm kiểm tra khi có môi trường tương ứng.
28 mô đun nghiệp vụ đã được bố trí thư mục và phạm vi, đang chờ lập trình.
Báo cáo Word lưu phương án thiết kế trước lần tách server/client, được đối chiếu
trong tai_lieu/Kien_truc_Server_Client_MVC.md.
