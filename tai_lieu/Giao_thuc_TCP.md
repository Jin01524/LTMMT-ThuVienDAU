# Giao thức TCP thư viện DAU

Ngày 21/11/2026: theo yêu cầu giảng viên, client và server dùng TCP Socket trực tiếp.
WinForms MVC → TcpClient → TcpListener → Controller → Service → MySQL.
Giữ nguyên 29 module, 20 bảng và phân quyền nghiệp vụ đã chốt. HT-01 hiện có hai lệnh;
HT-02 và các lệnh nghiệp vụ chưa triển khai.

## Định dạng bản tin

Mỗi bản tin = 4 byte số nguyên độ dài có dấu, big endian, theo sau bởi JSON UTF-8.
Độ dài là số byte JSON, từ 1 tới 1.048.576. Đọc đủ header và body bằng ReadExactlyAsync;
không giả định một lần nhận socket là một bản tin. Null/JSON sai/độ dài sai hoặc ngắt giữa bản tin:
đóng kết nối lỗi, server tiếp tục phục vụ client khác.
Một kết nối chỉ dùng cho một request và một response; muốn gửi lệnh tiếp theo thì mở kết nối mới.

Request mẫu (phần JSON, không phải toàn bộ frame):

    {"version":1,"requestId":"ma-duy-nhat","command":"PING"}

Response thành công:

    {"version":1,"requestId":"ma-duy-nhat","success":true,"data":{"status":"ok","application":"ThuVienDAU.Server"},"error":null,"errorNumber":null}

DATABASE_HEALTH trả data gồm serverVersion và databaseName; khi lỗi trả success=false,
error là thông báo và errorNumber là mã MySQL nếu có. Không dùng mã trạng thái HTTP.
Lệnh không hỗ trợ hoặc phiên bản không đúng trả lỗi có cùng requestId.
Client kiểm version và requestId trước khi đọc dữ liệu. Các lệnh không được thực thi như SQL tùy ý.

## Thiết lập và vòng đời

- Server: mặc định 127.0.0.1:5080; LAN dùng --host 0.0.0.0 --port 5080.
- Client: Server.Host, Port, TimeoutSeconds; cấu hình BaseUrl cũ bị từ chối kèm hướng dẫn chuyển đổi.
- Mỗi request giới hạn 15 giây ở server; client mặc định 15 giây, cấu hình từ 1–120 giây.
- Server giới hạn 32 kết nối xử lý đồng thời; client chậm không chặn toàn bộ các client còn lại.
- Ctrl+C hủy chờ kết nối và các yêu cầu đang xử lý, đóng listener.
- Đây là khung demo nội bộ chưa có mã hóa hoặc xác thực phiên. HT-02 phải kiểm phiên/quyền ở server trước lệnh nghiệp vụ.

Báo cáo Word/Excel và patch HTTP cũ là các mốc trước lần chuyển giao thức; không dùng patch đó để ghi đè mã TCP hiện hành.

## Nguồn API .NET

[TcpListener.AcceptTcpClientAsync](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener.accepttcpclientasync?view=net-10.0)
nhận kết nối bất đồng bộ có hủy; [Stream.ReadExactlyAsync](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readexactlyasync?view=net-10.0)
đọc đủ số byte hoặc báo EOF/hủy. Quy ước header, JSON và tên lệnh là thiết kế riêng của đồ án.
