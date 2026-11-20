# Từ điển CSDL thư viện DAU — bản gọn

20 bảng. Tên tiếng Việt không dấu. BieuGhiMARC.Truong giữ các trường và trường con theo thứ tự. DauTaiLieu.DanhSachTep giữ danh sách tệp đính kèm. Schema tạo mới: ThuVienDAU.

## Danh sách bảng

- Khoa (3 cột)
- NganhDaoTao (3 cột)
- DocGia (14 cột)
- TaiKhoan (7 cột)
- MucPhanLoaiDDC (9 cột)
- DauTaiLieu (14 cột)
- ThanhVienDeTai (3 cột)
- ViTriLuuTru (3 cột)
- BanTaiLieu (10 cột)
- PhieuMuon (5 cột)
- ChiTietPhieuMuon (11 cột)
- LuotVaoPhongDoc (6 cột)
- PhieuThu (6 cột)
- ViPham (12 cột)
- KiemKe (6 cột)
- ChiTietKiemKe (10 cột)
- ThanhLy (10 cột)
- PhanHoiDocGia (10 cột)
- NhatKy (7 cột)
- BieuGhiMARC (11 cột)

## Khoa

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdKhoa | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| MaKhoa | VARCHAR(40) NOT NULL UNIQUE |
| TenKhoa | VARCHAR(200) NOT NULL |

## NganhDaoTao

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| MaNganh | CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NOT NULL PRIMARY KEY |
| IdKhoa | INT NOT NULL |
| TenNganh | VARCHAR(200) NOT NULL |

Khóa ngoại:

- FOREIGN KEY (IdKhoa) REFERENCES Khoa(IdKhoa)

## DocGia

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdDocGia | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| MaDinhDanhTruong | VARCHAR(50) NOT NULL UNIQUE |
| HoTen | VARCHAR(200) NOT NULL |
| LoaiDocGia | VARCHAR(20) NOT NULL |
| IdKhoa | INT NULL |
| MaNganh | CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NULL |
| KhoaHoc | VARCHAR(30) NULL |
| TrangThaiHocTap | VARCHAR(50) NULL |
| ThuDienTu | VARCHAR(254) NULL |
| TrangThaiHoSo | VARCHAR(20) NOT NULL DEFAULT 'HOAT_DONG' |
| DuocMuon | BOOLEAN NOT NULL DEFAULT 1 |
| KhoaDen | DATETIME NULL |
| LyDoKhoa | VARCHAR(300) NULL |
| IdNguoiDoiQuyen | INT NULL |

Khóa ngoại:

- FOREIGN KEY (IdKhoa) REFERENCES Khoa(IdKhoa)
- FOREIGN KEY (MaNganh) REFERENCES NganhDaoTao(MaNganh)
- FOREIGN KEY (IdNguoiDoiQuyen) REFERENCES TaiKhoan(IdTaiKhoan)

## TaiKhoan

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdTaiKhoan | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDocGia | INT NULL UNIQUE |
| TenDangNhap | VARCHAR(100) NOT NULL UNIQUE |
| MatKhauDaBam | VARBINARY(512) NULL |
| DinhDanhNgoai | VARCHAR(200) NULL UNIQUE |
| VaiTro | VARCHAR(25) NOT NULL DEFAULT 'DOC_GIA' |
| DangHoatDong | BOOLEAN NOT NULL DEFAULT 0 |

Khóa ngoại:

- FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia)

## MucPhanLoaiDDC

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| MaDDC | VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL PRIMARY KEY |
| MaCha | VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NULL |
| TenMuc | VARCHAR(200) NOT NULL |
| NguonThamKhao | VARCHAR(600) NOT NULL |
| DaDoiChieu | BOOLEAN NOT NULL DEFAULT 0 |
| ViTriTraBanIn | VARCHAR(150) NULL |
| IdNguoiDoiChieu | INT NULL |
| ThoiDiemDoiChieu | DATETIME NULL |
| CachGhepSo | VARCHAR(600) NULL |

Khóa ngoại:

- FOREIGN KEY (MaCha) REFERENCES MucPhanLoaiDDC(MaDDC)
- FOREIGN KEY (IdNguoiDoiChieu) REFERENCES TaiKhoan(IdTaiKhoan)

## DauTaiLieu

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdDauTaiLieu | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| NhanDe | VARCHAR(500) NOT NULL |
| LoaiTaiLieu | VARCHAR(20) NOT NULL |
| MaNganh | CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NULL |
| KhoaHoc | VARCHAR(30) NULL |
| DanhSachTep | JSON NOT NULL DEFAULT (JSON_ARRAY()) |
| MaDDC | VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NULL |
| AnBanDDC | VARCHAR(5) NOT NULL DEFAULT '23' |
| TrangThaiPhanLoai | VARCHAR(20) NOT NULL DEFAULT 'CHUA_PHAN_LOAI' |
| ViTriTraDDC | VARCHAR(150) NULL |
| IdNguoiPhanLoai | INT NULL |
| ThoiDiemPhanLoai | DATETIME NULL |
| DangHienThi | BOOLEAN NOT NULL DEFAULT 1 |
| PhienBan | BIGINT UNSIGNED NOT NULL DEFAULT 1 |

Khóa ngoại:

- FOREIGN KEY (MaNganh) REFERENCES NganhDaoTao(MaNganh)
- FOREIGN KEY (MaDDC) REFERENCES MucPhanLoaiDDC(MaDDC)
- FOREIGN KEY (IdNguoiPhanLoai) REFERENCES TaiKhoan(IdTaiKhoan)

## ThanhVienDeTai

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdDauTaiLieu | INT NOT NULL |
| IdDocGia | INT NOT NULL |
| VaiTro | VARCHAR(20) NOT NULL |

Khóa ngoại:

- FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu)
- FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia)

## ViTriLuuTru

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdViTri | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| DayKe | VARCHAR(30) NOT NULL |
| Ngan | VARCHAR(30) NOT NULL |

## BanTaiLieu

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdBanTaiLieu | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDauTaiLieu | INT NOT NULL |
| SoDangKyCaBiet | VARCHAR(50) NOT NULL UNIQUE |
| MaVach | VARCHAR(100) NOT NULL UNIQUE |
| IdViTri | INT NULL |
| KyHieuXepGia | VARCHAR(100) NULL |
| TrangThai | VARCHAR(20) NOT NULL DEFAULT 'DANG_XU_LY' |
| TinhTrang | VARCHAR(20) NOT NULL DEFAULT 'TOT' |
| GiaBia | DECIMAL(12,0) NULL |
| NgayNhap | DATE NULL |

Khóa ngoại:

- FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu)
- FOREIGN KEY (IdViTri) REFERENCES ViTriLuuTru(IdViTri)

## PhieuMuon

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdPhieuMuon | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDocGia | INT NOT NULL |
| ThoiDiemMuon | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| IdThuThu | INT NOT NULL |
| TrangThai | VARCHAR(20) NOT NULL DEFAULT 'DANG_MO' |

Khóa ngoại:

- FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia)
- FOREIGN KEY (IdThuThu) REFERENCES TaiKhoan(IdTaiKhoan)

## ChiTietPhieuMuon

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdChiTietMuon | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdPhieuMuon | INT NOT NULL |
| IdDocGia | INT NOT NULL |
| IdBanTaiLieu | INT NOT NULL |
| IdDauTaiLieu | INT NOT NULL |
| HanTra | DATETIME NOT NULL |
| ThoiDiemTra | DATETIME NULL |
| IdNguoiNhanTra | INT NULL |
| TrangThai | VARCHAR(20) NOT NULL DEFAULT 'DANG_MUON' |
| SoLanGiaHan | SMALLINT NOT NULL DEFAULT 0 |
| DauDangMuon | TINYINT GENERATED ALWAYS AS (CASE WHEN TrangThai='DANG_MUON' THEN 1 ELSE NULL END) VIRTUAL |

Khóa ngoại:

- FOREIGN KEY (IdPhieuMuon,IdDocGia) REFERENCES PhieuMuon(IdPhieuMuon,IdDocGia)
- FOREIGN KEY (IdBanTaiLieu,IdDauTaiLieu) REFERENCES BanTaiLieu(IdBanTaiLieu,IdDauTaiLieu)
- FOREIGN KEY (IdNguoiNhanTra) REFERENCES TaiKhoan(IdTaiKhoan)

## LuotVaoPhongDoc

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdLuotDoc | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDocGia | INT NOT NULL |
| ThoiDiemVao | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| ThoiDiemRa | DATETIME NULL |
| IdThuThu | INT NOT NULL |
| DaTraThe | BOOLEAN NOT NULL DEFAULT 0 |

Khóa ngoại:

- FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia)
- FOREIGN KEY (IdThuThu) REFERENCES TaiKhoan(IdTaiKhoan)

## PhieuThu

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdPhieuThu | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDocGia | INT NOT NULL |
| ThoiDiemThu | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| IdNguoiThu | INT NOT NULL |
| HinhThuc | VARCHAR(20) NOT NULL |
| SoTienThu | DECIMAL(12,0) NOT NULL |

Khóa ngoại:

- FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia)
- FOREIGN KEY (IdNguoiThu) REFERENCES TaiKhoan(IdTaiKhoan)

## ViPham

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdViPham | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDocGia | INT NOT NULL |
| IdChiTietMuon | INT NULL |
| LoaiViPham | VARCHAR(25) NOT NULL |
| SoTien | DECIMAL(12,0) NOT NULL DEFAULT 0 |
| CanCu | VARCHAR(1000) NOT NULL |
| ThoiDiemLap | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| IdNguoiLap | INT NOT NULL |
| IdPhieuThu | INT NULL |
| TrangThai | VARCHAR(20) NOT NULL DEFAULT 'CHUA_XU_LY' |
| IdNguoiHuy | INT NULL |
| LyDoHuy | VARCHAR(300) NULL |

Khóa ngoại:

- FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia)
- FOREIGN KEY (IdChiTietMuon,IdDocGia) REFERENCES ChiTietPhieuMuon(IdChiTietMuon,IdDocGia)
- FOREIGN KEY (IdNguoiLap) REFERENCES TaiKhoan(IdTaiKhoan)
- FOREIGN KEY (IdNguoiHuy) REFERENCES TaiKhoan(IdTaiKhoan)
- FOREIGN KEY (IdPhieuThu,IdDocGia) REFERENCES PhieuThu(IdPhieuThu,IdDocGia)

## KiemKe

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdKiemKe | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| TenKiemKe | VARCHAR(200) NOT NULL |
| ThoiDiemBatDau | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| ThoiDiemKetThuc | DATETIME NULL |
| IdNguoiLap | INT NOT NULL |
| TrangThai | VARCHAR(20) NOT NULL DEFAULT 'DU_KIEN' |

Khóa ngoại:

- FOREIGN KEY (IdNguoiLap) REFERENCES TaiKhoan(IdTaiKhoan)

## ChiTietKiemKe

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdChiTietKiemKe | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdKiemKe | INT NOT NULL |
| IdBanTaiLieu | INT NULL |
| MaVachQuet | VARCHAR(100) NULL |
| CoTrongDanhSach | BOOLEAN NOT NULL DEFAULT 1 |
| IdViTriDuKien | INT NULL |
| IdViTriThucTe | INT NULL |
| KetQua | VARCHAR(20) NOT NULL DEFAULT 'CHUA_KIEM' |
| IdNguoiKiemKe | INT NULL |
| ThoiDiemKiem | DATETIME NULL |

Khóa ngoại:

- FOREIGN KEY (IdKiemKe) REFERENCES KiemKe(IdKiemKe)
- FOREIGN KEY (IdBanTaiLieu) REFERENCES BanTaiLieu(IdBanTaiLieu)
- FOREIGN KEY (IdViTriDuKien) REFERENCES ViTriLuuTru(IdViTri)
- FOREIGN KEY (IdViTriThucTe) REFERENCES ViTriLuuTru(IdViTri)
- FOREIGN KEY (IdNguoiKiemKe) REFERENCES TaiKhoan(IdTaiKhoan)

## ThanhLy

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdThanhLy | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdBanTaiLieu | INT NOT NULL |
| IdKiemKe | INT NULL |
| LyDo | VARCHAR(500) NOT NULL |
| ThoiDiemDeNghi | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| IdNguoiDeNghi | INT NOT NULL |
| TrangThai | VARCHAR(20) NOT NULL DEFAULT 'DE_NGHI' |
| IdNguoiXuLy | INT NULL |
| ThoiDiemXuLy | DATETIME NULL |
| DauDeNghi | TINYINT GENERATED ALWAYS AS (CASE WHEN TrangThai='DE_NGHI' THEN 1 ELSE NULL END) VIRTUAL |

Khóa ngoại:

- FOREIGN KEY (IdBanTaiLieu) REFERENCES BanTaiLieu(IdBanTaiLieu)
- FOREIGN KEY (IdKiemKe) REFERENCES KiemKe(IdKiemKe)
- FOREIGN KEY (IdNguoiDeNghi) REFERENCES TaiKhoan(IdTaiKhoan)
- FOREIGN KEY (IdNguoiXuLy) REFERENCES TaiKhoan(IdTaiKhoan)

## PhanHoiDocGia

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdPhanHoi | INT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDocGia | INT NOT NULL |
| LoaiPhanHoi | VARCHAR(20) NOT NULL |
| IdDauTaiLieu | INT NULL |
| TieuDe | VARCHAR(300) NOT NULL |
| NoiDung | TEXT NOT NULL |
| Diem | TINYINT NULL |
| ThoiDiemGui | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| TrangThai | VARCHAR(20) NOT NULL DEFAULT 'MOI' |
| IdNguoiXuLy | INT NULL |

Khóa ngoại:

- FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia)
- FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu)
- FOREIGN KEY (IdNguoiXuLy) REFERENCES TaiKhoan(IdTaiKhoan)

## NhatKy

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdNhatKy | BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdTaiKhoan | INT NULL |
| ThoiDiem | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| HanhDong | VARCHAR(50) NOT NULL |
| BangDuLieu | VARCHAR(50) NOT NULL |
| KhoaBanGhi | VARCHAR(100) NOT NULL |
| ChiTiet | JSON NOT NULL |

Khóa ngoại:

- FOREIGN KEY (IdTaiKhoan) REFERENCES TaiKhoan(IdTaiKhoan)

## BieuGhiMARC

| Cột | Kiểu dữ liệu và ràng buộc |
|---|---|
| IdBieuGhiMARC | BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY |
| IdDauTaiLieu | INT NULL UNIQUE |
| DauBieuMARC | VARCHAR(24) CHARACTER SET ascii COLLATE ascii_bin NOT NULL |
| Truong | JSON NOT NULL DEFAULT (JSON_ARRAY()) |
| TrangThai | VARCHAR(25) NOT NULL DEFAULT 'NHAP' |
| PhienBan | BIGINT UNSIGNED NOT NULL DEFAULT 1 |
| PhienBanDauTaiLieu | BIGINT UNSIGNED NULL |
| IdNguoiTao | INT NOT NULL |
| ThoiDiemTao | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP |
| IdNguoiKiemTra | INT NULL |
| ThoiDiemKiemTra | DATETIME NULL |

Khóa ngoại:

- FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu)
- FOREIGN KEY (IdNguoiTao) REFERENCES TaiKhoan(IdTaiKhoan)
- FOREIGN KEY (IdNguoiKiemTra) REFERENCES TaiKhoan(IdTaiKhoan)

## View, trigger và thủ tục

| Loại | Tên |
|---|---|
| PROCEDURE | KiemTraQuanLy |
| PROCEDURE | DoiTrangThaiDangNhapDocGia |
| TRIGGER | DocGia_ChanXoa |
| TRIGGER | DocGia_KiemTraThem |
| TRIGGER | DocGia_KiemTraSua |
| TRIGGER | DocGia_LuuDoiQuyen |
| TRIGGER | DDC_KiemTraThem |
| TRIGGER | DDC_KiemTraSua |
| PROCEDURE | KiemTraPhanLoai |
| TRIGGER | DauTaiLieu_Them |
| TRIGGER | DauTaiLieu_Sua |
| TRIGGER | DauTaiLieu_LuuPhanLoai |
| TRIGGER | ViPham_ChanXoa |
| PROCEDURE | KiemTraViPham |
| TRIGGER | ViPham_Them |
| TRIGGER | ViPham_Sua |
| TRIGGER | ViPham_LuuHuy |
| PROCEDURE | KiemTraDuocMuon |
| PROCEDURE | ThuTienPhat |
| PROCEDURE | GiaHanMuon |
| PROCEDURE | KiemTraCauTrucMARC |
| PROCEDURE | KiemTraNoiDungMARC |
| TRIGGER | MARC_Them |
| TRIGGER | MARC_Sua |
| PROCEDURE | KiemTraBieuGhiMARC |
| VIEW | vw_NganhTheoKhoa |
| VIEW | vw_NoiDungMARC |
| VIEW | vw_ISBNTaiLieu |
| VIEW | vw_TinhTrangMARC |
| VIEW | vw_TruongMARCChuaDinhNghia |
| VIEW | vw_SachSanSang |
| VIEW | vw_DangMuon |
| VIEW | vw_QuaHan |
| VIEW | vw_CongNo |
| VIEW | vw_KetQuaKiemKe |
