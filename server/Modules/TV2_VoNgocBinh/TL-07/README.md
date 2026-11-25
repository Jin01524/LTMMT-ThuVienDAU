# TL-07 In barcode và nhãn gáy

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất TL-04.

## Nghiệm thu

Xem trước/in mã vạch và nhãn gáy; phân biệt DDC, ký hiệu xếp giá và vị trí; thử máy in khi có thiết bị.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **BanTaiLieu, DauTaiLieu**.

Đã triển khai client/server ngày 25/11/2026; có kiểm thử dữ liệu giả riêng. Chờ rà soát nhóm, ghép HT-02 và nghiệm thu trên thiết bị thực tế.

Hướng dẫn và hợp đồng hiện hành: [bàn giao TV2](../../../../tai_lieu/TV2_Hoan_thien.md).
