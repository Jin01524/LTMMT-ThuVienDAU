# LT-02 Trả sách và tình trạng bản sách

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất LT-01.

## Nghiệm thu

Quét bản đang mượn; ghi ngày trả/tình trạng; trả một phần; trả lặp không ghi hai lần; chuyển căn cứ sang xử lý vi phạm.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **PhieuMuon, ChiTietPhieuMuon, BanTaiLieu**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
