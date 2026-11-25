# HT-03 Cấu hình và nhật ký

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất HT-01.

## Nghiệm thu

Dịch vụ ghi/tra cứu nhật ký; đọc cấu hình; giờ đóng cửa, lịch miễn phạt và gia hạn chưa chốt phải hiện rõ.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

`GET /api/system/configuration` đọc file cấu hình chuẩn được đóng gói cùng server
và trả các điểm chưa chốt. `GET /api/system/audit` lọc `NhatKy` theo thời gian,
phân quyền Quản trị/Quản lý, tối đa 500 bản ghi. Các lệnh cấp tài khoản và thay đổi
quyền mượn hiện ghi nhật ký bằng service/trigger tương ứng.

Chưa ghi nhật ký đăng nhập; quyết định về giờ mở/đóng cửa, lịch miễn phạt và gia hạn
vẫn được hiển thị là chưa xác nhận, không tự đặt giá trị.
