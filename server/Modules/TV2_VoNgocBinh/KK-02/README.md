# KK-02 Đề nghị và xử lý thanh lý

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất KK-01.

## Nghiệm thu

Lập đề nghị và ghi kết quả quyết định; không tự thanh lý khi kiểm kê thiếu; thẩm quyền duyệt cần chốt.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **ThanhLy, BanTaiLieu**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
