# HT-01 Khung ứng dụng và kết nối MySQL

Người phụ trách **Võ Ngọc Bình** (TV1, 2351220274). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất Không.

## Nghiệm thu

Chạy server API và client WinForms riêng. Client gọi API HTTP/JSON qua TCP;
server kết nối MySQL bằng giao thức MySQL qua TCP, đọc cấu hình và hiển thị lỗi kết nối.

## Trách nhiệm server

Controller API nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.
Kết nối server tới MySQL được cấu hình rõ dùng TCP, mặc định cổng 3306.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **Dùng chung**.

HT-01 đã có luồng kiểm tra kết nối. Các nghiệp vụ tiếp theo thực hiện theo kế hoạch.
