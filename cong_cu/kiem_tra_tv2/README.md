# Kiểm thử tích hợp TL-01/TL-02

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
