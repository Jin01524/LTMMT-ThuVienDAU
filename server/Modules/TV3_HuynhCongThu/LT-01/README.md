# LT-01 Mượn sách tại quầy

Người phụ trách **Huỳnh Công Thủ** (TV3, 2351220232). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất DG-02, DG-03, TL-04, HT-03.

## Nghiệm thu

SV 2 đầu/10 ngày, CB/GV 3 đầu/15 ngày; chặn trùng nhan đề, khóa/chưa đủ điều kiện, 15 phút cuối; giao dịch nguyên tử.

## Trách nhiệm server

Controller API nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **PhieuMuon, ChiTietPhieuMuon, BanTaiLieu, DocGia**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
