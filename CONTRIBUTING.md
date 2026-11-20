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

- TV1: hệ thống, hồ sơ độc giả, BC-01 và điều phối tích hợp.
- TV2: tài liệu, DDC23/MARC21, kho và BC-02.
- TV3: mượn/trả, vi phạm/thu tiền, phòng đọc/phản hồi và BC-03.
- Chỉ một người sửa MainForm hoặc một Form cụ thể tại một thời điểm.
- Commit đủ bộ `.cs`, `.Designer.cs`, `.resx` khi Designer tạo/sửa form.
- Đổi tên model, chữ ký dịch vụ, bảng/cột hoặc package dùng chung cần báo cả nhóm trước khi ghép.
- Word/Excel/PowerPoint khó tự ghép xung đột. Mỗi lần chỉ một người sửa một file tài liệu rồi bàn giao.

## Cấu hình MySQL trên từng máy

1. Chạy SQL tạo CSDL mới theo README khi schema chưa có. Không chạy lại để ghi đè CSDL hiện hữu.
2. Sao chép `src/ThuVienDAU.WinForms/appsettings.example.json` thành `appsettings.local.json` trong cùng thư mục.
3. Điền máy chủ, cổng, CSDL và tài khoản riêng được cấp quyền. File local được Git bỏ qua.
4. Build lại, chạy ứng dụng và chọn Công cụ → Kiểm tra kết nối MySQL.

Mỗi máy phát triển có thể dùng CSDL thử riêng nhưng phải cùng cấu trúc SQL chuẩn.
Khi demo Client–Server, các máy trạm dùng cùng máy chủ và dữ liệu demo đã thống nhất.
Không đưa mật khẩu thật vào code, PR hoặc cấu hình mẫu. Không dùng root cho máy khách khi triển khai chung.
Không bật AllowPublicKeyRetrieval để né lỗi xác thực; cấu hình TLS/chứng chỉ hoặc khóa máy chủ đúng môi trường.

## Khi gặp conflict

Không chọn toàn bộ “ours/theirs” nếu chưa hiểu nội dung. Trao đổi với người sửa cùng file,
giữ các thay đổi cần thiết của cả hai, rồi build/chạy lại trước khi commit phần giải quyết xung đột.
Không force-push hoặc reset main để bỏ qua lỗi.

## Giới hạn bản khởi đầu

Bản đầu có khung form, cấu hình mẫu và kiểm tra kết nối chỉ đọc. Chưa có đăng nhập/phân quyền
hay nghiệp vụ mượn/trả. Chưa dùng bản khởi đầu với dữ liệu vận hành thật.

Hạn nộp: 29/11/2026. Chốt chức năng 27/11, kiểm thử 28/11, rà soát và nộp 29/11.
