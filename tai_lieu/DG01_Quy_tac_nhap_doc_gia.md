# DG-01 — Quy tắc nhập/cập nhật hồ sơ độc giả

Phiên bản tham khảo ngày 27/11/2026, cập nhật theo quyết định nhóm và đối chiếu cấu trúc `DocGia`, `Khoa`, `NganhDaoTao`. Mẫu [Mau_nhap_doc_gia_DG01.csv](Mau_nhap_doc_gia_DG01.csv) chỉ chứa dữ liệu giả; không nạp vào CSDL thật.

## Quy trình

1. Nhận `.xlsx` hoặc `.csv`, tối đa 10 MiB và 500 dòng; đọc sheet đầu tiên, tìm hàng tiêu đề trong 20 dòng đầu và hiển thị xem trước.
2. Mã sinh viên bắt buộc cho `SINH_VIEN`; mã cán bộ/giảng viên bắt buộc cho các loại cán bộ; một dòng chỉ nhập một trong hai. Lưu vào `MaDinhDanhTruong`.
3. Bỏ toàn bộ khoảng trắng trong mã và so sánh không phân biệt hoa/thường. Mã chuẩn hóa được lưu dạng chữ hoa, bảo toàn các số 0 đầu. Mã trùng trong file sau chuẩn hóa làm tất cả dòng trùng bị loại khỏi lần nhập.
4. Kiểm tra họ tên, `LoaiDocGia`, trạng thái học tập/công tác, mã khoa/ngành; `LoaiDocGia` chỉ nhận `SINH_VIEN`, `CAN_BO`, `GIANG_VIEN`, `TRO_GIANG`, `CNV`. `MaNganh` nếu có phải gồm đúng 7 chữ số, tồn tại và thuộc khoa tương ứng. Các giá trị trạng thái được chuẩn hóa không dấu/không dấu câu:
   - Workbook nguồn hiện có trạng thái `Đang học` cho sinh viên và `Đang công tác` cho cán bộ/giảng viên; cả hai ánh xạ sang `HOAT_DONG`.
   - Sinh viên: `DANG_HOC`, `DANG_THEO_HOC`, `DANG_HOC_TAP` → `HOAT_DONG`; `BAO_LUU`, `TAM_NGHI`, `NGHI_HOC`, `THOI_HOC`, `DA_THOI_HOC`, `TOT_NGHIEP`, `DA_TOT_NGHIEP`, `NGUNG_HOAT_DONG` → `NGUNG_HOAT_DONG`.
   - Cán bộ/giảng viên: `DANG_CONG_TAC`, `DANG_LAM_VIEC`, `DANG_HOAT_DONG` → `HOAT_DONG`; `NGHI_VIEC`, `DA_NGHI`, `DA_NGHI_VIEC`, `THOI_VIEC`, `NGUNG_CONG_TAC`, `TAM_NGHI`, `NGUNG_HOAT_DONG` → `NGUNG_HOAT_DONG`.
   - Trạng thái ngoài danh sách bị báo lỗi, không tự đoán ánh xạ.
5. Màn hình xem trước trả số dòng hợp lệ/lỗi và dự kiến thêm/cập nhật. Chỉ `QUAN_LY_THU_VIEN` được xác nhận import; server kiểm tra lại quyền và dữ liệu.
6. Chỉ dòng hợp lệ được ghi trong một giao dịch. Lỗi khi ghi DB làm rollback batch và báo lỗi; dòng lỗi kiểm tra được xuất riêng CSV để sửa. Lần import thành công được audit trong `NhatKy` với người thao tác, mã lô và số liệu tổng/thêm/cập nhật/lỗi; không ghi bản sao toàn bộ dữ liệu cá nhân vào audit.
7. Nếu mã đã có, cập nhật các trường nguồn hợp lệ. Ô trống khi cập nhật giữ giá trị cũ; hồ sơ mới lưu NULL cho trường tùy chọn. Hồ sơ không xuất hiện trong file không bị xóa hoặc tự đổi trạng thái.

## Đồng bộ trạng thái và quyền mượn

Trạng thái trường được lưu ở `DocGia.TrangThaiHocTap` và chuyển thành `TrangThaiHoSo` theo bảng ánh xạ trên. `NGUNG_HOAT_DONG` ngăn hồ sơ tiếp tục luồng sử dụng; hồ sơ vẫn được giữ để bảo toàn lịch sử và các khoản mượn. Import không khóa tài khoản đăng nhập, không sửa `DuocMuon`, `KhoaDen`, `LyDoKhoa` hay người thay đổi quyền. Việc mở quyền mượn vẫn do Quản lý xử lý qua DG-03, độc lập với trạng thái đăng nhập. Trạng thái lạ bị từ chối và yêu cầu nhóm cập nhật bảng ánh xạ trước khi nạp.

“Mượn tại chỗ”, “mượn theo kỳ đối với giáo án/giáo trình” và “mượn ngắn ngày đối với sách/tài liệu” là chính sách lưu thông theo tài liệu/loại bản sách, không phải trạng thái hồ sơ độc giả; không lưu chúng trong `LoaiDocGia` hay `TrangThaiHocTap`.

## Cột file

Mẫu chuẩn dùng `MaSinhVien`, `MaGiangVien`, `HoTen`, `LoaiDocGia`, `MaKhoa`, `MaNganh`, `KhoaHoc`, `TrangThaiHocTap`, `ThuDienTu`. Parser cũng ánh xạ workbook nguồn đã xem cấu trúc: `Mã Độc Giả` + `Đối Tượng` → mã định danh chung theo loại; `Họ và Tên` → họ tên; `Khoa / Phòng Ban` → khoa; `Email` → email; `Trạng Thái` → trạng thái. Giá trị khoa có thể là mã hoặc tên khớp duy nhất với danh mục khoa. Các cột `STT`, giới tính, ngày sinh, lớp/chức danh và số điện thoại không được nhập vì không có trường tương ứng trong bảng `DocGia` hiện tại.

Mã ngành nếu có phải là mã 7 chữ số trong danh mục và thuộc khoa tương ứng. Để xóa một giá trị tùy chọn đã có, dùng quy trình nghiệp vụ riêng; ô trống không mang nghĩa xóa.

Các cột dữ liệu hiện không được phép chỉnh qua import: `IdDocGia`, `TrangThaiHoSo` (được tính từ trạng thái nguồn), `DuocMuon`, `KhoaDen`, `LyDoKhoa`, `IdNguoiDoiQuyen`; không tạo tài khoản hoặc mật khẩu từ file. Tên cột của workbook nguồn cần được ánh xạ sang các cột chuẩn; parser chấp nhận một số bí danh thường dùng và từ chối tiêu đề mơ hồ.
