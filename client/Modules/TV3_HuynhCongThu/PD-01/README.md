# PD-01 Lượt vào và ra phòng đọc

Người phụ trách **Huỳnh Công Thủ** (TV3, 2351220232). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất DG-02.

## Nghiệm thu

Định danh thẻ trường, ghi vào/ra; nhắc giữ/nhận thẻ và chìa khóa; quy định tối đa 2 cuốn là thao tác tại phòng.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
