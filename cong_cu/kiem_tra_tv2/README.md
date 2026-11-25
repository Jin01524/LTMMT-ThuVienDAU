# Kiểm thử tích hợp 9 module TV2

Cần Python 3, MySQL Server 8.0 có mysqld/mysql/mysqladmin và dotnet trên PATH.
Từ gốc repo, build Release rồi chạy:

    dotnet build ThuVienDAU.sln -c Release
    python cong_cu/kiem_tra_tv2/integration.py

Nếu MySQL ở vị trí khác, truyền đường dẫn thư mục bin làm đối số đầu tiên.
Script khởi tạo CSDL trong thư mục mới riêng .tooling/tv2-fixture-*, dùng cổng loopback tạm,
copy server build vào fixture rồi tạo cấu hình tổng hợp ở đó. Không sửa cấu hình client/server thật,
không đụng CSDL MySQL đã cài. Các server thử được dừng trong finally; thư mục fixture được giữ
trong .tooling (Git bỏ qua) để điều tra. Mọi trang DDC/người dùng/sách trong fixture là dữ liệu giả.
Không dùng các tài khoản hoặc nguồn giả đó cho báo cáo nghiệp vụ thực tế.

Bộ cũ có 62 ca, gồm sửa đồng thời bản sách, barcode/ĐKCB trùng, trạng thái mượn/thanh lý và quyền lookup của Thủ thư. `remaining.py` chạy thêm MARC, đồ án, PDF, kiểm kê, thanh lý và báo cáo trong cùng fixture. Log tại `kiem_tra/TV2_TL01_TL02_TL04.txt` và `kiem_tra/TV2_Module_con_lai.txt`.

Kiểm codec MARC, ảnh barcode và render WinForms (cần Windows):

    dotnet run --project cong_cu/kiem_tra_tv2_client --configuration Release

Log `kiem_tra/TV2_Client.txt`; ảnh `.tooling/tv2-client-review/`. Dựng/render form không được hiểu là đã tự động bấm thử toàn bộ giao diện. Máy in/quét thật và LAN cần nghiệm thu riêng.
