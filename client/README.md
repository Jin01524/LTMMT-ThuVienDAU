# Client WinForms MVC

Client chạy trên Windows với .NET 10 và Visual Studio.
Mở solution ThuVienDAU.sln ở gốc repo, chọn ThuVienDAU.WinForms để chạy giao diện.
Server cần được khởi động để dùng chức năng kiểm tra MySQL.

## Cấu hình

Client mặc định gọi http://localhost:5080/.
Để đổi máy chủ, sao chép appsettings.example.json thành appsettings.local.json
ngay trong client, sửa Server.BaseUrl rồi build lại. File local được Git bỏ qua.

Ví dụ Server.BaseUrl là http://192.168.1.10:5080/.
Thay bằng địa chỉ server của nhóm.

## Phân công mô đun

| Người làm | Thư mục | Mã |
|---|---|---|
| Võ Ngọc Bình | [TV1_VoNgocBinh](Modules/TV1_VoNgocBinh) | HT-01…04, DG-01…03, BC-01, TC-01…02 |
| Trần Mỹ Nhung | [TV2_TranMyNhung](Modules/TV2_TranMyNhung) | TL-01…05, TL-07, KK-01…02, BC-02 |
| Huỳnh Công Thủ | [TV3_HuynhCongThu](Modules/TV3_HuynhCongThu) | LT-01…04, VP-01…03, PD-01, PH-01, BC-03 |

Trong mô đun đã có code HT-01, Views chứa form và giao diện IMainView,
Controllers chứa MainController, Models chứa lỗi kiểm tra và các DTO liên kết,
Services chứa lớp gọi API. Program.cs nối form, controller và HttpClient.

Các DTO tại server/Contracts được Compile Link vào client để hai bên dùng cùng
kiểu JSON. Client phụ thuộc mã hợp đồng và thư viện .NET HTTP.
Những mô đun tiếp theo bổ sung Views, Controllers và Models theo cùng bố cục
khi bắt đầu lập trình.

## Luồng hiện có

Nút kiểm tra MySQL phát sự kiện tới MainController.
DatabaseHealthService gửi GET /api/database/health, đọc JSON và trả model
cho form hiển thị. Lỗi mất mạng, quá thời gian chờ, cấu hình và lỗi MySQL
được thông báo trên giao diện.

Chạy dotnet run --project client/ThuVienDAU.WinForms.csproj từ gốc repo.
