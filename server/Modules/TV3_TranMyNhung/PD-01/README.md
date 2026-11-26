# PD-01 Lượt vào và ra phòng đọc

Người phụ trách **Trần Mỹ Nhung** (TV3, 2351220273). Người rà soát **TV1**.

Phụ thuộc trước khi hoàn tất DG-02.

## Nghiệm thu

Định danh thẻ trường, ghi vào/ra; nhắc giữ/nhận thẻ và chìa khóa; quy định tối đa 2 cuốn là thao tác tại phòng.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **LuotVaoPhongDoc, DocGia**.

Đã tích hợp vào solution chung qua TCP. Xem [bản bàn giao TV3](../../../../tai_lieu/TV3_Tich_hop_TCP.md) và bộ kiểm thử `cong_cu/kiem_tra_tv3`.
