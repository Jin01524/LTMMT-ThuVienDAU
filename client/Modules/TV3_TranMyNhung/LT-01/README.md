# LT-01 Mượn sách tại quầy

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất DG-02, DG-03, TL-04, HT-03.

## Nghiệm thu

SV 2 đầu/10 ngày, CB/GV 3 đầu/15 ngày; chặn trùng nhan đề, khóa/chưa đủ điều kiện, 15 phút cuối; giao dịch nguyên tử.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
