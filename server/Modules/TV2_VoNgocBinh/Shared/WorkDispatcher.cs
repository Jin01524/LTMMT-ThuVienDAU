using ThuVienDAU.Protocol;
namespace ThuVienDAU.Catalog;
internal static class WorkDispatcher
{
    internal static bool Handles(string cmd)=>cmd.StartsWith("TV2_MARC_")||cmd.StartsWith("TV2_PROJECT_")||cmd.StartsWith("TV2_FILE_")||cmd.StartsWith("TV2_STOCK_")||cmd.StartsWith("TV2_DISPOSAL_")||cmd is "TV2_MAJOR_LIST" or "TV2_READER_LIST" or "TV2_BIBLIO_GET" or "TV2_REPORT";
    internal static async Task<object> Run(CatalogStore s,TcpRequest r,int actor,string role,CancellationToken ct)
    {
        switch(r.Command)
        {
            case "TV2_MARC_GET":return await MarcService.Get(s,WorkRules.Read<TitleQuery>(r).IdDauTaiLieu,ct);
            case "TV2_MARC_SAVE":return await MarcService.Save(s,WorkRules.Read<MarcItem>(r),actor,ct);
            case "TV2_MARC_CHECK":return await MarcService.Check(s,WorkRules.Read<MarcCheck>(r),actor,ct);
            case "TV2_BIBLIO_GET":return await MarcService.Summary(s,WorkRules.Read<TitleQuery>(r).IdDauTaiLieu,ct);
            case "TV2_PROJECT_GET":return await ProjectService.Get(s,WorkRules.Read<TitleQuery>(r).IdDauTaiLieu,ct);
            case "TV2_PROJECT_SAVE":return await ProjectService.Save(s,WorkRules.Read<ProjectItem>(r),actor,ct);
            case "TV2_FILE_BEGIN":return await AttachmentService.Begin(s,WorkRules.Read<FileBegin>(r),actor,ct);
            case "TV2_FILE_CHUNK":return await AttachmentService.Chunk(WorkRules.Read<FileChunk>(r),actor,ct);
            case "TV2_FILE_FINISH":return await AttachmentService.Finish(s,WorkRules.Read<FileTicket>(r),actor,ct);
            case "TV2_FILE_READ":return await AttachmentService.Read(s,WorkRules.Read<FileRead>(r),ct);
            case "TV2_STOCK_LIST":return await StocktakeService.List(s,WorkRules.Read<CatalogQuery>(r),ct);
            case "TV2_STOCK_CREATE":return await StocktakeService.Create(s,WorkRules.Read<StocktakeCreate>(r),actor,ct);
            case "TV2_STOCK_DETAIL":return await StocktakeService.Detail(s,WorkRules.Read<StocktakeQuery>(r),ct);
            case "TV2_STOCK_ACTION":return await StocktakeService.Action(s,WorkRules.Read<StocktakeAction>(r),actor,ct);
            case "TV2_STOCK_SCAN":return await StocktakeService.Scan(s,WorkRules.Read<StocktakeScan>(r),actor,ct);
            case "TV2_DISPOSAL_LIST":return await DisposalService.List(s,WorkRules.Read<CatalogQuery>(r),ct);
            case "TV2_DISPOSAL_CREATE":return await DisposalService.Create(s,WorkRules.Read<DisposalCreate>(r),actor,role,ct);
            case "TV2_DISPOSAL_DECIDE":return await DisposalService.Decide(s,WorkRules.Read<DisposalDecision>(r),actor,role,ct);
            case "TV2_REPORT":return await StockReportService.Get(s,WorkRules.Read<ReportFilter>(r),ct);
            case "TV2_MAJOR_LIST":
            {
                var q=WorkRules.Read<CatalogQuery>(r);var term=WorkRules.Like(q);
                return (await s.Rows("SELECT MaNganh,TenNganh FROM NganhDaoTao WHERE MaNganh LIKE @q ESCAPE '!' OR TenNganh LIKE @q ESCAPE '!' ORDER BY MaNganh LIMIT 100 OFFSET @off",ct,("q",term),("off",q.Offset))).Select(x=>new MajorItem((string)x["MaNganh"]!,(string)x["TenNganh"]!)).ToArray();
            }
            case "TV2_READER_LIST":
            {
                var q=WorkRules.Read<CatalogQuery>(r);var term=WorkRules.Like(q);
                return (await s.Rows("SELECT IdDocGia,MaDinhDanhTruong,HoTen,LoaiDocGia FROM DocGia WHERE MaDinhDanhTruong LIKE @q ESCAPE '!' OR HoTen LIKE @q ESCAPE '!' ORDER BY MaDinhDanhTruong LIMIT 100 OFFSET @off",ct,("q",term),("off",q.Offset))).Select(x=>new ReaderReference(Convert.ToInt32(x["IdDocGia"]),(string)x["MaDinhDanhTruong"]!,(string)x["HoTen"]!,(string)x["LoaiDocGia"]!)).ToArray();
            }
            default:throw new InvalidOperationException("Lệnh TV2 không hợp lệ.");
        }
    }
}
