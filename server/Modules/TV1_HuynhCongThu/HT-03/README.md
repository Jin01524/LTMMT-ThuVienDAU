# HT-03 Cấu hình và nhật ký

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất HT-01.

## Nghiệm thu

Dịch vụ ghi/tra cứu nhật ký; đọc cấu hình; giờ đóng cửa, lịch miễn phạt và gia hạn chưa chốt phải hiện rõ.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Lệnh TCP `TV1_CONFIGURATION` đọc cấu hình chuẩn cùng server và trả các điểm chưa chốt. `TV1_AUDIT` lọc `NhatKy` theo thời gian, phân quyền Quản trị/Quản lý, tối đa 500 bản ghi. Các lệnh cấp tài khoản, thay đổi quyền mượn, import hồ sơ và backup/restore ghi audit; HT-02 ghi đăng nhập thành công/thất bại và đăng xuất.

Quyết định về giờ mở/đóng cửa, lịch miễn phạt và gia hạn vẫn được hiển thị là chưa xác nhận, không tự đặt giá trị.
