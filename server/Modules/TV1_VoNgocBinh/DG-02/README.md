# DG-02 Tra cứu và định danh thẻ trường

Người phụ trách **Võ Ngọc Bình** (TV1, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất DG-01.

## Nghiệm thu

Tìm theo mã thẻ trường, xem nhóm/trạng thái/hạn mức; không cấp thẻ mới, không xóa độc giả.

## Trách nhiệm server

Controller API nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **DocGia**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
