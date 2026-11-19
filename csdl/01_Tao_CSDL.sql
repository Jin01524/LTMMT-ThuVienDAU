/* CSDL gọn cho đồ án thư viện DAU: MySQL 8.0.21+, DDC23 và MARC21.
 Tạo mới schema ThuVienDAU. Không DROP, không tự chuyển dữ liệu từ các bản trước.
 Phần mô tả thư mục chi tiết (ISBN, phụ đề, năm xuất bản, số trang, tóm tắt) lưu trong MARC.
 DDC23 bản in tiếng Việt rút gọn; 40 mục tham khảo chưa đối chiếu, 49 liên hệ ngành còn là đề xuất.
 Các kiểm tra nhiều dòng/bảng cần ứng dụng mở giao dịch và khóa dữ liệu phù hợp.
*/
SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
SET SESSION time_zone='+07:00';
SET SESSION sql_mode='STRICT_TRANS_TABLES,ONLY_FULL_GROUP_BY,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';
CREATE DATABASE ThuVienDAU CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE ThuVienDAU;

CREATE TABLE Khoa
(
    IdKhoa INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    MaKhoa VARCHAR(40) NOT NULL UNIQUE,
    TenKhoa VARCHAR(200) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE NganhDaoTao
(
    MaNganh CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NOT NULL PRIMARY KEY,
    IdKhoa INT NOT NULL,
    TenNganh VARCHAR(200) NOT NULL,
    CONSTRAINT RB_NganhDaoTao_1 FOREIGN KEY (IdKhoa) REFERENCES Khoa(IdKhoa),
    CONSTRAINT RB_NganhDaoTao_2 CHECK (REGEXP_LIKE(MaNganh,'^[0-9]{7}$','c'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE DocGia
(
    IdDocGia INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    MaDinhDanhTruong VARCHAR(50) NOT NULL UNIQUE,
    HoTen VARCHAR(200) NOT NULL,
    LoaiDocGia VARCHAR(20) NOT NULL,
    IdKhoa INT NULL,
    MaNganh CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NULL,
    KhoaHoc VARCHAR(30) NULL,
    TrangThaiHocTap VARCHAR(50) NULL,
    ThuDienTu VARCHAR(254) NULL,
    TrangThaiHoSo VARCHAR(20) NOT NULL DEFAULT 'HOAT_DONG',
    DuocMuon BOOLEAN NOT NULL DEFAULT 1,
    KhoaDen DATETIME NULL,
    LyDoKhoa VARCHAR(300) NULL,
    IdNguoiDoiQuyen INT NULL,
    CONSTRAINT RB_DocGia_1 CHECK (LoaiDocGia IN ('SINH_VIEN','CAN_BO','GIANG_VIEN','TRO_GIANG','CNV')),
    CONSTRAINT RB_DocGia_2 FOREIGN KEY (IdKhoa) REFERENCES Khoa(IdKhoa),
    CONSTRAINT RB_DocGia_3 FOREIGN KEY (MaNganh) REFERENCES NganhDaoTao(MaNganh),
    CONSTRAINT RB_DocGia_4 CHECK (TrangThaiHoSo IN ('HOAT_DONG','NGUNG_HOAT_DONG')),
    CONSTRAINT RB_DocGia_5 CHECK (DuocMuon IN (0,1)),
    CONSTRAINT RB_DocGia_6 CHECK ((DuocMuon=1 AND KhoaDen IS NULL AND LyDoKhoa IS NULL) OR (DuocMuon=0 AND LyDoKhoa IS NOT NULL AND CHAR_LENGTH(TRIM(LyDoKhoa))>0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE TaiKhoan
(
    IdTaiKhoan INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDocGia INT NULL UNIQUE,
    TenDangNhap VARCHAR(100) NOT NULL UNIQUE,
    MatKhauDaBam VARBINARY(512) NULL,
    DinhDanhNgoai VARCHAR(200) NULL UNIQUE,
    VaiTro VARCHAR(25) NOT NULL DEFAULT 'DOC_GIA',
    DangHoatDong BOOLEAN NOT NULL DEFAULT 0,
    CONSTRAINT RB_TaiKhoan_1 FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia),
    CONSTRAINT RB_TaiKhoan_2 CHECK (VaiTro IN ('QUAN_TRI','QUAN_LY_THU_VIEN','THU_THU','DOC_GIA')),
    CONSTRAINT RB_TaiKhoan_3 CHECK (DangHoatDong IN (0,1)),
    CONSTRAINT RB_TaiKhoan_4 CHECK (DangHoatDong=0 OR MatKhauDaBam IS NOT NULL OR DinhDanhNgoai IS NOT NULL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE MucPhanLoaiDDC
(
    MaDDC VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL PRIMARY KEY,
    MaCha VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NULL,
    TenMuc VARCHAR(200) NOT NULL,
    NguonThamKhao VARCHAR(600) NOT NULL,
    DaDoiChieu BOOLEAN NOT NULL DEFAULT 0,
    ViTriTraBanIn VARCHAR(150) NULL,
    IdNguoiDoiChieu INT NULL,
    ThoiDiemDoiChieu DATETIME NULL,
    CachGhepSo VARCHAR(600) NULL,
    CONSTRAINT RB_MucPhanLoaiDDC_1 FOREIGN KEY (MaCha) REFERENCES MucPhanLoaiDDC(MaDDC),
    CONSTRAINT RB_MucPhanLoaiDDC_2 FOREIGN KEY (IdNguoiDoiChieu) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_MucPhanLoaiDDC_3 CHECK (REGEXP_LIKE(MaDDC,'^[0-9]{3}([.][0-9]*[1-9])?$','c')),
    CONSTRAINT RB_MucPhanLoaiDDC_4 CHECK (DaDoiChieu IN (0,1)),
    CONSTRAINT RB_MucPhanLoaiDDC_5 CHECK (DaDoiChieu=0 OR (ViTriTraBanIn IS NOT NULL AND CHAR_LENGTH(TRIM(ViTriTraBanIn))>0 AND IdNguoiDoiChieu IS NOT NULL AND ThoiDiemDoiChieu IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE DauTaiLieu
(
    IdDauTaiLieu INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NhanDe VARCHAR(500) NOT NULL,
    LoaiTaiLieu VARCHAR(20) NOT NULL,
    MaNganh CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NULL,
    KhoaHoc VARCHAR(30) NULL,
    DanhSachTep JSON NOT NULL DEFAULT (JSON_ARRAY()),
    MaDDC VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NULL,
    AnBanDDC VARCHAR(5) NOT NULL DEFAULT '23',
    TrangThaiPhanLoai VARCHAR(20) NOT NULL DEFAULT 'CHUA_PHAN_LOAI',
    ViTriTraDDC VARCHAR(150) NULL,
    IdNguoiPhanLoai INT NULL,
    ThoiDiemPhanLoai DATETIME NULL,
    DangHienThi BOOLEAN NOT NULL DEFAULT 1,
    PhienBan BIGINT UNSIGNED NOT NULL DEFAULT 1,
    CONSTRAINT RB_DauTaiLieu_1 FOREIGN KEY (MaNganh) REFERENCES NganhDaoTao(MaNganh),
    CONSTRAINT RB_DauTaiLieu_2 FOREIGN KEY (MaDDC) REFERENCES MucPhanLoaiDDC(MaDDC),
    CONSTRAINT RB_DauTaiLieu_3 FOREIGN KEY (IdNguoiPhanLoai) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_DauTaiLieu_4 CHECK (JSON_SCHEMA_VALID('{"type":"array","items":{"type":"object","required":["TenTep","DuongDan"],"additionalProperties":false,"properties":{"TenTep":{"type":"string","maxLength":255},"DuongDan":{"type":"string","maxLength":1000}}}}', DanhSachTep)),
    CONSTRAINT RB_DauTaiLieu_5 CHECK (LoaiTaiLieu IN ('SACH','LUAN_VAN','DO_AN','KHAC')),
    CONSTRAINT RB_DauTaiLieu_6 CHECK (AnBanDDC='23' AND DangHienThi IN (0,1) AND PhienBan>0),
    CONSTRAINT RB_DauTaiLieu_7 CHECK ((TrangThaiPhanLoai='CHUA_PHAN_LOAI' AND MaDDC IS NULL AND ViTriTraDDC IS NULL AND IdNguoiPhanLoai IS NULL AND ThoiDiemPhanLoai IS NULL) OR (TrangThaiPhanLoai IN ('DU_KIEN','DA_XAC_NHAN') AND MaDDC IS NOT NULL AND IdNguoiPhanLoai IS NOT NULL AND ThoiDiemPhanLoai IS NOT NULL)),
    CONSTRAINT RB_DauTaiLieu_8 CHECK (TrangThaiPhanLoai<>'DA_XAC_NHAN' OR (ViTriTraDDC IS NOT NULL AND CHAR_LENGTH(TRIM(ViTriTraDDC))>0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ThanhVienDeTai
(
    IdDauTaiLieu INT NOT NULL,
    IdDocGia INT NOT NULL,
    VaiTro VARCHAR(20) NOT NULL,
    CONSTRAINT RB_ThanhVienDeTai_1 PRIMARY KEY (IdDauTaiLieu,IdDocGia,VaiTro),
    CONSTRAINT RB_ThanhVienDeTai_2 FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu),
    CONSTRAINT RB_ThanhVienDeTai_3 FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia),
    CONSTRAINT RB_ThanhVienDeTai_4 CHECK (VaiTro IN ('THUC_HIEN','HUONG_DAN'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ViTriLuuTru
(
    IdViTri INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    DayKe VARCHAR(30) NOT NULL,
    Ngan VARCHAR(30) NOT NULL,
    CONSTRAINT RB_ViTriLuuTru_1 UNIQUE (DayKe,Ngan)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE BanTaiLieu
(
    IdBanTaiLieu INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDauTaiLieu INT NOT NULL,
    SoDangKyCaBiet VARCHAR(50) NOT NULL UNIQUE,
    MaVach VARCHAR(100) NOT NULL UNIQUE,
    IdViTri INT NULL,
    KyHieuXepGia VARCHAR(100) NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'DANG_XU_LY',
    TinhTrang VARCHAR(20) NOT NULL DEFAULT 'TOT',
    GiaBia DECIMAL(12,0) NULL,
    NgayNhap DATE NULL,
    CONSTRAINT RB_BanTaiLieu_1 UNIQUE (IdBanTaiLieu,IdDauTaiLieu),
    CONSTRAINT RB_BanTaiLieu_2 FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu),
    CONSTRAINT RB_BanTaiLieu_3 FOREIGN KEY (IdViTri) REFERENCES ViTriLuuTru(IdViTri),
    CONSTRAINT RB_BanTaiLieu_4 CHECK (TrangThai IN ('DANG_XU_LY','SAN_SANG','DANG_MUON','THAT_LAC','HU_HONG','MAT','THANH_LY')),
    CONSTRAINT RB_BanTaiLieu_5 CHECK (TinhTrang IN ('TOT','CU','HU_HONG') AND (GiaBia IS NULL OR GiaBia>=0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE PhieuMuon
(
    IdPhieuMuon INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDocGia INT NOT NULL,
    ThoiDiemMuon DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdThuThu INT NOT NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'DANG_MO',
    CONSTRAINT RB_PhieuMuon_1 UNIQUE (IdPhieuMuon,IdDocGia),
    CONSTRAINT RB_PhieuMuon_2 FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia),
    CONSTRAINT RB_PhieuMuon_3 FOREIGN KEY (IdThuThu) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_PhieuMuon_4 CHECK (TrangThai IN ('DANG_MO','DA_DONG','HUY'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ChiTietPhieuMuon
(
    IdChiTietMuon INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdPhieuMuon INT NOT NULL,
    IdDocGia INT NOT NULL,
    IdBanTaiLieu INT NOT NULL,
    IdDauTaiLieu INT NOT NULL,
    HanTra DATETIME NOT NULL,
    ThoiDiemTra DATETIME NULL,
    IdNguoiNhanTra INT NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'DANG_MUON',
    SoLanGiaHan SMALLINT NOT NULL DEFAULT 0,
    DauDangMuon TINYINT GENERATED ALWAYS AS (CASE WHEN TrangThai='DANG_MUON' THEN 1 ELSE NULL END) VIRTUAL,
    CONSTRAINT RB_ChiTietPhieuMuon_1 UNIQUE (IdChiTietMuon,IdDocGia),
    CONSTRAINT RB_ChiTietPhieuMuon_2 UNIQUE (IdBanTaiLieu,DauDangMuon),
    CONSTRAINT RB_ChiTietPhieuMuon_3 UNIQUE (IdDocGia,IdDauTaiLieu,DauDangMuon),
    CONSTRAINT RB_ChiTietPhieuMuon_4 FOREIGN KEY (IdPhieuMuon,IdDocGia) REFERENCES PhieuMuon(IdPhieuMuon,IdDocGia),
    CONSTRAINT RB_ChiTietPhieuMuon_5 FOREIGN KEY (IdBanTaiLieu,IdDauTaiLieu) REFERENCES BanTaiLieu(IdBanTaiLieu,IdDauTaiLieu),
    CONSTRAINT RB_ChiTietPhieuMuon_6 FOREIGN KEY (IdNguoiNhanTra) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_ChiTietPhieuMuon_7 CHECK (TrangThai IN ('DANG_MUON','DA_TRA','MAT','HUY') AND SoLanGiaHan>=0),
    CONSTRAINT RB_ChiTietPhieuMuon_8 CHECK ((TrangThai='DANG_MUON' AND ThoiDiemTra IS NULL) OR (TrangThai='DA_TRA' AND ThoiDiemTra IS NOT NULL AND IdNguoiNhanTra IS NOT NULL) OR TrangThai IN ('MAT','HUY'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE LuotVaoPhongDoc
(
    IdLuotDoc INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDocGia INT NOT NULL,
    ThoiDiemVao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ThoiDiemRa DATETIME NULL,
    IdThuThu INT NOT NULL,
    DaTraThe BOOLEAN NOT NULL DEFAULT 0,
    CONSTRAINT RB_LuotVaoPhongDoc_1 FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia),
    CONSTRAINT RB_LuotVaoPhongDoc_2 FOREIGN KEY (IdThuThu) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_LuotVaoPhongDoc_3 CHECK (DaTraThe IN (0,1) AND (ThoiDiemRa IS NULL OR ThoiDiemRa>=ThoiDiemVao))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE PhieuThu
(
    IdPhieuThu INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDocGia INT NOT NULL,
    ThoiDiemThu DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdNguoiThu INT NOT NULL,
    HinhThuc VARCHAR(20) NOT NULL,
    SoTienThu DECIMAL(12,0) NOT NULL,
    CONSTRAINT RB_PhieuThu_1 UNIQUE (IdPhieuThu,IdDocGia),
    CONSTRAINT RB_PhieuThu_2 FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia),
    CONSTRAINT RB_PhieuThu_3 FOREIGN KEY (IdNguoiThu) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_PhieuThu_4 CHECK (HinhThuc IN ('TIEN_MAT','CHUYEN_KHOAN') AND SoTienThu>0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ViPham
(
    IdViPham INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDocGia INT NOT NULL,
    IdChiTietMuon INT NULL,
    LoaiViPham VARCHAR(25) NOT NULL,
    SoTien DECIMAL(12,0) NOT NULL DEFAULT 0,
    CanCu VARCHAR(1000) NOT NULL,
    ThoiDiemLap DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdNguoiLap INT NOT NULL,
    IdPhieuThu INT NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'CHUA_XU_LY',
    IdNguoiHuy INT NULL,
    LyDoHuy VARCHAR(300) NULL,
    CONSTRAINT RB_ViPham_1 FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia),
    CONSTRAINT RB_ViPham_2 FOREIGN KEY (IdChiTietMuon,IdDocGia) REFERENCES ChiTietPhieuMuon(IdChiTietMuon,IdDocGia),
    CONSTRAINT RB_ViPham_3 FOREIGN KEY (IdNguoiLap) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_ViPham_4 FOREIGN KEY (IdNguoiHuy) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_ViPham_5 FOREIGN KEY (IdPhieuThu,IdDocGia) REFERENCES PhieuThu(IdPhieuThu,IdDocGia),
    CONSTRAINT RB_ViPham_6 CHECK (LoaiViPham IN ('QUA_HAN','MAT_SACH','PHI_XU_LY','HONG_TRANG','THIET_HAI_KHAC') AND SoTien>=0),
    CONSTRAINT RB_ViPham_7 CHECK (TrangThai IN ('CHUA_XU_LY','DA_XU_LY','DA_HUY')),
    CONSTRAINT RB_ViPham_8 CHECK ((TrangThai='CHUA_XU_LY' AND IdPhieuThu IS NULL AND IdNguoiHuy IS NULL AND LyDoHuy IS NULL) OR (TrangThai='DA_XU_LY' AND (SoTien=0 OR IdPhieuThu IS NOT NULL) AND IdNguoiHuy IS NULL AND LyDoHuy IS NULL) OR (TrangThai='DA_HUY' AND IdNguoiHuy IS NOT NULL AND LyDoHuy IS NOT NULL AND CHAR_LENGTH(TRIM(LyDoHuy))>0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE KiemKe
(
    IdKiemKe INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    TenKiemKe VARCHAR(200) NOT NULL,
    ThoiDiemBatDau DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ThoiDiemKetThuc DATETIME NULL,
    IdNguoiLap INT NOT NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'DU_KIEN',
    CONSTRAINT RB_KiemKe_1 FOREIGN KEY (IdNguoiLap) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_KiemKe_2 CHECK (TrangThai IN ('DU_KIEN','DANG_KIEM','HOAN_TAT','HUY')),
    CONSTRAINT RB_KiemKe_3 CHECK (ThoiDiemKetThuc IS NULL OR ThoiDiemKetThuc>=ThoiDiemBatDau)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ChiTietKiemKe
(
    IdChiTietKiemKe INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdKiemKe INT NOT NULL,
    IdBanTaiLieu INT NULL,
    MaVachQuet VARCHAR(100) NULL,
    CoTrongDanhSach BOOLEAN NOT NULL DEFAULT 1,
    IdViTriDuKien INT NULL,
    IdViTriThucTe INT NULL,
    KetQua VARCHAR(20) NOT NULL DEFAULT 'CHUA_KIEM',
    IdNguoiKiemKe INT NULL,
    ThoiDiemKiem DATETIME NULL,
    CONSTRAINT RB_ChiTietKiemKe_1 UNIQUE (IdKiemKe,IdBanTaiLieu),
    CONSTRAINT RB_ChiTietKiemKe_2 UNIQUE (IdKiemKe,MaVachQuet),
    CONSTRAINT RB_ChiTietKiemKe_3 FOREIGN KEY (IdKiemKe) REFERENCES KiemKe(IdKiemKe),
    CONSTRAINT RB_ChiTietKiemKe_4 FOREIGN KEY (IdBanTaiLieu) REFERENCES BanTaiLieu(IdBanTaiLieu),
    CONSTRAINT RB_ChiTietKiemKe_5 FOREIGN KEY (IdViTriDuKien) REFERENCES ViTriLuuTru(IdViTri),
    CONSTRAINT RB_ChiTietKiemKe_6 FOREIGN KEY (IdViTriThucTe) REFERENCES ViTriLuuTru(IdViTri),
    CONSTRAINT RB_ChiTietKiemKe_7 FOREIGN KEY (IdNguoiKiemKe) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_ChiTietKiemKe_8 CHECK (CoTrongDanhSach IN (0,1) AND (IdBanTaiLieu IS NOT NULL OR MaVachQuet IS NOT NULL)),
    CONSTRAINT RB_ChiTietKiemKe_9 CHECK (KetQua IN ('CHUA_KIEM','KHOP','THIEU','THUA','SAI_VI_TRI','HU_HONG')),
    CONSTRAINT RB_ChiTietKiemKe_10 CHECK (CoTrongDanhSach=0 OR IdBanTaiLieu IS NOT NULL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ThanhLy
(
    IdThanhLy INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdBanTaiLieu INT NOT NULL,
    IdKiemKe INT NULL,
    LyDo VARCHAR(500) NOT NULL,
    ThoiDiemDeNghi DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdNguoiDeNghi INT NOT NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'DE_NGHI',
    IdNguoiXuLy INT NULL,
    ThoiDiemXuLy DATETIME NULL,
    DauDeNghi TINYINT GENERATED ALWAYS AS (CASE WHEN TrangThai='DE_NGHI' THEN 1 ELSE NULL END) VIRTUAL,
    CONSTRAINT RB_ThanhLy_1 UNIQUE (IdBanTaiLieu,DauDeNghi),
    CONSTRAINT RB_ThanhLy_2 FOREIGN KEY (IdBanTaiLieu) REFERENCES BanTaiLieu(IdBanTaiLieu),
    CONSTRAINT RB_ThanhLy_3 FOREIGN KEY (IdKiemKe) REFERENCES KiemKe(IdKiemKe),
    CONSTRAINT RB_ThanhLy_4 FOREIGN KEY (IdNguoiDeNghi) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_ThanhLy_5 FOREIGN KEY (IdNguoiXuLy) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_ThanhLy_6 CHECK (TrangThai IN ('DE_NGHI','DA_THANH_LY','TU_CHOI')),
    CONSTRAINT RB_ThanhLy_7 CHECK (TrangThai='DE_NGHI' OR (IdNguoiXuLy IS NOT NULL AND ThoiDiemXuLy IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE PhanHoiDocGia
(
    IdPhanHoi INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDocGia INT NOT NULL,
    LoaiPhanHoi VARCHAR(20) NOT NULL,
    IdDauTaiLieu INT NULL,
    TieuDe VARCHAR(300) NOT NULL,
    NoiDung TEXT NOT NULL,
    Diem TINYINT NULL,
    ThoiDiemGui DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'MOI',
    IdNguoiXuLy INT NULL,
    CONSTRAINT RB_PhanHoiDocGia_1 FOREIGN KEY (IdDocGia) REFERENCES DocGia(IdDocGia),
    CONSTRAINT RB_PhanHoiDocGia_2 FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu),
    CONSTRAINT RB_PhanHoiDocGia_3 FOREIGN KEY (IdNguoiXuLy) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_PhanHoiDocGia_4 CHECK (LoaiPhanHoi IN ('DE_XUAT','GOP_Y','DANH_GIA') AND TrangThai IN ('MOI','DA_XU_LY')),
    CONSTRAINT RB_PhanHoiDocGia_5 CHECK (Diem IS NULL OR Diem BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE NhatKy
(
    IdNhatKy BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdTaiKhoan INT NULL,
    ThoiDiem DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    HanhDong VARCHAR(50) NOT NULL,
    BangDuLieu VARCHAR(50) NOT NULL,
    KhoaBanGhi VARCHAR(100) NOT NULL,
    ChiTiet JSON NOT NULL,
    CONSTRAINT RB_NhatKy_1 FOREIGN KEY (IdTaiKhoan) REFERENCES TaiKhoan(IdTaiKhoan)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE BieuGhiMARC
(
    IdBieuGhiMARC BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    IdDauTaiLieu INT NULL UNIQUE,
    DauBieuMARC VARCHAR(24) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    Truong JSON NOT NULL DEFAULT (JSON_ARRAY()),
    TrangThai VARCHAR(25) NOT NULL DEFAULT 'NHAP',
    PhienBan BIGINT UNSIGNED NOT NULL DEFAULT 1,
    PhienBanDauTaiLieu BIGINT UNSIGNED NULL,
    IdNguoiTao INT NOT NULL,
    ThoiDiemTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdNguoiKiemTra INT NULL,
    ThoiDiemKiemTra DATETIME NULL,
    CONSTRAINT RB_BieuGhiMARC_1 FOREIGN KEY (IdDauTaiLieu) REFERENCES DauTaiLieu(IdDauTaiLieu),
    CONSTRAINT RB_BieuGhiMARC_2 FOREIGN KEY (IdNguoiTao) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_BieuGhiMARC_3 FOREIGN KEY (IdNguoiKiemTra) REFERENCES TaiKhoan(IdTaiKhoan),
    CONSTRAINT RB_BieuGhiMARC_4 CHECK (REGEXP_LIKE(DauBieuMARC,'^[ -~]{24}$','c') AND REGEXP_LIKE(SUBSTRING(DauBieuMARC,1,5),'^[0-9]{5}$','c') AND REGEXP_LIKE(SUBSTRING(DauBieuMARC,13,5),'^[0-9]{5}$','c') AND SUBSTRING(DauBieuMARC,11,2)='22' AND SUBSTRING(DauBieuMARC,21,4)='4500'),
    CONSTRAINT RB_BieuGhiMARC_5 CHECK (TrangThai IN ('NHAP','DA_KIEM_TRA') AND PhienBan>0),
    CONSTRAINT RB_BieuGhiMARC_6 CHECK ((TrangThai='NHAP' AND IdNguoiKiemTra IS NULL AND ThoiDiemKiemTra IS NULL AND PhienBanDauTaiLieu IS NULL) OR (TrangThai='DA_KIEM_TRA' AND IdNguoiKiemTra IS NOT NULL AND ThoiDiemKiemTra IS NOT NULL AND (IdDauTaiLieu IS NULL OR PhienBanDauTaiLieu IS NOT NULL)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE DocGia ADD CONSTRAINT FK_DocGia_NguoiDoiQuyen FOREIGN KEY (IdNguoiDoiQuyen) REFERENCES TaiKhoan(IdTaiKhoan);
CREATE INDEX IX_DocGia_HoTen ON DocGia(HoTen);
CREATE INDEX IX_DauTaiLieu_NhanDe ON DauTaiLieu(NhanDe);
CREATE INDEX IX_Muon_HanTra ON ChiTietPhieuMuon(TrangThai,HanTra);

/* Danh muc khoi tao; khong co tai khoan hay dau sach gia. */
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_KIEN_TRUC','Khoa Kiến trúc');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_TK_NGHE_THUAT','Khoa Thiết kế & Nghệ thuật');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_XAY_DUNG','Khoa Xây dựng');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_CAU_DUONG','Khoa Cầu đường');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_CNTT','Khoa Công nghệ Thông tin');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_DIEN_DIEN_TU','Khoa Kỹ thuật Điện - Điện tử');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_KINH_TE','Khoa Kinh tế');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_NGOAI_NGU','Khoa Ngoại ngữ');
INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES('KHOA_DU_LICH','Khoa Du lịch');
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7580101',IdKhoa,'Kiến trúc' FROM Khoa WHERE MaKhoa='KHOA_KIEN_TRUC';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7580105',IdKhoa,'Quy hoạch vùng và đô thị' FROM Khoa WHERE MaKhoa='KHOA_KIEN_TRUC';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7580108',IdKhoa,'Thiết kế nội thất' FROM Khoa WHERE MaKhoa='KHOA_KIEN_TRUC';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7210403',IdKhoa,'Thiết kế đồ họa' FROM Khoa WHERE MaKhoa='KHOA_TK_NGHE_THUAT';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7580201',IdKhoa,'Kỹ thuật xây dựng' FROM Khoa WHERE MaKhoa='KHOA_XAY_DUNG';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7580302',IdKhoa,'Quản lý xây dựng' FROM Khoa WHERE MaKhoa='KHOA_XAY_DUNG';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7580205',IdKhoa,'Kỹ thuật xây dựng công trình giao thông' FROM Khoa WHERE MaKhoa='KHOA_CAU_DUONG';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7580210',IdKhoa,'Kỹ thuật cơ sở hạ tầng' FROM Khoa WHERE MaKhoa='KHOA_CAU_DUONG';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7480201',IdKhoa,'Công nghệ thông tin' FROM Khoa WHERE MaKhoa='KHOA_CNTT';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7510301',IdKhoa,'Công nghệ kỹ thuật Điện – Điện tử' FROM Khoa WHERE MaKhoa='KHOA_DIEN_DIEN_TU';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7340101',IdKhoa,'Quản trị kinh doanh' FROM Khoa WHERE MaKhoa='KHOA_KINH_TE';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7340201',IdKhoa,'Tài chính – Ngân hàng' FROM Khoa WHERE MaKhoa='KHOA_KINH_TE';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7340301',IdKhoa,'Kế toán' FROM Khoa WHERE MaKhoa='KHOA_KINH_TE';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7510605',IdKhoa,'Logistics và Quản lý chuỗi cung ứng' FROM Khoa WHERE MaKhoa='KHOA_KINH_TE';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7220201',IdKhoa,'Ngôn ngữ Anh' FROM Khoa WHERE MaKhoa='KHOA_NGOAI_NGU';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7220204',IdKhoa,'Ngôn ngữ Trung Quốc' FROM Khoa WHERE MaKhoa='KHOA_NGOAI_NGU';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7810103',IdKhoa,'Quản trị dịch vụ du lịch và lữ hành' FROM Khoa WHERE MaKhoa='KHOA_DU_LICH';
INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT '7810201',IdKhoa,'Quản trị khách sạn' FROM Khoa WHERE MaKhoa='KHOA_DU_LICH';
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('000',NULL,'Tin học, thông tin, tổng quát','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('100',NULL,'Triết học, tâm lý','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('200',NULL,'Tôn giáo','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('300',NULL,'Xã hội','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('400',NULL,'Ngôn ngữ','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('500',NULL,'Khoa học','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('600',NULL,'Công nghệ','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('700',NULL,'Nghệ thuật, giải trí','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('800',NULL,'Văn học','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('900',NULL,'Lịch sử, địa lý','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=6');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('330','300','Kinh tế','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('380','300','Thương mại, vận tải','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('420','400','Tiếng Anh','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('490','400','Ngôn ngữ khác','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('620','600','Kỹ thuật','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('640','600','Gia đình','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('650','600','Quản trị','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('690','600','Xây dựng nhà','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('710','700','Quy hoạch, cảnh quan','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('720','700','Kiến trúc','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('740','700','Thiết kế, nghệ thuật','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('910','900','Địa lý, du hành','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=7');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('004','000','Tin học','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=8');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('005','000','Lập trình, dữ liệu','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=8');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('006','000','Phương pháp tin học','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=8');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('332','330','Tài chính','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=11');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('388','380','Vận tải','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=11');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('495','490','Ngôn ngữ Đông/Đông Nam Á','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=12');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('621','620','Vật lý ứng dụng','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('624','620','Công trình dân dụng','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('625','620','Đường bộ, đường sắt','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('627','620','Thủy công','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('628','620','Môi trường, vệ sinh','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('641','640','Ăn uống','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('647','640','Quản lý cơ sở phục vụ chung','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('657','650','Kế toán','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('658','650','Quản lý chung','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=14');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('711','710','Quy hoạch khu vực','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=15');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('741','740','Vẽ','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=15');
INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES('747','740','Trang trí nội thất','https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page=15');

DELIMITER $$
CREATE PROCEDURE KiemTraQuanLy(IN pNguoi INT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM TaiKhoan t
        WHERE t.IdTaiKhoan=pNguoi AND t.DangHoatDong=1 AND t.VaiTro='QUAN_LY_THU_VIEN') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Chi Quan ly thu vien duoc thuc hien thao tac nay.';
    END IF;
END$$
CREATE TRIGGER DocGia_ChanXoa BEFORE DELETE ON DocGia FOR EACH ROW
BEGIN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong xoa doc gia; hay khoa quyen hoac ngung hoat dong.'; END$$
CREATE TRIGGER DocGia_KiemTraThem BEFORE INSERT ON DocGia FOR EACH ROW
BEGIN IF NEW.DuocMuon=0 THEN CALL KiemTraQuanLy(NEW.IdNguoiDoiQuyen); END IF; END$$
CREATE TRIGGER DocGia_KiemTraSua BEFORE UPDATE ON DocGia FOR EACH ROW
BEGIN
    IF NEW.DuocMuon<>OLD.DuocMuon OR NOT(NEW.KhoaDen <=> OLD.KhoaDen) OR NOT(NEW.LyDoKhoa <=> OLD.LyDoKhoa) THEN
        CALL KiemTraQuanLy(NEW.IdNguoiDoiQuyen);
    END IF;
END$$
CREATE TRIGGER DocGia_LuuDoiQuyen AFTER UPDATE ON DocGia FOR EACH ROW
BEGIN
    IF NEW.DuocMuon<>OLD.DuocMuon OR NOT(NEW.KhoaDen <=> OLD.KhoaDen) OR NOT(NEW.LyDoKhoa <=> OLD.LyDoKhoa) THEN
        INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet)
        VALUES(NEW.IdNguoiDoiQuyen,'DOI_QUYEN_MUON','DocGia',NEW.IdDocGia,
            JSON_OBJECT('QuyenCu',OLD.DuocMuon,'QuyenMoi',NEW.DuocMuon,'KhoaDen',NEW.KhoaDen,'LyDo',NEW.LyDoKhoa));
    END IF;
END$$
CREATE TRIGGER DDC_KiemTraThem BEFORE INSERT ON MucPhanLoaiDDC FOR EACH ROW
BEGIN IF NEW.MaCha=NEW.MaDDC THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Muc DDC khong the la cha cua chinh no.'; END IF; END$$
CREATE TRIGGER DDC_KiemTraSua BEFORE UPDATE ON MucPhanLoaiDDC FOR EACH ROW
BEGIN
    DECLARE vDem INT DEFAULT 0;
    IF NOT(NEW.MaCha <=> OLD.MaCha) AND NEW.MaCha IS NOT NULL THEN
        WITH RECURSIVE ToTien AS (
            SELECT MaDDC,MaCha FROM MucPhanLoaiDDC WHERE MaDDC=NEW.MaCha
            UNION ALL SELECT m.MaDDC,m.MaCha FROM MucPhanLoaiDDC m JOIN ToTien t ON m.MaDDC=t.MaCha
        ) SELECT COUNT(*) INTO vDem FROM ToTien WHERE MaDDC=OLD.MaDDC;
        IF vDem>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong duoc tao vong lap cay DDC.'; END IF;
    END IF;
    IF NEW.DaDoiChieu=0 AND EXISTS (SELECT 1 FROM DauTaiLieu WHERE MaDDC=OLD.MaDDC AND TrangThaiPhanLoai='DA_XAC_NHAN') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Muc DDC dang dung cho tai lieu da xac nhan.';
    END IF;
END$$
CREATE PROCEDURE KiemTraPhanLoai(IN pMa VARCHAR(64),IN pTrangThai VARCHAR(20))
BEGIN
    IF pTrangThai='DA_XAC_NHAN' AND NOT EXISTS(SELECT 1 FROM MucPhanLoaiDDC WHERE MaDDC=pMa AND DaDoiChieu=1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Can doi chieu muc DDC voi bo ban in truoc khi xac nhan tai lieu.';
    END IF;
END$$
CREATE TRIGGER DauTaiLieu_Them BEFORE INSERT ON DauTaiLieu FOR EACH ROW
BEGIN CALL KiemTraPhanLoai(NEW.MaDDC,NEW.TrangThaiPhanLoai); END$$
CREATE TRIGGER DauTaiLieu_Sua BEFORE UPDATE ON DauTaiLieu FOR EACH ROW
BEGIN
    CALL KiemTraPhanLoai(NEW.MaDDC,NEW.TrangThaiPhanLoai);
    IF OLD.TrangThaiPhanLoai='DA_XAC_NHAN' AND NEW.TrangThaiPhanLoai='DA_XAC_NHAN'
       AND (NOT(OLD.MaDDC <=> NEW.MaDDC) OR NOT(OLD.ViTriTraDDC <=> NEW.ViTriTraDDC))
       AND (OLD.ThoiDiemPhanLoai <=> NEW.ThoiDiemPhanLoai) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Doi ma/nguon DDC da xac nhan can xac nhan lai hoac ve du kien.';
    END IF;
    SET NEW.PhienBan=OLD.PhienBan+1;
END$$
CREATE TRIGGER DauTaiLieu_LuuPhanLoai AFTER UPDATE ON DauTaiLieu FOR EACH ROW
BEGIN
    IF NOT(NEW.MaDDC <=> OLD.MaDDC) OR NEW.TrangThaiPhanLoai<>OLD.TrangThaiPhanLoai OR NOT(NEW.ViTriTraDDC <=> OLD.ViTriTraDDC) THEN
        INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet)
        VALUES(COALESCE(NEW.IdNguoiPhanLoai,OLD.IdNguoiPhanLoai),'DOI_PHAN_LOAI','DauTaiLieu',NEW.IdDauTaiLieu,
            JSON_OBJECT('MaCu',OLD.MaDDC,'MaMoi',NEW.MaDDC,'TrangThaiCu',OLD.TrangThaiPhanLoai,'TrangThaiMoi',NEW.TrangThaiPhanLoai,
                        'ViTriCu',OLD.ViTriTraDDC,'ViTriMoi',NEW.ViTriTraDDC));
    END IF;
END$$
CREATE TRIGGER ViPham_ChanXoa BEFORE DELETE ON ViPham FOR EACH ROW
BEGIN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong xoa vat ly vi pham; Quan ly thu vien huy hieu luc va luu ly do.'; END$$
CREATE PROCEDURE KiemTraViPham(IN pDocGia INT,IN pLoai VARCHAR(25),IN pTien DECIMAL(12,0),IN pTrangThai VARCHAR(20),IN pNguoiHuy INT)
BEGIN
    IF pLoai='QUA_HAN' AND pTien>0 AND EXISTS(SELECT 1 FROM DocGia WHERE IdDocGia=pDocGia AND LoaiDocGia<>'SINH_VIEN') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Can bo qua han chi thong bao don vi; khong tu ap phat tien.';
    END IF;
    IF pTrangThai='DA_HUY' THEN CALL KiemTraQuanLy(pNguoiHuy); END IF;
END$$
CREATE TRIGGER ViPham_Them BEFORE INSERT ON ViPham FOR EACH ROW
BEGIN CALL KiemTraViPham(NEW.IdDocGia,NEW.LoaiViPham,NEW.SoTien,NEW.TrangThai,NEW.IdNguoiHuy); END$$
CREATE TRIGGER ViPham_Sua BEFORE UPDATE ON ViPham FOR EACH ROW
BEGIN
    IF OLD.TrangThai='DA_HUY' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khoan da huy duoc giu nguyen lich su.'; END IF;
    CALL KiemTraViPham(NEW.IdDocGia,NEW.LoaiViPham,NEW.SoTien,NEW.TrangThai,NEW.IdNguoiHuy);
    IF OLD.IdPhieuThu IS NOT NULL AND (NOT(OLD.IdPhieuThu <=> NEW.IdPhieuThu) OR OLD.SoTien<>NEW.SoTien OR OLD.IdDocGia<>NEW.IdDocGia) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong doi so tien/doc gia/phieu thu cua khoan da thu.';
    END IF;
END$$
CREATE TRIGGER ViPham_LuuHuy AFTER UPDATE ON ViPham FOR EACH ROW
BEGIN
    IF NEW.TrangThai='DA_HUY' AND OLD.TrangThai<>'DA_HUY' THEN
        INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet)
        VALUES(NEW.IdNguoiHuy,'HUY_VI_PHAM','ViPham',NEW.IdViPham,JSON_OBJECT('LyDo',NEW.LyDoHuy,'SoTien',NEW.SoTien,'IdPhieuThu',NEW.IdPhieuThu));
    END IF;
END$$
CREATE PROCEDURE KiemTraDuocMuon(IN pDocGia INT)
BEGIN
    DECLARE vToiDa INT;
    DECLARE vDangMuon INT;
    IF NOT EXISTS(SELECT 1 FROM DocGia WHERE IdDocGia=pDocGia AND TrangThaiHoSo='HOAT_DONG' AND DuocMuon=1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Ho so/nguyen tac quyen muon chua cho phep muon.';
    END IF;
    IF EXISTS(SELECT 1 FROM ViPham WHERE IdDocGia=pDocGia AND SoTien>0 AND TrangThai='CHUA_XU_LY')
       OR EXISTS(SELECT 1 FROM ChiTietPhieuMuon WHERE IdDocGia=pDocGia AND TrangThai='DANG_MUON' AND HanTra<CURRENT_TIMESTAMP) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Can tra sach qua han va thanh toan phat truoc khi muon tiep.';
    END IF;
    SELECT CASE WHEN LoaiDocGia='SINH_VIEN' THEN 2 ELSE 3 END INTO vToiDa FROM DocGia WHERE IdDocGia=pDocGia;
    SELECT COUNT(*) INTO vDangMuon FROM ChiTietPhieuMuon WHERE IdDocGia=pDocGia AND TrangThai='DANG_MUON';
    IF vDangMuon>=vToiDa THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Da dat han muc muon.'; END IF;
END$$
CREATE PROCEDURE ThuTienPhat(IN pDocGia INT,IN pNguoi INT,IN pHinhThuc VARCHAR(20))
BEGIN
    DECLARE vTong DECIMAL(12,0); DECLARE vPhieu INT;
    IF NOT EXISTS(SELECT 1 FROM TaiKhoan WHERE IdTaiKhoan=pNguoi AND DangHoatDong=1 AND VaiTro IN ('THU_THU','QUAN_LY_THU_VIEN')) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Tai khoan khong co quyen thu tien.';
    END IF;
    -- Ung dung mo giao dich va khoa DocGia truoc khi goi; khong ho tro tra gop.
    SELECT SUM(SoTien) INTO vTong FROM ViPham WHERE IdDocGia=pDocGia AND TrangThai='CHUA_XU_LY' AND SoTien>0 FOR UPDATE;
    IF vTong IS NULL OR vTong<=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong co khoan phat can thu.'; END IF;
    INSERT INTO PhieuThu(IdDocGia,IdNguoiThu,HinhThuc,SoTienThu) VALUES(pDocGia,pNguoi,pHinhThuc,vTong);
    SET vPhieu=LAST_INSERT_ID();
    UPDATE ViPham SET IdPhieuThu=vPhieu,TrangThai='DA_XU_LY' WHERE IdDocGia=pDocGia AND TrangThai='CHUA_XU_LY' AND SoTien>0;
    INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet)
    VALUES(pNguoi,'THU_TIEN','PhieuThu',vPhieu,JSON_OBJECT('IdDocGia',pDocGia,'SoTien',vTong));
END$$
CREATE PROCEDURE GiaHanMuon(IN pChiTiet INT,IN pHanMoi DATETIME,IN pNguoi INT)
BEGIN
    DECLARE vDau INT; DECLARE vDocGia INT; DECLARE vHanCu DATETIME;
    SELECT IdDauTaiLieu,IdDocGia,HanTra INTO vDau,vDocGia,vHanCu FROM ChiTietPhieuMuon WHERE IdChiTietMuon=pChiTiet AND TrangThai='DANG_MUON' FOR UPDATE;
    IF vDau IS NULL OR pHanMoi IS NULL OR pHanMoi<=vHanCu THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Gia han can khoan dang muon va han moi lon hon han cu.'; END IF;
    IF NOT EXISTS(SELECT 1 FROM TaiKhoan WHERE IdTaiKhoan=pNguoi AND DangHoatDong=1 AND VaiTro IN ('THU_THU','QUAN_LY_THU_VIEN')) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Tai khoan xu ly khong hop le.'; END IF;
    IF (SELECT COUNT(*) FROM ChiTietPhieuMuon WHERE IdDocGia=vDocGia AND TrangThai='DANG_MUON')>
       (SELECT CASE WHEN LoaiDocGia='SINH_VIEN' THEN 2 ELSE 3 END FROM DocGia WHERE IdDocGia=vDocGia) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='So luong sach vuot han muc.';
    END IF;
    UPDATE ChiTietPhieuMuon SET HanTra=pHanMoi,SoLanGiaHan=SoLanGiaHan+1 WHERE IdChiTietMuon=pChiTiet;
    INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet)
    VALUES(pNguoi,'GIA_HAN','ChiTietPhieuMuon',pChiTiet,JSON_OBJECT('HanCu',vHanCu,'HanMoi',pHanMoi));
END$$

-- BieuGhiMARC.Truong bao toan thu tu bang mang. Kiem tra cau truc ca khi con nhap.
CREATE PROCEDURE KiemTraCauTrucMARC(IN pTruong JSON)
BEGIN
    IF pTruong IS NULL OR NOT JSON_SCHEMA_VALID('{
        "type": "array",
        "items": {
            "oneOf": [
                {
                    "type": "object",
                    "required": ["NhanTruong", "NoiDungDieuKhien"],
                    "additionalProperties": false,
                    "properties": {
                        "NhanTruong": {"type": "string", "minLength": 3, "maxLength": 3, "pattern": "^00[1-9]$"},
                        "NoiDungDieuKhien": {"type": "string"}
                    }
                },
                {
                    "type": "object",
                    "required": ["NhanTruong", "ChiThi1", "ChiThi2", "TruongCon"],
                    "additionalProperties": false,
                    "properties": {
                        "NhanTruong": {"type": "string", "minLength": 3, "maxLength": 3, "pattern": "^(0[1-9][0-9]|[1-9][0-9]{2})$"},
                        "ChiThi1": {"type": "string", "minLength": 1, "maxLength": 1, "pattern": "^[0-9a-z ]$"},
                        "ChiThi2": {"type": "string", "minLength": 1, "maxLength": 1, "pattern": "^[0-9a-z ]$"},
                        "TruongCon": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "required": ["MaTruongCon", "NoiDung"],
                                "additionalProperties": false,
                                "properties": {
                                    "MaTruongCon": {"type": "string", "minLength": 1, "maxLength": 1, "pattern": "^[0-9a-z]$"},
                                    "NoiDung": {"type": "string"}
                                }
                            }
                        }
                    }
                }
            ]
        }
    }',pTruong) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Cau truc truong MARC khong hop le.';
    END IF;
    IF EXISTS (
        SELECT 1 FROM JSON_TABLE(pTruong,'$[*]' COLUMNS (
            NhanTruong VARCHAR(3) PATH '$.NhanTruong',
            NoiDungDieuKhien LONGTEXT PATH '$.NoiDungDieuKhien',
            NESTED PATH '$.TruongCon[*]' COLUMNS (NoiDung LONGTEXT PATH '$.NoiDung')
        )) AS t
        WHERE (NhanTruong='005' AND NOT REGEXP_LIKE(NoiDungDieuKhien,'^[0-9]{14}[.][0-9]$','c'))
           OR (NhanTruong='006' AND NOT REGEXP_LIKE(NoiDungDieuKhien,'^[ -~]{18}$','c'))
           OR (NhanTruong='007' AND NOT REGEXP_LIKE(NoiDungDieuKhien,'^[ -~]+$','c'))
           OR (NhanTruong='008' AND NOT REGEXP_LIKE(NoiDungDieuKhien,'^[ -~]{40}$','c'))
           OR LOCATE(CHAR(29 USING binary),BINARY COALESCE(NoiDungDieuKhien,NoiDung))>0
           OR LOCATE(CHAR(30 USING binary),BINARY COALESCE(NoiDungDieuKhien,NoiDung))>0
           OR LOCATE(CHAR(31 USING binary),BINARY COALESCE(NoiDungDieuKhien,NoiDung))>0
    ) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Noi dung truong MARC co do dai hoac ky tu khong hop le.';
    END IF;
END$$

CREATE PROCEDURE KiemTraNoiDungMARC(IN pTruong JSON,IN pDauBieu VARCHAR(24),IN pDau INT)
BEGIN
    DECLARE vLoi VARCHAR(128);
    DECLARE vMa VARCHAR(64);
    SET vMa=(SELECT MaDDC FROM DauTaiLieu WHERE IdDauTaiLieu=pDau AND TrangThaiPhanLoai='DA_XAC_NHAN');

    WITH TruongDaTach AS (
        SELECT t.* FROM JSON_TABLE(pTruong,'$[*]' COLUMNS (
            ThuTuTruong FOR ORDINALITY,
            NhanTruong VARCHAR(3) PATH '$.NhanTruong',
            ChiThi1 VARCHAR(1) PATH '$.ChiThi1',
            ChiThi2 VARCHAR(1) PATH '$.ChiThi2',
            NoiDungDieuKhien LONGTEXT PATH '$.NoiDungDieuKhien',
            NESTED PATH '$.TruongCon[*]' COLUMNS (
                ThuTuTruongCon FOR ORDINALITY,
                MaTruongCon VARCHAR(1) PATH '$.MaTruongCon',
                NoiDung LONGTEXT PATH '$.NoiDung'
            )
        )) AS t
    )
    SELECT CASE
        WHEN (SELECT COUNT(DISTINCT NhanTruong) FROM TruongDaTach WHERE NhanTruong IN ('001','005','008','245'))<>4
            THEN 'Ho so DAU co ban can 001,005,008,245.'
        WHEN (SELECT COUNT(*) FROM TruongDaTach WHERE NhanTruong='245' AND MaTruongCon='a' AND CHAR_LENGTH(TRIM(NoiDung))>0)<>1
            THEN 'Can dung mot 245$a khong rong.'
        WHEN EXISTS (
            SELECT NhanTruong FROM TruongDaTach WHERE NhanTruong IN ('001','003','005','008','040','100','110','111','130','245')
            GROUP BY NhanTruong HAVING COUNT(DISTINCT ThuTuTruong)>1
        ) THEN 'Truong MARC khong lap dang bi lap.'
        WHEN EXISTS (
            SELECT t.ThuTuTruong FROM TruongDaTach t WHERE ((t.NhanTruong='020' AND t.MaTruongCon='a') OR (t.NhanTruong='082' AND t.MaTruongCon='2') OR (t.NhanTruong='100' AND t.MaTruongCon='a') OR (t.NhanTruong='245' AND t.MaTruongCon='a') OR (t.NhanTruong='245' AND t.MaTruongCon='b') OR (t.NhanTruong='245' AND t.MaTruongCon='c') OR (t.NhanTruong='250' AND t.MaTruongCon='a') OR (t.NhanTruong='250' AND t.MaTruongCon='b') OR (t.NhanTruong='700' AND t.MaTruongCon='a') OR (t.NhanTruong='650' AND t.MaTruongCon='a'))
            GROUP BY t.ThuTuTruong,t.MaTruongCon HAVING COUNT(*)>1
        ) THEN 'Truong con MARC khong lap dang bi lap.'
        WHEN (SELECT COUNT(DISTINCT ThuTuTruong) FROM TruongDaTach WHERE NhanTruong IN ('100','110','111','130'))>1
            THEN 'Chi mot tieu de chinh 100/110/111/130.'
        WHEN EXISTS (SELECT 1 FROM TruongDaTach WHERE NhanTruong>='010' AND ThuTuTruongCon IS NULL)
            THEN 'Truong du lieu phai co truong con.'
        WHEN EXISTS (SELECT 1 FROM TruongDaTach WHERE NhanTruong='001' AND CHAR_LENGTH(TRIM(NoiDungDieuKhien))=0)
            THEN '001 khong duoc rong.'
        WHEN EXISTS (
            SELECT 1 FROM TruongDaTach WHERE
               (NhanTruong IN ('020','040','250','300') AND (BINARY ChiThi1<>BINARY ' ' OR BINARY ChiThi2<>BINARY ' '))
            OR (NhanTruong='245' AND (ChiThi1 NOT IN ('0','1') OR NOT REGEXP_LIKE(ChiThi2,'^[0-9]$','c')))
            OR (NhanTruong='082' AND (ChiThi1 NOT IN ('0','1','7') OR ChiThi2 NOT IN (' ','0','4')))
            OR (NhanTruong='264' AND (ChiThi1 NOT IN (' ','2','3') OR ChiThi2 NOT IN ('0','1','2','3','4')))
        ) THEN 'Chi thi MARC chua phu hop ho so kiem tra.'
        WHEN BINARY SUBSTRING(pDauBieu,6,1) NOT IN ('a','c','d','n','p') OR BINARY SUBSTRING(pDauBieu,10,1) NOT IN (' ','a')
            THEN 'Can kiem tra trang thai/bo ma cua dau bieu.'
        WHEN vMa IS NOT NULL AND NOT EXISTS (
            SELECT ThuTuTruong FROM TruongDaTach WHERE NhanTruong='082' AND ChiThi1='1'
            GROUP BY ThuTuTruong
            HAVING MAX(MaTruongCon='a' AND BINARY REPLACE(NoiDung,'/','')=BINARY vMa)=1
               AND MAX(MaTruongCon='2' AND (NoiDung='23' OR NoiDung LIKE '23/%'))=1
        ) THEN '082 can chi thi 1=1 (rut gon), $a/$2 khop DDC du an.'
    END INTO vLoi;

    IF vLoi IS NOT NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT=vLoi; END IF;
END$$

CREATE TRIGGER MARC_Them BEFORE INSERT ON BieuGhiMARC FOR EACH ROW
BEGIN
    IF NEW.TrangThai<>'NHAP' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Bieu ghi moi phai o trang thai nhap.'; END IF;
    CALL KiemTraCauTrucMARC(NEW.Truong);
END$$
CREATE TRIGGER MARC_Sua BEFORE UPDATE ON BieuGhiMARC FOR EACH ROW
BEGIN
    SET NEW.PhienBan=OLD.PhienBan+1;
    IF NOT (NEW.Truong <=> OLD.Truong) THEN CALL KiemTraCauTrucMARC(NEW.Truong); END IF;
    IF NOT (NEW.Truong <=> OLD.Truong)
       OR NOT (BINARY NEW.DauBieuMARC <=> BINARY OLD.DauBieuMARC)
       OR NOT (NEW.IdDauTaiLieu <=> OLD.IdDauTaiLieu) THEN
        SET NEW.TrangThai='NHAP';
        SET NEW.IdNguoiKiemTra=NULL, NEW.ThoiDiemKiemTra=NULL, NEW.PhienBanDauTaiLieu=NULL;
    END IF;
    IF NEW.TrangThai='DA_KIEM_TRA' THEN
        CALL KiemTraNoiDungMARC(NEW.Truong,NEW.DauBieuMARC,NEW.IdDauTaiLieu);
        SET NEW.PhienBanDauTaiLieu=(SELECT PhienBan FROM DauTaiLieu WHERE IdDauTaiLieu=NEW.IdDauTaiLieu);
    END IF;
END$$
CREATE PROCEDURE KiemTraBieuGhiMARC(IN pId BIGINT,IN pNguoi INT)
BEGIN
    IF NOT EXISTS(SELECT 1 FROM BieuGhiMARC WHERE IdBieuGhiMARC=pId) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong tim thay bieu ghi.'; END IF;
    UPDATE BieuGhiMARC SET TrangThai='DA_KIEM_TRA',IdNguoiKiemTra=pNguoi,ThoiDiemKiemTra=CURRENT_TIMESTAMP WHERE IdBieuGhiMARC=pId;
END$$
DELIMITER ;

CREATE VIEW vw_NganhTheoKhoa AS SELECT n.MaNganh,n.TenNganh,d.TenKhoa AS TenKhoa FROM NganhDaoTao n JOIN Khoa d ON d.IdKhoa=n.IdKhoa;
CREATE VIEW vw_NoiDungMARC AS
SELECT b.IdBieuGhiMARC,t.*
FROM BieuGhiMARC b
CROSS JOIN JSON_TABLE(b.Truong,'$[*]' COLUMNS (
    ThuTuTruong FOR ORDINALITY,
    NhanTruong VARCHAR(3) PATH '$.NhanTruong',
    ChiThi1 VARCHAR(1) PATH '$.ChiThi1',
    ChiThi2 VARCHAR(1) PATH '$.ChiThi2',
    NoiDungDieuKhien LONGTEXT PATH '$.NoiDungDieuKhien',
    NESTED PATH '$.TruongCon[*]' COLUMNS (
        ThuTuTruongCon FOR ORDINALITY,
        MaTruongCon VARCHAR(1) PATH '$.MaTruongCon',
        NoiDung LONGTEXT PATH '$.NoiDung'
    )
)) AS t;
CREATE VIEW vw_ISBNTaiLieu AS
SELECT b.IdDauTaiLieu,t.NoiDung AS ISBN
FROM BieuGhiMARC b JOIN vw_NoiDungMARC t USING(IdBieuGhiMARC)
WHERE t.NhanTruong='020' AND t.MaTruongCon='a';
CREATE VIEW vw_TinhTrangMARC AS SELECT b.IdBieuGhiMARC,b.IdDauTaiLieu,b.TrangThai,b.PhienBan,CASE WHEN b.IdDauTaiLieu IS NULL THEN 'CHUA_GAN' WHEN b.TrangThai='NHAP' THEN 'CHUA_KIEM_TRA' WHEN b.PhienBanDauTaiLieu<>d.PhienBan THEN 'CAN_DOI_CHIEU_LAI' ELSE 'CUNG_PHIEN_BAN' END AS TinhTrangDoiChieu FROM BieuGhiMARC b LEFT JOIN DauTaiLieu d USING(IdDauTaiLieu);
CREATE VIEW vw_TruongMARCChuaDinhNghia AS
SELECT DISTINCT IdBieuGhiMARC,ThuTuTruong,NhanTruong
FROM vw_NoiDungMARC WHERE NhanTruong NOT IN ('001','003','005','006','007','008','020','040','041','082','100','110','111','130','245','246','250','260','264','300','500','504','520','650','700','710','856');
CREATE VIEW vw_SachSanSang AS SELECT d.IdDauTaiLieu,d.NhanDe,COUNT(b.IdBanTaiLieu) AS TongSoBan,SUM(CASE WHEN b.TrangThai='SAN_SANG' THEN 1 ELSE 0 END) AS SoBanSanSang FROM DauTaiLieu d LEFT JOIN BanTaiLieu b USING(IdDauTaiLieu) GROUP BY d.IdDauTaiLieu,d.NhanDe;
CREATE VIEW vw_DangMuon AS SELECT c.IdChiTietMuon,c.IdDocGia,d.HoTen,c.IdBanTaiLieu,t.NhanDe,c.HanTra FROM ChiTietPhieuMuon c JOIN DocGia d USING(IdDocGia) JOIN DauTaiLieu t USING(IdDauTaiLieu) WHERE c.TrangThai='DANG_MUON';
CREATE VIEW vw_QuaHan AS SELECT * FROM vw_DangMuon WHERE HanTra<CURRENT_TIMESTAMP;
CREATE VIEW vw_CongNo AS SELECT d.IdDocGia,d.HoTen,COALESCE(SUM(CASE WHEN v.TrangThai='CHUA_XU_LY' THEN v.SoTien ELSE 0 END),0) AS SoTienConNo FROM DocGia d LEFT JOIN ViPham v USING(IdDocGia) GROUP BY d.IdDocGia,d.HoTen;
CREATE VIEW vw_KetQuaKiemKe AS SELECT k.IdKiemKe,k.TenKiemKe,COUNT(c.IdChiTietKiemKe) AS SoDong,SUM(CASE WHEN c.CoTrongDanhSach=1 THEN 1 ELSE 0 END) AS SoBanDuKien,SUM(CASE WHEN c.KetQua='THIEU' THEN 1 ELSE 0 END) AS SoBanThieu,SUM(CASE WHEN c.CoTrongDanhSach=0 THEN 1 ELSE 0 END) AS SoBanNgoaiDanhSach FROM KiemKe k LEFT JOIN ChiTietKiemKe c USING(IdKiemKe) GROUP BY k.IdKiemKe,k.TenKiemKe;
