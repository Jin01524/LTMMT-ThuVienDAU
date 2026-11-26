# Bàn giao TV3 — lưu thông và dịch vụ thư viện

TV3: **Trần Mỹ Nhung — 2351220273**. TV2 **Võ Ngọc Bình — 2351220274** hỗ trợ sửa và tích hợp. Commit nhập phần gốc giữ tác giả Nhung; commit sửa/tích hợp mang tác giả Bình. Nhánh làm việc `feature/TV3-tich-hop-TCP` xuất phát từ `main`; không cần cho phép merge hai lịch sử Git độc lập.

## Chạy và cấu trúc

Mở `ThuVienDAU.sln`, chạy server và client theo README chung. Đăng nhập bằng `THU_THU` hoặc `QUAN_LY_THU_VIEN`, mở **TV3 · Lưu thông**. `DOC_GIA` chỉ được gửi/xem phản hồi của chính mình. Chỉ `QUAN_LY_THU_VIEN` có màn hình hủy hiệu lực vi phạm.

- Client giữ giao diện của Nhung và gọi `TcpCommandClient`; không cài MySql.Data ở client, không nhập ID nhân viên/quản lý để xin quyền.
- DTO chung: `server/Contracts/CirculationContracts.cs`, được client liên kết khi build.
- Dispatcher server kiểm token phiên, khóa/đọc lại vai trò tài khoản đang hoạt động, sau đó xử lý trong giao dịch. ID người mượn, nhận trả, thu tiền, lập/hủy vi phạm lấy từ phiên.
- Dùng đúng schema 20 bảng trong `csdl/01_Tao_CSDL.sql`; không thêm bảng đặt trước, lịch nghỉ, cấp thẻ hoặc phân hệ nhân viên riêng.
- Tình trạng lúc trả, ghi chú, chìa khóa tủ và nội dung trả lời phản hồi lưu trong `NhatKy.ChiTiet`; đường dẫn JSON tuân theo camelCase của giao thức.

## Nội quy đã áp dụng

**Giờ mở cửa:** T2–T6 07:30–11:30, 13:30–17:00; T7 07:30–11:30; chiều T7 và Chủ nhật nghỉ. Dừng nhận mượn từ **11:15** mỗi buổi sáng và **16:45** buổi chiều T2–T6. Ngoài giờ, giờ nghỉ trưa và cuối tuần không mở cửa đều bị chặn ở server; không chỉ khóa nút client.

**Mượn:** sinh viên 2 nhan đề/10 ngày; cán bộ, giảng viên, trợ giảng, CNV 3 nhan đề/15 ngày; không mượn hai bản cùng nhan đề. Hạn trả là cuối ngày đến hạn. Khóa hồ sơ/quyền mượn, sách chưa sẵn sàng, nợ, sách đang quá hạn hoặc vi phạm trả trễ/mất/hỏng chưa lập đều bị kiểm tra trước khi mượn tiếp. Khóa độc giả và bản sách trong giao dịch để tránh hai máy cùng mượn một cuốn.

**Trả:** hỗ trợ trả một phần hoặc nhiều cuốn; một yêu cầu được ghi nhận toàn bộ hoặc hoàn tác. Ghi người nhận trả từ phiên. Sách hỏng có khoản mượn `DA_TRA` và bản sách `HU_HONG`; sách mất có trạng thái `MAT`, không gán giá trị `MAT` vào cột tình trạng vật lý chỉ cho phép TOT/CU/HU_HONG. Phiếu mượn đóng khi không còn khoản đang mượn.

**Gia hạn:** chỉ tại quầy. Thủ thư xác nhận không có bạn đọc khác đăng ký mượn, chọn hạn mới và ghi căn cứ. Server kiểm quyền mượn, hạn mức, nợ/quá hạn và hạn mới. Không tạo bảng đặt trước, không có cổng gia hạn trực tuyến, không tự áp số lần hoặc số ngày gia hạn cố định khi nội quy chưa quy định. Hạn mới và người thao tác được ghi nhật ký.

**Vi phạm:** server tính lại từ dữ liệu khoản mượn, giá bìa và lịch miễn phạt; không tin số ngày/số tiền gợi ý từ client. Có thể lập riêng quá hạn và mất/hỏng cho cùng một khoản mượn. Một loại đã lập/hủy không tự xuất hiện lại để thu trùng.

- SV quá hạn: 2.000đ/ngày/cuốn, loại ngày lễ/Tết đã xác nhận. Khi `DaXacNhanLichMienPhat=false`, không chốt phạt quá hạn SV và không cho checkbox client bỏ qua.
- CB/GV/trợ giảng/CNV quá hạn: lập thông báo đơn vị, số tiền 0; xem/in thông báo tại VP-01 hoặc in lại từ LT-04. Hệ thống chưa tự gửi email/thông báo tới đơn vị.
- Mất sách, đền tiền: **2 × giá bìa**. Không có giá: Quản lý xác định giá sách tương đương; không mặc định 100.000đ.
- Đền sách: xác nhận đã nhận sách thay thế, thu thêm **20.000đ** phí kỹ thuật.
- Hỏng trang: Quản lý xác định chi phí tái tạo/thay thế trang, không tự dùng tỷ lệ phần trăm giá bìa.

**Thu/hủy:** một phiếu thu chỉ gồm các khoản chưa thu của cùng độc giả; chống chọn trùng, thu hai lần và thu một phần một khoản. Hủy vi phạm giữ biên lai đã thu, nhật ký và lý do; không tự hoàn tiền. Thu tiền hoặc hủy vi phạm **không thay đổi `DuocMuon`/`KhoaDen`**. Quản lý mở quyền mượn bằng chức năng DG-03 sau khi kiểm tra đủ điều kiện; việc khóa đăng nhập vẫn chỉ thuộc Biên mục.

**Phòng đọc:** dùng thẻ trường, xác nhận giữ/trả thẻ và giao/nhận chìa khóa. Chặn giao trùng chìa khóa cho hai lượt đang mở; đối chiếu chìa khóa khi trả. Cho phép ghi nhận ra sau giờ đóng cửa hoặc khi hồ sơ đã ngừng hoạt động để không kẹt lượt đang mở. Bạn đọc tự lấy tối đa 2 cuốn/lần và tự xếp lại; hệ thống không tạo phiếu mượn cho sách đọc tại chỗ.

**Phản hồi:** giữ trạng thái MOI/DA_XU_LY và loại GOP_Y/DANH_GIA/DE_XUAT đúng schema. Đánh giá cần đầu tài liệu và điểm 1–5. Độc giả được xác định từ tài khoản, không được mạo danh hoặc xem phản hồi người khác. Xử lý lưu nội dung và người trả lời trong nhật ký, chặn ghi đè phiếu đã xử lý.

**Thống kê/in:** tính độc lập số lượt, khoản phạt và tiền thực thu; không nhân đôi tiền khi một biên lai gồm nhiều vi phạm. Hủy vi phạm không làm mất số thực thu đã ghi nhận. LT-04 xem lịch sử và xem/in lại phiếu mượn, biên lai, thông báo. Dữ liệu thư mục trên phiếu dùng chung `MarcService.Summary` của TV2. Có xem nội dung không cần máy in; nút xem trước/in dùng PrintDocument, có phân trang. Hỏng kết nối/máy in sau khi lưu không được coi là lưu thất bại để lập lại phiếu.

## Hợp đồng TCP

Tất cả lệnh dùng khung TCP chung: header độ dài 4 byte big endian, JSON UTF-8, token ở `sessionToken`. DTO và chữ ký cụ thể nằm trong `CirculationContracts.cs`.

| Nhóm | Lệnh |
|---|---|
| LT-01 | TV3_READER_LOOKUP, TV3_COPY_LOOKUP, TV3_BORROW |
| LT-02 | TV3_RETURN_LOOKUP, TV3_RETURN |
| LT-03 | TV3_RENEW_LIST, TV3_RENEW |
| LT-04 | TV3_HISTORY, TV3_RECEIPT |
| VP-01 | TV3_VIOLATION_CANDIDATES, TV3_VIOLATION_CREATE |
| VP-02 | TV3_UNPAID, TV3_COLLECT |
| VP-03 | TV3_VIOLATION_LIST, TV3_VIOLATION_VOID |
| PD-01 | TV3_VISITS, TV3_VISIT_SCAN |
| PH-01 | TV3_FEEDBACK_SUBMIT, TV3_FEEDBACK_LIST, TV3_FEEDBACK_PROCESS |
| BC-03 | TV3_STATISTICS |

Danh sách tra cứu trả tối đa 100 dòng; dùng mã trường/barcode để lọc chính xác khi dữ liệu nhiều. Báo cáo BC-03 cho khoảng tối đa 367 ngày.

## Cấu hình và kiểm thử

Giờ mở cửa lưu trong `ThoiGianMoCua` ở `csdl/Cau_hinh_du_an.json`, nguồn sinh là `cong_cu/csdl/cau_hinh_du_an.mjs`. Không cần migration CSDL. Cập nhật source rồi build/copy cấu hình đi kèm server.

Ngày lễ/Tết còn để trống và **chưa xác nhận**. Thư viện cần bổ sung các ngày thực tế dạng yyyy-MM-dd, kiểm tra đầy đủ rồi đặt `DaXacNhanLichMienPhat=true` trên cấu hình server trước khi chốt phạt theo ngày. Bộ thử dùng lịch riêng, không sửa cấu hình hoặc dữ liệu thật.

```powershell
dotnet restore ThuVienDAU.sln --locked-mode
dotnet build ThuVienDAU.sln -c Release --no-restore
dotnet run --project cong_cu/kiem_tra_tcp -- .
dotnet build cong_cu/kiem_tra_tv3/FixtureHost -c Release
python cong_cu/kiem_tra_tv3/integration.py
dotnet run --project cong_cu/kiem_tra_tv3_client -c Release
```

FixtureHost là chương trình thử riêng, dùng MySQL trong thư mục `.tooling` và cổng loopback ngẫu nhiên; lệnh đổi đồng hồ/lịch thử không có trong server sản phẩm. Python tạo mật khẩu ngẫu nhiên, đăng nhập bằng AUTH thật và luôn dừng tiến trình trong finally.

Giới hạn xác minh: kiểm thử tự động nghiệp vụ TCP/MySQL, dựng control và render giao diện. Chưa kiểm thử thao tác đầy đủ trên hai máy LAN hoặc in giấy qua máy in vật lý. Ảnh form không thay thế kiểm thử các thao tác người dùng.
