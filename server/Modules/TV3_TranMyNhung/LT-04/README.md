# LT-04 In phiếu mượn và biên lai

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất LT-01, VP-02.

## Nghiệm thu

Xem trước/in đúng phiếu và hạn trả; in lại không tạo giao dịch mới; thử máy in khi có thiết bị.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **PhieuMuon, ChiTietPhieuMuon, PhieuThu**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
