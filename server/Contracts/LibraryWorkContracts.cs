using System.Text.Json;
namespace ThuVienDAU.Catalog;

public sealed record TitleQuery(int IdDauTaiLieu);
public sealed record MarcItem(int IdDauTaiLieu, string DauBieuMARC, JsonElement Truong, ulong PhienBan = 0,
    string TrangThai = "NHAP", ulong? PhienBanDauTaiLieu = null, ulong PhienBanHoSo = 0);
public sealed record MarcCheck(int IdDauTaiLieu, ulong PhienBan, ulong PhienBanHoSo);
public sealed record Bibliography(int IdDauTaiLieu, string NhanDe, string TacGia, string ISBN, string NhaXuatBan, string NamXuatBan,
    string TrangThaiMARC = "NHAP", bool CanDoiChieuLai = false);
public sealed record MajorItem(string MaNganh, string TenNganh);
public sealed record ReaderReference(int IdDocGia, string MaDinhDanhTruong, string HoTen, string LoaiDocGia);
public sealed record ProjectMember(int IdDocGia, string VaiTro, string HoTen = "", string MaDinhDanhTruong = "");
public sealed record AttachmentItem(string TenTep, string DuongDan);
public sealed record ProjectItem(int IdDauTaiLieu, string? MaNganh, string? KhoaHoc, ProjectMember[] ThanhVien,
    AttachmentItem[] DanhSachTep, ulong PhienBan);
public sealed record FileBegin(int IdDauTaiLieu, ulong PhienBan, string TenTep, int KichThuoc);
public sealed record FileTicket(string MaTep, int ViTri = 0);
public sealed record FileChunk(string MaTep, int ViTri, string DuLieu);
public sealed record FileRead(int IdDauTaiLieu, string MaTep, int ViTri = 0);
public sealed record FileData(string DuLieu, int KichThuoc);
public sealed record StocktakeCreate(string TenKiemKe, int? IdViTri = null);
public sealed record StocktakeQuery(int IdKiemKe, int Offset = 0);
public sealed record StocktakeAction(int IdKiemKe, string TrangThai);
public sealed record StocktakeScan(int IdKiemKe, string MaVach, int IdViTriThucTe, bool HuHong = false);
public sealed record DisposalCreate(int IdBanTaiLieu, string LyDo, int? IdKiemKe = null);
public sealed record DisposalDecision(int IdThanhLy, bool ChapNhan, string CanCu);
public sealed record ReportFilter(DateOnly? TuNgay = null, DateOnly? DenNgay = null, string? MaDDC = null, string? LoaiTaiLieu = null);
public sealed record TableResult(string[] Cot, string?[][] Dong);
