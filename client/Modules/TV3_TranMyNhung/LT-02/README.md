# LT-02 Trả sách và tình trạng bản sách

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất LT-01.

## Nghiệm thu

Quét bản đang mượn; ghi ngày trả/tình trạng; trả một phần; trả lặp không ghi hai lần; chuyển căn cứ sang xử lý vi phạm.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
