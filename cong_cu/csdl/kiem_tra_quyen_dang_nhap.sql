-- Chi chay tren MySQL thu nghiem doc lap, moi fixture duoc rollback.
USE ThuVienDAU;
DELIMITER $$
CREATE PROCEDURE KiemThuQuyenDangNhap()
BEGIN
 DECLARE bm INT; DECLARE ql INT; DECLARE tt INT; DECLARE qt INT; DECLARE dg INT; DECLARE tk INT;
 DECLARE chan BOOLEAN DEFAULT FALSE; DECLARE dem INT;
 START TRANSACTION;
 INSERT INTO TaiKhoan(TenDangNhap,VaiTro,MatKhauDaBam,DangHoatDong) VALUES('Q_BM','BIEN_MUC',X'01',1); SET bm=LAST_INSERT_ID();
 INSERT INTO TaiKhoan(TenDangNhap,VaiTro,MatKhauDaBam,DangHoatDong) VALUES('Q_QL','QUAN_LY_THU_VIEN',X'01',1); SET ql=LAST_INSERT_ID();
 INSERT INTO TaiKhoan(TenDangNhap,VaiTro,MatKhauDaBam,DangHoatDong) VALUES('Q_TT','THU_THU',X'01',1); SET tt=LAST_INSERT_ID();
 INSERT INTO TaiKhoan(TenDangNhap,VaiTro,MatKhauDaBam,DangHoatDong) VALUES('Q_QT','QUAN_TRI',X'01',1); SET qt=LAST_INSERT_ID();
 INSERT INTO DocGia(MaDinhDanhTruong,HoTen,LoaiDocGia) VALUES('Q_DG','Doc gia thu','SINH_VIEN'); SET dg=LAST_INSERT_ID();
 INSERT INTO TaiKhoan(IdDocGia,TenDangNhap,VaiTro,MatKhauDaBam,DangHoatDong) VALUES(dg,'Q_DG','DOC_GIA',X'01',1); SET tk=LAST_INSERT_ID();
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; CALL DoiTrangThaiDangNhapDocGia(ql,tk,0,'Thu sai quyen'); END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL manager can lock login'; END IF;
 SET chan=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; CALL DoiTrangThaiDangNhapDocGia(tt,tk,0,'Thu sai quyen'); END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL librarian can lock login'; END IF;
 SET chan=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; CALL DoiTrangThaiDangNhapDocGia(qt,tk,0,'Thu sai quyen'); END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL admin can lock login'; END IF;
 SET chan=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; CALL DoiTrangThaiDangNhapDocGia(tk,tk,0,'Thu sai quyen'); END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL reader can lock login'; END IF;
 CALL DoiTrangThaiDangNhapDocGia(bm,tk,0,'Khoa dang nhap');
 IF (SELECT DangHoatDong FROM TaiKhoan WHERE IdTaiKhoan=tk)<>0 OR (SELECT DuocMuon FROM DocGia WHERE IdDocGia=dg)<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL login lock changed borrowing'; END IF;
 -- Borrowing stays under manager; cataloger cannot change it.
 SET chan=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; UPDATE DocGia SET DuocMuon=0,LyDoKhoa='Kiem thu quyen muon',IdNguoiDoiQuyen=bm WHERE IdDocGia=dg; END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL cataloger changed borrowing'; END IF;
 UPDATE DocGia SET DuocMuon=0,LyDoKhoa='Kiem thu quyen muon',IdNguoiDoiQuyen=ql WHERE IdDocGia=dg;
 CALL DoiTrangThaiDangNhapDocGia(bm,tk,1,'Mo dang nhap');
 IF (SELECT DangHoatDong FROM TaiKhoan WHERE IdTaiKhoan=tk)<>1 OR (SELECT DuocMuon FROM DocGia WHERE IdDocGia=dg)<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL login unlock changed borrowing'; END IF;
 SELECT COUNT(*) INTO dem FROM NhatKy WHERE HanhDong='DOI_DANG_NHAP_DOC_GIA' AND IdTaiKhoan=bm AND KhoaBanGhi=CAST(tk AS CHAR);
 IF dem<>2 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL missing audit'; END IF;
 SET chan=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; CALL DoiTrangThaiDangNhapDocGia(bm,ql,0,'Sai doi tuong'); END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL staff account locked'; END IF;
 SET chan=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; CALL DoiTrangThaiDangNhapDocGia(bm,tk,0,' '); END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL empty reason'; END IF;
 UPDATE TaiKhoan SET DangHoatDong=0 WHERE IdTaiKhoan=bm;
 SET chan=FALSE;
 BEGIN DECLARE CONTINUE HANDLER FOR SQLSTATE '45000' SET chan=TRUE; CALL DoiTrangThaiDangNhapDocGia(bm,tk,0,'Inactive'); END;
 IF NOT chan THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='FAIL inactive cataloger'; END IF;
 ROLLBACK;
 SELECT 'PASS: BIEN_MUC-only login lock/unlock; manager-only borrowing; independent flags; audit; invalid actor/target/reason rejected' AS KetQua;
END$$
DELIMITER ;
CALL KiemThuQuyenDangNhap();
DROP PROCEDURE KiemThuQuyenDangNhap;
