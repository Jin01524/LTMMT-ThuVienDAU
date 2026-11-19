# Kiểm tra bản khởi đầu WinForms

Ngày: 19/11/2026.

- Solution: ThuVienDAU.sln; project ThuVienDAU.WinForms; target net10.0-windows.
- Môi trường máy kiểm tra: SDK 11.0.100, Windows Desktop Runtime 10.0.15.
- Connector/NET: MySql.Data 26.10.0, dependency được khóa bằng packages.lock.json.
- Restore và build Release thành công, 0 lỗi, 0 cảnh báo.
- Chạy executable Release ở chế độ ẩn để kiểm tra khởi động: tiến trình còn hoạt động và Responding=true sau 3 giây; đã đóng tiến trình thử.
- git check-ignore xác nhận bỏ qua .vs, bin, obj, appsettings.local.json và .tooling.
- Chưa thử Visual Studio Designer bằng giao diện; chưa thử menu kết nối với tài khoản MySQL thật.
- Chưa có đăng nhập/phân quyền hay module nghiệp vụ. Kiểm tra khởi động không thay thế kiểm thử nghiệp vụ hoặc nhiều máy.

GitHub Actions được cấu hình restore với --locked-mode và build Release trên Windows khi push main hoặc mở PR.
Trạng thái chạy Actions thực tế xem ở tab Actions của repo; kết quả build cục bộ không tự xác nhận Actions đã chạy.
