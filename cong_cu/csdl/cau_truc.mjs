// Mô hình gọn cho đồ án. Mỗi dòng là một cột; ràng buộc liên bảng được khai báo rõ.
const t=(ten,cot,rb=[])=>({ten,cot:cot.trim().split('\n').map(s=>s.trim()),rb});
const fk=(cot,bang,den=cot)=>`FOREIGN KEY (${cot}) REFERENCES ${bang}(${den})`;
const ck=s=>`CHECK (${s})`;
const cauTrucDanhSachTep = JSON.stringify({
  type: 'array',
  items: {
    type: 'object',
    required: ['TenTep', 'DuongDan'],
    additionalProperties: false,
    properties: {
      TenTep: { type: 'string', maxLength: 255 },
      DuongDan: { type: 'string', maxLength: 1000 }
    }
  }
});
export const bang=[
t('Khoa',`IdKhoa INT NOT NULL AUTO_INCREMENT PRIMARY KEY
MaKhoa VARCHAR(40) NOT NULL UNIQUE
TenKhoa VARCHAR(200) NOT NULL`),
t('NganhDaoTao',`MaNganh CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NOT NULL PRIMARY KEY
IdKhoa INT NOT NULL
TenNganh VARCHAR(200) NOT NULL`,[fk('IdKhoa','Khoa','IdKhoa'),ck("REGEXP_LIKE(MaNganh,'^[0-9]{7}$','c')")]),
t('DocGia',`IdDocGia INT NOT NULL AUTO_INCREMENT PRIMARY KEY
MaDinhDanhTruong VARCHAR(50) NOT NULL UNIQUE
HoTen VARCHAR(200) NOT NULL
LoaiDocGia VARCHAR(20) NOT NULL
IdKhoa INT NULL
MaNganh CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NULL
KhoaHoc VARCHAR(30) NULL
TrangThaiHocTap VARCHAR(50) NULL
ThuDienTu VARCHAR(254) NULL
TrangThaiHoSo VARCHAR(20) NOT NULL DEFAULT 'HOAT_DONG'
DuocMuon BOOLEAN NOT NULL DEFAULT 1
KhoaDen DATETIME NULL
LyDoKhoa VARCHAR(300) NULL
IdNguoiDoiQuyen INT NULL`,[ck("LoaiDocGia IN ('SINH_VIEN','CAN_BO','GIANG_VIEN','TRO_GIANG','CNV')"),fk('IdKhoa','Khoa'),fk('MaNganh','NganhDaoTao'),ck("TrangThaiHoSo IN ('HOAT_DONG','NGUNG_HOAT_DONG')"),ck('DuocMuon IN (0,1)'),ck('(DuocMuon=1 AND KhoaDen IS NULL AND LyDoKhoa IS NULL) OR (DuocMuon=0 AND LyDoKhoa IS NOT NULL AND CHAR_LENGTH(TRIM(LyDoKhoa))>0)')]),
t('TaiKhoan',`IdTaiKhoan INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDocGia INT NULL UNIQUE
TenDangNhap VARCHAR(100) NOT NULL UNIQUE
MatKhauDaBam VARBINARY(512) NULL
DinhDanhNgoai VARCHAR(200) NULL UNIQUE
VaiTro VARCHAR(25) NOT NULL DEFAULT 'DOC_GIA'
DangHoatDong BOOLEAN NOT NULL DEFAULT 0`,[fk('IdDocGia','DocGia'),ck("VaiTro IN ('QUAN_TRI','QUAN_LY_THU_VIEN','BIEN_MUC','THU_THU','DOC_GIA')"),ck('DangHoatDong IN (0,1)'),ck('DangHoatDong=0 OR MatKhauDaBam IS NOT NULL OR DinhDanhNgoai IS NOT NULL')]),
t('MucPhanLoaiDDC',`MaDDC VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL PRIMARY KEY
MaCha VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NULL
TenMuc VARCHAR(200) NOT NULL
NguonThamKhao VARCHAR(600) NOT NULL
DaDoiChieu BOOLEAN NOT NULL DEFAULT 0
ViTriTraBanIn VARCHAR(150) NULL
IdNguoiDoiChieu INT NULL
ThoiDiemDoiChieu DATETIME NULL
CachGhepSo VARCHAR(600) NULL`,[fk('MaCha','MucPhanLoaiDDC','MaDDC'),fk('IdNguoiDoiChieu','TaiKhoan','IdTaiKhoan'),ck("REGEXP_LIKE(MaDDC,'^[0-9]{3}([.][0-9]*[1-9])?$','c')"),ck('DaDoiChieu IN (0,1)'),ck('DaDoiChieu=0 OR (ViTriTraBanIn IS NOT NULL AND CHAR_LENGTH(TRIM(ViTriTraBanIn))>0 AND IdNguoiDoiChieu IS NOT NULL AND ThoiDiemDoiChieu IS NOT NULL)')]),
t('DauTaiLieu',`IdDauTaiLieu INT NOT NULL AUTO_INCREMENT PRIMARY KEY
NhanDe VARCHAR(500) NOT NULL
LoaiTaiLieu VARCHAR(20) NOT NULL
MaNganh CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NULL
KhoaHoc VARCHAR(30) NULL
DanhSachTep JSON NOT NULL DEFAULT (JSON_ARRAY())
MaDDC VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NULL
AnBanDDC VARCHAR(5) NOT NULL DEFAULT '23'
TrangThaiPhanLoai VARCHAR(20) NOT NULL DEFAULT 'CHUA_PHAN_LOAI'
ViTriTraDDC VARCHAR(150) NULL
IdNguoiPhanLoai INT NULL
ThoiDiemPhanLoai DATETIME NULL
DangHienThi BOOLEAN NOT NULL DEFAULT 1
PhienBan BIGINT UNSIGNED NOT NULL DEFAULT 1`,[fk('MaNganh','NganhDaoTao'),fk('MaDDC','MucPhanLoaiDDC'),fk('IdNguoiPhanLoai','TaiKhoan','IdTaiKhoan'),ck(`JSON_SCHEMA_VALID('${cauTrucDanhSachTep}', DanhSachTep)`),ck("LoaiTaiLieu IN ('SACH','LUAN_VAN','DO_AN','KHAC')"),ck("AnBanDDC='23' AND DangHienThi IN (0,1) AND PhienBan>0"),ck("(TrangThaiPhanLoai='CHUA_PHAN_LOAI' AND MaDDC IS NULL AND ViTriTraDDC IS NULL AND IdNguoiPhanLoai IS NULL AND ThoiDiemPhanLoai IS NULL) OR (TrangThaiPhanLoai IN ('DU_KIEN','DA_XAC_NHAN') AND MaDDC IS NOT NULL AND IdNguoiPhanLoai IS NOT NULL AND ThoiDiemPhanLoai IS NOT NULL)"),ck("TrangThaiPhanLoai<>'DA_XAC_NHAN' OR (ViTriTraDDC IS NOT NULL AND CHAR_LENGTH(TRIM(ViTriTraDDC))>0)")]),
t('ThanhVienDeTai',`IdDauTaiLieu INT NOT NULL
IdDocGia INT NOT NULL
VaiTro VARCHAR(20) NOT NULL`,['PRIMARY KEY (IdDauTaiLieu,IdDocGia,VaiTro)',fk('IdDauTaiLieu','DauTaiLieu'),fk('IdDocGia','DocGia'),ck("VaiTro IN ('THUC_HIEN','HUONG_DAN')")]),
t('ViTriLuuTru',`IdViTri INT NOT NULL AUTO_INCREMENT PRIMARY KEY
DayKe VARCHAR(30) NOT NULL
Ngan VARCHAR(30) NOT NULL`,['UNIQUE (DayKe,Ngan)']),
t('BanTaiLieu',`IdBanTaiLieu INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDauTaiLieu INT NOT NULL
SoDangKyCaBiet VARCHAR(50) NOT NULL UNIQUE
MaVach VARCHAR(100) NOT NULL UNIQUE
IdViTri INT NULL
KyHieuXepGia VARCHAR(100) NULL
TrangThai VARCHAR(20) NOT NULL DEFAULT 'DANG_XU_LY'
TinhTrang VARCHAR(20) NOT NULL DEFAULT 'TOT'
GiaBia DECIMAL(12,0) NULL
NgayNhap DATE NULL`,['UNIQUE (IdBanTaiLieu,IdDauTaiLieu)',fk('IdDauTaiLieu','DauTaiLieu'),fk('IdViTri','ViTriLuuTru'),ck("TrangThai IN ('DANG_XU_LY','SAN_SANG','DANG_MUON','THAT_LAC','HU_HONG','MAT','THANH_LY')"),ck("TinhTrang IN ('TOT','CU','HU_HONG') AND (GiaBia IS NULL OR GiaBia>=0)")]),
t('PhieuMuon',`IdPhieuMuon INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDocGia INT NOT NULL
ThoiDiemMuon DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
IdThuThu INT NOT NULL
TrangThai VARCHAR(20) NOT NULL DEFAULT 'DANG_MO'`,['UNIQUE (IdPhieuMuon,IdDocGia)',fk('IdDocGia','DocGia'),fk('IdThuThu','TaiKhoan','IdTaiKhoan'),ck("TrangThai IN ('DANG_MO','DA_DONG','HUY')")]),
t('ChiTietPhieuMuon',`IdChiTietMuon INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdPhieuMuon INT NOT NULL
IdDocGia INT NOT NULL
IdBanTaiLieu INT NOT NULL
IdDauTaiLieu INT NOT NULL
HanTra DATETIME NOT NULL
ThoiDiemTra DATETIME NULL
IdNguoiNhanTra INT NULL
TrangThai VARCHAR(20) NOT NULL DEFAULT 'DANG_MUON'
SoLanGiaHan SMALLINT NOT NULL DEFAULT 0
DauDangMuon TINYINT GENERATED ALWAYS AS (CASE WHEN TrangThai='DANG_MUON' THEN 1 ELSE NULL END) VIRTUAL`,['UNIQUE (IdChiTietMuon,IdDocGia)','UNIQUE (IdBanTaiLieu,DauDangMuon)','UNIQUE (IdDocGia,IdDauTaiLieu,DauDangMuon)',fk('IdPhieuMuon,IdDocGia','PhieuMuon','IdPhieuMuon,IdDocGia'),fk('IdBanTaiLieu,IdDauTaiLieu','BanTaiLieu','IdBanTaiLieu,IdDauTaiLieu'),fk('IdNguoiNhanTra','TaiKhoan','IdTaiKhoan'),ck("TrangThai IN ('DANG_MUON','DA_TRA','MAT','HUY') AND SoLanGiaHan>=0"),ck("(TrangThai='DANG_MUON' AND ThoiDiemTra IS NULL) OR (TrangThai='DA_TRA' AND ThoiDiemTra IS NOT NULL AND IdNguoiNhanTra IS NOT NULL) OR TrangThai IN ('MAT','HUY')")]),
t('LuotVaoPhongDoc',`IdLuotDoc INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDocGia INT NOT NULL
ThoiDiemVao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
ThoiDiemRa DATETIME NULL
IdThuThu INT NOT NULL
DaTraThe BOOLEAN NOT NULL DEFAULT 0`,[fk('IdDocGia','DocGia'),fk('IdThuThu','TaiKhoan','IdTaiKhoan'),ck('DaTraThe IN (0,1) AND (ThoiDiemRa IS NULL OR ThoiDiemRa>=ThoiDiemVao)')]),
t('PhieuThu',`IdPhieuThu INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDocGia INT NOT NULL
ThoiDiemThu DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
IdNguoiThu INT NOT NULL
HinhThuc VARCHAR(20) NOT NULL
SoTienThu DECIMAL(12,0) NOT NULL`,['UNIQUE (IdPhieuThu,IdDocGia)',fk('IdDocGia','DocGia'),fk('IdNguoiThu','TaiKhoan','IdTaiKhoan'),ck("HinhThuc IN ('TIEN_MAT','CHUYEN_KHOAN') AND SoTienThu>0")]),
t('ViPham',`IdViPham INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDocGia INT NOT NULL
IdChiTietMuon INT NULL
LoaiViPham VARCHAR(25) NOT NULL
SoTien DECIMAL(12,0) NOT NULL DEFAULT 0
CanCu VARCHAR(1000) NOT NULL
ThoiDiemLap DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
IdNguoiLap INT NOT NULL
IdPhieuThu INT NULL
TrangThai VARCHAR(20) NOT NULL DEFAULT 'CHUA_XU_LY'
IdNguoiHuy INT NULL
LyDoHuy VARCHAR(300) NULL`,[fk('IdDocGia','DocGia'),fk('IdChiTietMuon,IdDocGia','ChiTietPhieuMuon','IdChiTietMuon,IdDocGia'),fk('IdNguoiLap','TaiKhoan','IdTaiKhoan'),fk('IdNguoiHuy','TaiKhoan','IdTaiKhoan'),fk('IdPhieuThu,IdDocGia','PhieuThu','IdPhieuThu,IdDocGia'),ck("LoaiViPham IN ('QUA_HAN','MAT_SACH','PHI_XU_LY','HONG_TRANG','THIET_HAI_KHAC') AND SoTien>=0"),ck("TrangThai IN ('CHUA_XU_LY','DA_XU_LY','DA_HUY')"),ck("(TrangThai='CHUA_XU_LY' AND IdPhieuThu IS NULL AND IdNguoiHuy IS NULL AND LyDoHuy IS NULL) OR (TrangThai='DA_XU_LY' AND (SoTien=0 OR IdPhieuThu IS NOT NULL) AND IdNguoiHuy IS NULL AND LyDoHuy IS NULL) OR (TrangThai='DA_HUY' AND IdNguoiHuy IS NOT NULL AND LyDoHuy IS NOT NULL AND CHAR_LENGTH(TRIM(LyDoHuy))>0)")]),
t('KiemKe',`IdKiemKe INT NOT NULL AUTO_INCREMENT PRIMARY KEY
TenKiemKe VARCHAR(200) NOT NULL
ThoiDiemBatDau DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
ThoiDiemKetThuc DATETIME NULL
IdNguoiLap INT NOT NULL
TrangThai VARCHAR(20) NOT NULL DEFAULT 'DU_KIEN'`,[fk('IdNguoiLap','TaiKhoan','IdTaiKhoan'),ck("TrangThai IN ('DU_KIEN','DANG_KIEM','HOAN_TAT','HUY')"),ck('ThoiDiemKetThuc IS NULL OR ThoiDiemKetThuc>=ThoiDiemBatDau')]),
t('ChiTietKiemKe',`IdChiTietKiemKe INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdKiemKe INT NOT NULL
IdBanTaiLieu INT NULL
MaVachQuet VARCHAR(100) NULL
CoTrongDanhSach BOOLEAN NOT NULL DEFAULT 1
IdViTriDuKien INT NULL
IdViTriThucTe INT NULL
KetQua VARCHAR(20) NOT NULL DEFAULT 'CHUA_KIEM'
IdNguoiKiemKe INT NULL
ThoiDiemKiem DATETIME NULL`,['UNIQUE (IdKiemKe,IdBanTaiLieu)','UNIQUE (IdKiemKe,MaVachQuet)',fk('IdKiemKe','KiemKe'),fk('IdBanTaiLieu','BanTaiLieu'),fk('IdViTriDuKien','ViTriLuuTru','IdViTri'),fk('IdViTriThucTe','ViTriLuuTru','IdViTri'),fk('IdNguoiKiemKe','TaiKhoan','IdTaiKhoan'),ck('CoTrongDanhSach IN (0,1) AND (IdBanTaiLieu IS NOT NULL OR MaVachQuet IS NOT NULL)'),ck("KetQua IN ('CHUA_KIEM','KHOP','THIEU','THUA','SAI_VI_TRI','HU_HONG')"),ck('CoTrongDanhSach=0 OR IdBanTaiLieu IS NOT NULL')]),
t('ThanhLy',`IdThanhLy INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdBanTaiLieu INT NOT NULL
IdKiemKe INT NULL
LyDo VARCHAR(500) NOT NULL
ThoiDiemDeNghi DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
IdNguoiDeNghi INT NOT NULL
TrangThai VARCHAR(20) NOT NULL DEFAULT 'DE_NGHI'
IdNguoiXuLy INT NULL
ThoiDiemXuLy DATETIME NULL
DauDeNghi TINYINT GENERATED ALWAYS AS (CASE WHEN TrangThai='DE_NGHI' THEN 1 ELSE NULL END) VIRTUAL`,['UNIQUE (IdBanTaiLieu,DauDeNghi)',fk('IdBanTaiLieu','BanTaiLieu'),fk('IdKiemKe','KiemKe'),fk('IdNguoiDeNghi','TaiKhoan','IdTaiKhoan'),fk('IdNguoiXuLy','TaiKhoan','IdTaiKhoan'),ck("TrangThai IN ('DE_NGHI','DA_THANH_LY','TU_CHOI')"),ck("TrangThai='DE_NGHI' OR (IdNguoiXuLy IS NOT NULL AND ThoiDiemXuLy IS NOT NULL)")]),
t('PhanHoiDocGia',`IdPhanHoi INT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDocGia INT NOT NULL
LoaiPhanHoi VARCHAR(20) NOT NULL
IdDauTaiLieu INT NULL
TieuDe VARCHAR(300) NOT NULL
NoiDung TEXT NOT NULL
Diem TINYINT NULL
ThoiDiemGui DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
TrangThai VARCHAR(20) NOT NULL DEFAULT 'MOI'
IdNguoiXuLy INT NULL`,[fk('IdDocGia','DocGia'),fk('IdDauTaiLieu','DauTaiLieu'),fk('IdNguoiXuLy','TaiKhoan','IdTaiKhoan'),ck("LoaiPhanHoi IN ('DE_XUAT','GOP_Y','DANH_GIA') AND TrangThai IN ('MOI','DA_XU_LY')"),ck('Diem IS NULL OR Diem BETWEEN 1 AND 5')]),
t('NhatKy',`IdNhatKy BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdTaiKhoan INT NULL
ThoiDiem DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
HanhDong VARCHAR(50) NOT NULL
BangDuLieu VARCHAR(50) NOT NULL
KhoaBanGhi VARCHAR(100) NOT NULL
ChiTiet JSON NOT NULL`,[fk('IdTaiKhoan','TaiKhoan')]),
t('BieuGhiMARC',`IdBieuGhiMARC BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY
IdDauTaiLieu INT NULL UNIQUE
DauBieuMARC VARCHAR(24) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
Truong JSON NOT NULL DEFAULT (JSON_ARRAY())
TrangThai VARCHAR(25) NOT NULL DEFAULT 'NHAP'
PhienBan BIGINT UNSIGNED NOT NULL DEFAULT 1
PhienBanDauTaiLieu BIGINT UNSIGNED NULL
IdNguoiTao INT NOT NULL
ThoiDiemTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
IdNguoiKiemTra INT NULL
ThoiDiemKiemTra DATETIME NULL`,[fk('IdDauTaiLieu','DauTaiLieu'),fk('IdNguoiTao','TaiKhoan','IdTaiKhoan'),fk('IdNguoiKiemTra','TaiKhoan','IdTaiKhoan'),ck("REGEXP_LIKE(DauBieuMARC,'^[ -~]{24}$','c') AND REGEXP_LIKE(SUBSTRING(DauBieuMARC,1,5),'^[0-9]{5}$','c') AND REGEXP_LIKE(SUBSTRING(DauBieuMARC,13,5),'^[0-9]{5}$','c') AND SUBSTRING(DauBieuMARC,11,2)='22' AND SUBSTRING(DauBieuMARC,21,4)='4500'"),ck("TrangThai IN ('NHAP','DA_KIEM_TRA') AND PhienBan>0"),ck("(TrangThai='NHAP' AND IdNguoiKiemTra IS NULL AND ThoiDiemKiemTra IS NULL AND PhienBanDauTaiLieu IS NULL) OR (TrangThai='DA_KIEM_TRA' AND IdNguoiKiemTra IS NOT NULL AND ThoiDiemKiemTra IS NOT NULL AND (IdDauTaiLieu IS NULL OR PhienBanDauTaiLieu IS NOT NULL))")])
];
export function ddl(){
 return `/* CSDL gọn cho đồ án thư viện DAU: MySQL 8.0.21+, DDC23 và MARC21.
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
\n`+bang.map(b=>`CREATE TABLE ${b.ten}\n(\n`+[...b.cot.map(c=>'    '+c),...b.rb.map((r,i)=>'    CONSTRAINT RB_'+b.ten+'_'+(i+1)+' '+r)].join(',\n')+'\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;\n').join('\n')+`
ALTER TABLE DocGia ADD CONSTRAINT FK_DocGia_NguoiDoiQuyen FOREIGN KEY (IdNguoiDoiQuyen) REFERENCES TaiKhoan(IdTaiKhoan);
CREATE INDEX IX_DocGia_HoTen ON DocGia(HoTen);
CREATE INDEX IX_DauTaiLieu_NhanDe ON DauTaiLieu(NhanDe);
CREATE INDEX IX_Muon_HanTra ON ChiTietPhieuMuon(TrangThai,HanTra);
`;
}
