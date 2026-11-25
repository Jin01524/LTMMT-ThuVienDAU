# Giải xung đột nhánh TV1 — 25/11/2026

PR #3: `feature/TV1-HT02-login-session`. Đầu TV1 trước khi ghép: 5d14e07;
main được đưa vào: f8c6ade. Merge thường, giữ lịch sử/tác giả của hai bên.

Nguyên nhân: main đổi TV1 sang Huỳnh Công Thủ và chuyển HTTP sang TCP, trong khi
TV1 thêm login/độc giả/nhật ký/báo cáo trên cấu trúc HTTP/tên thư mục cũ.

Đã xử lý toàn bộ content/file-location conflict. Giữ menu và dịch vụ TV1 mới;
đưa tên thư mục/namespace về TV1_HuynhCongThu; giữ client WinForms/server Console
TCP và TL-01/TL-02/TL-04 của main. Đăng nhập dùng cùng phiên cho TV1/TV2; không
nhận vai trò hoặc actor giả từ payload. Không merge PR #4 của TV2 vào PR #3.

Kết quả thực tế:

- Restore locked-mode, build Release: **0 lỗi, 0 cảnh báo**.
- **19** kiểm tra giao thức TCP với listener và transport thật: đạt.
- **62** kiểm tra hồi quy TL-01/TL-02/TL-04 trên MySQL/TCP: đạt.
- **38** kiểm tra server auth/TV1/TV2, trạng thái/vai trò hiện tại, tách quyền
  đăng nhập/mượn, logout và phản hồi quá 1 MiB: đạt.
- **9** kiểm tra qua service client thật: login, tra độc giả, cấu hình/nhật ký,
  báo cáo, khóa/mở mượn, khóa/mở đăng nhập, phiên TV2 và đăng xuất: đạt.
- Tổng **128 ca đạt**; không còn marker conflict trong source hoặc mục unmerged.

Các fixture dùng DB/server tạm riêng và tài khoản giả, dừng trong finally; không
sửa DB/config đang dùng. Bài RMI 2351220274 không đưa vào commit.

Chưa nghiệm thu UI/LAN đầy đủ và chưa làm thay DG-01, HT-04, TC-02 hoặc audit
login/logout. Hướng dẫn/giới hạn: `tai_lieu/TV1_Ghep_TCP.md`.
