# TL-03 Biên mục MARC21

Người phụ trách **Trần Mỹ Nhung** (TV2, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất TL-02.

## Nghiệm thu

Form thân thiện lưu JSON trường/trường con; kiểm profile cục bộ; giữ thứ tự/lặp/chỉ thị; 082 khớp DDC, sửa về nháp.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
