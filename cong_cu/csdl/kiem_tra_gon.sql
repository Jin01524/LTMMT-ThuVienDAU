-- Chi chay tren MySQL tam/doc lap. Cac tai khoan, sach, trang DDC deu la du lieu gia lap.
USE ThuVienDAU;
DELIMITER $$
CREATE PROCEDURE KiemThuNghiepVu()
BEGIN
 DECLARE ql INT; DECLARE tt INT; DECLARE sv INT; DECLARE cb INT;
 DECLARE d1 INT; DECLARE d2 INT; DECLARE b1 INT; DECLARE b2 INT; DECLARE b3 INT;
 DECLARE p1 INT; DECLARE p2 INT; DECLARE ct INT; DECLARE vp INT; DECLARE phieu INT;
 DECLARE vt INT; DECLARE dot INT; DECLARE dem INT; DECLARE tuChoi BOOLEAN DEFAULT FALSE;
 START TRANSACTION;
 SELECT COUNT(*) INTO dem FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('PhieuDatTruoc','ChinhSachDocGia','NgayNghi','VaiTro','TaiKhoanVaiTro','PhanCongKiemKe','DotKiemKe');
 IF dem<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: obsolete tables remain'; END IF;
 INSERT INTO TaiKhoan(TenDangNhap,MatKhauDaBam,VaiTro,DangHoatDong) VALUES('TEST_QL',X'01','QUAN_LY_THU_VIEN',1); SET ql=LAST_INSERT_ID();
 INSERT INTO TaiKhoan(TenDangNhap,MatKhauDaBam,VaiTro,DangHoatDong) VALUES('TEST_TT',X'01','THU_THU',1); SET tt=LAST_INSERT_ID();
 INSERT INTO DocGia(MaDinhDanhTruong,HoTen,LoaiDocGia,MaNganh) VALUES('TEST_SV','Sinh viên kiểm thử','SINH_VIEN','7480201'); SET sv=LAST_INSERT_ID();
 INSERT INTO DocGia(MaDinhDanhTruong,HoTen,LoaiDocGia) VALUES('TEST_CB','Cán bộ kiểm thử','CAN_BO'); SET cb=LAST_INSERT_ID();
 CALL KiemTraDuocMuon(sv);
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  UPDATE DocGia SET DuocMuon=0,LyDoKhoa='KIEM THU',IdNguoiDoiQuyen=tt WHERE IdDocGia=sv;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: cashier locked reader'; END IF;
 UPDATE DocGia SET DuocMuon=0,LyDoKhoa='KIEM THU',IdNguoiDoiQuyen=ql WHERE IdDocGia=sv;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; CALL KiemTraDuocMuon(sv); END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: blocked reader eligible'; END IF;
 UPDATE DocGia SET DuocMuon=1,LyDoKhoa=NULL,IdNguoiDoiQuyen=ql WHERE IdDocGia=sv;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; DELETE FROM DocGia WHERE IdDocGia=sv; END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: reader physical delete'; END IF;
 INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu) VALUES('Sách thử một','SACH'); SET d1=LAST_INSERT_ID();
 INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu) VALUES('Sách thử hai','SACH'); SET d2=LAST_INSERT_ID();
 INSERT INTO BanTaiLieu(IdDauTaiLieu,SoDangKyCaBiet,MaVach) VALUES(d1,'TEST_B1','TEST_B1');SET b1=LAST_INSERT_ID();
 INSERT INTO BanTaiLieu(IdDauTaiLieu,SoDangKyCaBiet,MaVach) VALUES(d1,'TEST_B2','TEST_B2');SET b2=LAST_INSERT_ID();
 INSERT INTO BanTaiLieu(IdDauTaiLieu,SoDangKyCaBiet,MaVach) VALUES(d2,'TEST_B3','TEST_B3');SET b3=LAST_INSERT_ID();
 INSERT INTO PhieuMuon(IdDocGia,IdThuThu) VALUES(sv,tt); SET p1=LAST_INSERT_ID();
 INSERT INTO PhieuMuon(IdDocGia,IdThuThu) VALUES(cb,tt); SET p2=LAST_INSERT_ID();
 INSERT INTO ChiTietPhieuMuon(IdPhieuMuon,IdDocGia,IdBanTaiLieu,IdDauTaiLieu,HanTra) VALUES(p1,sv,b1,d1,'2030-01-01');SET ct=LAST_INSERT_ID();
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR 1062 SET tuChoi=TRUE;
  INSERT INTO ChiTietPhieuMuon(IdPhieuMuon,IdDocGia,IdBanTaiLieu,IdDauTaiLieu,HanTra) VALUES(p2,cb,b1,d1,'2030-01-01');
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: duplicate active copy'; END IF;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR 1062 SET tuChoi=TRUE;
  INSERT INTO ChiTietPhieuMuon(IdPhieuMuon,IdDocGia,IdBanTaiLieu,IdDauTaiLieu,HanTra) VALUES(p1,sv,b2,d1,'2030-01-01');
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: duplicate active title'; END IF;
 CALL GiaHanMuon(ct,'2030-02-01',tt);
 SELECT COUNT(*) INTO dem FROM NhatKy WHERE HanhDong='GIA_HAN' AND KhoaBanGhi=CAST(ct AS CHAR);
 IF dem<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: renewal shared history'; END IF;
 UPDATE ChiTietPhieuMuon SET TrangThai='DA_TRA',ThoiDiemTra=NOW(),IdNguoiNhanTra=tt WHERE IdChiTietMuon=ct;
 INSERT INTO ChiTietPhieuMuon(IdPhieuMuon,IdDocGia,IdBanTaiLieu,IdDauTaiLieu,HanTra) VALUES(p1,sv,b1,d1,'2030-01-01'),(p1,sv,b3,d2,'2030-01-01');
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; CALL KiemTraDuocMuon(sv); END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: student quota'; END IF;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  INSERT INTO ViPham(IdDocGia,LoaiViPham,SoTien,CanCu,IdNguoiLap) VALUES(cb,'QUA_HAN',2000,'TEST',tt);
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: staff overdue cash fine'; END IF;
 INSERT INTO ViPham(IdDocGia,LoaiViPham,SoTien,CanCu,IdNguoiLap) VALUES(cb,'QUA_HAN',0,'Thong bao don vi',tt);
 INSERT INTO ViPham(IdDocGia,LoaiViPham,SoTien,CanCu,IdNguoiLap) VALUES(sv,'QUA_HAN',2000,'1 ngay x 2000',tt); SET vp=LAST_INSERT_ID();
 INSERT INTO PhieuThu(IdDocGia,IdNguoiThu,HinhThuc,SoTienThu) VALUES(cb,tt,'TIEN_MAT',2000);SET phieu=LAST_INSERT_ID();
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR 1452 SET tuChoi=TRUE;
  UPDATE ViPham SET IdPhieuThu=phieu,TrangThai='DA_XU_LY' WHERE IdViPham=vp;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: cross-reader receipt'; END IF;
 CALL ThuTienPhat(sv,tt,'TIEN_MAT');
 SELECT IdPhieuThu INTO phieu FROM ViPham WHERE IdViPham=vp;
 SELECT COUNT(*) INTO dem FROM PhieuThu WHERE IdPhieuThu=phieu AND IdDocGia=sv AND SoTienThu=2000;
 IF dem<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: full payment receipt'; END IF;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  UPDATE ViPham SET TrangThai='DA_HUY',IdNguoiHuy=tt,LyDoHuy='TEST' WHERE IdViPham=vp;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: cashier voided charge'; END IF;
 UPDATE ViPham SET TrangThai='DA_HUY',IdNguoiHuy=ql,LyDoHuy='TEST' WHERE IdViPham=vp;
 SELECT COUNT(*) INTO dem FROM PhieuThu WHERE IdPhieuThu=phieu AND SoTienThu=2000;
 IF dem<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: charge void erased actual money'; END IF;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; DELETE FROM ViPham WHERE IdViPham=vp; END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: physical fine deletion'; END IF;
 INSERT INTO ViTriLuuTru(DayKe,Ngan) VALUES('K1','01');SET vt=LAST_INSERT_ID();
 INSERT INTO KiemKe(TenKiemKe,IdNguoiLap) VALUES('TEST KIEM KE',tt);SET dot=LAST_INSERT_ID();
 INSERT INTO ChiTietKiemKe(IdKiemKe,IdBanTaiLieu,IdViTriDuKien,IdViTriThucTe,KetQua,IdNguoiKiemKe,ThoiDiemKiem)
  VALUES(dot,b2,vt,vt,'KHOP',tt,NOW());
 INSERT INTO ChiTietKiemKe(IdKiemKe,MaVachQuet,CoTrongDanhSach,KetQua,IdNguoiKiemKe,ThoiDiemKiem)
  VALUES(dot,'TEST_BARCODE_UNKNOWN',0,'THUA',tt,NOW());
 SELECT COUNT(*) INTO dem FROM vw_KetQuaKiemKe WHERE IdKiemKe=dot AND SoBanDuKien=1 AND SoBanNgoaiDanhSach=1;
 IF dem<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: inventory expected vs extra'; END IF;
 INSERT INTO ThanhLy(IdBanTaiLieu,IdKiemKe,LyDo,IdNguoiDeNghi) VALUES(b2,dot,'TEST',tt);
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR 1062 SET tuChoi=TRUE;
  INSERT INTO ThanhLy(IdBanTaiLieu,IdKiemKe,LyDo,IdNguoiDeNghi) VALUES(b2,dot,'TEST DUP',tt);
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: duplicate pending disposal'; END IF;
 INSERT INTO PhanHoiDocGia(IdDocGia,LoaiPhanHoi,TieuDe,NoiDung) VALUES(sv,'DE_XUAT','Sach de xuat','Tac gia, ISBN neu co'),(sv,'GOP_Y','Gop y','Noi dung gop y');

 -- Can bo: duoc 3 dau sach, doc lap voi sinh vien 2 dau sach.
 INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu) VALUES('Sach can bo 1','SACH'); SET d1=LAST_INSERT_ID();
 INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu) VALUES('Sach can bo 2','SACH'); SET d2=LAST_INSERT_ID();
 INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu) VALUES('Sach can bo 3','SACH'); SET b3=LAST_INSERT_ID();
 INSERT INTO BanTaiLieu(IdDauTaiLieu,SoDangKyCaBiet,MaVach) VALUES(d1,'TEST_CB1','TEST_CB1'); SET b1=LAST_INSERT_ID();
 INSERT INTO BanTaiLieu(IdDauTaiLieu,SoDangKyCaBiet,MaVach) VALUES(d2,'TEST_CB2','TEST_CB2'); SET b2=LAST_INSERT_ID();
 INSERT INTO BanTaiLieu(IdDauTaiLieu,SoDangKyCaBiet,MaVach) VALUES(b3,'TEST_CB3','TEST_CB3'); SET ct=LAST_INSERT_ID();
 INSERT INTO ChiTietPhieuMuon(IdPhieuMuon,IdDocGia,IdBanTaiLieu,IdDauTaiLieu,HanTra) VALUES(p2,cb,b1,d1,'2030-01-01'),(p2,cb,b2,d2,'2030-01-01');
 CALL KiemTraDuocMuon(cb);
 INSERT INTO ChiTietPhieuMuon(IdPhieuMuon,IdDocGia,IdBanTaiLieu,IdDauTaiLieu,HanTra) VALUES(p2,cb,ct,b3,'2030-01-01');
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; CALL KiemTraDuocMuon(cb); END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: staff quota three'; END IF;
 INSERT INTO TaiKhoan(TenDangNhap,MatKhauDaBam,VaiTro,DangHoatDong) VALUES('TEST_DG',X'01','DOC_GIA',1); SET ct=LAST_INSERT_ID();
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; CALL ThuTienPhat(cb,ct,'TIEN_MAT'); END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: reader can collect money'; END IF;
 ROLLBACK;
 SELECT 'PASS: manager-only locks/voids; no reader/fine delete; copy/title uniqueness; quota; renewals/history; staff notices; receipts; two-table inventory; disposal; feedback' AS KetQua;
END$$

CREATE PROCEDURE KiemThuDDC_MARC()
BEGIN
 DECLARE nguoi INT; DECLARE dau INT; DECLARE bg BIGINT; 
 DECLARE dem INT; DECLARE tuChoi BOOLEAN DEFAULT FALSE; DECLARE ldr VARCHAR(24); DECLARE chuoi VARCHAR(100);
 SET ldr=CONCAT('00000','n','a','m',' ','a','22','00000',' ','i',' ','4500');
 START TRANSACTION;
 SELECT COUNT(*) INTO dem FROM Khoa; IF dem<>9 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: faculties'; END IF;

 SELECT COUNT(*) INTO dem FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND (COLUMN_NAME='LaKhoa' OR TABLE_NAME='DonVi');
 IF dem<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: old faculty marker remains'; END IF;
 SELECT COUNT(*) INTO dem FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND REFERENCED_TABLE_NAME='Khoa';
 IF dem<>2 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: faculty foreign keys'; END IF;
 SELECT COUNT(*) INTO dem FROM NganhDaoTao; IF dem<>18 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: majors'; END IF;
 SELECT COUNT(*) INTO dem FROM MucPhanLoaiDDC WHERE DaDoiChieu=0; IF dem<>40 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: pending DDC'; END IF;
 INSERT INTO TaiKhoan(TenDangNhap,MatKhauDaBam,VaiTro,DangHoatDong) VALUES('TEST_BM',X'01','THU_THU',1);SET nguoi=LAST_INSERT_ID();
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR 3819 SET tuChoi=TRUE;
  INSERT INTO MucPhanLoaiDDC(MaDDC,TenMuc,NguonThamKhao) VALUES('005.10','TEST','TEST');
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: DDC notation'; END IF;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; UPDATE MucPhanLoaiDDC SET MaCha='005' WHERE MaDDC='000'; END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: DDC cycle'; END IF;
 INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu,MaDDC,TrangThaiPhanLoai,IdNguoiPhanLoai,ThoiDiemPhanLoai) VALUES('Tai lieu kiem thu','SACH','005','DU_KIEN',nguoi,NOW());SET dau=LAST_INSERT_ID();
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  UPDATE DauTaiLieu SET TrangThaiPhanLoai='DA_XAC_NHAN',ViTriTraDDC='GIA LAP' WHERE IdDauTaiLieu=dau;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: unverified DDC'; END IF;
 UPDATE MucPhanLoaiDDC SET DaDoiChieu=1,ViTriTraBanIn='GIA LAP KHONG PHAI TRANG THAT',IdNguoiDoiChieu=nguoi,ThoiDiemDoiChieu=NOW() WHERE MaDDC='005';
 UPDATE DauTaiLieu SET TrangThaiPhanLoai='DA_XAC_NHAN',ViTriTraDDC='GIA LAP KHONG PHAI TRANG THAT' WHERE IdDauTaiLieu=dau;
 SELECT COUNT(*) INTO dem FROM NhatKy WHERE HanhDong='DOI_PHAN_LOAI'; IF dem=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: shared DDC history'; END IF;
 INSERT INTO BieuGhiMARC(IdDauTaiLieu,DauBieuMARC,IdNguoiTao,Truong)
 VALUES(dau,ldr,nguoi,JSON_ARRAY(
  JSON_OBJECT('NhanTruong','001','NoiDungDieuKhien','TEST_MARC'),
  JSON_OBJECT('NhanTruong','005','NoiDungDieuKhien','20261108120000.0'),
  JSON_OBJECT('NhanTruong','008','NoiDungDieuKhien',REPEAT('|',40)),
  JSON_OBJECT('NhanTruong','245','ChiThi1','1','ChiThi2','0','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','Nhan đề tiếng Việt'),
   JSON_OBJECT('MaTruongCon','b','NoiDung','Phụ đề'))),
  JSON_OBJECT('NhanTruong','020','ChiThi1',' ','ChiThi2',' ','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','9780306406157'),
   JSON_OBJECT('MaTruongCon','q','NoiDung','Bìa mềm'),
   JSON_OBJECT('MaTruongCon','q','NoiDung','Tập 1'))),
  JSON_OBJECT('NhanTruong','020','ChiThi1',' ','ChiThi2',' ','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','9783161484100')))
 ));
 SET bg=LAST_INSERT_ID();

 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  UPDATE BieuGhiMARC SET Truong=JSON_SET(Truong,'$[0].TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','BAD'))) WHERE IdBieuGhiMARC=bg;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: control field child'; END IF;

 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  UPDATE BieuGhiMARC SET Truong=JSON_SET(Truong,'$[2].NoiDungDieuKhien',CONCAT('é',REPEAT('|',38)))
  WHERE IdBieuGhiMARC=bg;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: 008 positions'; END IF;

 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  UPDATE BieuGhiMARC
  SET Truong=JSON_SET(Truong,'$[3].TruongCon[0].NoiDung',CONCAT('A',CHAR(31 USING utf8mb4),'B'))
  WHERE IdBieuGhiMARC=bg;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: MARC separator inside subfield'; END IF;

 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE;
  UPDATE BieuGhiMARC SET Truong=JSON_SET(Truong,'$[4].NhanTruong',20) WHERE IdBieuGhiMARC=bg;
 END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: numeric tag loses leading zero'; END IF;

 SELECT GROUP_CONCAT(MaTruongCon ORDER BY ThuTuTruongCon SEPARATOR '') INTO chuoi
 FROM vw_NoiDungMARC WHERE IdBieuGhiMARC=bg AND ThuTuTruong=5;
 IF NOT (chuoi <=> 'aqq') THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: subfield order'; END IF;
 SELECT COUNT(*) INTO dem FROM vw_ISBNTaiLieu WHERE IdDauTaiLieu=dau;
 IF dem<>2 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: multiple ISBN'; END IF;
 SELECT COUNT(*) INTO dem FROM vw_NoiDungMARC
 WHERE IdBieuGhiMARC=bg AND NhanTruong='020' AND MaTruongCon='a' AND HEX(ChiThi1)='20';
 IF dem<>2 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: blank indicator'; END IF;
 SELECT COUNT(*) INTO dem FROM vw_NoiDungMARC
 WHERE IdBieuGhiMARC=bg AND ThuTuTruong=4 AND MaTruongCon='a' AND BINARY NoiDung=BINARY 'Nhan đề tiếng Việt';
 IF dem<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: UTF-8 MARC text'; END IF;

 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; CALL KiemTraBieuGhiMARC(bg,nguoi); END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: missing 082 for confirmed DDC'; END IF;
 UPDATE BieuGhiMARC SET Truong=JSON_ARRAY_APPEND(Truong,'$',JSON_OBJECT(
  'NhanTruong','082','ChiThi1','1','ChiThi2','4','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','005'),
   JSON_OBJECT('MaTruongCon','2','NoiDung','23')))) WHERE IdBieuGhiMARC=bg;
 CALL KiemTraBieuGhiMARC(bg,nguoi);

 -- Nguon rut gon phai co chi thi dau 1; khong chap nhan 0/full.
 UPDATE BieuGhiMARC SET Truong=JSON_SET(Truong,'$[6].ChiThi1','0') WHERE IdBieuGhiMARC=bg;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; CALL KiemTraBieuGhiMARC(bg,nguoi); END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: abridged DDC accepted full indicator'; END IF;
 UPDATE BieuGhiMARC SET Truong=JSON_SET(Truong,'$[6].ChiThi1','1') WHERE IdBieuGhiMARC=bg;
 CALL KiemTraBieuGhiMARC(bg,nguoi);
 UPDATE BieuGhiMARC SET Truong=JSON_ARRAY_APPEND(Truong,'$[3].TruongCon',
  JSON_OBJECT('MaTruongCon','a','NoiDung','LAP SAI')) WHERE IdBieuGhiMARC=bg;
 SET tuChoi=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET tuChoi=TRUE; CALL KiemTraBieuGhiMARC(bg,nguoi); END;
 IF NOT tuChoi THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: nonrepeatable 245a'; END IF;
 UPDATE BieuGhiMARC SET Truong=JSON_REMOVE(Truong,'$[3].TruongCon[2]') WHERE IdBieuGhiMARC=bg;
 CALL KiemTraBieuGhiMARC(bg,nguoi);
 UPDATE BieuGhiMARC SET Truong=JSON_SET(Truong,'$[3].TruongCon[1].NoiDung','Sửa phụ đề') WHERE IdBieuGhiMARC=bg;
 SELECT COUNT(*) INTO dem FROM BieuGhiMARC WHERE IdBieuGhiMARC=bg AND TrangThai='NHAP' AND IdNguoiKiemTra IS NULL;
 IF dem<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: MARC edit invalidation'; END IF;
 CALL KiemTraBieuGhiMARC(bg,nguoi);
 UPDATE DauTaiLieu SET NhanDe='Đầu tài liệu thay đổi' WHERE IdDauTaiLieu=dau;
 SELECT COUNT(*) INTO dem FROM vw_TinhTrangMARC WHERE IdBieuGhiMARC=bg AND TinhTrangDoiChieu='CAN_DOI_CHIEU_LAI';
 IF dem<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: MARC source version'; END IF;

 UPDATE BieuGhiMARC SET Truong=JSON_ARRAY_APPEND(Truong,
  '$',JSON_OBJECT('NhanTruong','100','ChiThi1','1','ChiThi2',' ','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','Tac gia thu'))),
  '$',JSON_OBJECT('NhanTruong','264','ChiThi1',' ','ChiThi2','1','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','b','NoiDung','Nha xuat ban thu'),
   JSON_OBJECT('MaTruongCon','c','NoiDung','2026'))),
  '$',JSON_OBJECT('NhanTruong','300','ChiThi1',' ','ChiThi2',' ','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','123 trang'))),
  '$',JSON_OBJECT('NhanTruong','520','ChiThi1',' ','ChiThi2',' ','TruongCon',JSON_ARRAY(
   JSON_OBJECT('MaTruongCon','a','NoiDung','Tom tat thu')))
 ) WHERE IdBieuGhiMARC=bg;
 CALL KiemTraBieuGhiMARC(bg,nguoi);
 SELECT COUNT(*) INTO dem FROM vw_NoiDungMARC WHERE IdBieuGhiMARC=bg AND NhanTruong IN ('100','264','300','520');
 IF dem<>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL: bibliographic MARC storage'; END IF;
 ROLLBACK;
 SELECT 'PASS: 9/18 academic data; 40 pending DDC; notation/cycles; print confirmation; shared history; MARC JSON controls/blank/leading zero/repeated ISBN/order/NR/edit invalidation/version/082-abridged' AS KetQua;
END$$
DELIMITER ;
CALL KiemThuNghiepVu();
CALL KiemThuDDC_MARC();
DROP PROCEDURE KiemThuNghiepVu;
DROP PROCEDURE KiemThuDDC_MARC;
SELECT TABLE_TYPE,COUNT(*) AS SoLuong FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() GROUP BY TABLE_TYPE;
SELECT COUNT(*) AS SoCot FROM information_schema.COLUMNS c JOIN information_schema.TABLES t USING(TABLE_SCHEMA,TABLE_NAME) WHERE c.TABLE_SCHEMA=DATABASE() AND t.TABLE_TYPE='BASE TABLE';
SELECT COUNT(*) AS SoTaiKhoanThu FROM TaiKhoan;
SELECT COUNT(*) AS SoDocGiaThu FROM DocGia;
