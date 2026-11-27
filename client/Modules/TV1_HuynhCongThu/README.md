# Mô đun do Huỳnh Công Thủ phụ trách

Mã thành viên **TV1**, mã sinh viên **2351220232**. Tài khoản GitHub **Chưa cung cấp**.
Vai trò phụ trách giữ theo kế hoạch nhóm ngày 19/11/2026.

| Mã | Mô đun | Rà soát |
|---|---|---|
| [HT-01](HT-01/README.md) | Khung ứng dụng và kết nối MySQL | TV2 |
| [HT-02](HT-02/README.md) | Đăng nhập, phiên và vai trò | TV3 |
| [HT-03](HT-03/README.md) | Cấu hình và nhật ký | TV3 |
| [HT-04](HT-04/README.md) | Sao lưu và phục hồi | TV2 |
| [DG-01](DG-01/README.md) | Khoa, ngành và nhập hồ sơ | TV2 |
| [DG-02](DG-02/README.md) | Tra cứu và định danh thẻ trường | TV3 |
| [DG-03](DG-03/README.md) | Khóa và mở quyền mượn | TV3 |
| [BC-01](BC-01/README.md) | Thống kê độc giả và nhật ký truy cập | TV2 |
| [TC-01](TC-01/README.md) | Tích hợp giao diện và bản cài | TV2, TV3 |
| [TC-02](TC-02/README.md) | Kiểm thử toàn hệ thống và demo | TV2, TV3 |

Mỗi người phát triển phần client của mô đun mình, tự kiểm và tạo PR.
Tài khoản GitHub của từng người cần được ghi đúng sau khi thành viên cung cấp.

## Tình trạng triển khai hiện tại

HT-01 có kiểm tra MySQL qua server; HT-02 có đăng nhập, phiên và audit đăng nhập/đăng xuất.
DG-02 có tra cứu; DG-03 có khóa/mở quyền mượn theo vai trò; HT-03 có cấu hình/nhật ký;
BC-01 có báo cáo và xuất CSV. Các phần này vẫn cần rà soát bởi TV2/TV3 và kiểm thử trên dữ liệu đủ.

DG-01 có luồng import `.xlsx`/`.csv`, xem trước, validate phía server, nhập một phần,
audit và xuất dòng lỗi; parser có test bằng dữ liệu giả. HT-04 có giao diện và service
backup/restore nhưng chưa chạy trên fixture. TC-01 có script publish thử self-contained
Windows x64; chưa là installer đã ký số. TC-02, kiểm thử MySQL/TCP đầy đủ, backup/restore,
UI/LAN và demo vẫn còn phải nghiệm thu.
