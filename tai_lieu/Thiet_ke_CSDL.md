# Thiết kế CSDL thư viện DAU — phạm vi báo cáo học tập

Cập nhật tổ chức mã nguồn ngày **21/11/2026** theo
[kiến trúc server, client và MVC](Kien_truc_Server_Client_MVC.md).
Giao diện MVC ở client gọi HTTP/JSON; server thực hiện xử lý và truy cập MySQL.
Phân công mô đun áp dụng cho cả hai phía.

Cập nhật ngày **20/11/2026**. CSDL gồm **20 bảng, 160 cột, 45 khóa ngoại, 10 view, 15 trigger và 10 procedure**. Phiên bản tối thiểu là MySQL **8.0.21**, mốc `JSON_TABLE` đọc giá trị JSON null thành SQL `NULL`. [Tài liệu MySQL](https://dev.mysql.com/doc/refman/8.0/en/json-table-functions.html)

## 1. Cách chạy

Mở [01_Tao_CSDL.sql](../csdl/01_Tao_CSDL.sql) trong MySQL Workbench và chạy toàn bộ trên server đã kết nối. Schema tạo mới là **ThuVienDAU**; chỉ chạy khi schema chưa tồn tại. Script không DROP và không chuyển dữ liệu từ bản cũ. Nếu lỗi, dừng kiểm tra; DDL MySQL có implicit commit.

Bản trước mỗi lần chỉnh sửa đã được sao lưu và kiểm tra SHA-256. Đường dẫn ZIP ghi ở [README](../README.md). Lần này sửa file dự án và thử trên server riêng, chưa đổi DB thật. Các script nâng cấp cũ không áp dụng cho bản này.

## 2. Các quyết định thu gọn

| Vấn đề | Thiết kế hiện hành |
|---|---|
| Đặt trước | Bỏ toàn bộ bảng, trạng thái giữ chỗ, quyền và luồng đặt trước |
| Kiểm kê | Chỉ KiemKe và ChiTietKiemKe, không bảng phân công riêng |
| Kho một tầng, một phòng | ViTriLuuTru chỉ IdViTri, DayKe, Ngan; duy nhất theo dãy kệ/ngăn |
| Chính sách độc giả | Bỏ bảng; DocGia.LoaiDocGia phân biệt đối tượng, quy định cố định trong file cấu hình |
| Vai trò/tài khoản | Một TaiKhoan, cột VaiTro; một tài khoản một vai trò |
| Ngày nghỉ | Bỏ bảng; danh sách ngày lễ/Tết miễn phạt lưu cấu hình |
| Cấu hình hệ thống | File JSON thay bảng CauHinh |
| Nhật ký nhập danh sách | Dùng NhatKy chung thay DotNhapDocGia |
| Tác giả, nhà xuất bản | Lưu trực tiếp trong các trường MARC, bỏ ba bảng danh mục/nối và cột NXB trên đầu tài liệu |
| Đồ án/luận văn | MaNganh, KhoaHoc nằm trên DauTaiLieu; ThanhVienDeTai liên kết sinh viên/giảng viên |
| Tệp đính kèm | Gộp `TepTaiLieu` vào `DauTaiLieu.DanhSachTep`. Mỗi phần tử lưu tên tệp và đường dẫn |
| Biểu ghi MARC | Gộp `TruongMARC` và `TruongConMARC` vào `BieuGhiMARC.Truong`. Hai cấp mảng giữ thứ tự và trường lặp |
| Gợi ý ngành–DDC | Chuyển thành tài liệu JSON tham khảo, không bảng nghiệp vụ |

So với bản 23 bảng, mô hình mới bớt **3 bảng, 14 cột và 3 khóa ngoại**. Tệp đính kèm thuộc về một đầu tài liệu. Trường và trường con thuộc về một biểu ghi MARC. Lưu các thành phần này trong hồ sơ cha giúp đọc và cập nhật chúng cùng nhau.

### Tài khoản

TaiKhoan có VaiTro với năm giá trị cố định:

| Giá trị | Chức năng |
|---|---|
| QUAN_TRI | Cấu hình, gán vai trò cho tài khoản nhập từ danh sách trường |
| QUAN_LY_THU_VIEN | Nghiệp vụ thư viện; riêng vai trò này được khóa/mở mượn và hủy vi phạm |
| BIEN_MUC | Biên mục tài liệu; độc quyền khóa/mở tài khoản đăng nhập độc giả |
| THU_THU | mượn/trả, thu tiền, phòng đọc, kiểm kê |
| DOC_GIA | phản hồi và xem thông tin của mình |

Vai trò BIEN_MUC tách riêng khỏi THU_THU. Hồ sơ cán bộ/giảng viên vẫn nhập từ Excel, không thêm phân hệ quản lý nhân viên riêng. IdDocGia liên kết tài khoản với hồ sơ trường; chưa tạo tài khoản thật, chưa chốt cách cấp mật khẩu hay SSO.

### Kiểm kê

- KiemKe: thông tin chung của một lần kiểm kê — tên, thời gian bắt đầu/kết thúc, người lập, trạng thái.
- ChiTietKiemKe: từng bản cần kiểm, vị trí dự kiến/thực tế, kết quả, người kiểm và thời điểm. Cột IdNguoiKiemKe có thể ghi người phụ trách dòng đó; không có hệ thống phân công theo khu vực riêng.
- Khi lập kiểm kê, tạo trước các dòng dự kiến. Kết quả quét bổ sung vào chính các dòng này; sách ngoài danh sách có thể thêm bằng mã vạch.
- vw_KetQuaKiemKe tổng hợp số dự kiến, thiếu và ngoài danh sách. Báo cáo là dữ liệu truy vấn, không thêm một bảng báo cáo.
- ThanhLy vẫn tách vì là quyết định xử lý tài liệu, không phải mỗi lần kiểm kê đều thanh lý.

### Chính sách và ngày lễ/Tết

Bảng ChinhSachDocGia trước đây chứa loại đối tượng, số đầu sách tối đa, số ngày mượn và tiền phạt/ngày. Với đồ án này các giá trị cố định nên chuyển sang [Cau_hinh_du_an.json](../csdl/Cau_hinh_du_an.json):

- Sinh viên: tối đa **2 đầu sách / 10 ngày**, quá hạn **2.000 đồng/ngày/cuốn**, trừ lễ/Tết.
- Cán bộ/giảng viên/trợ giảng/CNV: **3 đầu sách / 15 ngày**, quá hạn thông báo đơn vị; không tự áp phạt tiền.
- Ngừng cho mượn 15 phút trước đóng cửa; phòng đọc 2 cuốn/lần.
- Đền tiền 2 lần giá bìa/giá tương đương; đền sách thêm 20.000 đồng phí kỹ thuật; đình chỉ sinh viên 7–28 ngày theo quyết định.
- NgayLeTetMienPhat chứa các ngày cụ thể dạng YYYY-MM-DD. Hiện danh sách rỗng và DaXacNhanLichMienPhat=false vì chưa có lịch thật, **không được hiểu là không có ngày miễn phạt**. Trong bản demo có thể nhập số ngày phạt đã loại lễ/Tết và lưu căn cứ tại ViPham.CanCu; nếu tự tính thì phải bổ sung lịch đúng trước.

Nguồn cấu hình ở cong_cu/csdl/cau_hinh_du_an.mjs. Hạn mức được generator đưa vào procedure; nếu sửa hạn mức phải sinh lại SQL và đồng bộ procedure ở DB thử. Thời hạn, tiền phạt, ngày miễn phạt trong file là đầu vào cho code tương lai, không phải cơ chế DB tự đọc JSON ngoài ổ đĩa.

Bảng Khoa gồm IdKhoa, MaKhoa, TenKhoa. NganhDaoTao.IdKhoa và DocGia.IdKhoa tham chiếu Khoa; không lưu phòng/ban trong bảng này. IdKhoa của độc giả có thể để NULL khi chưa có khoa phù hợp.

## 3. Danh mục 20 bảng

| Nhóm | Bảng |
|---|---|
| Tổ chức, hồ sơ, tài khoản (4) | Khoa, NganhDaoTao, DocGia, TaiKhoan |
| Phân loại, thư mục, kho (5) | MucPhanLoaiDDC, DauTaiLieu, ThanhVienDeTai, ViTriLuuTru, BanTaiLieu |
| Mượn/trả, phòng đọc (3) | PhieuMuon, ChiTietPhieuMuon, LuotVaoPhongDoc |
| Vi phạm, thu tiền (2) | ViPham, PhieuThu |
| Kiểm kê, thanh lý (3) | KiemKe, ChiTietKiemKe, ThanhLy |
| Phản hồi, lịch sử (2) | PhanHoiDocGia, NhatKy |
| MARC21 (1) | BieuGhiMARC |

Đầu tài liệu và bản sách nằm ở hai bảng vì mỗi nhan đề có thể có nhiều bản với ĐKCB, mã vạch, vị trí và lịch sử mượn riêng. Một phiếu mượn, một phiếu thu và một đợt kiểm kê cũng có thể liên kết với nhiều dòng chi tiết. [Từ điển CSDL](Tu_dien_CSDL.md)

`DauTaiLieu.DanhSachTep` lưu mảng JSON. Mỗi tệp gồm `TenTep` để hiển thị và `DuongDan` để tìm tệp. Một đầu tài liệu có thể lưu nhiều tệp, chẳng hạn bản luận văn và bản vẽ kèm theo. Ràng buộc `JSON_SCHEMA_VALID` kiểm tra từng phần tử có đúng hai thuộc tính dạng chuỗi.

## 4. Nghiệp vụ và trách nhiệm của code

Không cấp thẻ thư viện; định danh bằng thẻ trường. Không xóa độc giả. Trigger chặn khóa/mở mượn và hủy vi phạm nếu ID người xử lý không phải quản lý đang hoạt động; ghi nhật ký. Hủy phiếu phạt là hủy hiệu lực và giữ lý do, không xóa chứng từ thu.

KiemTraDuocMuon kiểm tra hồ sơ, quyền mượn, quá hạn, khoản tiền chưa xử lý và hạn mức 2/3. Khóa duy nhất chặn một bản đang cho hai người mượn và một người đang mượn hai bản cùng nhan đề. Procedure phải được code gọi trước khi ghi phiếu, trong giao dịch có khóa hàng; INSERT trực tiếp không tự gọi kiểm tra hạn mức.

GiaHanMuon kiểm tra khoản đang mượn, hạn mới lớn hơn hạn cũ, quyền người xử lý và hạn mức, rồi ghi lịch sử. Không có cơ chế giữ chỗ/hàng đợi. Quy định thư viện có điều kiện “không có người khác đăng ký mượn”; do dự án không xây dựng đăng ký trước, thủ thư kiểm tra thực tế tại quầy khi gia hạn. Chưa chốt số ngày/số lần gia hạn tối đa.

ThuTienPhat thu đủ các khoản tiền còn chưa xử lý của một độc giả, không trả góp hoặc hoàn tiền. Hủy vi phạm đã thu giữ nguyên số tiền thực nhận của PhieuThu. Tiền bồi thường và thông báo đơn vị cần cán bộ xác định/lưu căn cứ đúng quy định.

Lập mượn, nhận trả và đồng bộ tình trạng bản sách cần giao dịch. ThuTienPhat/GiaHanMuon yêu cầu bên gọi mở giao dịch và rollback khi lỗi. Thời gian dùng +07:00 trên mọi kết nối. Chưa kiểm thử tải đồng thời.

Id người xử lý không thay xác thực: code phải lấy danh tính từ phiên đăng nhập, kiểm tra vai trò và hạn chế ghi trực tiếp DB. Các thao tác đổi quyền, đổi phân loại, gia hạn, thu tiền, hủy vi phạm có nhật ký tự động; nhập Excel và thao tác khác cần code ghi thêm vào NhatKy. Số ngày phạt, lịch miễn phạt, giờ đóng cửa, thông báo và giao diện chưa được lập trình.

## 5. DDC23 và 18 ngành

Theo xác nhận người dùng, trường dùng **DDC23 bản in tiếng Việt rút gọn**. Cấu hình DDC23BanIn.PhamVi=RUT_GON và ChiThi1MARC082=1. Chưa có năm in, ISBN và số ấn bản ghi trên trang tên sách; các thông tin này vẫn null.

Giữ đúng **9 khoa / 18 ngành** do người dùng cung cấp. Mã KHOA_* là mã nội bộ. MucPhanLoaiDDC có **40 mục tham khảo**, tất cả DaDoiChieu=0. **49 liên hệ ngành–DDC** chuyển sang [Dinh_huong_DDC_nganh.json](Dinh_huong_DDC_nganh.json), chưa duyệt; không tự gán mã theo ngành.

| Khoa | Ngành / mã ngành | Các vùng tra cứu DDC đề xuất |
|---|---|---|
| Kiến trúc | Kiến trúc — 7580101 | 720, 624, 690 |
| Kiến trúc | Quy hoạch vùng và đô thị — 7580105 | 711, 710, 388 |
| Kiến trúc | Thiết kế nội thất — 7580108 | 747, 720, 740 |
| Thiết kế & Nghệ thuật | Thiết kế đồ họa — 7210403 | 740, 741, 006 |
| Xây dựng | Kỹ thuật xây dựng — 7580201 | 624, 690, 620 |
| Xây dựng | Quản lý xây dựng — 7580302 | 690, 658, 624 |
| Cầu đường | Kỹ thuật xây dựng công trình giao thông — 7580205 | 625, 624, 388 |
| Cầu đường | Kỹ thuật cơ sở hạ tầng — 7580210 | 624, 627, 628, 625 |
| Công nghệ Thông tin | Công nghệ thông tin — 7480201 | 004, 005, 006 |
| Kỹ thuật Điện - Điện tử | Công nghệ kỹ thuật Điện – Điện tử — 7510301 | 621, 620, 004 |
| Kinh tế | Quản trị kinh doanh — 7340101 | 658, 330 |
| Kinh tế | Tài chính – Ngân hàng — 7340201 | 332, 330 |
| Kinh tế | Kế toán — 7340301 | 657, 332 |
| Kinh tế | Logistics và Quản lý chuỗi cung ứng — 7510605 | 658, 388, 380 |
| Ngoại ngữ | Ngôn ngữ Anh — 7220201 | 420, 400 |
| Ngoại ngữ | Ngôn ngữ Trung Quốc — 7220204 | 495, 490 |
| Du lịch | Quản trị dịch vụ du lịch và lữ hành — 7810103 | 910, 658 |
| Du lịch | Quản trị khách sạn — 7810201 | 647, 658, 641 |

MucPhanLoaiDDC lưu mã dạng chuỗi, cha–con, nguồn, vị trí tra, người/thời điểm đối chiếu và căn cứ ghép. DauTaiLieu lưu mã, ấn bản 23, trạng thái dự kiến/xác nhận và căn cứ bản in. Xác nhận đầu tài liệu phải dùng mục đã đối chiếu; đổi mã đã xác nhận cần xác nhận lại hoặc về dự kiến.

Định dạng và khóa ngoại không chứng minh mã đúng nội dung sách. Cán bộ phải tra sách thật; tiêu đề tiếng Việt khởi tạo là diễn giải dự án. DDC, ký hiệu xếp giá và vị trí ngăn kệ là dữ liệu riêng, không thay thế nhau.

Nguồn nghiên cứu kế thừa: [OCLC DDC23 Summaries](https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf), [OCLC Introduction](https://www.oclc.org/content/dam/oclc/dewey/versions/print/intro.pdf), [LOC Dewey FAQ](https://www.loc.gov/aba/dewey/faq.html). Lần thu gọn không xác nhận thêm mã từ bộ bản in thực tế.

## 6. Lưu biểu ghi MARC21

Mỗi dòng `BieuGhiMARC` lưu Leader, trạng thái, phiên bản và mảng JSON `Truong`. Mỗi trường có `NhanTruong`. Trường điều khiển lưu `NoiDungDieuKhien`. Trường dữ liệu lưu hai chỉ thị và mảng `TruongCon`. Mỗi trường con có `MaTruongCon` và `NoiDung`. Thứ tự trong mảng giữ nguyên trình tự và cho phép nhãn trường xuất hiện nhiều lần. Một đầu tài liệu có tối đa một biểu ghi hiện hành. Biểu ghi nháp có thể tạo trước khi gắn với đầu tài liệu.

Đây là một phần của mảng `Truong` khi biên mục sách *Cơ sở dữ liệu* có phụ đề *Từ mô hình đến SQL*.

```json
[
  {
    "NhanTruong": "245",
    "ChiThi1": "0",
    "ChiThi2": "0",
    "TruongCon": [
      { "MaTruongCon": "a", "NoiDung": "Cơ sở dữ liệu" },
      { "MaTruongCon": "b", "NoiDung": "Từ mô hình đến SQL" }
    ]
  }
]
```

`vw_NoiDungMARC` đọc mảng thành từng dòng trường và trường con để tra cứu. `vw_ISBNTaiLieu` trả các ISBN của từng đầu tài liệu từ trường `020$a`. Vị trí `ThuTuTruong` và `ThuTuTruongCon` trong view được tính từ thứ tự mảng.

Khi ứng dụng gửi lại cả mảng đã đọc, điều kiện `WHERE PhienBan = ...` trên bản ghi cha giúp phát hiện chỉnh sửa đồng thời. Khi câu cập nhật trả về 0 dòng, ứng dụng đọc lại bản hiện hành để xử lý xung đột.

| Thông tin | Nơi lưu |
|---|---|
| Nhan đề | 245$a; DauTaiLieu.NhanDe là bản chiếu phục vụ nghiệp vụ |
| Phụ đề/trách nhiệm | 245$b / $c |
| Tác giả chính/bổ sung | 100/110/111 và 700/710/711 tùy loại |
| Nhà xuất bản/nơi/ngày xuất bản | 264$b / $a / $c; giữ 260 nếu nguồn dùng 260 |
| ISBN và phân biệt tập/bìa | Các trường 020$a / $q; vw_ISBNTaiLieu trả nhiều dòng |
| Lần xuất bản | 250 |
| Số trang/mô tả vật lý | 300$a |
| Tóm tắt | 520$a |
| DDC và ấn bản | 082$a / $2 |

Không còn bảng tác giả/NXB riêng: tìm theo tác giả/NXB phải truy vấn các trường MARC. Đây là lựa chọn giảm quy mô cho báo cáo; chưa có quản lý authority hoặc danh mục tác giả độc lập.

Hồ sơ cục bộ yêu cầu 001, 005, 008, 245; kiểm tra một phần Leader, độ dài điều khiển, tính lặp của danh mục chọn lọc, một tiêu đề chính, một số chỉ thị và 082 khớp DDC23 đã xác nhận. Danh mục **27 nhãn / 22 trường con** tại [Cau_hinh_MARC21.json](../csdl/Cau_hinh_MARC21.json); nhãn chưa khai báo vẫn lưu được, cần đối chiếu thủ công.

DA_KIEM_TRA chỉ nghĩa qua hồ sơ cấu trúc cục bộ. Sửa trường/trường con/Leader hủy dấu kiểm tra; vw_TinhTrangMARC báo đầu tài liệu đổi phiên bản. Nhan đề quan hệ và MARC không tự đồng bộ: code cần ánh xạ và lưu trong cùng giao dịch.

Nguồn chuẩn: [LOC MARC Bibliographic](https://www.loc.gov/marc/bibliographic/), [Leader](https://www.loc.gov/marc/bibliographic/bdleader.html), [020](https://www.loc.gov/marc/bibliographic/bd020.html), [245](https://www.loc.gov/marc/bibliographic/bd245.html), [264](https://www.loc.gov/marc/bibliographic/bd264.html), [082](https://www.loc.gov/marc/bibliographic/bd082.html). Chưa có parser/export MARCXML/ISO2709, kiểm định mọi mã 008 hoặc xác nhận đầy đủ MARC21/AACR2/RDA. Biểu ghi gắn đầu tài liệu đã xác nhận phải có 082 với chỉ thị đầu 1 (nguồn rút gọn). Chỉ thị thứ hai 4 dùng khi thư viện tự gán số; giữ đúng nguồn nếu chép biểu ghi ngoài. [LOC 082](https://www.loc.gov/marc/bibliographic/bd082.html) quy định 1 là rút gọn, còn $2 ghi thông tin ấn bản.

Giữ nhãn DDC23 theo thông tin dự án cung cấp; chưa tự điền số ấn bản trên trang tên sách. LOC phân biệt [Abridged Edition 15 và DDC23](https://www.loc.gov/aba/dewey/segmentation.html), nên không suy rằng bản rút gọn chính thức mang số 23 hoặc tự đổi DB thành 15. Cần đối chiếu trang tên sách trước khi xuất biểu ghi trao đổi thật. Kiểm tra $2 trong DB hiện vẫn theo hồ sơ đồ án 23/23/...; đây chưa phải xác nhận ấn bản chuẩn của bộ sách.

## 7. Duy trì và kiểm tra

Chỉnh nguồn trong cong_cu/csdl, chạy **node cong_cu/csdl/tao_sql.mjs** để sinh SQL, từ điển và ba file JSON cấu hình/tham khảo. Bộ kiem_tra_gon.sql chỉ dùng trên server thử độc lập, dữ liệu tổng hợp được rollback.

Bộ kiểm tra MySQL **8.0.46** bao quát tạo mới 20 bảng, quyền quản lý, hạn mức mượn, gia hạn, thanh toán, kiểm kê, DDC và lưu MARC. [Kết quả kiểm tra và SHA-256](../kiem_tra/Ket_qua_MySQL80.txt)

Chưa thử giao diện, import Excel/Portal, tính phạt bằng lịch thật, file MARC thật, migration dữ liệu cũ hay đồng thời nhiều máy. Hai PPTX cũ đã xóa khỏi bộ hiện hành sau khi sao lưu vì không còn đúng phạm vi. Không dùng FigJam cũ làm căn cứ DB mới. Tài liệu Markdown/SQL và [ảnh ERD](../so_do/So_do_CSDL.png) là bộ hiện hành.

## 8. Sơ đồ CSDL và tái tạo ảnh

Sơ đồ thể hiện 20 bảng, 160 cột và 45 khóa ngoại. Bản PNG dùng để chèn báo cáo, bản SVG dùng để phóng to hoặc chỉnh sửa vector. [PNG](../so_do/So_do_CSDL.png) · [SVG](../so_do/So_do_CSDL.svg) · [Slide thuyết trình](../slides/Database_Thu_vien_DAU.pptx)

Nguồn vẽ: cong_cu/csdl/ve_so_do.mjs, dùng cùng cau_truc.mjs với generator SQL. Chạy node cong_cu/csdl/ve_so_do.mjs với thư viện @viz-js/viz và sharp; có thể đặt ERD_NODE_MODULES tới node_modules của runtime. Cần sinh lại ảnh khi cấu trúc thay đổi.

## Quyền khóa tài khoản và quyền mượn

- **Biên mục (BIEN_MUC):** duy nhất được khóa/mở tài khoản đăng nhập của độc giả (`TaiKhoan.DangHoatDong`), chỉ áp dụng tài khoản có vai trò `DOC_GIA` và liên kết hồ sơ độc giả. Bắt buộc lý do và nhật ký.
- **Quản lý thư viện (QUAN_LY_THU_VIEN):** khóa/mở quyền mượn (`DocGia.DuocMuon`, `KhoaDen`, `LyDoKhoa`) và hủy hiệu lực phiếu phạt.
- Quản trị, Thủ thư và Độc giả không có quyền khóa/mở đăng nhập độc giả. Mở đăng nhập không mở quyền mượn; mở quyền mượn không mở đăng nhập. Tại quầy vẫn kiểm quyền mượn riêng.
- HT-02 do TV1 triển khai màn hình và kiểm quyền khóa/mở đăng nhập; người sử dụng nghiệp vụ là Biên mục. Vai trò cố định gồm QUAN_TRI, QUAN_LY_THU_VIEN, BIEN_MUC, THU_THU, DOC_GIA.

`DoiTrangThaiDangNhapDocGia` được gọi trong giao dịch của Service (BEGIN → CALL → COMMIT; lỗi thì ROLLBACK). ID người thao tác lấy từ phiên đã xác thực. Tài khoản MySQL dùng chung không thay thế vai trò nghiệp vụ; cần giới hạn cập nhật trực tiếp cột DangHoatDong và kiểm quyền ở Service. Truy vấn đăng nhập phải kiểm DangHoatDong=1; trước thao tác bảo vệ cần kiểm lại trạng thái phiên để tài khoản vừa khóa không tiếp tục sử dụng phiên cũ. Quy tắc này là hợp đồng triển khai HT-02; khung WinForms hiện chưa có đăng nhập nghiệp vụ.
