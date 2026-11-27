# HT-04 Sao lưu và phục hồi

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2**.

## Luồng client

Form cho vai trò quản trị/quản lý xem danh sách backup, tạo bản mới và phục hồi bản đã chọn. Trước khi gửi yêu cầu phục hồi, người dùng phải xác nhận cảnh báo và nhập `PHUC_HOI <Database>`; khi thành công, ứng dụng báo mã backup an toàn và yêu cầu đăng nhập lại.

Backup lưu trên server, không tải archive về máy client. Cần xác minh backup/restore thực tế trên CSDL thử trước khi dùng ở môi trường vận hành.
