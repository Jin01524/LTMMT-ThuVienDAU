# TC-01 Tích hợp giao diện và bản cài

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2, TV3**.

## Client

WinForms có menu theo vai trò, cấu hình TCP `Host`/`Port`/timeout tại `appsettings.local.json` và gọi nghiệp vụ qua dịch vụ TCP. Client không truy cập MySQL trực tiếp.

## Đóng gói thử Windows x64

Chạy từ gốc repo: `powershell -ExecutionPolicy Bypass -File cong_cu/phat_hanh_tv1.ps1`. Kết quả nằm trong `artifacts/TV1/win-x64/client/`. Cấu hình đầu ra được tạo từ mẫu loopback; cập nhật `Host` thành IP máy server trên từng máy client.

Đây là thư mục publish self-contained, chưa phải MSI/installer đã ký số. Chưa nghiệm thu cài đặt trên máy sạch hoặc giao tiếp nhiều máy LAN.
