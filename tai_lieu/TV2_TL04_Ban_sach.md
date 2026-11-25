# TL-04 Bản sách và vị trí kho

Ngày 23/11/2026. TV2 Võ Ngọc Bình; người rà soát TV3 Trần Mỹ Nhung.
Nhánh feature/TV2-TL04-ban-sach nối tiếp feature/TV2-TL01-TL02 do phụ thuộc dữ liệu đầu tài liệu.

## Phạm vi bản đầu

- Thêm/sửa/tìm bản sách theo ĐKCB, barcode hoặc nhan đề; danh sách 100 dòng/trang.
- Chọn đầu tài liệu và vị trí từ hộp tìm kiếm, không nhập ID thủ công.
- Lưu ký hiệu xếp giá, giá bìa (số đồng nguyên, có thể chưa biết), ngày nhập (có thể trống),
  trạng thái đang xử lý/sẵn sàng/hư hỏng và tình trạng tốt/cũ/hư hỏng.
- Thêm/sửa/tìm dãy kệ/ngăn; không thêm tầng hoặc phòng. Cặp dãy kệ/ngăn, ĐKCB và barcode đều được kiểm tra trùng.
- Có kiểm quyền trên server, giao dịch, nhật ký trước/sau, chống ghi đè bằng revision của dữ liệu đã lưu.
- Không thêm bảng/cột; dùng BanTaiLieu, ViTriLuuTru, DauTaiLieu, ChiTietPhieuMuon và NhatKy hiện có.

## Chạy thử

Chuẩn bị như [bản triển khai TV2](TV2_Trien_khai.md): build solution, cấu hình MySQL thử,
chạy server loopback với --tv2-demo-account của tài khoản BIEN_MUC hoặc QUAN_LY_THU_VIEN đang hoạt động,
rồi nhập token ở menu TV2 · Biên mục.

1. Mở **TL-04 · Dãy kệ/ngăn**, thêm vị trí hoặc chỉnh tên vị trí hiện có.
2. Chuẩn bị đầu tài liệu ở TL-02.
3. Mở **TL-04 · Bản sách**, chọn Thêm mới, chọn đầu tài liệu, nhập ĐKCB và barcode.
4. Chọn vị trí, ký hiệu xếp giá, tình trạng, giá và ngày nhập. Sách chưa xếp có thể để vị trí trống khi đang xử lý.
5. Khi lưu Sẵn sàng, cần vị trí, ký hiệu xếp giá và sách không hư hỏng.
6. Lưu rồi tải lại để kiểm dữ liệu. Nếu bản ghi đã bị sửa ở máy khác, tải bản mới trước khi sửa tiếp.

Không sửa đầu tài liệu gắn với cuốn sách đã tạo. Không xóa vật lý bản sách/vị trí từ màn hình này.
TL-04 không cho sửa bản đang mượn, mất/thất lạc hoặc đã thanh lý; không thể dùng màn hình này để tự chuyển
sang trạng thái đang mượn, mất hay thanh lý. Nếu có khoản mượn mở, server chặn sửa kể cả khi trạng thái kho bị sai.
Sửa thông tin vị trí giữ nguyên ID/liên kết; không tự ghi rằng sách đã được di chuyển thực tế.

## Điểm bàn giao TV3

| Lệnh TCP | Payload | Quyền |
|---|---|---|
| TV2_LOCATION_LIST | CatalogQuery(Text, Offset) | Biên mục/Quản lý |
| TV2_LOCATION_SAVE | StorageLocationItem; thêm mới ID=0, sửa có Revision | Biên mục/Quản lý |
| TV2_COPY_LIST | CatalogQuery(Text, Offset) | Biên mục/Quản lý |
| TV2_COPY_SAVE | CopyItem; thêm mới ID=0, sửa có Revision | Biên mục/Quản lý |
| TV2_COPY_LOOKUP | BarcodeQuery(MaVach), tìm khớp toàn bộ barcode | Biên mục/Quản lý/Thủ thư |

Mọi lệnh cần phiên do server xác thực. Payload không được quyết định người thao tác.
Tên lệnh dùng envelope và frame hiện hành, không mở thêm giao thức riêng.

TV2_COPY_LOOKUP trả CopyLookup gồm BanSach (ID bản/đầu tài liệu, ĐKCB, barcode, giá, trạng thái,
ngày nhập, ký hiệu xếp giá, dãy kệ/ngăn) và DangCoKhoanMuonMo. Lệnh chỉ đọc, không đặt chỗ,
không đổi trạng thái và không tự xác nhận được mượn.

TV3 cần kiểm lại toàn bộ điều kiện mượn và khóa hàng BanTaiLieu bằng SELECT ... FOR UPDATE
trong cùng giao dịch ghi phiếu/chi tiết/trạng thái. Kết quả lookup đã trả qua TCP không giữ khóa
sang yêu cầu sau; không dùng nó thay kiểm tra tại thời điểm ghi mượn. Thống nhất thứ tự khóa
với luồng TV2 để tránh deadlock; lỗi deadlock/timeout phải rollback trước khi thử lại.

Revision bản sách chỉ dựa trên các cột của BanTaiLieu, không dựa trên nhan đề hoặc tên kệ được join.
Không truyền Revision tùy ý; dùng đúng giá trị server trả về. Khi thêm mới, các trường nhan đề/tên kệ
trong payload không được dùng làm dữ liệu nguồn: server tự lấy từ các bảng tham chiếu.

## Kiểm thử đã thực hiện

- Build Release: 0 lỗi, 0 cảnh báo.
- 19 ca TCP cũ vẫn đạt.
- 62 ca tích hợp TV2 (TL-01/TL-02/TL-04) đạt trên MySQL và server thử riêng, dữ liệu giả.
- Đã thử hai request sửa cùng revision đồng thời: chỉ một request được ghi.
- Đã dựng ảnh từ form WinForms thật với dữ liệu giả để rà bố cục, nhãn cột và dropdown trạng thái.
- Chưa nghiệm thu đầy đủ thao tác người dùng, máy quét/máy in hoặc nhiều máy LAN; HT-02/phiên thật còn chờ TV1.

Chạy lại:

    dotnet build ThuVienDAU.sln -c Release
    dotnet run --project cong_cu/kiem_tra_tcp -- .
    python cong_cu/kiem_tra_tv2/integration.py

Log: [TV2_TL01_TL02_TL04.txt](../kiem_tra/TV2_TL01_TL02_TL04.txt). Các log TL-01/TL-02 cũ được giữ làm mốc trước TL-04.

Tiếp theo của TV2 là TL-03 (biên mục MARC21); các module TL-05/TL-07/KK-01/KK-02/BC-02 vẫn đang chờ triển khai.
