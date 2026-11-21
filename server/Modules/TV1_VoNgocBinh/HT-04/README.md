# HT-04 Sao lưu và phục hồi

Người phụ trách **Võ Ngọc Bình** (TV1, 2351220274). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất HT-02, HT-03.

## Nghiệm thu

Quản trị tạo bản sao lưu; phục hồi thử trên DB riêng; có xác nhận trước ghi đè và nhật ký kết quả.

## Trách nhiệm server

Controller API nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **Toàn bộ CSDL**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
