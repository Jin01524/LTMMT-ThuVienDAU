# LT-03 Gia hạn tại quầy

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất LT-01, HT-03.

## Nghiệm thu

Kiểm hạn mức, nợ/quá hạn và quyền mượn; Thủ thư chọn hạn mới, ghi căn cứ và xác nhận không có bạn đọc khác đăng ký. Không tự áp số lần hoặc số ngày gia hạn cố định khi nội quy chưa quy định.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
