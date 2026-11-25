# Kiểm thử khung TCP

Từ gốc repo, build server/client trước rồi chạy:

    dotnet restore ThuVienDAU.sln --locked-mode
    dotnet build ThuVienDAU.sln -c Release --no-restore
    dotnet run --project cong_cu/kiem_tra_tcp -- .

Chương trình dùng TcpCommandClient và DatabaseHealthService thật, khởi động server thật
trên cổng loopback tạm. Các trường hợp phản hồi giả chạy bằng TcpListener riêng để kiểm
lỗi client. File cấu hình thử nằm cạnh assembly của bộ kiểm thử; được khôi phục/xóa khi kết thúc.
Không chạy SQL, không thay cấu hình client/server hoặc dữ liệu DB. Server thử được dừng sau kiểm tra.
Chưa thay thế kiểm thử giao diện Windows và nhiều máy LAN.
