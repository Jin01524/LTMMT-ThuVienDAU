# PD-01 Lượt vào và ra phòng đọc

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất DG-02.

## Nghiệm thu

Định danh thẻ trường, ghi vào/ra; nhắc giữ/nhận thẻ và chìa khóa; quy định tối đa 2 cuốn là thao tác tại phòng.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
