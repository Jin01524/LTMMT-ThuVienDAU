# HT-02 Đăng nhập, phiên và vai trò

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất HT-01.

## Nghiệm thu

Đăng nhập năm vai trò; chỉ BIEN_MUC khóa/mở đăng nhập DOC_GIA, bắt buộc lý do/nhật ký; không đổi quyền mượn. Lấy ID thao tác từ phiên đã xác thực.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **TaiKhoan, DocGia**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
