# Kiểm tra control giao diện TV3

```powershell
dotnet run --project cong_cu/kiem_tra_tv3_client -c Release
```

Windows + .NET 10. Dựng các control WinForms thật và render ngoài màn hình vào `kiem_tra/anh_tv3`; kiểm tra menu theo vai trò, không có ô nhập ID quản lý/người xem, in lại và hiển thị tiếng Việt trên phiếu.

Các sự kiện tải dữ liệu ban đầu được tắt trong lượt render này. Đây không phải kiểm thử thao tác người dùng, kết nối LAN hoặc máy in vật lý. Nghiệp vụ được kiểm riêng bằng bộ TCP/MySQL ở `cong_cu/kiem_tra_tv3`.
