# Hệ thống quản lý thư viện Đại học Kiến trúc Đà Nẵng

Đồ án học phần **Lập trình mạng máy tính**, lớp **23CT3**. Hệ thống gồm client Windows Forms và server Console .NET 10, giao tiếp bằng **TCP Socket trực tiếp**. Server xử lý nghiệp vụ và truy cập MySQL; client hiển thị giao diện theo MVC.

Cập nhật: **25/11/2026**. Hạn nộp: **29/11/2026**.

## Trạng thái triển khai

| Phần | Hiện trạng |
|---|---|
| Khung client/server | Đã có truyền nhận TCP/JSON, timeout, giới hạn bản tin và xử lý nhiều client |
| HT-01 — kết nối | Client yêu cầu server kiểm tra MySQL và hiển thị kết quả |
| TL-01 — DDC23 | Đã có bản đầu tra cây/danh mục, tìm, thêm/sửa và đối chiếu bản in |
| TL-02 — đầu tài liệu | Đã có bản đầu tìm, thêm/sửa và quản lý trạng thái phân loại |
| TL-04 — bản sách/vị trí | Đã có bản đầu ĐKCB/barcode, giá/ngày, tình trạng và dãy kệ/ngăn |
| HT-02 — đăng nhập/phiên | Đã ghép đăng nhập 5 vai trò qua TCP, phiên hết hạn/đăng xuất và quyền tài khoản độc giả |
| Các module khác | Đã phân công; đang chờ triển khai theo kế hoạch |

TL-01/TL-02/TL-04 đã có giao dịch, kiểm quyền phía server, nhật ký và kiểm tra phiên bản khi sửa. Đã ghép phiên HT-02; còn cần nghiệm thu thao tác UI/LAN thực tế. Kết quả kiểm thử không đồng nghĩa toàn bộ 29 module đã hoàn tất.

## Thành viên và phạm vi

| Mã | Thành viên | MSSV | Phụ trách | Số module |
|---|---|---|---|---:|
| TV1 | Huỳnh Công Thủ | 2351220232 | Hệ thống, độc giả, tích hợp, BC-01 | 10 |
| TV2 | **Võ Ngọc Bình — trưởng nhóm** | 2351220274 | Tài liệu, DDC23/MARC21, bản sách, kho, BC-02 | 9 |
| TV3 | Trần Mỹ Nhung | 2351220273 | Mượn–trả, vi phạm, thu tiền, phòng đọc, phản hồi, BC-03 | 10 |

Phân công chi tiết và lịch bàn giao: [kế hoạch nhóm](tai_lieu/Ke_hoach_va_phan_cong_module.md), [bảng theo dõi Excel](outputs/phan_cong_nhom_3/Theo_doi_cong_viec_den_29_11_2026.xlsx).

## Chuẩn bị môi trường

- Windows để chạy client WinForms.
- Visual Studio hỗ trợ .NET 10, workload **.NET desktop development**, hoặc .NET 10 SDK để build/chạy bằng terminal.
- MySQL **8.0.21 trở lên** trên máy server. Có thể dùng MySQL Workbench để chạy SQL và xem dữ liệu.
- Git để lấy mã nguồn và làm việc nhóm.

Kiểm tra SDK:

```powershell
dotnet --list-sdks
```

Máy chưa có repo thì clone một lần:

```powershell
git clone https://github.com/Jin01524/LTMMT-ThuVienDAU.git
cd LTMMT-ThuVienDAU
```

Các lệnh bên dưới chạy tại **thư mục chứa `ThuVienDAU.sln`**. Nếu đã có repo, cập nhật theo [CONTRIBUTING.md](CONTRIBUTING.md), không cần clone lại mỗi lần.

## Chạy lần đầu

### 1. Chuẩn bị CSDL và cấu hình MySQL

Nếu chưa có schema `ThuVienDAU`, mở [csdl/01_Tao_CSDL.sql](csdl/01_Tao_CSDL.sql) trong Workbench và chạy trên MySQL thử của nhóm. Đây là script **tạo mới**, không phải migration: không chạy lại lên CSDL đang có dữ liệu.

Tạo cấu hình riêng cho server nếu file chưa tồn tại:

```powershell
if (-not (Test-Path server/appsettings.local.json)) {
    Copy-Item server/appsettings.example.json server/appsettings.local.json
}
```

Mở `server/appsettings.local.json` và điền thông tin trong mục `MySql`:

| Khóa | Nội dung |
|---|---|
| `Server`, `Port` | Máy chủ và cổng MySQL; cổng thường dùng là 3306 |
| `Database` | `ThuVienDAU` |
| `User`, `Password` | Tài khoản MySQL được cấp quyền trên CSDL |
| `SslMode` | Theo cấu hình kết nối của server; xem file mẫu |

File `appsettings.local.json` được Git bỏ qua. Mật khẩu MySQL chỉ đặt ở server; giữ file `appsettings.example.json` làm mẫu cho các thành viên khác.

### 2. Restore và build

```powershell
dotnet restore ThuVienDAU.sln --locked-mode
dotnet build ThuVienDAU.sln -c Release --no-restore
```

### 3. Chạy server trước

Trong terminal thứ nhất:

```powershell
dotnet run --project server/ThuVienDAU.Server.csproj
```

Giữ terminal mở. Khi sẵn sàng, server in:

```text
ThuVienDAU TCP listening on 127.0.0.1:5080
```

### 4. Chạy client

Trong terminal thứ hai, tại gốc repo:

```powershell
dotnet run --project client/ThuVienDAU.WinForms.csproj
```

Đăng nhập bằng tài khoản đã cấp, rồi chọn **Công cụ → Kiểm tra kết nối MySQL**. Client kết nối TCP tới server, sau đó server mới kiểm tra MySQL.

Trong Visual Studio, mở `ThuVienDAU.sln` và cấu hình hai Startup Projects: **ThuVienDAU.Server** và **ThuVienDAU.WinForms** cùng ở trạng thái **Start**. Nếu client báo chưa kết nối được server lúc vừa khởi động, đợi server sẵn sàng rồi thử lại.

Sau khi đổi cấu hình local, dừng và chạy lại project tương ứng để file được chép sang thư mục chạy.

## Cấu hình TCP và chạy trên LAN

Mặc định client dùng:

```json
{
  "Server": {
    "Host": "127.0.0.1",
    "Port": 5080,
    "TimeoutSeconds": 15
  }
}
```

Muốn đổi địa chỉ, sao chép `client/appsettings.example.json` thành `client/appsettings.local.json` nếu chưa có, rồi sửa `Server.Host` và `Server.Port`. `Host` là IP/tên máy, không phải URL HTTP.

Trên máy server của mạng demo:

```powershell
dotnet run --project server/ThuVienDAU.Server.csproj -- --host 0.0.0.0 --port 5080
```

Trên máy client, đặt `Host` là IP thật của máy server, chẳng hạn `192.168.1.10`; cho phép cổng TCP đã chọn trong mạng demo. Cổng **5080** là kết nối client–server; cổng **3306** là kết nối server–MySQL.

Khung TCP hiện có phiên đăng nhập, chưa có mã hóa đường truyền. Chế độ thử TV2 dưới đây chỉ dùng trên loopback, không dùng khi chạy LAN.

Chi tiết đóng khung bản tin và tên lệnh: [Giao_thuc_TCP.md](tai_lieu/Giao_thuc_TCP.md).

## Chạy TL-01, TL-02 và TL-04 sau đăng nhập

Chạy server, mở client và đăng nhập tài khoản BIEN_MUC hoặc QUAN_LY_THU_VIEN đã được cấp. Chọn **TV2 · Biên mục**; phiên được chuyển tự động, không cần nhập token bằng tay. Đọc [bản ghép TV1](tai_lieu/TV1_Ghep_TCP.md) để biết phần đăng nhập, quyền mượn, cấu hình/nhật ký và báo cáo.

Chế độ `--tv2-demo-account` vẫn giữ cho fixture tự động và lệnh TCP thử trên loopback, không thay đăng nhập thật của giao diện. Không tự tạo hồ sơ nhân viên/thẻ thư viện thay quy trình đã chốt. Hướng dẫn DDC/bản sách trong [TV2_Trien_khai.md](tai_lieu/TV2_Trien_khai.md) có mốc trước khi ghép login; cách mở UI hiện hành theo đoạn này.

## Cấu trúc repo

| Đường dẫn | Trách nhiệm |
|---|---|
| `ThuVienDAU.sln` | Solution chứa hai project |
| `client/` | WinForms MVC; gọi server bằng TCP, không truy cập MySQL trực tiếp |
| `server/` | TCP listener, xử lý nghiệp vụ, kiểm quyền và truy cập MySQL |
| `server/Contracts/` | DTO và quy ước TCP dùng chung, liên kết khi build client |
| `client/Modules/` và `server/Modules/` | Module chia theo TV1/TV2/TV3 và người phụ trách |
| `csdl/` | SQL tạo mới, cấu hình DDC23/MARC21 và nghiệp vụ |
| `cong_cu/` | Công cụ sinh CSDL và các bộ kiểm thử |
| `tai_lieu/`, `slides/`, `so_do/` | Báo cáo, thiết kế, slide và sơ đồ |
| `outputs/phan_cong_nhom_3/` | Bảng theo dõi công việc |
| `kiem_tra/` | Kết quả kiểm tra đã ghi nhận |

Ví dụ phần TV2: [client](client/Modules/TV2_VoNgocBinh), [server](server/Modules/TV2_VoNgocBinh). Mỗi module có README riêng ghi phụ thuộc, người rà soát và tiêu chí nghiệm thu.

## Phạm vi nghiệp vụ đã chốt

CSDL hiện có **20 bảng**. DDC23 dùng bộ **bản in tiếng Việt rút gọn**; dữ liệu DDC tham khảo chỉ là gợi ý, cán bộ phải đối chiếu bản in trước khi xác nhận. MARC21 áp dụng profile cục bộ, lưu trường/trường con theo thứ tự trong JSON; giao diện biên mục MARC thuộc TL-03, chưa hoàn tất trong bản này.

Hệ thống dùng thẻ trường để định danh; không cấp thẻ thư viện, không có OPAC hoặc đặt trước. Hồ sơ độc giả lấy từ danh sách trường; vị trí kho chỉ gồm dãy kệ/ngăn. Quyền khóa/mở **đăng nhập độc giả** thuộc Biên mục; quyền khóa/mở **mượn** và hủy hiệu lực phiếu phạt thuộc Quản lý thư viện. Đây là các quyền độc lập, cần giữ khi triển khai HT-02 và nghiệp vụ tiếp theo.

## Kiểm thử

Sau khi build Release, kiểm tra khung TCP:

```powershell
dotnet run --project cong_cu/kiem_tra_tcp -- .
```

Kiểm tra TL-01/TL-02/TL-04 cần thêm Python 3 và bộ MySQL Server có `mysqld`, `mysql`, `mysqladmin`:

```powershell
python cong_cu/kiem_tra_tv2/integration.py
```

Bộ TV2 tạo MySQL và server thử riêng, dùng dữ liệu giả; không sửa CSDL hoặc cấu hình đang dùng. Xem [hướng dẫn fixture](cong_cu/kiem_tra_tv2/README.md) nếu MySQL không cài ở đường dẫn mặc định.

Lần rà soát ngày 23/11/2026: build 0 lỗi/0 cảnh báo, **19 ca TCP và 62 ca TV2 đạt**. Chưa thay thế kiểm thử giao diện đầy đủ hoặc nhiều máy LAN. GitHub Actions chạy build và bộ TCP khi có PR hoặc cập nhật `main`; bộ MySQL TV2 được chạy riêng.

## Khi gặp lỗi kết nối

| Thông báo/tình huống | Kiểm tra |
|---|---|
| Không kết nối được server | Server đã chạy chưa? `Host`/`Port` có khớp? Cổng TCP có được phép kết nối? |
| Server cần cấu hình MySQL | Kiểm tra `server/appsettings.local.json`, JSON hợp lệ, rồi chạy lại server |
| Server chưa kết nối được MySQL | Kiểm tra dịch vụ MySQL, máy chủ/cổng, tài khoản và quyền trên CSDL |
| Chưa có phiên Biên mục | Dùng đúng chế độ thử loopback hoặc chờ ghép phiên HT-02 |
| Dữ liệu đã thay đổi | Tải lại bản mới trước khi sửa; không ghi đè bản của thành viên khác |

## Tài liệu tham khảo trong repo

- [Quy trình Git và quy ước đóng góp](CONTRIBUTING.md).
- [Kế hoạch 29 module](tai_lieu/Ke_hoach_va_phan_cong_module.md) và [bảng theo dõi](outputs/phan_cong_nhom_3/Theo_doi_cong_viec_den_29_11_2026.xlsx).
- [Cây chức năng](tai_lieu/Cay_module_chuc_nang.md), [workflow](tai_lieu/Workflow.md).
- [Thiết kế CSDL](tai_lieu/Thiet_ke_CSDL.md), [từ điển CSDL](tai_lieu/Tu_dien_CSDL.md), [sơ đồ CSDL](so_do/So_do_CSDL.png).
- [Báo cáo Word](tai_lieu/Bao_cao_Quan_ly_Thu_vien_DAU.docx), [slide database](slides/Database_Thu_vien_DAU.pptx).
- [Kết quả rà soát gần nhất](kiem_tra/Ra_soat_2026-11-23.md), [kết quả TL-01/TL-02/TL-04](kiem_tra/TV2_TL01_TL02_TL04.txt).

Báo cáo Word/Excel còn lưu một số mốc thiết kế trước khi tách TCP server/client. Khi đối chiếu mã đang chạy, đọc README này cùng [giao thức TCP](tai_lieu/Giao_thuc_TCP.md) và [bản triển khai TV2](tai_lieu/TV2_Trien_khai.md). Việc đồng bộ báo cáo không thay thế nghiệm thu chức năng.

Bản TL-04 và hợp đồng tra barcode cho TV3: [TV2_TL04_Ban_sach.md](tai_lieu/TV2_TL04_Ban_sach.md).

Bản ghép TV1 và kết quả tự kiểm: [TV1_Ghep_TCP.md](tai_lieu/TV1_Ghep_TCP.md).
