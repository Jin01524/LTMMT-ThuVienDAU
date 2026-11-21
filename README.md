# Hệ thống quản lý thư viện DAU

## Mở và chạy project

Mở **ThuVienDAU.sln** bằng Visual Studio có workload **.NET desktop development** và hỗ trợ **.NET 10**.
Chọn **ThuVienDAU.WinForms** làm Startup Project rồi nhấn F5. Lần đầu cần kết nối Internet để restore NuGet.

```powershell
dotnet restore ThuVienDAU.sln --locked-mode
dotnet build ThuVienDAU.sln -c Release --no-restore
dotnet run --project src/ThuVienDAU.WinForms
```

Khung ban đầu chạy được khi chưa có MySQL. Để kiểm tra kết nối, sao chép
`src/ThuVienDAU.WinForms/appsettings.example.json` thành `appsettings.local.json` cùng thư mục,
điền thông tin máy mình rồi build lại. Trong app chọn **Công cụ → Kiểm tra kết nối MySQL**.
Ứng dụng không tự chạy SQL tạo bảng, không tự nhập dữ liệu và chưa có đăng nhập/nghiệp vụ.

Project WinForms tổ chức theo MVC. `Forms/` chứa View, `Controllers/` điều phối thao tác
từ giao diện và `Models/` chứa các kiểu dữ liệu. `Services/` thực hiện xử lý ứng dụng,
`Data/` phụ trách kết nối CSDL và `Configuration/` đọc cấu hình.
`Program.cs` khởi tạo và kết nối các thành phần.

Luồng kiểm tra MySQL đi từ `MainForm` qua `MainController` đến
`DatabaseHealthService` và `MySqlConnectionFactory`. Service trả về
`DatabaseConnectionInfo` để form hiển thị kết quả.

Quy ước nhánh, PR và hướng dẫn cho cả nhóm ở [CONTRIBUTING.md](CONTRIBUTING.md).
Các file bin/obj/.vs và cấu hình local được Git bỏ qua.

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
