# PH-01 Góp ý, đánh giá và đề xuất sách

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất HT-02, TL-02.

## Nghiệm thu

Gửi đề xuất/góp ý/đánh giá; xem/xử lý theo quyền; đề xuất không phải đặt trước tài liệu.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **PhanHoiDocGia**.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
