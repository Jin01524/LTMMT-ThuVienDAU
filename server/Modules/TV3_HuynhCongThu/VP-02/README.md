# VP-02 Thu tiền và biên lai

Người phụ trách **Huỳnh Công Thủ** (TV3, 2351220232). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất VP-01.

## Nghiệm thu

Thu đủ khoản được chọn; một phiếu nhiều khoản; không thu lặp; lưu người/ngày/căn cứ; kiểm tra quyền ở dịch vụ.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **PhieuThu, ViPham**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
