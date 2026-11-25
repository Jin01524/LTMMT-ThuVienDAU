# TL-03 Biên mục MARC21

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất TL-02.

## Nghiệm thu

Form thân thiện lưu JSON trường/trường con; kiểm profile cục bộ; giữ thứ tự/lặp/chỉ thị; 082 khớp DDC, sửa về nháp.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã triển khai client/server ngày 25/11/2026; có kiểm thử dữ liệu giả riêng. Chờ rà soát nhóm, ghép HT-02 và nghiệm thu trên thiết bị thực tế.

Hướng dẫn và hợp đồng hiện hành: [bàn giao TV2](../../../../tai_lieu/TV2_Hoan_thien.md).
