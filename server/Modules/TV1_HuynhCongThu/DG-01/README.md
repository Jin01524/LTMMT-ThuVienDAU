# DG-01 Khoa, ngành và nhập hồ sơ

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2**.

## Luồng đã triển khai

Client đọc `.xlsx`/`.csv`, xem trước và gửi tối đa 500 dòng tới server. Server chỉ cho `QUAN_LY_THU_VIEN` chạy import; kiểm tra lại mã, nhóm độc giả, trạng thái, khoa/ngành và quan hệ ngành-khoa. Mã trường được chuẩn hóa bỏ khoảng trắng và chuyển chữ hoa.

Các dòng hợp lệ được thêm/cập nhật trong một giao dịch; dòng lỗi được trả riêng để xuất CSV. Lô được ghi audit trong cùng giao dịch. Mã sinh viên chỉ dùng cho sinh viên; mã cán bộ/giảng viên dùng cho các loại còn lại. Ô trống khi cập nhật giữ giá trị cũ; hồ sơ không có trong file không bị xóa.

Trạng thái học tập/công tác được lưu nguyên văn và ánh xạ sang `TrangThaiHoSo`. Hồ sơ không hoạt động không được mượn theo kiểm tra nghiệp vụ hiện hành; quyền `DuocMuon` vẫn là quyền quản lý độc lập, không tự bật/tắt theo import. Giá trị trạng thái chưa được ánh xạ bị từ chối theo dòng.

Các cột không có trong schema `DocGia` không được nhập. Danh mục trạng thái còn lại cần đối chiếu dữ liệu chính thức của trường trước nghiệm thu.

## Kiểm tra

Parser CSV/XLSX có test với tiêu đề workbook và dữ liệu giả. Chưa chạy fixture MySQL/TCP hoặc nghiệm thu giao diện trên máy người dùng.
