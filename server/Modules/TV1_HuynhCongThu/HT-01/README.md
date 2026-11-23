# HT-01 Khung ứng dụng và kết nối MySQL

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất Không.

## Nghiệm thu

Chạy server TCP và client WinForms riêng; client gọi TCP Socket/JSON, server đọc cấu hình và truy vấn MySQL; hiển thị được lỗi kết nối.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **Dùng chung**.

HT-01 đã có luồng kiểm tra kết nối. Các nghiệp vụ tiếp theo thực hiện theo kế hoạch.
