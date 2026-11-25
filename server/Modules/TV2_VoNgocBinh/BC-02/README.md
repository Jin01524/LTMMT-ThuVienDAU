# BC-02 Thống kê tài liệu và kho

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất KK-01, TL-03.

## Nghiệm thu

Số đầu tài liệu/bản sách theo DDC/loại/trạng thái; kết quả kiểm kê; xuất báo cáo có kỳ/bộ lọc.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **DauTaiLieu, BanTaiLieu, KiemKe, ChiTietKiemKe**.

Đã triển khai client/server ngày 25/11/2026; có kiểm thử dữ liệu giả riêng. Chờ rà soát nhóm, ghép HT-02 và nghiệm thu trên thiết bị thực tế.

Hướng dẫn và hợp đồng hiện hành: [bàn giao TV2](../../../../tai_lieu/TV2_Hoan_thien.md).
