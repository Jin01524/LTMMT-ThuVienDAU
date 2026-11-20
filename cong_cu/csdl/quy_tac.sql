DELIMITER $$
CREATE PROCEDURE KiemTraQuanLy(IN pNguoi INT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM TaiKhoan t
        WHERE t.IdTaiKhoan=pNguoi AND t.DangHoatDong=1 AND t.VaiTro='QUAN_LY_THU_VIEN') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Chi Quan ly thu vien duoc thuc hien thao tac nay.';
    END IF;
END$$
-- Goi trong transaction cua Service; Id nguoi xu ly lay tu phien dang nhap, khong tu UI.
-- Service phai ROLLBACK neu CALL that bai; khong cap UPDATE DangHoatDong truc tiep cho luong nay.
CREATE PROCEDURE DoiTrangThaiDangNhapDocGia(IN pNguoi INT, IN pTaiKhoan INT, IN pHoatDong BOOLEAN, IN pLyDo VARCHAR(300))
BEGIN
    DECLARE vVaiTro VARCHAR(25) DEFAULT NULL;
    DECLARE vDocGia INT DEFAULT NULL;
    DECLARE vCu BOOLEAN DEFAULT NULL;
    DECLARE vNguoi VARCHAR(25) DEFAULT NULL;
    SELECT VaiTro INTO vNguoi FROM TaiKhoan WHERE IdTaiKhoan=pNguoi AND DangHoatDong=1 FOR UPDATE;
    IF vNguoi IS NULL OR vNguoi<>'BIEN_MUC' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Chi Bien muc duoc khoa/mo dang nhap doc gia.';
    END IF;
    IF pHoatDong IS NULL OR pHoatDong NOT IN (0,1) OR pLyDo IS NULL OR CHAR_LENGTH(TRIM(pLyDo))=0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Can trang thai 0/1 va ly do.';
    END IF;
    SELECT VaiTro,IdDocGia,DangHoatDong INTO vVaiTro,vDocGia,vCu FROM TaiKhoan WHERE IdTaiKhoan=pTaiKhoan FOR UPDATE;
    IF vVaiTro IS NULL OR vVaiTro<>'DOC_GIA' OR vDocGia IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Chi ap dung tai khoan dang nhap cua doc gia.';
    END IF;
    UPDATE TaiKhoan SET DangHoatDong=pHoatDong WHERE IdTaiKhoan=pTaiKhoan;
    INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet)
    VALUES(pNguoi,'DOI_DANG_NHAP_DOC_GIA','TaiKhoan',pTaiKhoan,
        JSON_OBJECT('TrangThaiCu',vCu,'TrangThaiMoi',pHoatDong,'LyDo',pLyDo));
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
    SELECT CASE WHEN LoaiDocGia='SINH_VIEN' THEN __SV_TOIDA__ ELSE __CB_TOIDA__ END INTO vToiDa FROM DocGia WHERE IdDocGia=pDocGia;
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
       (SELECT CASE WHEN LoaiDocGia='SINH_VIEN' THEN __SV_TOIDA__ ELSE __CB_TOIDA__ END FROM DocGia WHERE IdDocGia=vDocGia) THEN
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
            SELECT NhanTruong FROM TruongDaTach WHERE NhanTruong IN (__NHAN_KHONG_LAP__)
            GROUP BY NhanTruong HAVING COUNT(DISTINCT ThuTuTruong)>1
        ) THEN 'Truong MARC khong lap dang bi lap.'
        WHEN EXISTS (
            SELECT t.ThuTuTruong FROM TruongDaTach t WHERE (__TRUONGCON_KHONG_LAP__)
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
FROM vw_NoiDungMARC WHERE NhanTruong NOT IN (__NHAN_DA_BIET__);
CREATE VIEW vw_SachSanSang AS SELECT d.IdDauTaiLieu,d.NhanDe,COUNT(b.IdBanTaiLieu) AS TongSoBan,SUM(CASE WHEN b.TrangThai='SAN_SANG' THEN 1 ELSE 0 END) AS SoBanSanSang FROM DauTaiLieu d LEFT JOIN BanTaiLieu b USING(IdDauTaiLieu) GROUP BY d.IdDauTaiLieu,d.NhanDe;
CREATE VIEW vw_DangMuon AS SELECT c.IdChiTietMuon,c.IdDocGia,d.HoTen,c.IdBanTaiLieu,t.NhanDe,c.HanTra FROM ChiTietPhieuMuon c JOIN DocGia d USING(IdDocGia) JOIN DauTaiLieu t USING(IdDauTaiLieu) WHERE c.TrangThai='DANG_MUON';
CREATE VIEW vw_QuaHan AS SELECT * FROM vw_DangMuon WHERE HanTra<CURRENT_TIMESTAMP;
CREATE VIEW vw_CongNo AS SELECT d.IdDocGia,d.HoTen,COALESCE(SUM(CASE WHEN v.TrangThai='CHUA_XU_LY' THEN v.SoTien ELSE 0 END),0) AS SoTienConNo FROM DocGia d LEFT JOIN ViPham v USING(IdDocGia) GROUP BY d.IdDocGia,d.HoTen;
CREATE VIEW vw_KetQuaKiemKe AS SELECT k.IdKiemKe,k.TenKiemKe,COUNT(c.IdChiTietKiemKe) AS SoDong,SUM(CASE WHEN c.CoTrongDanhSach=1 THEN 1 ELSE 0 END) AS SoBanDuKien,SUM(CASE WHEN c.KetQua='THIEU' THEN 1 ELSE 0 END) AS SoBanThieu,SUM(CASE WHEN c.CoTrongDanhSach=0 THEN 1 ELSE 0 END) AS SoBanNgoaiDanhSach FROM KiemKe k LEFT JOIN ChiTietKiemKe c USING(IdKiemKe) GROUP BY k.IdKiemKe,k.TenKiemKe;
