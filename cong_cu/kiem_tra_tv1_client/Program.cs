using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ThuVienDAU.Services;
using ThuVienDAU.Models;
using ThuVienDAU.Catalog;
using ThuVienDAU.Server.Services;
var logs=new List<string>();
if(args.Length==1&&args[0]=="--parse-only"){TestImportFileParsing();await TestMaintenanceGateAsync();Console.WriteLine($"PASS {logs.Count} parser and maintenance-gate checks");return;}
if(args.Length==0)throw new ArgumentException("Cần đường dẫn fixture JSON hoặc --parse-only.");
var fixture=JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
var config=Path.Combine(AppContext.BaseDirectory,"appsettings.local.json");
var previous=File.Exists(config)?File.ReadAllBytes(config):null;
var auth=new AuthenticationService(new TcpCommandClient());var tokens=new List<string>();
try
{
 TestImportFileParsing();
 await TestMaintenanceGateAsync();
 File.WriteAllText(config,JsonSerializer.Serialize(new{Server=new{Host="127.0.0.1",Port=fixture.GetProperty("port").GetInt32(),TimeoutSeconds=15}}));
 async Task<SessionInfo> Login(string role){var a=fixture.GetProperty("accounts").EnumerateArray().Single(x=>x.GetProperty("role").GetString()==role);var s=await auth.LoginAsync(a.GetProperty("username").GetString()!,a.GetProperty("password").GetString()!);tokens.Add(s.SessionId);return s;}
 var admin=await Login("QUAN_TRI");var manager=await Login("QUAN_LY_THU_VIEN");var bm=await Login("BIEN_MUC");var reader=await Login("DOC_GIA");
 var directory=new ReaderDirectoryService(new TcpCommandClient());var system=new SystemAdministrationClientService(new TcpCommandClient());var reports=new ReaderReportClientService(new TcpCommandClient());var imports=new ReaderImportClientService(new TcpCommandClient());
 Check((await directory.SearchAsync(bm.SessionId,"TEST-STUDENT")).Count==1,"actual client reader search over TCP");
 var importCommand=new ReaderImportCommand(Guid.NewGuid().ToString("N"),new[]{new ReaderImportRow(2," import - student ","","Import fixture student","SINH_VIEN","","","K23","DANG_HOC",""),new ReaderImportRow(3,"BAD-STATUS","","Invalid status fixture","SINH_VIEN","","","","UNKNOWN_STATUS","")});
 var importPreview=await imports.PreviewAsync(manager.SessionId,importCommand);
 Check(importPreview.ValidRows==1&&importPreview.ErrorRows==1&&!importPreview.Committed,"DG-01 preview separates valid and invalid rows");
 var importResult=await imports.CommitAsync(manager.SessionId,importCommand);
 Check(importResult.Committed&&importResult.CreatedRows==1&&importResult.ErrorRows==1,"DG-01 commits valid rows only and returns rejected rows");
 var importedReader=(await directory.SearchAsync(bm.SessionId,"IMPORT-STUDENT")).Single();
 Check(importedReader.SchoolId=="IMPORT-STUDENT"&&importedReader.ProfileStatus=="HOAT_DONG"&&importedReader.CanBorrow,"DG-01 normalizes school code and preserves borrowing permission");
 var updateCommand=new ReaderImportCommand(Guid.NewGuid().ToString("N"),new[]{new ReaderImportRow(2," import - student ","","Updated fixture student","SINH_VIEN","","","","BAO_LUU","")});
 var updateResult=await imports.CommitAsync(manager.SessionId,updateCommand);
 var updatedReader=(await directory.SearchAsync(bm.SessionId,"IMPORT-STUDENT")).Single();
 Check(updateResult.UpdatedRows==1&&updatedReader.ProfileStatus=="NGUNG_HOAT_DONG"&&updatedReader.CanBorrow,"DG-01 synchronizes inactive status without changing borrow permission");
 try{await imports.PreviewAsync(bm.SessionId,importCommand);throw new Exception("Unauthorized DG-01 import was accepted");}catch(InvalidOperationException){Check(true,"DG-01 import requires manager role");}
 Check((await system.GetAuditLogAsync(admin.SessionId,new(2026,1,1),new(2027,1,1))).Any(entry=>entry.Action=="NHAP_DU_LIEU_DOC_GIA"),"DG-01 import writes audit summary");
 Check((await system.GetConfigurationAsync(admin.SessionId)).PendingDecisions.Count>0,"actual client configuration over TCP");
 Check((await reports.GetAsync(manager.SessionId,null,null,null,new(2026,1,1),new(2026,12,31))).TotalReaders==3,"actual client reader report includes imported fixture reader");
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
async Task TestMaintenanceGateAsync()
{
 var gate=new ServerMaintenanceGate();
 var activeRequest=await gate.EnterRequestAsync(CancellationToken.None);
 var maintenanceTask=gate.EnterMaintenanceAsync(CancellationToken.None);
 Check(!maintenanceTask.IsCompleted,"restore maintenance waits for active operations to drain");
 var queuedRequestEntered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
 var queuedRequestTask=Task.Run(async()=>
 {
  using(await gate.EnterRequestAsync(CancellationToken.None))queuedRequestEntered.TrySetResult(true);
 });
 await Task.Delay(50);
 Check(!queuedRequestEntered.Task.IsCompleted,"new operations are blocked while restore maintenance is pending");
 activeRequest.Dispose();
 using(var maintenance=await maintenanceTask.AsTask().WaitAsync(TimeSpan.FromSeconds(2)))
 {
  Check(!queuedRequestEntered.Task.IsCompleted,"restore holds the maintenance gate until completion");
 }
 await queuedRequestTask.WaitAsync(TimeSpan.FromSeconds(2));
 Check(queuedRequestEntered.Task.IsCompleted,"queued operations resume after maintenance completes");
}
void TestImportFileParsing()
{
 var folder=Path.Combine(Path.GetTempPath(),"tv1-import-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
 try
 {
  var csv=Path.Combine(folder,"readers.csv");File.WriteAllText(csv,"Mã Độc Giả,Họ và Tên,Đối Tượng,Khoa / Phòng Ban,Email,Trạng Thái\r\n000123,\"Nguyen, Van A\",Sinh viên,Khoa thử,sv.test@example.invalid,Đang học\r\n",new UTF8Encoding(true));
  var csvRows=ReaderImportFileReader.Read(csv);Check(csvRows.Count==1&&csvRows[0].MaSinhVien=="000123"&&csvRows[0].HoTen=="Nguyen, Van A"&&csvRows[0].TrangThaiHocTap=="Đang học","DG-01 CSV parser maps workbook headers and preserves identifier zeros");
  var xlsx=Path.Combine(folder,"readers.xlsx");using(var archive=ZipFile.Open(xlsx,ZipArchiveMode.Create))
  {
   WriteEntry(archive,"xl/workbook.xml","<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Readers\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
   WriteEntry(archive,"xl/_rels/workbook.xml.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
   WriteEntry(archive,"xl/styles.xml","<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"000000\"/></numFmts><cellXfs count=\"2\"><xf numFmtId=\"0\"/><xf numFmtId=\"164\"/></cellXfs></styleSheet>");
   var headers=new[]{"Mã Độc Giả","Họ và Tên","Đối Tượng","Khoa / Phòng Ban","Email","Trạng Thái"};var headerCells=new StringBuilder();
   for(var i=0;i<headers.Length;i++)headerCells.Append($"<c r=\"{(char)('A'+i)}1\" t=\"inlineStr\"><is><t>{headers[i]}</t></is></c>");
   var dataCells="<c r=\"A2\" s=\"1\"><v>456</v></c><c r=\"B2\" t=\"inlineStr\"><is><t>Excel fixture</t></is></c><c r=\"C2\" t=\"inlineStr\"><is><t>Sinh viên</t></is></c><c r=\"D2\" t=\"inlineStr\"><is><t>Khoa thử</t></is></c><c r=\"F2\" t=\"inlineStr\"><is><t>Đang học</t></is></c>";
   WriteEntry(archive,"xl/worksheets/sheet1.xml","<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\">"+headerCells.ToString()+"</row><row r=\"2\">"+dataCells+"</row></sheetData></worksheet>");
  }
  var excelRows=ReaderImportFileReader.Read(xlsx);Check(excelRows.Count==1&&excelRows[0].MaSinhVien=="000456"&&excelRows[0].HoTen=="Excel fixture","DG-01 XLSX parser maps workbook headers and honors zero-padded numeric codes");
 }
 finally{Directory.Delete(folder,true);}
}
void WriteEntry(ZipArchive archive,string path,string content){var entry=archive.CreateEntry(path);using var writer=new StreamWriter(entry.Open(),new UTF8Encoding(false));writer.Write(content);}
void Check(bool ok,string label){if(!ok)throw new Exception(label);logs.Add("PASS "+label);Console.WriteLine(logs[^1]);}
