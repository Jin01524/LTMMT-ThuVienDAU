# Triển khai TV2 đợt đầu

Ngày 22/11/2026. Nhánh codex/tv2-catalog dựa trên khung TCP ea87c4e.
Đợt đầu triển khai TL-01 và TL-02 để cung cấp dữ liệu nền cho các module phụ thuộc.
Chưa đánh dấu hoàn tất nghiệm thu cả 9 module TV2 hoặc hoàn tất HT-02.

## Những gì đã có

- TL-01: danh mục/cây DDC của trang hiện tại, tìm mã/tên, phân trang 100 mục, thêm/sửa;
  đối chiếu bản in tiếng Việt rút gọn có nguồn, tập/trang, căn cứ ghép số, người và thời điểm do server ghi.
  Các mục gợi ý khởi tạo vẫn chưa duyệt; không tự gán DDC từ ngành đào tạo.
- TL-02: tìm/thêm/sửa đầu tài liệu, loại tài liệu, phân loại chưa phân loại/dự kiến/đã xác nhận,
  mã và vị trí tra DDC, cờ hiển thị hồ sơ nội bộ. Không có OPAC cho độc giả.
  Tác giả, NXB, ISBN dành cho TL-03, ngành/tệp/thành viên đề tài dành cho TL-05.
- Ghi có giao dịch và nhật ký, truy vấn có tham số, chống ghi đè bằng revision DDC và PhienBan đầu tài liệu.
  Khi đổi phân loại đã xác nhận, lưu về dự kiến rồi xác nhận lại sau khi đối chiếu.
- Không thêm bảng/cột. Trigger hiện hành tiếp tục chặn vòng lặp DDC, mã chưa duyệt và dữ liệu không hợp lệ.

## Chạy thử

1. Build solution; chuẩn bị CSDL THỬ theo schema hiện hành và cấu hình server/appsettings.local.json.
   Không chạy lại script tạo schema trên dữ liệu đang dùng.
2. Cần tài khoản đã nhập/gán vai trò BIEN_MUC hoặc QUAN_LY_THU_VIEN và DangHoatDong=1.
   Code không tự tạo nhân viên hoặc cấp tài khoản/thẻ thư viện.
3. Server chế độ thử riêng:

       dotnet run --project server -- --host 127.0.0.1 --port 5080 --tv2-demo-account ID_TAI_KHOAN_THU

4. Server in token ngẫu nhiên một lần khi khởi động. Mở client:

       dotnet run --project client

5. Chọn menu TV2 · Biên mục, nhập token thử và mở TL-01 hoặc TL-02.
   Token chỉ tồn tại đến khi server dừng; không ghi vào cấu hình client hoặc log kiểm thử.

Chế độ thử không được bind LAN. Khi chạy bình thường, các lệnh TV2 bị từ chối cho tới khi
TV1 ghép HT-02 vào ICatalogSessionResolver. Không lấy ID người xử lý từ payload client.
Role/trạng thái tài khoản được kiểm tra lại ở DB trong từng giao dịch; token thử không thay thế đăng nhập thật.
Chỉ Biên mục được khóa/mở đăng nhập độc giả theo quy tắc trước đây; đợt TV2 này không thay đổi quyền đó.

## Hợp đồng bàn giao TV1

TcpRequest thêm Payload (JsonElement tùy chọn) và SessionToken (chuỗi tùy chọn), giữ tương thích PING/DATABASE_HEALTH.
Lệnh TV2_DDC_LIST, TV2_TITLE_LIST nhận CatalogQuery(Text, Offset); SAVE nhận DdcItem hoặc TitleItem.
Server kiểm phiên trước mọi truy vấn TV2; lỗi không được hiểu là dữ liệu rỗng.
TV1 cung cấp resolver ánh xạ phiên đã xác thực thành ID tài khoản, có hết hạn/thu hồi theo HT-02;
server đã để điểm đăng ký riêng. Khi chưa ghép HT-02, chỉ dùng chế độ thử loopback trên dữ liệu giả.
MainForm chỉ thêm một mục menu TV2, không thay nghiệp vụ TV1/TV3.

Code dùng chung hai module đặt trong Modules/TV2_VoNgocBinh/Shared:
client có View, Controller gọi TcpCommandClient; server có Controller, CatalogService và CatalogStore.
Hai form riêng đặt trong TL-01/Views và TL-02/Views; DTO liên kết từ server/Contracts/CatalogContracts.cs.

## Kiểm tra và giới hạn

Build Release không lỗi/cảnh báo. Bộ cong_cu/kiem_tra_tcp đạt 19 ca, gồm payload/phiên tùy chọn.
Bộ cong_cu/kiem_tra_tv2/integration.py chạy server và MySQL riêng với dữ liệu tổng hợp,
kiểm quyền, DDC, phân loại, sửa đồng thời và nhật ký; kết quả ghi ở kiem_tra/TV2_TL01_TL02.txt.
Form được dựng và kiểm tra bố cục bằng dữ liệu giả; chưa kiểm thao tác người dùng đầy đủ hoặc nhiều máy LAN.
Không tự cập nhật phần trăm hoàn tất trong Excel khi chưa được rà soát và chưa ghép HT-02.

## Thứ tự tiếp theo

| Module | Việc tiếp theo |
|---|---|
| TL-04 | Bản sách, ĐKCB/barcode duy nhất, dãy kệ/ngăn; bàn giao cho TV3 lưu thông |
| TL-03 | Editor MARC thân thiện, profile/chỉ thị/lặp/082; giao dịch đồng bộ hồ sơ thư mục |
| TL-05 | Ngành/khóa, thành viên đề tài, tệp đính kèm và quyền truy cập |
| TL-07 | In barcode, nhãn gáy, xem trước và kiểm thiết bị |
| KK-01 | Danh sách dự kiến, quét thực tế, đối chiếu và báo cáo |
| KK-02 | Đề nghị thanh lý; phải chốt người có quyền duyệt trước khi cho xử lý |
| BC-02 | Số đầu/bản sách, phân loại, tồn kho và kiểm kê; đối chiếu phép đếm |
