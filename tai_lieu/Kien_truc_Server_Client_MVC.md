# Kiến trúc server client và MVC

Ngày cập nhật **21/11/2026**. Bài tập yêu cầu hai thư mục server và client
trên GitHub, trong đó các mô đun thể hiện người đảm nhiệm.
Cấu trúc này nối tiếp bản MVC tại commit 21856d8.

## Kiến trúc thực thi

Client Windows Forms .NET 10 gọi server ASP.NET Core .NET 10 bằng HTTP/JSON.
Server thực hiện truy cập MySQL. HTTP/JSON được chọn để triển khai khung mạng,
yêu cầu giảng viên cung cấp chưa chỉ định giao thức truyền.

View WinForms phát sự kiện tới Controller client. Dịch vụ HTTP nhận model JSON
từ Controller API trên server. Service và Data server thực hiện đọc dữ liệu.
Program.cs ở mỗi phía là điểm khởi động riêng.
Các DTO dùng chung đặt tại server/Contracts và liên kết vào client khi build.

| Vị trí | Trách nhiệm |
|---|---|
| client/Modules/TV1_VoNgocBinh/HT-01/Views | MainForm, Designer và IMainView |
| client/Modules/TV1_VoNgocBinh/HT-01/Controllers | MainController |
| client/Modules/TV1_VoNgocBinh/HT-01/Models | Ngoại lệ kiểm tra và DTO Compile Link |
| client/Modules/TV1_VoNgocBinh/HT-01/Services | DatabaseHealthService gọi API |
| server/Modules/TV1_VoNgocBinh/HT-01/Controllers | DatabaseHealthController |
| server/Modules/TV1_VoNgocBinh/HT-01/Services | DatabaseHealthService truy vấn MySQL |
| server/Data | MySqlConnectionFactory |
| server/Configuration | DatabaseOptions |
| server/Contracts | Dữ liệu thành công và lỗi qua mạng |

Cấu hình client giữ Server.BaseUrl. Cấu hình server giữ MySql.
Chạy server rồi client. Trên LAN, đặt URL server thực tế ở client;
--urls của server xác định giao diện mạng lắng nghe.

## Phân công đủ 29 mô đun

Mã mô đun, chủ trì và phạm vi theo kế hoạch nhóm và bảng Excel hiện hành.

| Mã | Mô đun | Người đảm nhiệm | Vị trí |
|---|---|---|---|
| HT-01 | Khung ứng dụng và kết nối MySQL | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/HT-01) · [server](../server/Modules/TV1_VoNgocBinh/HT-01) |
| HT-02 | Đăng nhập, phiên và vai trò | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/HT-02) · [server](../server/Modules/TV1_VoNgocBinh/HT-02) |
| HT-03 | Cấu hình và nhật ký | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/HT-03) · [server](../server/Modules/TV1_VoNgocBinh/HT-03) |
| HT-04 | Sao lưu và phục hồi | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/HT-04) · [server](../server/Modules/TV1_VoNgocBinh/HT-04) |
| DG-01 | Khoa, ngành và nhập hồ sơ | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/DG-01) · [server](../server/Modules/TV1_VoNgocBinh/DG-01) |
| DG-02 | Tra cứu và định danh thẻ trường | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/DG-02) · [server](../server/Modules/TV1_VoNgocBinh/DG-02) |
| DG-03 | Khóa và mở quyền mượn | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/DG-03) · [server](../server/Modules/TV1_VoNgocBinh/DG-03) |
| BC-01 | Thống kê độc giả và nhật ký truy cập | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/BC-01) · [server](../server/Modules/TV1_VoNgocBinh/BC-01) |
| TC-01 | Tích hợp giao diện và bản cài | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/TC-01) · [server](../server/Modules/TV1_VoNgocBinh/TC-01) |
| TC-02 | Kiểm thử toàn hệ thống và demo | Võ Ngọc Bình | [client](../client/Modules/TV1_VoNgocBinh/TC-02) · [server](../server/Modules/TV1_VoNgocBinh/TC-02) |
| TL-01 | Danh mục DDC23 và xác nhận phân loại | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/TL-01) · [server](../server/Modules/TV2_TranMyNhung/TL-01) |
| TL-02 | Hồ sơ đầu tài liệu | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/TL-02) · [server](../server/Modules/TV2_TranMyNhung/TL-02) |
| TL-03 | Biên mục MARC21 | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/TL-03) · [server](../server/Modules/TV2_TranMyNhung/TL-03) |
| TL-04 | Bản sách và dãy kệ/ngăn | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/TL-04) · [server](../server/Modules/TV2_TranMyNhung/TL-04) |
| TL-05 | Đồ án, luận văn và tệp đính kèm | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/TL-05) · [server](../server/Modules/TV2_TranMyNhung/TL-05) |
| TL-07 | In barcode và nhãn gáy | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/TL-07) · [server](../server/Modules/TV2_TranMyNhung/TL-07) |
| KK-01 | Kiểm kê kho | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/KK-01) · [server](../server/Modules/TV2_TranMyNhung/KK-01) |
| KK-02 | Đề nghị và xử lý thanh lý | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/KK-02) · [server](../server/Modules/TV2_TranMyNhung/KK-02) |
| BC-02 | Thống kê tài liệu và kho | Trần Mỹ Nhung | [client](../client/Modules/TV2_TranMyNhung/BC-02) · [server](../server/Modules/TV2_TranMyNhung/BC-02) |
| LT-01 | Mượn sách tại quầy | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/LT-01) · [server](../server/Modules/TV3_HuynhCongThu/LT-01) |
| LT-02 | Trả sách và tình trạng bản sách | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/LT-02) · [server](../server/Modules/TV3_HuynhCongThu/LT-02) |
| LT-03 | Gia hạn tại quầy | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/LT-03) · [server](../server/Modules/TV3_HuynhCongThu/LT-03) |
| VP-01 | Quá hạn và bồi thường | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/VP-01) · [server](../server/Modules/TV3_HuynhCongThu/VP-01) |
| VP-02 | Thu tiền và biên lai | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/VP-02) · [server](../server/Modules/TV3_HuynhCongThu/VP-02) |
| VP-03 | Hủy hiệu lực phiếu phạt | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/VP-03) · [server](../server/Modules/TV3_HuynhCongThu/VP-03) |
| PD-01 | Lượt vào và ra phòng đọc | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/PD-01) · [server](../server/Modules/TV3_HuynhCongThu/PD-01) |
| PH-01 | Góp ý, đánh giá và đề xuất sách | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/PH-01) · [server](../server/Modules/TV3_HuynhCongThu/PH-01) |
| BC-03 | Thống kê lưu thông, quá hạn và thu tiền | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/BC-03) · [server](../server/Modules/TV3_HuynhCongThu/BC-03) |
| LT-04 | In phiếu mượn và biên lai | Huỳnh Công Thủ | [client](../client/Modules/TV3_HuynhCongThu/LT-04) · [server](../server/Modules/TV3_HuynhCongThu/LT-04) |

HT-01 có mã chạy thật cho kiểm tra kết nối.
28 mô đun còn lại đã có thư mục và hợp đồng bàn giao, đang chờ lập trình nghiệp vụ.
Các README từng mô đun lưu người rà soát, dữ liệu liên quan và điều kiện hoàn tất.
Mỗi thành viên làm phần client và server của mô đun mình.

## Tài khoản GitHub để đối chiếu

| Thành viên | Tài khoản đã có căn cứ |
|---|---|
| Võ Ngọc Bình | [Jin01524](https://github.com/Jin01524) |
| Trần Mỹ Nhung | Chưa được cung cấp trong repo |
| Huỳnh Công Thủ | Chưa được cung cấp trong repo |

Thành viên dùng tài khoản cá nhân để commit và tạo PR phần mình.
Tên thư mục thể hiện người nhận việc. Lịch sử Git thể hiện người đã thực hiện commit.

## Kết quả rà soát tài liệu

| Nguồn đã đối chiếu | Nội dung liên quan và cách áp dụng |
|---|---|
| README.md, CONTRIBUTING.md, solution và mã C# | Cập nhật đường dẫn, hai điểm khởi động, cấu hình và luồng mạng |
| Ke_hoach_va_phan_cong_module.md | 29 mã mô đun và ba người phụ trách khớp với thư mục mới |
| Theo_doi_cong_viec_den_29_11_2026.xlsx | Giữ phân công 10/9/10, ngày và độ khó. HT-01 nay kiểm mạng qua server; TC-01 đóng gói hai ứng dụng |
| Workflow.md, Cay_module_chuc_nang.md | Giữ nghiệp vụ. Bổ sung thông tin server/client và vị trí kiểm quyền |
| Thiet_ke_CSDL.md, Tu_dien_CSDL.md | Đối chiếu tên bảng và giao dịch để đặt trách nhiệm trên server |
| Bao_cao_Quan_ly_Thu_vien_DAU.docx | Mục 1.4 và hình kiến trúc lưu phương án WinForms truy cập DB trực tiếp ở bản trước. Bản triển khai mới dùng kiến trúc mô tả tại tài liệu này |
| Mẫu báo cáo.docx | Mẫu bố cục báo cáo tham khảo, chưa chứa mã nguồn hoặc phân công riêng |
| Database_Thu_vien_DAU.pptx | Nội dung tập trung vào CSDL, DDC và MARC, dùng để đối chiếu phạm vi dữ liệu |
| So_do_CSDL.png, So_do_CSDL.svg | Sơ đồ quan hệ CSDL áp dụng cho server |
| SQL, JSON và công cụ trong csdl, cong_cu/csdl | Bộ nguồn dữ liệu, quy tắc, profile MARC, DDC và sinh SQL tiếp tục dùng chung |
| kiem_tra/Khoi_tao_WinForms.md và kết quả MySQL | Kết quả lịch sử trước khi tách server/client. Kiểm tra khung mới ghi riêng ở Server_Client_MVC.md |

Báo cáo Word và Excel đang lưu mốc thiết kế/phân công trước thay đổi ngày 21/11.
Khi thuyết trình cấu trúc đang chạy, dùng README gốc và tài liệu kiến trúc này.
CSDL giữ 20 bảng và các quy tắc quyền, DDC23/MARC21 đã chốt.
Kiểm quyền và giao dịch của mô đun tương lai cần triển khai ở server theo tiêu chí nghiệm thu.
