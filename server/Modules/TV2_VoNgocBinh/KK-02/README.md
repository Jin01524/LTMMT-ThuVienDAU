# KK-02 Đề nghị và xử lý thanh lý

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất KK-01.

## Nghiệm thu

Lập đề nghị và ghi kết quả quyết định; không tự thanh lý khi kiểm kê thiếu; Biên mục đề nghị, Quản lý thư viện duyệt/chốt hoặc từ chối.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **ThanhLy, BanTaiLieu**.

Đã triển khai client/server ngày 25/11/2026; có kiểm thử dữ liệu giả riêng. Chờ rà soát nhóm, ghép HT-02 và nghiệm thu trên thiết bị thực tế.

Hướng dẫn và hợp đồng hiện hành: [bàn giao TV2](../../../../tai_lieu/TV2_Hoan_thien.md).
