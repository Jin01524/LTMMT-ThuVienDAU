# VP-02 Thu tiền và biên lai

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất VP-01.

## Nghiệm thu

Thu đủ khoản được chọn; một phiếu nhiều khoản; không thu lặp; lưu người/ngày/căn cứ; kiểm tra quyền ở dịch vụ.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **PhieuThu, ViPham**.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
