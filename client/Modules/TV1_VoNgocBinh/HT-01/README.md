# HT-01 Khung ứng dụng và kết nối MySQL

Người phụ trách **Võ Ngọc Bình** (TV1, 2351220274). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất Không.

## Nghiệm thu

Chạy server TCP và client WinForms riêng; client gọi TCP Socket/JSON, server đọc cấu hình và truy vấn MySQL; hiển thị được lỗi kết nối.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

HT-01 đã có luồng kiểm tra kết nối. Các nghiệp vụ tiếp theo thực hiện theo kế hoạch.
