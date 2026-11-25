# BC-02 Thống kê tài liệu và kho

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất KK-01, TL-03.

## Nghiệm thu

Số đầu tài liệu/bản sách theo DDC/loại/trạng thái; kết quả kiểm kê; xuất báo cáo có kỳ/bộ lọc.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **DauTaiLieu, BanTaiLieu, KiemKe, ChiTietKiemKe**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
