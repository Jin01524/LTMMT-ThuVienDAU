# TL-01 Danh mục DDC23 và xác nhận phân loại

Người phụ trách **Trần Mỹ Nhung** (TV2, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất HT-02, HT-03.

## Nghiệm thu

Tra cây DDC23 bản in tiếng Việt rút gọn; lưu nguồn/người/vị trí tra; mục chưa đối chiếu không tự xác nhận.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **MucPhanLoaiDDC, DauTaiLieu**.

Đã có bản đầu giao diện và xử lý TCP ngày 22/11/2026. Chưa nghiệm thu cuối vì cần ghép phiên HT-02. Xem tai_lieu/TV2_Trien_khai.md ở gốc repo để chạy thử và rà soát.
