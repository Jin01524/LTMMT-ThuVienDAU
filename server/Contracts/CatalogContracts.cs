namespace ThuVienDAU.Catalog;
public sealed record CatalogQuery(string Text = "", int Offset = 0);
public sealed record DdcItem(string MaDDC, string? MaCha, string TenMuc, string NguonThamKhao,
    bool DaDoiChieu, string? ViTriTraBanIn, string? CachGhepSo, int? IdNguoiDoiChieu = null,
    DateTime? ThoiDiemDoiChieu = null, string Revision = "");
public sealed record TitleItem(int IdDauTaiLieu, string NhanDe, string LoaiTaiLieu,
    string? MaDDC, string TrangThaiPhanLoai, string? ViTriTraDDC, bool DangHienThi = true, ulong PhienBan = 0);

public sealed record StorageLocationItem(int IdViTri, string DayKe, string Ngan, string Revision = "");
public sealed record CopyItem(int IdBanTaiLieu, int IdDauTaiLieu, string SoDangKyCaBiet, string MaVach,
    int? IdViTri, string? KyHieuXepGia, string TrangThai, string TinhTrang,
    decimal? GiaBia, DateOnly? NgayNhap, string NhanDe = "", string? DayKe = null, string? Ngan = null, string Revision = "");
public sealed record BarcodeQuery(string MaVach);
public sealed record CopyLookup(CopyItem BanSach, bool DangCoKhoanMuonMo);
