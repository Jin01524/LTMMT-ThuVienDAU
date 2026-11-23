# TL-02 Hồ sơ đầu tài liệu

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất TL-01.

## Nghiệm thu

Tạo/sửa/tra cứu đầu tài liệu, loại tài liệu và phân loại; thông tin tác giả/NXB/ISBN chi tiết thuộc MARC.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **DauTaiLieu**.

Đã có bản đầu giao diện và xử lý TCP ngày 22/11/2026. Chưa nghiệm thu cuối vì cần ghép phiên HT-02. Xem tai_lieu/TV2_Trien_khai.md ở gốc repo để chạy thử và rà soát.
