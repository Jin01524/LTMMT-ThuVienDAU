# Hệ thống quản lý thư viện DAU

## Tổ chức server và client

Cấu trúc cập nhật ngày **21/11/2026** theo yêu cầu bài tập MVC.
Solution **ThuVienDAU.sln** có hai chương trình chạy riêng.

| Thư mục | Chương trình và trách nhiệm |
|---|---|
| [server](server/README.md) | TCP Socket Console .NET 10, xử lý nghiệp vụ và truy cập MySQL |
| [client](client/README.md) | Windows Forms .NET 10, giao diện MVC và gọi server qua TCP Socket/JSON |
| [server/Contracts](server/Contracts) | Kiểu dữ liệu JSON dùng chung, được client liên kết khi biên dịch |
| [csdl](csdl) | Bộ SQL và cấu hình nghiệp vụ dùng chung cho server |
| [tai_lieu](tai_lieu) | Thiết kế nghiệp vụ, kế hoạch và đối chiếu kiến trúc |

Các mô đun được bố trí đối xứng trong client/Modules và server/Modules.

| Thành viên | Thư mục trong Modules | Số mô đun | Tài khoản GitHub |
|---|---|---:|---|
| Võ Ngọc Bình | [TV1_VoNgocBinh](client/Modules/TV1_VoNgocBinh) | 10 | [Jin01524](https://github.com/Jin01524) |
| Trần Mỹ Nhung | [TV2_TranMyNhung](client/Modules/TV2_TranMyNhung) | 9 | Chưa cung cấp |
| Huỳnh Công Thủ | [TV3_HuynhCongThu](client/Modules/TV3_HuynhCongThu) | 10 | Chưa cung cấp |

Mỗi thư mục mô đun có README ghi người làm, người rà soát, phụ thuộc,
trách nhiệm ở mỗi phía và điều kiện nghiệm thu. Phân công đầy đủ nằm tại
[kiến trúc và đối chiếu tài liệu](tai_lieu/Kien_truc_Server_Client_MVC.md).

## Mở và chạy project

Dùng Visual Studio có workload **.NET desktop development**
và SDK **.NET 10**. Mở ThuVienDAU.sln.

    dotnet restore ThuVienDAU.sln --locked-mode
    dotnet build ThuVienDAU.sln -c Release --no-restore

Chạy server trong terminal thứ nhất.

    dotnet run --project server/ThuVienDAU.Server.csproj

Server mặc định lắng nghe TCP tại 127.0.0.1:5080.
Lệnh PING kiểm tra tiến trình; DATABASE_HEALTH kiểm tra MySQL.
Chạy client trong terminal thứ hai trên Windows.

    dotnet run --project client/ThuVienDAU.WinForms.csproj

Trong Visual Studio có thể chọn **Configure Startup Projects** và đặt cả
**ThuVienDAU.Server** và **ThuVienDAU.WinForms** sang **Start**.

Để kiểm tra MySQL, sao chép server/appsettings.example.json thành
server/appsettings.local.json, điền tài khoản DB rồi build lại server.
Chọn **Công cụ → Kiểm tra kết nối MySQL** trong client.
Client gửi yêu cầu đến server, server truy vấn MySQL và trả kết quả để hiển thị.

Khi chạy máy khác, đặt Server.Host là địa chỉ IP/tên máy server, Server.Port là cổng TCP trong
client/appsettings.local.json theo mẫu ở client và build lại client.
Chạy server với --host 0.0.0.0 --port 5080 trên mạng demo nội bộ
được cho phép. Chi tiết cấu hình nằm trong README của hai thư mục.

HT-01 có luồng kiểm tra kết nối qua mạng. Các mô đun nghiệp vụ đã có vị trí,
phân công và tiêu chí bàn giao, đang chờ triển khai chức năng.
Kết quả xác minh khung mới ghi ở
[kiểm tra server và client](kiem_tra/Server_Client_MVC.md).

Quy ước làm việc nhóm nằm ở [CONTRIBUTING.md](CONTRIBUTING.md).

## Tài liệu và CSDL hiện hành

Cập nhật ngày **20/11/2026**. CSDL gồm **20 bảng, 160 cột và 45 khóa ngoại**, dùng MySQL **8.0.21+**. Thư viện dùng DDC23 bản in tiếng Việt rút gọn để phân loại và MARC21 để biên mục.

| Nội dung | File |
|---|---|
| Báo cáo học phần Lập trình mạng máy tính theo mẫu | [Báo cáo quản lý thư viện DAU](tai_lieu/Bao_cao_Quan_ly_Thu_vien_DAU.docx) |
| Slide thuyết trình database — 12 trang | [Database_Thu_vien_DAU.pptx](slides/Database_Thu_vien_DAU.pptx) |
| SQL tạo mới ThuVienDAU | [01_Tao_CSDL.sql](csdl/01_Tao_CSDL.sql) |
| Ảnh sơ đồ CSDL — chèn báo cáo | [So_do_CSDL.png](so_do/So_do_CSDL.png) |
| Sơ đồ vector — phóng to rõ chữ | [So_do_CSDL.svg](so_do/So_do_CSDL.svg) |
| Thiết kế, phạm vi và giới hạn | [Thiet_ke_CSDL.md](tai_lieu/Thiet_ke_CSDL.md) |
| Tất cả cột/khóa ngoại | [Tu_dien_CSDL.md](tai_lieu/Tu_dien_CSDL.md) |
| Cây chức năng và workflow | [Cây module](tai_lieu/Cay_module_chuc_nang.md), [Workflow](tai_lieu/Workflow.md) |
| Kế hoạch triển khai nhóm 3 thành viên | [Luồng làm việc và phân công](tai_lieu/Ke_hoach_va_phan_cong_module.md) |
| Bảng theo dõi 29 module đến hạn nộp 29/11/2026 | [Phân công module](outputs/phan_cong_nhom_3/Theo_doi_cong_viec_den_29_11_2026.xlsx) |
| Kiểm tra SQL và ảnh hiện hành | [Ket_qua_MySQL80.txt](kiem_tra/Ket_qua_MySQL80.txt) |

## Phạm vi học tập

Không đặt trước. Một bảng TaiKhoan, một vai trò/tài khoản. Kiểm kê gồm KiemKe + ChiTietKiemKe. Vị trí chỉ dãy kệ/ngăn vì thư viện một tầng/một phòng. Thông tin tác giả, NXB, ISBN và mô tả chi tiết lưu trong MARC.

Mỗi biểu ghi lưu các trường và trường con trong một mảng JSON ở `BieuGhiMARC.Truong`. Mỗi đầu tài liệu lưu các tệp đính kèm trong `DauTaiLieu.DanhSachTep`. Hai mảng giữ thứ tự, và một đầu tài liệu có thể đính kèm nhiều tệp.

Quy định mượn/lễ-Tết và thông tin bộ DDC: [Cau_hinh_du_an.json](csdl/Cau_hinh_du_an.json). Nguồn DDC rút gọn đã chốt; năm in, ISBN và số ấn bản trên trang tên sách vẫn chưa có. Không tự coi dữ liệu tham khảo là đã được thư viện đối chiếu: còn 40 mục DDC và [49 đề xuất ngành–DDC](tai_lieu/Dinh_huong_DDC_nganh.json) chờ kiểm tra. Hồ sơ MARC chọn lọc ở [Cau_hinh_MARC21.json](csdl/Cau_hinh_MARC21.json).

## Cách dùng và tái tạo

Mở SQL trong Workbench, chạy toàn bộ khi schema **ThuVienDAU chưa tồn tại**. Không dùng làm migration hoặc chạy đè DB cũ. Script không DROP; nếu lỗi cần dừng kiểm tra. Chưa thay đổi DB đang dùng của người dùng.

- Sinh SQL, từ điển và cấu hình: **node cong_cu/csdl/tao_sql.mjs**.
- Vẽ lại ảnh: **node cong_cu/csdl/ve_so_do.mjs**, cần @viz-js/viz và sharp. Có thể đặt ERD_NODE_MODULES tới thư mục node_modules của runtime.
- **kiem_tra_gon.sql** chỉ dùng trên server thử riêng; fixtures được rollback.

## Dọn file cũ

Đã bỏ hai slide thiết kế/workflow cũ vì còn nội dung khác phạm vi hiện hành. Giữ một bộ SQL, tài liệu, cấu hình và ảnh sơ đồ. FigJam cũ không dùng làm căn cứ DB mới.

Sao lưu **21 file** của bản 23 bảng và đối chiếu SHA-256 trong `C:/Users/ngocb/.codex/visualizations/2026/11/01/01a19766-c3b9-7f82-830e-4d367ef38209/LTMMT_truoc_thu_gon_20_bang_2026-11-09.zip`.

Quyền cập nhật 20/11/2026: BIEN_MUC độc quyền khóa/mở đăng nhập độc giả; QUAN_LY_THU_VIEN giữ khóa/mở mượn và hủy phiếu phạt. [Kết quả kiểm thử quyền](kiem_tra/Quyen_dang_nhap_2026-11-20.txt). Khung WinForms chưa triển khai đăng nhập nghiệp vụ.

Giao thức hiện hành: [TCP Socket](tai_lieu/Giao_thuc_TCP.md). Khung mạng đã chuyển khỏi HTTP; bản ghi Server_Client_MVC.md trước đây là lịch sử. Kiểm thử hiện hành: `dotnet run --project cong_cu/kiem_tra_tcp`.

## TV2 đợt đầu ngày 22/11/2026

Đã triển khai TL-01 DDC23 và TL-02 đầu tài liệu qua TCP. Menu **TV2 · Biên mục** có trong client.
Chạy thử loopback bằng tài khoản thử được chỉ định ở server; HT-02 chưa có phiên thật.
[Hướng dẫn và bàn giao TV2](tai_lieu/TV2_Trien_khai.md). Các module TV2 còn lại chưa được triển khai trong đợt này.
