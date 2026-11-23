# BC-03 Thống kê lưu thông, quá hạn và thu tiền

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất VP-02, PD-01, LT-03.

## Nghiệm thu

Dashboard theo quyền; mượn/trả/gia hạn/quá hạn/lượt đọc; đối chiếu tiền phạt với thực thu, không đếm trùng.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **PhieuMuon, ChiTietPhieuMuon, ViPham, PhieuThu, LuotVaoPhongDoc**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
