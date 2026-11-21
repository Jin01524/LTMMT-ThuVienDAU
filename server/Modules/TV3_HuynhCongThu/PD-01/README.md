# PD-01 Lượt vào và ra phòng đọc

Người phụ trách **Huỳnh Công Thủ** (TV3, 2351220232). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất DG-02.

## Nghiệm thu

Định danh thẻ trường, ghi vào/ra; nhắc giữ/nhận thẻ và chìa khóa; quy định tối đa 2 cuốn là thao tác tại phòng.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **LuotVaoPhongDoc, DocGia**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
