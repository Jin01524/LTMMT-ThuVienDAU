# TL-04 Bản sách và dãy kệ/ngăn

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất TL-02.

## Nghiệm thu

Tạo bản sách có ĐKCB/barcode duy nhất; giá, trạng thái, ký hiệu xếp giá và dãy kệ/ngăn; không thêm tầng/phòng.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **BanTaiLieu, ViTriLuuTru**.

Đã có bản đầu TL-04 ngày 23/11/2026: bản sách/vị trí qua TCP, giao dịch, nhật ký, kiểm mã trùng và chống ghi đè. Xem tai_lieu/TV2_TL04_Ban_sach.md ở gốc repo. Cần ghép phiên HT-02 và rà soát trước nghiệm thu.
