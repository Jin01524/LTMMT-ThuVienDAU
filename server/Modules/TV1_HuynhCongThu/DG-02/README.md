# DG-02 Tra cứu và định danh thẻ trường

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất DG-01.

## Nghiệm thu

Tìm theo mã thẻ trường, xem nhóm/trạng thái/hạn mức; không cấp thẻ mới, không xóa độc giả.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

API `GET /api/readers?sessionId=...&q=...` tìm theo mã trường hoặc họ tên,
giới hạn 100 kết quả; truy vấn có tham số và chỉ cho phiên nhân viên.
Không cấp mã thẻ mới, không xóa hồ sơ. Dữ liệu lấy từ **DocGia, Khoa,
NganhDaoTao, ChiTietPhieuMuon, ViPham**.

Việc nhập/cập nhật theo Excel còn chờ DG-01.
