# Kiểm thử merge TV1 qua TCP

Build Release rồi chạy từ gốc repo:

```powershell
python cong_cu/kiem_tra_tv1/integration.py
```

Cần MySQL Server 8 bin tại đường dẫn mặc định; truyền thư mục bin làm đối số đầu
nếu khác. Fixture tạo DB/server riêng, 5 tài khoản giả, kiểm đăng nhập/phân quyền,
độc giả, mượn, nhật ký/cấu hình, báo cáo, phiên TV2 và giới hạn frame. Sau đó gọi
bộ `kiem_tra_tv1_client` để kiểm dịch vụ client thật, không chỉ gửi JSON thủ công.
Kết quả ở `kiem_tra/TV1_Merge_TCP.txt` và `TV1_Client_TCP.txt`. Tệp fixture chứa cấu
hình/tài khoản giả nằm trong `.tooling/`, Git bỏ qua; không dùng lại cho dữ liệu thật.

Không sửa cấu hình/DB thật; tiến trình fixture dừng trong finally. Chưa bao gồm
thao tác UI đầy đủ hoặc nhiều máy LAN.
