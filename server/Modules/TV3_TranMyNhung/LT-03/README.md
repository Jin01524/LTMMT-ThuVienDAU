# LT-03 Gia hạn tại quầy

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất LT-01, HT-03.

## Nghiệm thu

Kiểm hạn mức, nợ/quá hạn và quyền mượn; Thủ thư chọn hạn mới, ghi căn cứ và xác nhận không có bạn đọc khác đăng ký. Không tự áp số lần hoặc số ngày gia hạn cố định khi nội quy chưa quy định.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **ChiTietPhieuMuon**.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
