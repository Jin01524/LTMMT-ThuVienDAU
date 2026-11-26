# VP-03 Hủy hiệu lực phiếu phạt

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất VP-02, HT-02.

## Nghiệm thu

Chỉ Quản lý thư viện được hủy; giữ bản ghi và lý do/nhật ký; phiếu đã thu vẫn giữ chứng từ tiền đã thu.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **ViPham, PhieuThu, NhatKy**.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
