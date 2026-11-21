# Cây module chức năng – Hệ thống thông tin quản lý thư viện DAU

Cập nhật tổ chức mã nguồn ngày **21/11/2026** theo
[kiến trúc server, client và MVC](Kien_truc_Server_Client_MVC.md).
Giao diện MVC ở client gọi HTTP/JSON; server thực hiện xử lý và truy cập MySQL.
Phân công mô đun áp dụng cho cả hai phía.

## 1. Căn cứ và cách phân rã

Cây chức năng dựa trên quy định thư viện và phạm vi người dùng đã chốt. Bản CSDL ngày 09/11/2026 dùng MySQL 8.0.21+, DDC23 bản in tiếng Việt rút gọn và MARC21. [Thiết kế CSDL](Thiet_ke_CSDL.md)

**Quy ước:** nút lá là một kết quả nghiệp vụ độc lập, quan sát/kiểm tra được. Không tách sâu thành hàm xử lý nội bộ hay thao tác chuột. `[Gốc]` là chức năng có trong tài liệu tiến độ; `[Phân rã]` là chi tiết được tách từ chức năng mức cao, cần chủ dự án xác nhận trước khi chốt yêu cầu; `[Yêu cầu]` là điều chỉnh theo yêu cầu/quy định thư viện người dùng cung cấp.

## 2. Cây phân hệ chức năng

```text
HTTT QUẢN LÝ THƯ VIỆN ĐẠI HỌC KIẾN TRÚC ĐÀ NẴNG
│
├── 1. Quản trị hệ thống và kiểm soát truy cập [AD]
│   ├── 1.1 Xác thực tài khoản
│   │   ├── 1.1.1 Đăng nhập nhân viên [Gốc]
│   │   ├── 1.1.2 Đổi mật khẩu [Gốc]
│   │   └── 1.1.3 Đăng xuất và kết thúc phiên [Phân rã]
│   ├── 1.2 Phân vai trò cố định [Yêu cầu]
│   │   ├── 1.2.1 Gán một vai trò cho tài khoản nhập từ danh sách trường
│   │   └── 1.2.2 Kiểm tra quyền theo vai trò khi truy cập chức năng
│   ├── 1.3 Sao lưu, phục hồi và cấu hình
│   │   ├── 1.3.1 Tạo bản sao lưu CSDL [Gốc]
│   │   ├── 1.3.2 Phục hồi CSDL từ bản sao lưu [Gốc]
│   │   └── 1.3.3 Cập nhật tham số hệ thống [Gốc]
│   └── 1.4 Nhật ký kiểm toán
│       ├── 1.4.1 Ghi nhận người dùng, thao tác, thời điểm và máy trạm [Gốc]
│       └── 1.4.2 Tra cứu nhật ký theo điều kiện [Phân rã]
│
├── 2. Quản lý danh mục và biên mục tài liệu [CA]
│   ├── 2.1 Quản lý danh mục phân loại/ngành
│   │   ├── 2.1.1 Thêm danh mục phân loại [Phân rã]
│   │   ├── 2.1.2 Cập nhật danh mục phân loại [Phân rã]
│   │   ├── 2.1.3 Xóa danh mục phân loại [Phân rã]
│   │   └── 2.1.4 Tra cứu danh mục phân loại [Phân rã]
│   ├── 2.2 Biên mục tác giả và nhà xuất bản trong MARC [Yêu cầu]
│   │   ├── 2.2.1 Nhập/sửa trường tác giả 100/700 và tên tổ chức liên quan
│   │   ├── 2.2.2 Nhập/sửa thông tin xuất bản 264 hoặc 260
│   │   └── 2.2.3 Tra cứu tác giả/nhà xuất bản từ biểu ghi MARC
│   ├── 2.3 Quản lý vị trí lưu trữ
│   │   ├── 2.3.1 Thêm vị trí dãy kệ/ngăn trong một phòng [Phân rã]
│   │   ├── 2.3.2 Cập nhật nút vị trí [Phân rã]
│   │   ├── 2.3.3 Xóa nút vị trí [Phân rã]
│   │   └── 2.3.4 Gán vị trí lưu trữ cho bản sách [Phân rã]
│   ├── 2.4 Biên mục sách
│   │   ├── 2.4.1 Tạo hồ sơ thư mục sách (ISBN, tên, năm XB, số trang, tóm tắt) [Gốc]
│   │   ├── 2.4.2 Cập nhật hồ sơ thư mục sách [Phân rã]
│   │   ├── 2.4.3 Gán tác giả, nhà xuất bản và phân loại cho hồ sơ [Phân rã]
│   │   └── 2.4.4 Tra cứu/chọn hồ sơ thư mục [Phân rã]
│   ├── 2.5 Quản lý đồ án/luận văn
│   │   ├── 2.5.1 Tạo hồ sơ đề tài [Gốc]
│   │   ├── 2.5.2 Cập nhật hồ sơ đề tài [Phân rã]
│   │   ├── 2.5.2 Ghi nhận giảng viên hướng dẫn [Gốc]
│   │   ├── 2.5.4 Ghi nhận sinh viên và khóa học [Gốc]
│   │   └── 2.5.5 Đính kèm bản vẽ hoặc các tệp tài liệu vào hồ sơ đề tài [Gốc]
│   ├── 2.6 Quản lý bản sách vật lý
│   │   ├── 2.6.1 Tạo bản sách từ hồ sơ thư mục [Gốc]
│   │   ├── 2.6.2 Sinh mã đăng ký cá biệt (ĐKCB) duy nhất [Gốc]
│   │   └── 2.6.3 Ghi nhận trạng thái và vị trí của bản sách [Phân rã]
│   ├── 2.7 In nhãn
│   │   ├── 2.7.1 Tạo nhãn mã vạch cho bản sách [Gốc]
│   │   └── 2.7.2 Tạo nhãn gáy và gửi lệnh in [Gốc]
│   ├── 2.8 Đối chiếu phân loại DDC23 [Yêu cầu]
│   │   ├── 2.8.1 Tra mục phân loại và nguồn bản in tiếng Việt
│   │   ├── 2.8.2 Ghi căn cứ số chi tiết/số ghép khi cần
│   │   ├── 2.8.3 Ghi người, thời điểm và vị trí tra cứu để xác nhận
│   │   └── 2.8.4 Lưu lịch sử thay đổi phân loại
│   └── 2.9 Biên mục theo MARC21 [Yêu cầu]
│       ├── 2.9.1 Lưu đầu biểu và mảng trường MARC, giữ chỉ thị và thứ tự trường con
│       ├── 2.9.2 Kiểm tra cấu trúc và tính lặp theo hồ sơ cục bộ
│       ├── 2.9.3 Đối chiếu trường 082 với DDC23 đã xác nhận
│       └── 2.9.4 Đưa về nháp khi sửa và theo dõi phiên bản đầu tài liệu
│
├── 3. Quản lý hồ sơ độc giả và định danh bằng thẻ trường [CI-01]
│   ├── 3.1 Tiếp nhận/cập nhật hồ sơ độc giả
│   │   ├── 3.1.1 Nạp danh sách sinh viên có mã số, họ tên, trạng thái [Gốc]
│   │   ├── 3.1.2 Nhập danh sách Excel cán bộ/giảng viên, gồm nhân sự thư viện [Yêu cầu]
│   │   ├── 3.1.3 Kiểm tra bản ghi thiếu hoặc trùng trước khi nhập [Phân rã]
│   │   ├── 3.1.4 Nhập/cập nhật hồ sơ từ danh sách hợp lệ; cơ chế cấp đăng nhập cần chốt [Phân rã]
│   │   ├── 3.1.5 Đối chiếu trạng thái học tập và đề nghị Quản lý thư viện khóa/mở quyền mượn [Phân rã]
│   │   └── 3.1.6 Biên mục khóa/mở tài khoản đăng nhập độc giả, ghi lý do và nhật ký [Yêu cầu]
│   └── 3.2 Định danh người đọc bằng thẻ trường
│       ├── 3.2.1 Đối chiếu thẻ trường với hồ sơ độc giả đã có [Yêu cầu]
│       └── 3.2.2 Tra cứu hồ sơ theo mã định danh trường [Phân rã]
│
├── 4. Quản lý lưu thông tại quầy [CI]
│   ├── 4.1 Phục vụ phòng đọc mở
│   │   ├── 4.1.1 Đối chiếu thẻ Sinh viên/cán bộ với hồ sơ độc giả [Yêu cầu]
│   │   ├── 4.1.2 Giữ thẻ Sinh viên tại quầy trong thời gian đọc [Yêu cầu]
│   │   └── 4.1.3 Ghi nhận lượt vào phòng đọc [Gốc]
│   ├── 4.2 Mượn tài liệu
│   │   ├── 4.2.1 Định danh người mượn bằng thẻ trường [Yêu cầu]
│   │   ├── 4.2.2 Kiểm tra hạn mức, vi phạm và thời điểm đóng cửa [Yêu cầu]
│   │   ├── 4.2.3 Quét barcode sách và chặn hai cuốn cùng nhan đề [Yêu cầu]
│   │   ├── 4.2.4 Lập phiếu, đặt hạn trả và cập nhật trạng thái bản sách [Gốc]
│   │   └── 4.2.5 In phiếu mượn có hạn trả [Gốc]
│   ├── 4.3 Trả tài liệu
│   │   ├── 4.3.1 Quét bản sách được trả [Gốc]
│   │   ├── 4.3.2 Đối chiếu bản sách với phiếu mượn đang mở [Phân rã]
│   │   ├── 4.3.3 Ghi nhận trả và cập nhật trạng thái bản sách [Gốc]
│   │   └── 4.3.4 Tính số ngày quá hạn [Gốc]
│   ├── 4.4 Gia hạn mượn
│   │   ├── 4.4.1 Đối chiếu yêu cầu gia hạn tại quầy (không có đăng ký trước trong hệ thống) [Yêu cầu]
│   │   ├── 4.4.2 Kiểm tra hạn mức sau gia hạn [Yêu cầu]
│   │   ├── 4.4.3 Ghi nhận hạn trả mới [Phân rã]
│   │   └── 4.4.4 Từ chối yêu cầu và thông báo kết quả [Phân rã]
│   ├── 4.5 Trễ hạn, bồi thường và thu tiền
│   │   ├── 4.5.1 Tính phạt sinh viên 2.000 đồng/ngày/cuốn, trừ lễ/Tết [Yêu cầu]
│   │   ├── 4.5.2 Thông báo vi phạm cán bộ/giảng viên tới đơn vị quản lý [Yêu cầu]
│   │   ├── 4.5.2 Tính đền tiền gấp 2 lần giá bìa/giá sách tương đương [Yêu cầu]
│   │   ├── 4.5.4 Ghi nhận sách đền và phí kỹ thuật 20.000 đồng [Yêu cầu]
│   │   ├── 4.5.5 Ghi nhận chi phí tái tạo trang hoặc bồi thường toàn bộ [Yêu cầu]
│   │   └── 4.5.6 Ghi khoản thu, thanh toán và biên lai [Gốc]
│   └── 4.6 Trạng thái quyền mượn
│       ├── 4.6.1 Quản lý thư viện khóa quyền mượn khi còn vi phạm/chưa thanh toán phạt [Gốc]
│       ├── 4.6.2 Đình chỉ sinh viên 1–4 tuần theo mức vi phạm [Yêu cầu]
│       └── 4.6.3 Quản lý thư viện mở lại quyền mượn sau khi trả sách và thanh toán đủ [Yêu cầu]
│
├── 5. Tiện ích bạn đọc [PH]
│   └── 5.1 Góp ý và đề xuất
│       ├── 5.1.1 Gửi đề xuất bổ sung tài liệu [Gốc]
│       └── 5.1.2 Gửi góp ý/đánh giá tài liệu [Gốc]
│
├── 6. Kiểm kê và thanh lý kho [CA-08]
│   ├── 6.1 Kiểm kê bản sách
│   │   ├── 6.1.1 Lập danh sách và số lượng dự kiến theo vị trí
│   │   ├── 6.1.2 Ghi người kiểm trên từng dòng chi tiết
│   │   ├── 6.1.3 Quét mã và ghi nhận tình trạng thực tế
│   │   ├── 6.1.4 Đối chiếu thiếu/thừa/sai vị trí/hư hỏng
│   │   └── 6.1.5 Xuất báo cáo kiểm kê
│   └── 6.2 Thanh lý bản sách
│       ├── 6.2.1 Lập danh sách bản sách đề nghị thanh lý
│       └── 6.2.2 Cập nhật trạng thái sau xử lý thanh lý
│
├── 7. Báo cáo và thống kê
│   ├── 7.1 Xem Dashboard theo quyền vai trò [Gốc]
│   ├── 7.2 Thống kê lượt mượn [Phân rã]
│   ├── 7.3 Thống kê lượt trả và gia hạn [Phân rã]
│   ├── 7.4 Thống kê đầu mục/bản sách [Phân rã]
│   ├── 7.5 Thống kê kết quả kiểm kê [Phân rã]
│   ├── 7.6 Thống kê hồ sơ độc giả theo nhóm [Phân rã]
│   ├── 7.7 Thống kê lượt vào phòng đọc [Phân rã]
│   ├── 7.8 Thống kê tài liệu quá hạn [Phân rã]
│   └── 7.9 Thống kê tiền phạt và khoản thu [Phân rã]
│
└── 8. Tích hợp thiết bị và hệ thống ngoài
    ├── 8.1 Máy quét mã vạch
    │   ├── 8.1.1 Nhận chuỗi mã từ máy quét tại ô nhập liệu đang chọn [Gốc]
    │   └── 8.1.2 Chuyển mã quét đến đúng nghiệp vụ đang thao tác [Phân rã]
    ├── 8.2 Máy in
    │   ├── 8.2.1 Gửi phiếu mượn/biên lai đến máy in nhiệt [Gốc]
    │   └── 8.2.2 Gửi tem mã vạch/nhãn gáy đến máy in nhãn [Gốc]
    └── 8.3 Kết nối Portal đào tạo DAU
        └── 8.3.1 Truy xuất nguồn dữ liệu sinh viên cho chức năng 3.1.1 [Gốc]
```

**Một quy trình cho dữ liệu sinh viên:** chức năng 3.1.1 nhận cùng bộ trường dữ liệu (mã số, họ tên, trạng thái học tập) và tạo/cập nhật một hồ sơ. Ưu tiên Portal DAU làm nguồn chính nếu có đủ dữ liệu và phương thức kết nối; Excel Phòng Đào tạo là cách nhập đầu hoặc phương án dự phòng. Không chạy hai luồng nạp song song cho cùng danh sách. Hồ sơ cán bộ/giảng viên tiếp tục được nhập từ Excel. Nhánh 8.3 chỉ mô tả kết nối kỹ thuật với Portal để cấp dữ liệu cho 3.1.1, không phải nghiệp vụ nhập hồ sơ thứ hai.

## 3. Quyền truy cập dùng chung

Mỗi tài khoản có một vai trò cố định: QUAN_TRI, QUAN_LY_THU_VIEN, BIEN_MUC, THU_THU hoặc DOC_GIA. Biên mục phụ trách tài liệu và khóa/mở tài khoản đăng nhập của độc giả; Thủ thư phụ trách lưu thông; chỉ Quản lý thư viện được khóa/mở quyền mượn và hủy hiệu lực phiếu phạt. Hồ sơ nhân sự nhập từ danh sách trường, không có phân hệ quản lý nhân viên riêng. Không tạo nhóm quyền động; code kiểm tra quyền theo vai trò.

## 4. Quy trình và quy định thư viện đã cung cấp

- **Không cấp thẻ thư viện.** Dùng thẻ Sinh viên hoặc thẻ cán bộ/giảng viên để định danh; khi vào phòng đọc mở, bạn đọc gửi thẻ Sinh viên tại quầy trong suốt lượt đọc.
- **Phòng đọc mở:** gửi tư trang vào tủ và giữ chìa khóa; chỉ mang vở, dụng cụ học tập vào phòng. Nhờ thủ thư hướng dẫn hoặc theo biển chỉ dẫn dãy kệ/ngăn, mỗi lượt lấy tối đa 02 cuốn. Đọc xong tự xếp sách về vị trí cũ; khi ra về trả chìa khóa và nhận lại thẻ.
- **Mượn về:** cán bộ/giảng viên, trợ giảng, CNV được mượn tối đa 03 đầu sách trong 15 ngày; sinh viên tối đa 02 đầu sách trong 10 ngày. Không mượn hai cuốn cùng nhan đề và không làm thủ tục trong 15 phút trước giờ đóng cửa. Chỉ gia hạn khi chưa có bạn đọc khác đăng ký và vẫn trong hạn mức.
- **Trễ hạn:** sinh viên chịu phạt 2.000 đồng/ngày/cuốn, không tính ngày lễ/Tết; có thể bị đình chỉ quyền sử dụng thư viện từ 1–4 tuần. Với cán bộ/giảng viên, trợ giảng, CNV, thư viện thông báo đơn vị quản lý và báo cáo lãnh đạo nhà trường để xem xét xử lý. Sau khi trả sách và thanh toán đủ khoản phạt, bạn đọc mới được tiếp tục mượn.
- **Mất/hỏng tài liệu:** đền tiền bằng 2 lần giá bìa; nếu đền sách thì nộp thêm 20.000 đồng phí kỹ thuật. Sách không ghi giá được tính theo sách tương đương do Thư viện xác định rồi nhân 2. Rách/hỏng trang bồi thường theo chi phí tái tạo/thay thế; thiệt hại lớn phải bồi thường toàn bộ và có thể bị kỷ luật.

## 5. Ranh giới kỹ thuật và điểm cần chốt

- Nền tảng hiện tại: C# .NET/Windows Forms, MySQL 8.0 và Client–Server. DDC23/MARC21 hiện đã được hỗ trợ ở cấu trúc DB; các giao diện và xử lý nghiệp vụ DDC23/MARC21 đang chờ triển khai trên khung server/client.
- Các lá gắn `[Phân rã]` là đề xuất để làm rõ các chức năng mức cao; luồng ngoại lệ, quy tắc dữ liệu và tiêu chí nghiệm thu chi tiết vẫn cần đặc tả.
- Các workflow cho các quy trình nghiệp vụ chính và danh sách điểm cần chốt được lập riêng tại `Workflow.md`.
- CSDL 20 bảng. Mỗi đầu tài liệu giữ danh sách tệp đính kèm và mỗi biểu ghi MARC giữ mảng trường, trường con. Vị trí kho gồm dãy kệ và ngăn. Kiểm kê gồm đợt kiểm kê và các dòng chi tiết.
- Nên xem mỗi nút lá là một đơn vị nghiệp vụ để lập use case/đặc tả và giao việc. Với nghiệp vụ cần tính toàn vẹn dữ liệu như lập phiếu mượn, các bước kiểm tra, ghi nhận giao dịch và cập nhật tồn phải commit nhất quán trong một giao dịch.

## 6. Giai đoạn dự án

Cây này mô tả chức năng thiết kế theo tài liệu tiến độ và phạm vi đã thống nhất, gồm bản thu gọn 20 bảng ngày 09/11/2026. Đã có khung server API và client MVC để tiếp tục triển khai nghiệp vụ.

## Quyền khóa tài khoản và quyền mượn

- **Biên mục (BIEN_MUC):** duy nhất được khóa/mở tài khoản đăng nhập của độc giả (`TaiKhoan.DangHoatDong`), chỉ áp dụng tài khoản có vai trò `DOC_GIA` và liên kết hồ sơ độc giả. Bắt buộc lý do và nhật ký.
- **Quản lý thư viện (QUAN_LY_THU_VIEN):** khóa/mở quyền mượn (`DocGia.DuocMuon`, `KhoaDen`, `LyDoKhoa`) và hủy hiệu lực phiếu phạt.
- Quản trị, Thủ thư và Độc giả không có quyền khóa/mở đăng nhập độc giả. Mở đăng nhập không mở quyền mượn; mở quyền mượn không mở đăng nhập. Tại quầy vẫn kiểm quyền mượn riêng.
- HT-02 do TV1 triển khai màn hình và kiểm quyền khóa/mở đăng nhập; người sử dụng nghiệp vụ là Biên mục. Vai trò cố định gồm QUAN_TRI, QUAN_LY_THU_VIEN, BIEN_MUC, THU_THU, DOC_GIA.
