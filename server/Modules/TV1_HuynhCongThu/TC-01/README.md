# TC-01 Tích hợp giao diện và bản cài

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2, TV3**.

## Server

Server TCP đọc `server/appsettings.local.json` cho cấu hình MySQL; cấu hình không được lấy từ client. Menu TV1 gọi dịch vụ server qua cùng giao thức TCP và quyền được xác minh lại ở server.

## Đóng gói thử Windows x64

Từ gốc repo chạy `powershell -ExecutionPolicy Bypass -File cong_cu/phat_hanh_tv1.ps1`. Script publish self-contained server và WinForms client vào `artifacts/TV1/win-x64/`, sau đó tạo cấu hình local từ file mẫu để không phát hành mật khẩu local của máy build.

Trước khi chạy, chỉnh `server/appsettings.local.json` ở thư mục server; trên từng máy client chỉnh `client/appsettings.local.json` với IP/cổng server. Server cần MySQL 8+, schema đã tạo, và MySQL client `mysql.exe`/`mysqldump.exe` trong PATH để dùng HT-04. Không có MSI, ký số hay cài đặt Windows service trong phạm vi script này.

Chưa chạy publish trên máy đích hoặc nghiệm thu LAN/UI thực tế.
