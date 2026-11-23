/* Tai khoan Quan tri thuc cho HT-02.
   Khi script da chay, khong tao lai tai khoan neu TenDangNhap da ton tai.
   Mat khau: TV1@Admin2026
   Mat khau tren luu dung dinh dang PBKDF2-SHA256 ma hoa boi server.
*/
USE ThuVienDAU;

INSERT INTO TaiKhoan
	(TenDangNhap, MatKhauDaBam, VaiTro, DangHoatDong, IdDocGia)
SELECT
	'quantri_tv1',
	UNHEX('0131d04f824c318814201b36039e3326c1c115c0531a4465314269b4e5b3b6f0dd0e3d6ab96757f09219c6e3f819e59ef1'),
	'QUAN_TRI',
	1,
	NULL
WHERE NOT EXISTS
	(SELECT 1 FROM TaiKhoan WHERE TenDangNhap = 'quantri_tv1');

INSERT INTO NhatKy(IdTaiKhoan, HanhDong, BangDuLieu, KhoaBanGhi, ChiTiet)
SELECT
	(SELECT IdTaiKhoan FROM TaiKhoan WHERE TenDangNhap = 'quantri_tv1'),
	'KHOI_TAO_QUAN_TRI_TV1',
	'TaiKhoan',
	(SELECT IdTaiKhoan FROM TaiKhoan WHERE TenDangNhap = 'quantri_tv1'),
	JSON_OBJECT('Nguon', 'Script HT-02', 'VaiTro', 'QUAN_TRI')
WHERE NOT EXISTS
	(
		SELECT 1 FROM NhatKy
		WHERE HanhDong = 'KHOI_TAO_QUAN_TRI_TV1'
		  AND KhoaBanGhi = (SELECT IdTaiKhoan FROM TaiKhoan WHERE TenDangNhap = 'quantri_tv1')
	);

SELECT IdTaiKhoan, TenDangNhap, VaiTro, DangHoatDong
FROM TaiKhoan
WHERE TenDangNhap = 'quantri_tv1';
