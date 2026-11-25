# Kiểm tra TCP Socket ngày 21/11/2026

Chuyển khung server/client từ HTTP sang TCP Socket trực tiếp theo yêu cầu giảng viên.
Bản kiểm tra HTTP Server_Client_MVC.md trước đó là lịch sử.

Restore locked-mode và build Release trên Windows: 0 lỗi, 0 cảnh báo.
Bộ kiểm thử tự động: cong_cu/kiem_tra_tcp; log thực thi ở TCP_Socket_ket_qua.txt.
Bao gồm server thật, 40 client đồng thời, bản tin phân mảnh, lệnh/phiên bản không hợp lệ,
JSON sai/null, độ dài vượt giới hạn, EOF giữa bản tin, phản hồi sai RequestId, timeout,
lỗi MySQL và cấu hình HTTP cũ. Không yêu cầu tài khoản MySQL thật.

Chưa kiểm giao diện WinForms, kết nối MySQL thành công và nhiều máy LAN.
Các module nghiệp vụ còn đang chờ triển khai; thay đổi này hoàn thiện khung truyền dữ liệu.
