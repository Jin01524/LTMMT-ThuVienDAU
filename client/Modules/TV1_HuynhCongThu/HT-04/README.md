# HT-04 Sao lưu và phục hồi

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất HT-02, HT-03.

## Nghiệm thu

Quản trị tạo bản sao lưu; phục hồi thử trên DB riêng; có xác nhận trước ghi đè và nhật ký kết quả.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
