# BC-01 Thống kê độc giả và nhật ký truy cập

Người phụ trách **Võ Ngọc Bình** (TV1, 2351220274). Người rà soát **TV2**.

Phụ thuộc trước khi hoàn tất DG-01, HT-03.

## Nghiệm thu

Thống kê độc giả theo đối tượng/khoa/ngành; lọc kỳ; xuất danh sách đúng bộ lọc.

## Trách nhiệm server

Controller API nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

`GET /api/reports/readers` chỉ cho QUAN_TRI/QUAN_LY_THU_VIEN; truy vấn tổng,
nhóm theo `LoaiDocGia`, khoa, ngành; nhận bộ lọc danh sách tham số hóa và khoảng
thời gian cho nhật ký `NhatKy` của `DocGia`. Danh sách tối đa 5.000 hồ sơ, hoạt động
nhật ký tối đa 500 dòng. Schema hiện không có ngày tạo hồ sơ; do đó khoảng ngày chỉ
áp dụng cho nhật ký và không được diễn giải thành thống kê hồ sơ mới trong kỳ.
