using System.Text.Json;
using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
using ThuVienDAU.Services;
namespace ThuVienDAU.Circulation
{
 public class CirculationClient(SessionInfo session)
 {
  protected SessionInfo Session { get; } = session;
  public async Task<T> Call<T>(string command, object payload)
  {
   var response = await new TcpCommandClient().SendAsync(command,payload,Session.SessionId);
   if(!response.Success) throw new InvalidOperationException(response.Error ?? "Server từ chối yêu cầu.");
   return response.Data is null ? throw new InvalidDataException("Thiếu dữ liệu phản hồi.") : response.Data.Value.Deserialize<T>(TcpProtocol.Json)!;
  }
  public Task PreviewAsync(IWin32Window owner,string kind,int id) => ReceiptPrinter.ShowAsync(owner,Session,kind,id);
 }
}

namespace ThuVienDAU.Circulation.LT01
{
 public sealed class BorrowService(SessionInfo session) : CirculationClient(session)
 {
  public Task<BorrowReaderLookup> LookupReaderAsync(string code) => Call<BorrowReaderLookup>("TV3_READER_LOOKUP",new CirculationQuery(code));
  public Task<BorrowCopyLookup> LookupCopyAsync(string code) => Call<BorrowCopyLookup>("TV3_COPY_LOOKUP",new CirculationQuery(code));
  public Task<BorrowReceiptResult> CreateBorrowAsync(int readerId,List<int> copyIds) => Call<BorrowReceiptResult>("TV3_BORROW",new BorrowCommand(readerId,copyIds.ToArray()));
 }
}

namespace ThuVienDAU.Circulation.LT02
{
 public sealed class ReturnService(SessionInfo session) : CirculationClient(session)
 {
  public Task<ActiveLoanCopyInfo> LookupActiveBorrowByBarcodeAsync(string code) => Call<ActiveLoanCopyInfo>("TV3_RETURN_LOOKUP",new CirculationQuery(code));
  public Task<List<ReturnProcessedItem>> ProcessReturnsAsync(List<ReturnItemInput> items) => Call<List<ReturnProcessedItem>>("TV3_RETURN",new ReturnCommand(items));
 }
}

namespace ThuVienDAU.Circulation.LT03
{
 public sealed class RenewService(SessionInfo session) : CirculationClient(session)
 {
  public Task<List<RenewableLoanItem>> GetActiveLoansAsync(string keyword) => Call<List<RenewableLoanItem>>("TV3_RENEW_LIST",new CirculationQuery(keyword));
  public Task<RenewResult> RenewAsync(RenewCommand command) => Call<RenewResult>("TV3_RENEW",command);
 }
}

namespace ThuVienDAU.Circulation.LT04
{
 public sealed class LoanHistoryService(SessionInfo session) : CirculationClient(session)
 {
  public Task<List<LoanHistoryRecord>> SearchHistoryAsync(LoanHistoryFilter filter) => Call<List<LoanHistoryRecord>>("TV3_HISTORY",filter);
 }
}

namespace ThuVienDAU.Circulation.VP01
{
 public sealed class ViolationService(SessionInfo session) : CirculationClient(session)
 {
  public Task<List<ViolationCandidate>> GetPendingCandidatesAsync(string keyword = "") => Call<List<ViolationCandidate>>("TV3_VIOLATION_CANDIDATES",new CirculationQuery(keyword));
  public Task<ViolationRecordResult> CreateViolationAsync(CreateViolationInput input) => Call<ViolationRecordResult>("TV3_VIOLATION_CREATE",input);
 }
}

namespace ThuVienDAU.Circulation.VP02
{
 public sealed class FineReceiptService(SessionInfo session) : CirculationClient(session)
 {
  public Task<List<UnpaidViolationItem>> GetUnpaidViolationsAsync(string keyword = "") => Call<List<UnpaidViolationItem>>("TV3_UNPAID",new CirculationQuery(keyword));
  public Task<FineReceiptResult> CollectFinesAsync(CollectFineRequest req) => Call<FineReceiptResult>("TV3_COLLECT",req);
 }
}

namespace ThuVienDAU.Circulation.VP03
{
 public sealed class VoidViolationService(SessionInfo session) : CirculationClient(session)
 {
  public Task<List<ViolationLookupItem>> SearchViolationsAsync(string keyword = "",bool includeVoided = false) => Call<List<ViolationLookupItem>>("TV3_VIOLATION_LIST",new CirculationQuery(keyword,includeVoided));
  public Task<VoidViolationResult> VoidViolationAsync(VoidViolationRequest req) => Call<VoidViolationResult>("TV3_VIOLATION_VOID",req);
 }
}

namespace ThuVienDAU.Circulation.PD01
{
 public sealed class ReadingRoomService(SessionInfo session) : CirculationClient(session)
 {
  public Task<List<ReadingRoomVisitItem>> GetTodayVisitsAsync(string keyword = "",bool onlyInside = false) => Call<List<ReadingRoomVisitItem>>("TV3_VISITS",new CirculationQuery(keyword,onlyInside));
  public Task<CheckInOutResult> ScanCardCheckInOrOutAsync(string code,string locker = "",string note = "") => Call<CheckInOutResult>("TV3_VISIT_SCAN",new VisitCommand(code,locker,note,true));
 }
}

namespace ThuVienDAU.Circulation.PH01
{
 public sealed class FeedbackService(SessionInfo session) : CirculationClient(session)
 {
  public Task<int> SubmitFeedbackAsync(SubmitFeedbackRequest req) => Call<int>("TV3_FEEDBACK_SUBMIT",req);
  public Task<string> ProcessFeedbackAsync(ProcessFeedbackRequest req) => Call<string>("TV3_FEEDBACK_PROCESS",req);
  public async Task<List<FeedbackItem>> SearchFeedbacksAsync(string keyword = "",string typeFilter = "TAT_CA",string statusFilter = "TAT_CA")
  {
   var items = await Call<List<FeedbackItem>>("TV3_FEEDBACK_LIST",new CirculationQuery(keyword));
   return items.Where(x=>(typeFilter=="TAT_CA"||x.FeedbackType==typeFilter)&&(statusFilter=="TAT_CA"||x.Status==statusFilter)).ToList();
  }
 }
}

namespace ThuVienDAU.Circulation.BC03
{
 public sealed class CirculationStatsService(SessionInfo session) : CirculationClient(session)
 {
  public Task<CirculationDashboardReport> GetDashboardReportAsync(DateTime from,DateTime to) => Call<CirculationDashboardReport>("TV3_STATISTICS",new StatisticsQuery(from,to));
 }
}
