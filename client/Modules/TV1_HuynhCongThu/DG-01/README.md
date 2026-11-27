# DG-01 Khoa, ngành và nhập hồ sơ

Người phụ trách **Huỳnh Công Thủ** (TV1, 2351220232). Người rà soát **TV2**.

## Luồng client

Form nhập hỗ trợ `.xlsx`/`.csv`, tối đa 10 MiB và 500 dòng; hiển thị xem trước, kết quả kiểm tra và cho xuất riêng các dòng lỗi. Có thể hủy trước khi ghi; khi xác nhận, chỉ dòng hợp lệ được gửi vào giao dịch server.

Mẫu CSV và quy tắc cột/giá trị: [Mau_nhap_doc_gia_DG01.csv](../../../../tai_lieu/Mau_nhap_doc_gia_DG01.csv), [DG01_Quy_tac_nhap_doc_gia.md](../../../../tai_lieu/DG01_Quy_tac_nhap_doc_gia.md).

Kiểm thử parser dùng tiêu đề workbook và dữ liệu giả; chưa nghiệm thu thao tác UI/LAN với danh sách chính thức của trường.
