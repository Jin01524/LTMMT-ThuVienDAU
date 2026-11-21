# Làm việc nhóm với Git

Repo dùng một solution `ThuVienDAU.sln`; mỗi người clone một lần và mở solution này trong Visual Studio.
Project WinForms dùng .NET 10. Cài workload .NET desktop development và .NET 10 SDK/targeting pack.
SDK mới hơn có thể build target .NET 10 khi có bộ targeting pack tương ứng.

## Nhận việc và tạo nhánh

1. Chuyển sang main, Fetch/Pull lấy bản mới nhất. Hoàn tất commit công việc đang làm trước khi đổi nhánh.
2. Tạo nhánh từ main, ví dụ `feature/TV2-TL-04-ban-sach`.
3. Làm từng phần, build/chạy thử, commit có mã module: `[TL-04] Kiểm tra barcode trùng`.
4. Push nhánh, tạo Pull Request vào main bằng mẫu có sẵn.
5. Người rà soát kiểm thay đổi. Ghép sau khi đã rà soát và build thành công.
6. Cả nhóm cập nhật main; nhiệm vụ mới tạo nhánh mới từ main đã cập nhật.

Không cần tạo sẵn 29 nhánh. Một nhánh cho một nhiệm vụ vừa đủ để rà soát.
Người khởi tạo repo có thể là TV2; việc đó không đổi phân công nghiệp vụ.

## Phân chia file

- TV1 Võ Ngọc Bình — Modules/TV1_VoNgocBinh ở cả client và server. Hệ thống, độc giả, BC-01 và tích hợp.
- TV2 Trần Mỹ Nhung — Modules/TV2_TranMyNhung ở cả client và server. Tài liệu, DDC23/MARC21, kho và BC-02.
- TV3 Huỳnh Công Thủ — Modules/TV3_HuynhCongThu ở cả client và server. Mượn/trả, vi phạm/thu tiền, phòng đọc/phản hồi và BC-03.
- Chỉ một người sửa MainForm hoặc một Form cụ thể tại một thời điểm.
- Commit đủ bộ `.cs`, `.Designer.cs`, `.resx` khi Designer tạo/sửa form.
- Đổi tên model, chữ ký dịch vụ, bảng/cột hoặc package dùng chung cần báo cả nhóm trước khi ghép.
- Word/Excel/PowerPoint khó tự ghép xung đột. Mỗi lần chỉ một người sửa một file tài liệu rồi bàn giao.

## Cấu hình khi phát triển

1. Cập nhật mã nguồn và mở ThuVienDAU.sln ở gốc repo.
2. Trên máy server, chuẩn bị CSDL thử theo README và tạo server/appsettings.local.json từ mẫu.
3. Trên máy client, tạo client/appsettings.local.json theo mẫu nếu cần đổi Server.BaseUrl.
4. Build solution, chạy server trước và client sau; kiểm tra MySQL qua menu client.
5. Mỗi người sửa mô đun của mình trong cả client/Modules và server/Modules.
   DTO dùng chung đặt tại server/Contracts; thay đổi hợp đồng cần báo người gọi API.

Mỗi máy phát triển có thể chạy cả hai chương trình với CSDL thử riêng cùng cấu trúc.
Khi demo nhiều máy, các client dùng cùng URL server và dữ liệu demo thống nhất.
Cấu hình MySQL được đặt ở máy server. Dùng tài khoản DB giới hạn quyền.
Địa chỉ và cổng server phải khớp cấu hình client. Bổ sung tài khoản GitHub
của TV2 và TV3 vào README khi hai bạn cung cấp để giảng viên đối chiếu đóng góp.

## Khi gặp conflict

Không chọn toàn bộ “ours/theirs” nếu chưa hiểu nội dung. Trao đổi với người sửa cùng file,
giữ các thay đổi cần thiết của cả hai, rồi build/chạy lại trước khi commit phần giải quyết xung đột.
Không force-push hoặc reset main để bỏ qua lỗi.

## Giới hạn bản khởi đầu

Khung hiện tại có server API, client MVC, cấu hình riêng mỗi phía và kiểm tra kết nối chỉ đọc. Chưa có đăng nhập/phân quyền
hay nghiệp vụ mượn/trả. Chưa dùng bản khởi đầu với dữ liệu vận hành thật.

Hạn nộp: 29/11/2026. Chốt chức năng 27/11, kiểm thử 28/11, rà soát và nộp 29/11.
