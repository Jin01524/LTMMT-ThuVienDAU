# HT-04 Sao lưu và phục hồi

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2**.

## Luồng đã triển khai

Quản trị hoặc Quản lý thư viện có thể xem danh sách, tạo backup và phục hồi qua TCP. Server gọi `mysqldump.exe`/`mysql.exe`; gói ZIP gồm dump CSDL, manifest và PDF hợp lệ trong `data/tep-do-an`. Tạo file tạm, kiểm tra archive rồi đổi tên sang `.zip` để danh sách không lộ file chưa hoàn tất.

Phục hồi yêu cầu xác nhận chính xác `PHUC_HOI <Database>`, kiểm tra tên CSDL/archive, tạo backup an toàn trước, chuyển PDF theo thư mục staging và thu hồi mọi phiên sau thành công. Nếu thao tác đích lỗi, service thử khôi phục từ backup an toàn và báo lỗi rõ nếu rollback cũng thất bại. Các lần backup/restore được ghi audit.

## Ràng buộc triển khai

Cần cài MySQL client trên máy server và cho phép tìm thấy hai executable; mật khẩu truyền qua biến môi trường tiến trình con, không đặt trong tham số dòng lệnh. Archive chứa dữ liệu cá nhân chưa được mã hóa; bảo vệ thư mục backup bằng quyền hệ điều hành và chính sách lưu trữ phù hợp.

Build đã qua; chưa chạy backup/restore trên CSDL fixture, chưa xác minh rollback hoặc nghiệm thu UI. Không thử trên CSDL thật.
