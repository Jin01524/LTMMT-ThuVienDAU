# Quy ước làm việc nhóm

Tài liệu này hướng dẫn ba thành viên nhận việc, tạo nhánh, kiểm tra và bàn giao thay đổi. Cách cài đặt/chạy chương trình nằm trong [README.md](README.md).

## Phân công

| Mã | Thành viên | Phần chịu trách nhiệm |
|---|---|---|
| TV1 | Huỳnh Công Thủ — 2351220232 | Hệ thống, độc giả, tích hợp, BC-01 |
| TV2 | Võ Ngọc Bình — 2351220274, **trưởng nhóm** | Tài liệu, DDC23/MARC21, bản sách, kho, BC-02 |
| TV3 | Trần Mỹ Nhung — 2351220273 | Mượn–trả, vi phạm, thu tiền, phòng đọc, phản hồi, BC-03 |

Mỗi người phụ trách cả giao diện client, xử lý server, tự kiểm và tài liệu phần mình. Người rà soát theo README của từng module và [kế hoạch nhóm](tai_lieu/Ke_hoach_va_phan_cong_module.md). Trưởng nhóm điều phối tiến độ; quyền sở hữu module vẫn theo mã đã phân công.

Đường dẫn module tương ứng ở cả hai phía:

- `client/Modules/TV1_HuynhCongThu/` và `server/Modules/TV1_HuynhCongThu/`.
- `client/Modules/TV2_VoNgocBinh/` và `server/Modules/TV2_VoNgocBinh/`.
- `client/Modules/TV3_TranMyNhung/` và `server/Modules/TV3_TranMyNhung/`.

## 1. Chuẩn bị trước khi nhận việc

Clone repo một lần, mở chung solution `ThuVienDAU.sln` và chạy được cả server/client theo README. Không tạo thêm solution hoặc schema khác cấu trúc cho mỗi thành viên.

Trước khi sửa một module, đọc README của module để biết phụ thuộc, đầu vào/đầu ra và điều kiện nghiệm thu. Thống nhất trước nếu cần đổi DTO, tên lệnh TCP, phương thức service hoặc cấu trúc CSDL.

## 2. Tạo nhánh cho một đầu việc

Kiểm tra trước:

```powershell
git status
```

Nếu còn thay đổi đang làm, lưu thành commit trên nhánh hiện tại trước khi chuyển nhánh. Sau đó lấy `main` mới nhất và tạo nhánh, ví dụ TV2 làm TL-04:

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/TV2-TL-04-ban-sach
```

Một nhánh dành cho một đầu việc đủ rõ để rà soát; không cần tạo sẵn nhánh cho toàn bộ module. Khi việc cũ đã ghép, lấy `main` mới và tạo nhánh cho việc tiếp theo. Các nhánh `codex/...` là nhánh triển khai do trợ lý tạo; vẫn cần đọc phạm vi và rà soát trước khi ghép.

## 3. Lập trình và lưu commit

Làm trong thư mục module được giao. Ví dụ TL-04 gồm bản sách và vị trí kho ở cả `client` lẫn `server`. Nếu sửa file dùng chung, ghi rõ tác động đến hai thành viên còn lại.

Kiểm tra các file trước khi commit:

```powershell
git status
git diff
```

Thêm đúng đường dẫn vào commit, ví dụ:

```powershell
git add client/Modules/TV2_VoNgocBinh/TL-04 server/Modules/TV2_VoNgocBinh/TL-04
git diff --cached
git commit -m "[TL-04] Them ban sach va kiem tra barcode trung"
```

Nếu có thay đổi DTO hoặc service dùng chung, thêm các file đó vào cùng commit hoặc commit liên quan để nhánh build được. Với form do Designer tạo, commit đủ `.cs`, `.Designer.cs` và `.resx` tương ứng.

Commit nên có mã module, nói rõ việc thay đổi; tránh gom nhiều module không liên quan hoặc đặt tên như `update`, `fix all`.

## 4. Kiểm tra trước khi push

Chạy tại gốc repo khi thay đổi mã nguồn:

```powershell
dotnet restore ThuVienDAU.sln --locked-mode
dotnet build ThuVienDAU.sln -c Release --no-restore
dotnet run --project cong_cu/kiem_tra_tcp -- .
```

Với thay đổi nghiệp vụ TL-01/TL-02, có đủ môi trường MySQL/Python thì chạy thêm:

```powershell
python cong_cu/kiem_tra_tv2/integration.py
```

Ngoài bộ tự động, chạy luồng giao diện liên quan: nhập dữ liệu, lưu/tải lại, thử dữ liệu sai và lỗi kết nối. Kiểm tra kết nối được không đồng nghĩa nghiệp vụ đã đúng. Nếu chỉ sửa tài liệu, kiểm tra đường dẫn, tên người, mã module và lệnh hướng dẫn; không cần chạy lại CSDL chỉ vì đổi câu chữ.

Không đánh dấu hoàn thành khi còn lỗi build hoặc chưa đạt tiêu chí module. Nếu thiếu API, thiết bị, phiên HT-02 hoặc quy định nghiệp vụ, ghi rõ phần đang chờ.

## 5. Push và tạo Pull Request

Ví dụ đẩy nhánh TL-04:

```powershell
git push -u origin feature/TV2-TL-04-ban-sach
```

Trên GitHub, tạo PR với **base `main`**, **compare là nhánh vừa push**. Dùng [mẫu PR](.github/pull_request_template.md), ghi:

- Mã module, vấn đề được xử lý và hành vi sau thay đổi.
- Cách kiểm tra cùng kết quả thực tế.
- Thay đổi CSDL/cấu hình/hợp đồng TCP và việc các bạn khác cần làm.
- Phần còn thiếu hoặc chưa thử.

Gửi người rà soát theo phân công. Ghép sau khi build/kiểm thử đạt, đã xử lý nhận xét và có thành viên khác rà lại. Không tự ghi đè `main` để bỏ qua xung đột, lỗi kiểm thử hoặc quy trình rà soát.

Sau khi PR được ghép:

```powershell
git switch main
git pull --ff-only origin main
```

Cả nhóm cập nhật trước khi nhận việc mới để dùng cùng DTO, giao thức và schema.

## Kiến trúc cần tuân thủ

- View WinForms phát sự kiện; Controller client điều phối và gọi dịch vụ TCP. Không đặt truy vấn MySQL trong form.
- Server nhận lệnh TCP, kiểm phiên/quyền, gọi Service xử lý nghiệp vụ và Data/Repository truy cập CSDL.
- Dùng DTO và mã đóng khung tại `server/Contracts/`; client liên kết các file này khi build. Không tự tạo định dạng bản tin riêng cho từng module.
- Truy vấn có tham số. Mượn, trả, thu tiền và các thay đổi liên quan phải dùng giao dịch phù hợp ở server.
- Lấy ID người thao tác từ phiên do server xác thực, không tin ID hoặc vai trò gửi trong payload client.

Quy ước TCP hiện dùng header độ dài 4 byte big endian và JSON UTF-8, một request/response trên mỗi kết nối. Đọc đủ header/body; không giả định một lần nhận socket là một bản tin. Xem [giao thức TCP](tai_lieu/Giao_thuc_TCP.md).

HT-02 chưa hoàn chỉnh. `--tv2-demo-account` chỉ là chế độ thử loopback trên CSDL thử; không xem đó là đăng nhập thật hoặc dùng trên LAN. Điểm ghép phiên cho TV1 được ghi ở [bản bàn giao TV2](tai_lieu/TV2_Trien_khai.md).

## Phối hợp khi sửa file chung

Các file cần báo nhau trước khi sửa gồm `ThuVienDAU.sln`, `MainForm`, `Program.cs`, `server/Contracts/`, cấu hình/generator CSDL và các lớp dùng chung.

Một thời điểm chỉ một người sửa một form/Designer. Khi thay hợp đồng, thống nhất tên lệnh, cấu trúc payload, dữ liệu trả về, thông báo lỗi và nơi kiểm quyền. Bàn giao cả phần client/server liên quan để nhánh không phụ thuộc vào file chưa commit trên máy khác.

Word, Excel và PowerPoint khó ghép tự động: thống nhất người đang giữ file, lưu thay đổi rồi đóng file trước khi bàn giao. Cập nhật trạng thái trong bảng theo dõi theo kết quả thực tế, không lấy số commit làm phần trăm hoàn thành.

## CSDL, DDC23 và MARC21

Mỗi người có thể dùng MySQL thử riêng nhưng cùng cấu trúc SQL chuẩn. Bộ `csdl/01_Tao_CSDL.sql` dành cho schema mới, không dùng để ghi đè dữ liệu hiện hữu.

Khi đổi schema, sửa nguồn trong `cong_cu/csdl/`, sinh lại SQL/từ điển/cấu hình liên quan và kiểm tra trên CSDL thử. Ghi rõ cách nâng cấp CSDL đang tồn tại trong PR; không chỉ gửi một bản SQL sửa tay khác với generator. Không đổi tên bảng/cột đã bàn giao mà không cập nhật người gọi.

Giữ phạm vi đã thống nhất: không OPAC, đặt trước, cấp thẻ thư viện hoặc phân hệ nhân viên riêng; vị trí chỉ dãy kệ/ngăn. DDC tham khảo chưa phải dữ liệu được duyệt. Xác nhận phân loại cần căn cứ tra bộ DDC23 bản in tiếng Việt rút gọn; MARC dùng profile cục bộ đã chốt.

## Cấu hình và dữ liệu riêng

- Giữ `appsettings.example.json` trong repo với giá trị mẫu; sao chép thành `appsettings.local.json` để dùng trên từng máy.
- MySQL nằm ở cấu hình server; client chỉ có địa chỉ/cổng TCP và thời gian chờ.
- Không commit mật khẩu thật, token, file `.env`, bản sao DB, thư mục `.vs`, `bin`, `obj` hoặc `.tooling`.
- Kiểm tra `git diff --cached` trước mỗi commit. Nếu đã đưa bí mật lên Git, báo trưởng nhóm và đổi thông tin đó; xóa file ở commit sau không xóa được lịch sử đã công bố.

Không sửa `.gitignore` để đưa cấu hình cá nhân vào repo. Chỉ thay package khi cần; cập nhật và commit `packages.lock.json` cùng thay đổi liên quan, rồi kiểm tra lại restore bằng `--locked-mode`.

## Khi gặp conflict

Dừng để đọc từng phần xung đột và trao đổi với người sửa cùng file. Giữ cả thay đổi cần thiết của hai bên; không chọn toàn bộ `ours`/`theirs` khi chưa hiểu tác động. Không force-push hoặc reset `main` để bỏ qua lỗi.

Sau khi giải quyết, chạy lại build và các luồng liên quan rồi mới commit phần ghép. Với file Office, lấy bản của người đang giữ file làm nguồn và nhập lại phần chỉnh sửa còn thiếu, tránh thay thế mất nội dung đã chốt.

## Mốc bàn giao

- **27/11/2026:** chốt chức năng để tích hợp.
- **28/11/2026:** kiểm thử, sửa lỗi và chạy kịch bản demo.
- **29/11/2026:** rà soát bản cuối và nộp.

Nếu một module chậm hoặc vướng phụ thuộc, báo sớm cho trưởng nhóm và người dùng dữ liệu của module đó. Mọi người cùng chịu trách nhiệm kiểm thử tích hợp và phần thuyết trình của mình.
