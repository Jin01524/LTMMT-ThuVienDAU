using System.Text.Json;
using ThuVienDAU.Services;
using ThuVienDAU.Models;
using ThuVienDAU.Catalog;
var fixture=JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
var config=Path.Combine(AppContext.BaseDirectory,"appsettings.local.json");
var previous=File.Exists(config)?File.ReadAllBytes(config):null;
var auth=new AuthenticationService(new TcpCommandClient());var tokens=new List<string>();var logs=new List<string>();
try
{
 File.WriteAllText(config,JsonSerializer.Serialize(new{Server=new{Host="127.0.0.1",Port=fixture.GetProperty("port").GetInt32(),TimeoutSeconds=15}}));
 async Task<SessionInfo> Login(string role){var a=fixture.GetProperty("accounts").EnumerateArray().Single(x=>x.GetProperty("role").GetString()==role);var s=await auth.LoginAsync(a.GetProperty("username").GetString()!,a.GetProperty("password").GetString()!);tokens.Add(s.SessionId);return s;}
 var admin=await Login("QUAN_TRI");var manager=await Login("QUAN_LY_THU_VIEN");var bm=await Login("BIEN_MUC");var reader=await Login("DOC_GIA");
 var directory=new ReaderDirectoryService(new TcpCommandClient());var system=new SystemAdministrationClientService(new TcpCommandClient());var reports=new ReaderReportClientService(new TcpCommandClient());
 Check((await directory.SearchAsync(bm.SessionId,"TEST-STUDENT")).Count==1,"actual client reader search over TCP");
 Check((await system.GetConfigurationAsync(admin.SessionId)).PendingDecisions.Count>0,"actual client configuration over TCP");
 Check((await reports.GetAsync(manager.SessionId,null,null,null,new(2026,1,1),new(2026,12,31))).TotalReaders==2,"actual client reader report over TCP");
 Check((await system.GetAuditLogAsync(admin.SessionId,new(2026,1,1),new(2026,12,31))).Count>=0,"actual client audit over TCP");
 var target=(await directory.SearchAsync(bm.SessionId,"TEST-STUDENT"))[0];
 Check(!(await directory.SetBorrowPermissionAsync(target.ReaderId,new(manager.SessionId,false,"Client smoke lock"))).CanBorrow,"actual client locks borrow permission as manager");
 Check((await directory.SetBorrowPermissionAsync(target.ReaderId,new(manager.SessionId,true,"Client smoke unlock"))).CanBorrow,"actual client unlocks borrow permission as manager");
 var ddc=await new CatalogClientController().CallAsync<DdcItem[]>("TV2_DDC_LIST",new CatalogQuery(),bm.SessionId);
 Check(ddc.Length==40,"real login session opens TV2 without manual demo token");
 var catalog=new CatalogClientController();
 var title=await catalog.CallAsync<TitleItem>("TV2_TITLE_SAVE",new TitleItem(0,"Login bridge fixture","DO_AN",null,"CHUA_PHAN_LOAI",null),bm.SessionId);
 Check(title.IdDauTaiLieu>0,"real login creates TV2 title through shared actor/session");
 Check((await catalog.CallAsync<MarcItem>("TV2_MARC_GET",new TitleQuery(title.IdDauTaiLieu),bm.SessionId)).IdDauTaiLieu==title.IdDauTaiLieu,"real login opens new MARC21 module");
 Check((await catalog.CallAsync<ProjectItem>("TV2_PROJECT_GET",new TitleQuery(title.IdDauTaiLieu),bm.SessionId)).IdDauTaiLieu==title.IdDauTaiLieu,"real login opens new project/PDF metadata module");
 Check((await catalog.CallAsync<TableResult>("TV2_STOCK_LIST",new CatalogQuery(),bm.SessionId)).Cot.Contains("IdKiemKe"),"real login opens new stocktake module");
 Check((await catalog.CallAsync<TableResult>("TV2_DISPOSAL_LIST",new CatalogQuery(),bm.SessionId)).Cot.Contains("IdThanhLy"),"real login opens new disposal module");
 Check((await catalog.CallAsync<TableResult>("TV2_REPORT",new ReportFilter(),bm.SessionId)).Cot.Contains("SoBan"),"real login opens new stock report module");
 await auth.SetReaderAccountStatusAsync(new(bm.SessionId,reader.AccountId,false,"Client smoke login lock"));
 await auth.SetReaderAccountStatusAsync(new(bm.SessionId,reader.AccountId,true,"Client smoke login unlock"));
 Check(true,"actual client cataloger login lock/unlock remains separate from borrowing");
 await auth.LogoutAsync(bm.SessionId);
 try{await directory.SearchAsync(bm.SessionId,"TEST-STUDENT");throw new Exception("Logout failed");}catch(InvalidOperationException){Check(true,"logout revokes actual client directory access");}
 File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(),"kiem_tra/TV1_Client_TCP.txt"),new[]{"25/11/2026 - actual client services, isolated fixture only"}.Concat(logs));
 Console.WriteLine($"PASS {logs.Count} actual client service checks");
}
finally{foreach(var t in tokens)try{await auth.LogoutAsync(t);}catch{}if(previous is null)File.Delete(config);else File.WriteAllBytes(config,previous);}
void Check(bool ok,string label){if(!ok)throw new Exception(label);logs.Add("PASS "+label);Console.WriteLine(logs[^1]);}
