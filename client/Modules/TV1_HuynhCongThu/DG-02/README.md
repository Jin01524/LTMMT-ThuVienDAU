# DG-02 Tra cứu và định danh thẻ trường

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất DG-01.

## Nghiệm thu

Tìm theo mã thẻ trường, xem nhóm/trạng thái/hạn mức; không cấp thẻ mới, không xóa độc giả.

## Trách nhiệm client

`Views/ReaderDirectoryForm` tra cứu mã thẻ/họ tên và hiển thị nhóm, khoa/ngành,
trạng thái hồ sơ, quyền mượn, số khoản mượn đang mở và vi phạm chưa xử lý.
Tra cứu yêu cầu ít nhất 2 ký tự; kết quả tối đa 100 hồ sơ. Tài khoản DOC_GIA
không được mở công cụ nhân viên.

Chưa có pipeline nhập Excel DG-01; dữ liệu hiện có thể được tra cứu nhưng chưa
được nhập/cập nhật từ danh sách trường qua ứng dụng.

Đã ghép theo giao thức TCP; xem `tai_lieu/TV1_Ghep_TCP.md` để biết lệnh và cách chạy.
