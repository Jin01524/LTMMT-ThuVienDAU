# Bàn giao các module TV2 còn lại — 25/11/2026

Võ Ngọc Bình (TV2): TL-03, TL-05, TL-07, KK-01, KK-02, BC-02. Triển khai trên
nhánh `feature/TV2-hoan-thien-module`, nối tiếp TL-01/TL-02/TL-04 trên main.
Giữ nguyên 20 bảng, không cần migration hoặc chạy lại SQL tạo CSDL.
Các module đã có client/server và tự kiểm; còn cần các bạn rà soát, ghép HT-02
và nghiệm thu trên máy/thiết bị của nhóm trước khi chốt toàn hệ thống.

## Cách sử dụng

Build/chạy theo README gốc. Trong khi main chưa có HT-02, dùng phiên thử được
cấp bởi server **chỉ trên loopback**, trên DB thử. Mở **TV2 · Biên mục**, nhập
token, chọn module. Giao diện không tự tạo hồ sơ SV/GV hoặc tài khoản.
Đổi vai trò trên DB chỉ dành cho fixture kiểm thử; khi ghép HT-02 phải đăng nhập
bằng tài khoản đúng vai trò. Các lệnh nghiệp vụ kiểm lại vai trò/trạng thái từ DB.

| Module | Luồng thực hiện |
| --- | --- |
| TL-03 | Chọn đầu tài liệu → mẫu cơ bản/thêm dòng → lưu nháp → kiểm tra/chốt |
| TL-05 | Chọn đồ án/luận văn đã tạo ở TL-02 → chọn ngành/khóa → thêm SV/GV từ danh sách trường → lưu → thêm/tải PDF |
| TL-07 | Chọn các bản sách → xem trước nhãn A4 → in bằng nút trong cửa sổ xem trước |
| KK-01 | Nhập tên → chọn phạm vi dãy kệ/ngăn hoặc toàn kho → lập danh sách → xem đợt → bắt đầu → chọn vị trí thực tế/quét → chốt → xuất CSV |
| KK-02 | Biên mục chọn bản, lập đề nghị và lý do → Quản lý chọn đề nghị, ghi căn cứ, duyệt/chốt hoặc từ chối |
| BC-02 | Chọn khoảng ngày nhập/DDC/loại (hoặc để tất cả) → lập báo cáo → xuất CSV; mở kiểm kê để xem/xuất chi tiết đợt |

## MARC21 và DDC23

Editor trình bày một dòng cho mỗi trường điều khiển hoặc trường con. **Nhóm** là
một lần xuất hiện của trường; các dòng cùng nhóm phải liên tiếp và cùng nhãn/chỉ
thị. Trường 650/700 lặp dùng nhóm khác nhau; không gộp làm mất thứ tự. Có nút lên/
xuống và Delete để chỉnh dòng. Trường điều khiển 001–009 không có chỉ thị/trường con.
Nội dung 008 giữ đủ 40 ký tự; mẫu khoảng trắng là nháp cần điền từ tài liệu thật.

Lưu nháp kiểm cấu trúc bằng các thủ tục/trigger hiện hành. Kiểm tra/chốt kiểm
profile cục bộ: trường bắt buộc, lặp/chỉ thị, 082 và DDC. Đổi nội dung luôn trở về
nháp, xóa dấu kiểm. Kiểm tra/chốt đồng bộ 245$a sang nhan đề trong cùng giao dịch;
lỗi ở một bước rollback toàn bộ. Mọi lưu/chốt kiểm cả phiên MARC và phiên đầu tài
liệu để tránh ghi đè thay đổi của người khác. Đầu tài liệu đổi sau khi MARC đã kiểm
được báo cần đối chiếu lại, không coi bản MARC cũ là còn hiệu lực.

Giữ cấu hình dự án đã chốt: DDC23 bản in tiếng Việt rút gọn, 082 chỉ thị thứ nhất
`1`, $a khớp mã đã xác nhận, $2 là `23` theo profile hiện hành. Chỉ thị 1 biểu thị
bản rút gọn theo [Library of Congress — 082](https://loc.gov/marc/bibliographic/concise/bd082.html).
245$a là nhan đề và là trường không lặp theo
[Library of Congress — 245](https://www.loc.gov/marc/bibliographic/bd245.html).
Đây là profile MARC chọn lọc của đồ án; không có nhập/xuất ISO2709/MARCXML hoặc tự
khẳng định mọi quy tắc MARC21 đều được kiểm. Mã DDC khởi tạo vẫn chờ đối chiếu bản in.

## Tệp PDF và hồ sơ đề tài

Thành viên thực hiện chọn hồ sơ sinh viên; hướng dẫn chọn cán bộ/giảng viên/trợ
giảng từ `DocGia`. Server ghi lại tên/mã trường từ DB, không tin tên gửi từ client.
Mỗi đề tài tối đa 30 dòng vai trò không trùng, 20 PDF, tối đa 20 MiB/PDF. PDF truyền
theo khối tối đa 192 KiB, vẫn giữ giới hạn frame TCP 1 MiB. Server kiểm thứ tự khối,
kích thước và chữ ký PDF; đây không phải trình kiểm định toàn bộ nội dung PDF.

File lưu trong **`data/tep-do-an/` cạnh assembly server**; `DanhSachTep` chỉ chứa
tên và mã tệp ngẫu nhiên, không chứa đường dẫn tùy ý từ client. Download kiểm liên
kết đề tài trong DB. Chỉ Biên mục/Quản lý được tải; chưa mở API tệp cho độc giả.
Upload có chủ sở hữu, hết hạn sau 1 giờ; phần tải dở cũ được dọn khi bắt đầu lần tải
mới. Lỗi lúc hoàn tất có thể để lại PDF chưa gắn DB; tệp đó không thể tải qua API.
Hiện chưa có màn hình xóa tệp hoặc tự dọn PDF hoàn tất không được tham chiếu.

**Bàn giao HT-04:** sao lưu/phục hồi cả DB và thư mục `data/tep-do-an/` cùng mốc.
Chỉ backup SQL sẽ không chứa nội dung PDF. Khi chuyển bản build phải giữ thư mục
dữ liệu này; Git bỏ qua tệp người dùng. Các đường dẫn cũ do nhập ngoài ứng dụng
cần chuyển sang tệp được upload qua API; ứng dụng không mở đường dẫn tùy ý.

## Kiểm kê và thanh lý

Quy tắc được Võ Ngọc Bình xác nhận ngày 25/11/2026:

- Lập danh sách dự kiến chụp các bản trong phạm vi, loại bản đang mượn/đã thanh lý.
- Nếu một bản chưa kiểm phát sinh khoản mượn, không cho chốt là thiếu; đợi trả,
  kiểm lại hoặc hủy đợt. Mỗi dòng quét ghi người/thời điểm từ server.
- Quét phân biệt khớp, sai vị trí, hỏng, ngoài danh sách; barcode lạ vẫn được ghi
  thành thừa. Khi chốt, dòng dự kiến chưa kiểm mới chuyển thành thiếu. Chi tiết
  giữ vị trí dự kiến, không bị sửa theo vị trí bản sách sau đó.
- Kiểm kê không sửa trạng thái sách thành mất/thanh lý, không đổi vị trí/tình
  trạng kho và không sinh bồi thường. Sách hỏng ngoài danh sách ghi THUA; cờ hỏng
  bổ sung được giữ trong nhật ký quét để xử lý sau.
- **BIEN_MUC lập đề nghị; QUAN_LY_THU_VIEN duyệt/chốt hoặc từ chối.** Quản trị không
  kế thừa quyền này. Khi lập và khi duyệt đều kiểm khoản mượn mở/trạng thái sách.
  Một bản chỉ có một đề nghị đang chờ. Duyệt cùng giao dịch với chuyển sách sang
  THANH_LY; từ chối giữ nguyên bản sách. Không có bước xóa vật lý chứng từ/sách.
- Lý do đề nghị giữ trong ThanhLy; căn cứ quyết định, người xử lý và trước/sau
  giữ trong nhật ký. Đã xử lý không được duyệt lại. Đợt hoàn tất/hủy không quét tiếp.

## Báo cáo và in

BC-02 đếm DISTINCT đầu tài liệu, đếm bản riêng, không nhân số lượng do join MARC.
Không lọc ngày bao gồm đầu chưa có bản; lọc ngày áp dụng **ngày nhập bản sách**,
bao gồm hai đầu mốc, loại bản chưa biết ngày. Đây là báo cáo tồn hiện tại theo lô
nhập, không phải tái dựng tồn kho lịch sử tại cuối kỳ. Báo cáo nhóm theo DDC/loại,
có sẵn sàng/đang mượn/thanh lý/MARC đã kiểm còn khớp phiên. Chi tiết kiểm kê được
phân trang 100 dòng và xuất toàn bộ theo đợt. CSV UTF-8 có bộ lọc/thời điểm xuất,
quote dữ liệu và chặn công thức Excel trong ô văn bản.

TL-07 dùng [ZXing.Net 0.16.11](https://www.nuget.org/packages/ZXing.Net/0.16.11),
Code 128, 1–40 ký tự ASCII in được; mã không hỗ trợ báo lỗi trước khi in. Preview
tải lại bản sách từ server. Nhãn A4 hai cột gồm barcode/ĐKCB/ký hiệu xếp giá, phân
trang tự động. Chưa nghiệm thu máy in, tem/cuộn nhãn hay máy quét thật.

## Hợp đồng bàn giao

Tất cả lệnh dùng envelope TCP hiện có, `SessionToken` ở frame; server lấy actor
từ resolver rồi kiểm DB. DTO tại `server/Contracts/LibraryWorkContracts.cs` được
client liên kết build, không cần định nghĩa lại.

| Lệnh | Payload / trả về |
| --- | --- |
| TV2_MARC_GET | TitleQuery → MarcItem |
| TV2_MARC_SAVE | MarcItem (cả PhienBan và PhienBanHoSo) → MarcItem nháp |
| TV2_MARC_CHECK | MarcCheck (cả hai phiên) → MarcItem đã kiểm |
| TV2_BIBLIO_GET | TitleQuery → Bibliography; Thủ thư cũng được đọc chính xác theo ID cho LT-04 |
| TV2_MAJOR_LIST / TV2_READER_LIST | CatalogQuery → MajorItem[] / ReaderReference[] |
| TV2_PROJECT_GET / SAVE | TitleQuery / ProjectItem → ProjectItem |
| TV2_FILE_BEGIN | FileBegin → FileTicket (mã tệp và vị trí) |
| TV2_FILE_CHUNK | FileChunk (Base64, vị trí) → FileTicket |
| TV2_FILE_FINISH | FileTicket → ProjectItem được cập nhật |
| TV2_FILE_READ | FileRead (ID đề tài, mã tệp không có .pdf, vị trí) → FileData |
| TV2_STOCK_LIST | CatalogQuery → TableResult |
| TV2_STOCK_CREATE | StocktakeCreate → IdKiemKe, SoBanDuKien |
| TV2_STOCK_DETAIL | StocktakeQuery → TableResult (100 dòng/trang) |
| TV2_STOCK_ACTION | StocktakeAction → IdKiemKe, TrangThai |
| TV2_STOCK_SCAN | StocktakeScan → KetQua |
| TV2_DISPOSAL_LIST | CatalogQuery → TableResult |
| TV2_DISPOSAL_CREATE | DisposalCreate → IdThanhLy |
| TV2_DISPOSAL_DECIDE | DisposalDecision → IdThanhLy, TrangThai |
| TV2_REPORT | ReportFilter → TableResult |

Bibliography trả tên/tác giả/ISBN/NXB/năm xuất bản và trạng thái đối chiếu; TV3
không cần tự parse JSON. HT-02 của TV1 tiếp tục ghép vào `ICatalogSessionResolver`;
nhánh này không ghép/đẩy phần login của TV1 thay người phụ trách.

## Kiểm thử và giới hạn nghiệm thu

```powershell
dotnet restore ThuVienDAU.sln --locked-mode
dotnet build ThuVienDAU.sln -c Release --no-restore
dotnet run --project cong_cu/kiem_tra_tcp --configuration Release -- .
python cong_cu/kiem_tra_tv2/integration.py
dotnet run --project cong_cu/kiem_tra_tv2_client --configuration Release
```

Fixture MySQL/TCP tạo dữ liệu giả riêng; không đổi cấu hình/DB đang dùng. Log:
`kiem_tra/TV2_TL01_TL02_TL04.txt`, `TV2_Module_con_lai.txt`, `TV2_Client.txt`.
Kiểm client gồm roundtrip MARC, giải mã ảnh nhãn đã vẽ và dựng/render 7 form.
Ảnh QA ở `.tooling/tv2-client-review/`, Git bỏ qua. Render không được tính là đã
bấm thử đầy đủ các thao tác giao diện. Còn cần nhóm thử LAN, máy in/máy quét và
luồng người dùng thật sau khi ghép đăng nhập.
