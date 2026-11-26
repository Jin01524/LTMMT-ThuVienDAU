# VP-01 Quá hạn và bồi thường

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất LT-02, HT-03.

## Nghiệm thu

SV 2.000đ/ngày/cuốn trừ lễ/Tết; CB/GV lập thông báo đơn vị; mất/hỏng tính đúng nhánh; chưa chốt lịch thì không tự tính đủ ngày.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **ViPham, ChiTietPhieuMuon, BanTaiLieu**.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
