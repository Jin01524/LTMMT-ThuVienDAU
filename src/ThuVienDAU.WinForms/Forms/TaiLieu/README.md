# Tài liệu

TV2 phụ trách TL-01 đến TL-05 và TL-07. Form DDC23, MARC21, đầu tài liệu, bản sách và đồ án đặt tại đây.
Các form sử dụng chung Services và Data. Không tạo thêm schema riêng cho module.

## Quyền khóa tài khoản và quyền mượn

- **Biên mục (BIEN_MUC):** duy nhất được khóa/mở tài khoản đăng nhập của độc giả (`TaiKhoan.DangHoatDong`), chỉ áp dụng tài khoản có vai trò `DOC_GIA` và liên kết hồ sơ độc giả. Bắt buộc lý do và nhật ký.
- **Quản lý thư viện (QUAN_LY_THU_VIEN):** khóa/mở quyền mượn (`DocGia.DuocMuon`, `KhoaDen`, `LyDoKhoa`) và hủy hiệu lực phiếu phạt.
- Quản trị, Thủ thư và Độc giả không có quyền khóa/mở đăng nhập độc giả. Mở đăng nhập không mở quyền mượn; mở quyền mượn không mở đăng nhập. Tại quầy vẫn kiểm quyền mượn riêng.
- HT-02 do TV1 triển khai màn hình và kiểm quyền khóa/mở đăng nhập; người sử dụng nghiệp vụ là Biên mục. Vai trò cố định gồm QUAN_TRI, QUAN_LY_THU_VIEN, BIEN_MUC, THU_THU, DOC_GIA.
