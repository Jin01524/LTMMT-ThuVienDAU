# DG-03 Khóa và mở quyền mượn

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV3**.

Phụ thuộc trước khi hoàn tất DG-02, HT-03.

## Nghiệm thu

Chỉ Quản lý thư viện thao tác; ghi lý do/thời gian; chỉ mở khi đáp ứng điều kiện trả sách, thanh toán và đình chỉ.

## Trách nhiệm client

Trong cửa sổ tra cứu độc giả, chỉ vai trò QUAN_LY_THU_VIEN thấy thao tác khóa/mở.
Mọi thay đổi yêu cầu lý do và xác nhận; server kiểm vai trò lại, ghi thay đổi trong
giao dịch và dùng trigger hiện có để ghi NhatKy. Mở quyền bị từ chối nếu còn bản
đang mượn hoặc vi phạm CHUA_XU_LY.

Quy tắc đình chỉ riêng chưa có trường/bảng hoặc chính sách được chốt trong schema;
hiện không tự suy diễn thêm điều kiện đó.

Đã ghép theo giao thức TCP; xem `tai_lieu/TV1_Ghep_TCP.md` để biết lệnh và cách chạy.
