# TL-03 Biên mục MARC21

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất TL-02.

## Nghiệm thu

Form thân thiện lưu JSON trường/trường con; kiểm profile cục bộ; giữ thứ tự/lặp/chỉ thị; 082 khớp DDC, sửa về nháp.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **BieuGhiMARC, DauTaiLieu**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
