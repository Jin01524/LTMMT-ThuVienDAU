# Kiểm thử TV3

Tại gốc repo:

```powershell
dotnet build cong_cu/kiem_tra_tv3/FixtureHost -c Release
python cong_cu/kiem_tra_tv3/integration.py
```

Cần .NET 10, Python 3 và MySQL Server 8.0 tại `C:/Program Files/MySQL/MySQL Server 8.0/bin`; có thể truyền đường dẫn thư mục bin MySQL làm tham số đầu của script Python.

Script tạo MySQL riêng trong `.tooling`, cổng loopback ngẫu nhiên, schema chung và các tài khoản/mật khẩu thử ngẫu nhiên. Không đọc hay ghi đè cấu hình MySQL thật. Nó khởi chạy FixtureHost để kiểm tra TCP thật với đồng hồ và lịch lễ/Tết thử riêng; các lệnh đổi đồng hồ không có trong server sản phẩm. Tiến trình được dừng trong finally.

Kiểm tra: phiên/quyền, thời điểm mở/đóng từng buổi, hạn mức/trùng nhan đề, đồng thời, hoàn tác mượn/trả, gia hạn tại quầy, phạt lễ/Tết, mất/hỏng/đền sách, thu/hủy, in lại, phòng đọc, phản hồi riêng tư và thống kê không trùng tiền.

Kết quả chỉ được ghi vào `kiem_tra/TV3_TCP_ket_qua.txt` khi toàn bộ lượt chạy thành công. CSDL và cấu hình thử ở `.tooling` không được commit.
