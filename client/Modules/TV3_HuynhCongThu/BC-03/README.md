# BC-03 Thống kê lưu thông, quá hạn và thu tiền

Người phụ trách **Huỳnh Công Thủ** (TV3, 2351220232). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất VP-02, PD-01, LT-03.

## Nghiệm thu

Dashboard theo quyền; mượn/trả/gia hạn/quá hạn/lượt đọc; đối chiếu tiền phạt với thực thu, không đếm trùng.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi HTTP API qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
