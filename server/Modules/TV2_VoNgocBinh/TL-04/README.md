# TL-04 Bản sách và dãy kệ/ngăn

Người phụ trách **Võ Ngọc Bình** (TV2, 2351220274). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất TL-02.

## Nghiệm thu

Tạo bản sách có ĐKCB/barcode duy nhất; giá, trạng thái, ký hiệu xếp giá và dãy kệ/ngăn; không thêm tầng/phòng.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

Các bảng hoặc dữ liệu liên quan theo kế hoạch **BanTaiLieu, ViTriLuuTru**.

Đã bố trí thư mục và phạm vi bàn giao. Mô đun đang chờ triển khai nghiệp vụ.
