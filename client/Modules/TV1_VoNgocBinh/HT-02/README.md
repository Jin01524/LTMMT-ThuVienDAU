# HT-02 Đăng nhập, phiên và vai trò

Người phụ trách **Võ Ngọc Bình** (TV1, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất HT-01.

## Nghiệm thu

Đăng nhập năm vai trò; chỉ BIEN_MUC khóa/mở đăng nhập DOC_GIA, bắt buộc lý do/nhật ký; không đổi quyền mượn. Lấy ID thao tác từ phiên đã xác thực.

## Trách nhiệm client

Các lớp giao diện đặt trong `Views/`, bộ điều phối thao tác đặt trong `Controllers/`,
dữ liệu trình bày đặt trong `Models/`. Gọi TCP Socket qua dịch vụ client.
Kiểm quyền và giao dịch dữ liệu được thực hiện trên server.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
