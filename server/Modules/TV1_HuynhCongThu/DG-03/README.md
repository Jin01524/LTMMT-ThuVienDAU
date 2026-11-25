# DG-03 Khóa và mở quyền mượn

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất DG-02, HT-03.

## Nghiệm thu

Chỉ Quản lý thư viện thao tác; ghi lý do/thời gian; chỉ mở khi đáp ứng điều kiện trả sách, thanh toán và đình chỉ.

## Trách nhiệm server

Controller TCP nhận yêu cầu và trả JSON; Service kiểm quyền, xử lý nghiệp vụ;
Repository/Data thực hiện truy vấn có tham số và dùng chung giao dịch khi cần.

`PUT /api/readers/{readerId}/borrow-permission` chỉ chấp nhận phiên
QUAN_LY_THU_VIEN; yêu cầu lý do 1–300 ký tự. Server khóa hồ sơ bằng `FOR UPDATE`,
không cho mở khi còn bản `DANG_MUON`, vi phạm `CHUA_XU_LY`, hoặc thời hạn
`KhoaDen` còn trong tương lai; cập nhật `DocGia.DuocMuon/KhoaDen/LyDoKhoa/
IdNguoiDoiQuyen` trong giao dịch. Khóa thủ công là vô thời hạn (`KhoaDen` để NULL);
thời điểm thao tác được trigger ghi trong `NhatKy`. Trigger `DocGia_KiemTraSua`
và `DocGia_LuuDoiQuyen` xác thực quyền và ghi lịch sử.

Chưa có chính sách đình chỉ độc lập ngoài trường `KhoaDen`; cần nhóm chốt trước khi mở rộng.
