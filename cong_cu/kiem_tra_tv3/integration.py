"""Real TCP server + isolated MySQL; no production database/config mutations."""
from pathlib import Path
from datetime import datetime,timedelta,timezone
import subprocess,socket,json,struct,time,shutil,os,sys,secrets,hashlib
from concurrent.futures import ThreadPoolExecutor
r=Path(__file__).resolve().parents[2];work=r/'.tooling'/('tv3-integration-fixture-'+str(time.time_ns()));work.mkdir(parents=True);data=work/'mysql-data';data.mkdir();bindir=work/'server-fixture';shutil.copytree(r/'cong_cu/kiem_tra_tv3/FixtureHost/bin/Release/net10.0',bindir,ignore=shutil.ignore_patterns('appsettings.local.json'))
mysqlbin=Path(sys.argv[1]) if len(sys.argv)>1 else Path('C:/Program Files/MySQL/MySQL Server 8.0/bin');flags=subprocess.CREATE_NO_WINDOW
logs=[];dbproc=None;server=None;accounts=[]
def port():
 with socket.socket() as s:s.bind(('127.0.0.1',0));return s.getsockname()[1]
def run(args,**kw):return subprocess.run(list(map(str,args)),capture_output=True,creationflags=flags,**kw)
pdb=port();pserver=port();args=[mysqlbin/'mysql.exe','--no-defaults','--protocol=TCP','-h127.0.0.1',f'-P{pdb}','-uroot','--default-character-set=utf8mb4','--batch','--skip-column-names']
def sql(text):
 v=run(args,input=text.encode('utf8'));assert v.returncode==0,v.stderr.decode('utf8',errors='replace');return v.stdout.decode('utf8').strip()
def check(ok,label):
 assert ok,label;logs.append('PASS '+label);print(logs[-1],flush=True)
def request(cmd,payload=None,token=None):
 req={'version':1,'requestId':str(time.time_ns()),'command':cmd,'payload':payload,'sessionToken':token};body=json.dumps(req).encode()
 with socket.create_connection(('127.0.0.1',pserver),timeout=15) as s:
  s.sendall(struct.pack('>i',len(body))+body)
  def exact(n):
   out=b''
   while len(out)<n:
    b=s.recv(n-len(out));assert b,'truncated TCP response';out+=b
   return out
  return json.loads(exact(struct.unpack('>i',exact(4))[0]))
def login(role):
 a=next(x for x in accounts if x['role']==role);v=request('AUTH_LOGIN',{'username':a['username'],'password':a['password']});assert v['success'],v;return v['data']['sessionId']
try:
 v=run([mysqlbin/'mysqld.exe','--no-defaults','--initialize-insecure',f'--datadir={data}']);assert v.returncode==0,v.stderr
 dbproc=subprocess.Popen([str(mysqlbin/'mysqld.exe'),'--no-defaults',f'--datadir={data}',f'--port={pdb}','--bind-address=127.0.0.1','--mysqlx=OFF',f'--log-error={work}/mysql.log'],creationflags=flags)
 for _ in range(120):
  if run(args+['-e','SELECT 1']).returncode==0:break
  time.sleep(.25)
 else:raise RuntimeError('MySQL startup timeout')
 sql((r/'csdl/01_Tao_CSDL.sql').read_text('utf8'))
 sql("USE ThuVienDAU;INSERT INTO DocGia(MaDinhDanhTruong,HoTen,LoaiDocGia) VALUES('TEST-STUDENT','Student fixture','SINH_VIEN'),('TEST-TEACHER','Teacher fixture','GIANG_VIEN');")
 for role in ['QUAN_TRI','QUAN_LY_THU_VIEN','BIEN_MUC','THU_THU','DOC_GIA']:
  password=secrets.token_urlsafe(18);salt=secrets.token_bytes(16);hashvalue=b'\x01'+salt+hashlib.pbkdf2_hmac('sha256',password.encode(),salt,100000,32)
  identity=int(sql(f"USE ThuVienDAU;INSERT INTO TaiKhoan(TenDangNhap,MatKhauDaBam,VaiTro,DangHoatDong,IdDocGia) VALUES('{role}',X'{hashvalue.hex()}','{role}',1,{'1' if role=='DOC_GIA' else 'NULL'});SELECT LAST_INSERT_ID();"))
  accounts.append({'username':role,'role':role,'password':password,'id':identity})
 password=secrets.token_urlsafe(24);sql(f"CREATE USER 'fixture'@'127.0.0.1' IDENTIFIED BY '{password}';GRANT ALL PRIVILEGES ON ThuVienDAU.* TO 'fixture'@'127.0.0.1';")
 (bindir/'appsettings.local.json').write_text(json.dumps({'MySql':{'Server':'127.0.0.1','Port':pdb,'Database':'ThuVienDAU','User':'fixture','Password':password,'SslMode':'Preferred'}}),'utf8')
 server=subprocess.Popen(['dotnet',str(bindir/'FixtureHost.dll'),str(pserver)],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=flags)
 for _ in range(80):
  try:request('TV3_HISTORY',{});break
  except OSError:time.sleep(.1)
 else:raise RuntimeError('TCP server startup timeout')
 tokens={role:login(role) for role in ['QUAN_TRI','QUAN_LY_THU_VIEN','BIEN_MUC','THU_THU','DOC_GIA']};check(len(tokens)==5,'all five login roles work over TCP')
 admin=tokens['QUAN_TRI'];manager=tokens['QUAN_LY_THU_VIEN'];bm=tokens['BIEN_MUC'];reader=tokens['DOC_GIA'];librarian=tokens['THU_THU']

 def good(cmd,payload,token=librarian,label=None):
  v=request(cmd,payload,token);assert v['success'],(cmd,v);check(True,label or cmd);return v['data']
 def bad(cmd,payload,token=librarian,label=None):
  v=request(cmd,payload,token);check(not v['success'],label or (cmd+' denied'));return v
 def clock(stamp):good('FIXTURE_CLOCK',{'now':stamp},None,'fixture clock '+stamp)
 def calendar(confirmed,holidays=[]):good('FIXTURE_CALENDAR',{'confirmed':confirmed,'holidays':holidays},None,'fixture holiday configuration')
 actor=next(x['id'] for x in accounts if x['role']=='THU_THU');manager_id=next(x['id'] for x in accounts if x['role']=='QUAN_LY_THU_VIEN')
 def new_reader(code,typ='SINH_VIEN'):
  return int(sql(f"USE ThuVienDAU;INSERT INTO DocGia(MaDinhDanhTruong,HoTen,LoaiDocGia) VALUES('{code}','{code}','{typ}');SELECT LAST_INSERT_ID();"))
 def copy(title=None,price=80000,status='SAN_SANG'):
  if title is None:title=int(sql("USE ThuVienDAU;INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu) VALUES('Fixture Book','SACH');SELECT LAST_INSERT_ID();"))
  suffix=secrets.token_hex(8)
  ident=int(sql(f"USE ThuVienDAU;INSERT INTO BanTaiLieu(IdDauTaiLieu,MaVach,SoDangKyCaBiet,GiaBia,TrangThai) VALUES({title},'BC{suffix}','DK{suffix}',{'NULL' if price is None else price},'{status}');SELECT LAST_INSERT_ID();"))
  return ident,title,'BC'+suffix
 def borrow(rid,ids,token=librarian):return good('TV3_BORROW',{'readerId':rid,'copyIds':ids},token,'atomic borrow')
 def returned(detail,outcome='BINH_THUONG'):return good('TV3_RETURN',{'items':[{'detailId':detail,'returnOutcome':outcome,'bookCondition':'MAT','note':'Fixture return'}]},label='return '+outcome)
 def candidate(detail,typ):
  items=good('TV3_VIOLATION_CANDIDATES',{'keyword':''});return next(x for x in items if x['detailId']==detail and x['violationType']==typ)
 def violation(detail,typ,token=librarian,**kw):return good('TV3_VIOLATION_CREATE',dict(detailId=detail,violationType=typ,description='Fixture basis',**kw),token,'create violation '+typ)
 def count(table):return int(sql('USE ThuVienDAU;SELECT COUNT(*) FROM '+table))
 # Every business read uses the shared schema, and unknown/forged sessions cannot reach it.
 for cmd,payload in [('TV3_HISTORY',{'statusFilter':'TAT_CA'}),('TV3_VIOLATION_CANDIDATES',{}),('TV3_UNPAID',{}),('TV3_VIOLATION_LIST',{}),('TV3_VISITS',{}),('TV3_FEEDBACK_LIST',{}),('TV3_STATISTICS',{'fromDate':'2026-11-01','toDate':'2026-11-30'})]:
  good(cmd,payload,label='schema compatible '+cmd);bad(cmd,payload,None,'unauthenticated '+cmd);bad(cmd,payload,'0'*64,'forged session '+cmd)
 bad('TV3_VIOLATION_VOID',{'violationId':1,'managerAccountId':manager_id,'reason':'fake manager'},librarian,'payload ID cannot elevate librarian to manager')
 bad('TV3_BORROW',{'readerId':1,'copyIds':[1]},reader,'reader cannot issue loans')
 bad('TV3_BORROW',{'readerId':1,'copyIds':[1]},bm,'cataloger cannot issue loans')
 bad('TV3_BORROW',{'readerId':1,'copyIds':[1]},admin,'system administrator has no circulation business privilege')
 # Closing boundary for each session, lunch gap, weekend. No shared test data is changed on rejection.
 cut_reader=new_reader('HOURS');cut_copy=copy()[0]
 for stamp in ['2026-11-26T07:29:59+07:00','2026-11-26T11:15:00+07:00','2026-11-26T11:29:59+07:00','2026-11-26T11:30:00+07:00','2026-11-26T12:30:00+07:00','2026-11-26T13:29:59+07:00','2026-11-26T16:45:00+07:00','2026-11-26T17:00:00+07:00','2026-11-28T11:15:00+07:00','2026-11-28T13:30:00+07:00','2026-11-29T09:00:00+07:00']:
  clock(stamp);before=count('PhieuMuon');bad('TV3_BORROW',{'readerId':cut_reader,'copyIds':[cut_copy]},label='closed/cutoff '+stamp);check(count('PhieuMuon')==before,'closed borrowing leaves no ticket')
 for i,stamp in enumerate(['2026-11-26T07:30:00+07:00','2026-11-26T11:14:59+07:00','2026-11-26T13:30:00+07:00','2026-11-26T16:44:59+07:00','2026-11-28T07:30:00+07:00','2026-11-28T11:14:59+07:00']):
  clock(stamp);borrow(new_reader('OPEN'+str(i)),[copy()[0]])
 clock('2026-11-26T09:00:00+07:00')
 sv=new_reader('LOAN-SV');b1=copy();b2=copy();loan=borrow(sv,[b1[0],b2[0]])
 check(all(x['dueDate']=='2026-12-06T23:59:59' for x in loan['items']),'student has 2 titles / 10 calendar days, end of due day')
 bad('TV3_BORROW',{'readerId':sv,'copyIds':[copy()[0]]},label='student quota enforced')
 dup_reader=new_reader('DUPLICATE');same=copy();same2=copy(title=same[1]);before=count('PhieuMuon')
 bad('TV3_BORROW',{'readerId':dup_reader,'copyIds':[same[0],same2[0]]},label='duplicate title blocked');check(count('PhieuMuon')==before,'duplicate title rollback')
 bad('TV3_BORROW',{'readerId':dup_reader,'copyIds':[same[0],same[0]]},label='duplicate copy IDs blocked')
 damaged=copy(status='HU_HONG');bad('TV3_BORROW',{'readerId':dup_reader,'copyIds':[same[0],damaged[0]]},label='unavailable copy blocks entire batch');check(sql(f'USE ThuVienDAU;SELECT TrangThai FROM BanTaiLieu WHERE IdBanTaiLieu={same[0]}')=='SAN_SANG','failed batch leaves earlier copy available')
 staff=new_reader('LOAN-STAFF','GIANG_VIEN');staff_loan=borrow(staff,[copy()[0],copy()[0],copy()[0]])
 check(all(x['dueDate']=='2026-12-11T23:59:59' for x in staff_loan['items']),'staff has 3 titles / 15 days')
 # Client lookup methods also use school identification and shared shelf schema.
 check(good('TV3_READER_LOOKUP',{'keyword':'LOAN-SV'})['activeBorrowCount']==2,'reader lookup shows active loan count')
 check(good('TV3_COPY_LOOKUP',{'keyword':b1[2]})['copyId']==b1[0],'barcode lookup')
 info=good('TV3_RETURN_LOOKUP',{'keyword':b1[2]});check(info['detailId']==loan['items'][0]['detailId'],'return lookup shares correct detail primary key')
 first=loan['items'][0]['detailId'];second=loan['items'][1]['detailId'];returned(first)
 check(sql(f'USE ThuVienDAU;SELECT TrangThai FROM PhieuMuon WHERE IdPhieuMuon={loan["loanId"]}')=='DANG_MO','partial return keeps loan open')
 check(sql(f'USE ThuVienDAU;SELECT IdNguoiNhanTra FROM ChiTietPhieuMuon WHERE IdChiTietMuon={first}')==str(actor),'return actor comes from authenticated session')
 check(sql(f'USE ThuVienDAU;SELECT TinhTrang FROM BanTaiLieu WHERE IdBanTaiLieu={b1[0]}')=='TOT','client cannot turn normal return condition into invalid MAT enum')
 bad('TV3_RETURN',{'items':[{'detailId':first,'returnOutcome':'BINH_THUONG'}]},label='double return rejected')
 good('TV3_RENEW_LIST',{'keyword':'LOAN-SV'},label='renew lookup has no reservation-table dependency')
 cmd={'detailId':second,'newDueDate':'2026-12-10','confirmedNoOtherRequest':False,'reason':'Fixture renewal'}
 bad('TV3_RENEW',cmd,label='renew requires physical confirmation no other request')
 result=good('TV3_RENEW',cmd|{'confirmedNoOtherRequest':True},label='renew chooses explicit new due date');check(result['newRenewCount']==1 and result['newDueDate']=='2026-12-10T23:59:59','renew persisted selected date and count')
 bad('TV3_RENEW',cmd|{'confirmedNoOtherRequest':True,'newDueDate':'2026-12-01'},label='renew cannot shorten due date')
 returned(second);check(sql(f'USE ThuVienDAU;SELECT TrangThai FROM PhieuMuon WHERE IdPhieuMuon={loan["loanId"]}')=='DA_DONG','final return closes loan')
 # All selected returns are atomic if a later detail is stale.
 test_r=new_reader('RETURN-ATOMIC');test_l=borrow(test_r,[copy()[0],copy()[0]]);a,b=[x['detailId'] for x in test_l['items']];returned(b)
 bad('TV3_RETURN',{'items':[{'detailId':a,'returnOutcome':'BINH_THUONG'},{'detailId':b,'returnOutcome':'BINH_THUONG'}]},label='stale return aborts whole batch');check(sql(f'USE ThuVienDAU;SELECT TrangThai FROM ChiTietPhieuMuon WHERE IdChiTietMuon={a}')=='DANG_MUON','earlier return rolled back')
 # Concurrent checkout of one physical copy yields one committed loan only.
 ra=new_reader('RACE-A');rb=new_reader('RACE-B');race_copy=copy()[0]
 with ThreadPoolExecutor(2) as pool:res=list(pool.map(lambda rid:request('TV3_BORROW',{'readerId':rid,'copyIds':[race_copy]},librarian),[ra,rb]))
 check(sum(x['success'] for x in res)==1,'concurrent requests cannot borrow the same copy twice')
 # Lost book and late fine can coexist. Holiday policy is enforced on server, not checkbox payload.
 lost_r=new_reader('LOST-LATE');lost_c=copy(price=80000);lost_l=borrow(lost_r,[lost_c[0]]);detail=lost_l['items'][0]['detailId']
 sql(f"USE ThuVienDAU;UPDATE ChiTietPhieuMuon SET HanTra='2026-11-23 23:59:59' WHERE IdChiTietMuon={detail};")
 returned(detail,'MAT');bad('TV3_BORROW',{'readerId':lost_r,'copyIds':[copy()[0]]},label='unassessed lost/late return blocks borrowing');calendar(False)
 candidate(detail,'QUA_HAN');bad('TV3_VIOLATION_CREATE',{'detailId':detail,'violationType':'QUA_HAN','amount':1,'description':'Ignore calendar','confirmUnfinalizedCalendar':True},label='client cannot bypass unconfirmed holiday calendar')
 calendar(True,['2026-11-25']);c=candidate(detail,'QUA_HAN');check(c['chargeableDays']==2 and c['suggestedAmount']==4000,'student overdue excludes configured holiday')
 fine=violation(detail,'QUA_HAN',amount=1,chargeableDays=1);check(fine['amount']==4000,'server recomputes fine, ignores client amount/day count')
 c=candidate(detail,'MAT_SACH');check(c['suggestedAmount']==160000,'lost compensation suggestion is twice 80000')
 loss=violation(detail,'MAT_SACH',settlementMethod='DEN_TIEN',amount=1);check(loss['amount']==160000,'lost compensation is twice cover price')
 bad('TV3_VIOLATION_CREATE',{'detailId':detail,'violationType':'MAT_SACH','settlementMethod':'DEN_TIEN','description':'Duplicate'},label='duplicate compensation rejected')
 bad('TV3_BORROW',{'readerId':lost_r,'copyIds':[copy()[0]]},label='unpaid violation blocks borrowing')
 sql(f"USE ThuVienDAU;UPDATE DocGia SET DuocMuon=0,KhoaDen='2026-12-20 23:59:59',LyDoKhoa='Manager suspension',IdNguoiDoiQuyen={manager_id} WHERE IdDocGia={lost_r};")
 check(next(x for x in good('TV3_UNPAID',{}) if x['violationId']==fine['violationId'])['overdueDays']==2,'unpaid fine shows chargeable holiday-adjusted days')
 receipt=good('TV3_COLLECT',{'violationIds':[fine['violationId'],loss['violationId']],'paymentMethod':'TIEN_MAT','librarianAccountId':manager_id},label='collect multiple violations in one receipt')
 check(receipt['totalAmount']==164000 and not receipt['readerUnlocked'],'receipt total is correct without auto-unlock')
 check(sql(f'USE ThuVienDAU;SELECT DuocMuon FROM DocGia WHERE IdDocGia={lost_r}')=='0','payment preserves manager suspension and avoids KhoaDen constraint failure')
 check(sql(f'USE ThuVienDAU;SELECT IdNguoiThu FROM PhieuThu WHERE IdPhieuThu={receipt["receiptId"]}')==str(actor),'collector identity ignores spoofed payload ID')
 before=count('PhieuThu');bad('TV3_COLLECT',{'violationIds':[fine['violationId']],'paymentMethod':'TIEN_MAT'},label='double collection rejected');check(count('PhieuThu')==before,'double collection creates no receipt')
 for kind,ident in [('LOAN',lost_l['loanId']),('RECEIPT',receipt['receiptId'])]:check(len(good('TV3_RECEIPT',{'kind':kind,'id':ident})['lines'])>3,'print data '+kind)
 bad('TV3_VIOLATION_VOID',{'violationId':loss['violationId'],'reason':'Forbidden','managerAccountId':manager_id},label='librarian cannot void by entering manager ID')
 good('TV3_VIOLATION_VOID',{'violationId':loss['violationId'],'reason':'Manager documented waiver'},manager,'manager voids collected violation')
 check(count('PhieuThu')==before and sql(f'USE ThuVienDAU;SELECT DuocMuon FROM DocGia WHERE IdDocGia={lost_r}')=='0','void preserves receipt and borrowing suspension')
 bad('TV3_VIOLATION_VOID',{'violationId':loss['violationId'],'reason':'Again'},manager,'void is not repeatable')
 # A separate reader cannot be grouped into the existing reader receipt.
 unknown_r=new_reader('UNKNOWN-PRICE');unknown_l=borrow(unknown_r,[copy(price=None)[0]]);ud=unknown_l['items'][0]['detailId'];returned(ud,'MAT')
 check(not candidate(ud,'MAT_SACH')['hasBookPrice'],'unknown price stays unknown instead of default 100000')
 bad('TV3_VIOLATION_CREATE',{'detailId':ud,'violationType':'MAT_SACH','settlementMethod':'DEN_TIEN','assessedEquivalentPrice':90000,'description':'Staff guess'},label='only manager may assess missing cover price')
 u=violation(ud,'MAT_SACH',manager,settlementMethod='DEN_TIEN',assessedEquivalentPrice=90000);check(u['amount']==180000,'manager equivalent-price compensation')
 replace_r=new_reader('REPLACEMENT');replace_l=borrow(replace_r,[copy()[0]]);rd=replace_l['items'][0]['detailId'];returned(rd,'MAT');replacement=violation(rd,'MAT_SACH',settlementMethod='DEN_SACH');check(replacement['amount']==20000 and replacement['violationType']=='PHI_XU_LY','replacement book technical fee')
 before=count('PhieuThu');bad('TV3_COLLECT',{'violationIds':[u['violationId'],replacement['violationId']],'paymentMethod':'TIEN_MAT'},label='mixed-reader receipt rejected');check(count('PhieuThu')==before,'mixed-reader collection is atomic')
 hurt_r=new_reader('DAMAGED');hurt_l=borrow(hurt_r,[copy()[0]]);hd=hurt_l['items'][0]['detailId'];returned(hd,'HU_HONG');candidate(hd,'HONG_TRANG')
 bad('TV3_VIOLATION_CREATE',{'detailId':hd,'violationType':'HONG_TRANG','amount':5000,'description':'Repair pages'},label='page repair cost needs manager decision')
 damage=violation(hd,'HONG_TRANG',manager,amount=5000);check(damage['amount']==5000,'damage uses actual assessed page repair cost')
 sd=staff_loan['items'][0]['detailId'];sql(f"USE ThuVienDAU;UPDATE ChiTietPhieuMuon SET HanTra='2026-11-20 23:59:59' WHERE IdChiTietMuon={sd};");returned(sd)
 notice=violation(sd,'QUA_HAN',amount=999999);check(notice['amount']==0 and notice['violationType']=='THONG_BAO_DON_VI','staff overdue produces notice without monetary fine')
 check('THÔNG BÁO' in '\n'.join(good('TV3_RECEIPT',{'kind':'NOTICE','id':notice['violationId']})['lines']),'unit notice printable')
 # Reading room keeps school card (no new library card) and accepts checkout after closing.
 visit=good('TV3_VISIT_SCAN',{'readerCode':'TEST-STUDENT','lockerKey':'K01','confirmedPhysicalHandover':True},label='reading-room entry with card/key handover')
 bad('TV3_VISIT_SCAN',{'readerCode':'TEST-TEACHER','lockerKey':'K01','confirmedPhysicalHandover':True},label='locker key cannot be assigned twice')
 bad('TV3_VISIT_SCAN',{'readerCode':'TEST-TEACHER'},label='physical handover confirmation required')
 bad('TV3_VISIT_SCAN',{'readerCode':'TEST-STUDENT','lockerKey':'WRONG','confirmedPhysicalHandover':True},label='wrong returned locker key rejected')
 clock('2026-11-26T17:10:00+07:00');out=good('TV3_VISIT_SCAN',{'readerCode':'TEST-STUDENT','confirmedPhysicalHandover':True},label='checkout possible after closing');check(out['actionType']=='CHECK_OUT' and out['lockerKey']=='K01','scan toggles exit and returns assigned locker key')
 check(sql(f'USE ThuVienDAU;SELECT DaTraThe FROM LuotVaoPhongDoc WHERE IdLuotDoc={visit["visitId"]}')=='1','checkout records school-card return')
 clock('2026-11-26T09:00:00+07:00')
 # Reader can submit and see own feedback only, even with a forged code.
 feedback=good('TV3_FEEDBACK_SUBMIT',{'readerCode':'TEST-TEACHER','feedbackType':'GOP_Y','subject':'Fixture feedback','content':'Please improve service'},reader,'reader feedback derives identity from session')
 check(sql(f'USE ThuVienDAU;SELECT IdDocGia FROM PhanHoiDocGia WHERE IdPhanHoi={feedback}')=='1','reader cannot impersonate another reader in feedback')
 own=good('TV3_FEEDBACK_LIST',{},reader);check(all(x['readerId']==1 for x in own),'reader feedback listing scoped to self')
 bad('TV3_FEEDBACK_PROCESS',{'feedbackId':feedback,'newStatus':'DA_XU_LY','staffReply':'Fake reply'},reader,'reader cannot process feedback')
 good('TV3_FEEDBACK_PROCESS',{'feedbackId':feedback,'newStatus':'DA_XU_LY','staffReply':'Received, thank you'},label='staff processes feedback')
 own=good('TV3_FEEDBACK_LIST',{},reader);check(own[0]['staffReply']=='Received, thank you' and own[0]['processedAt'] is not None,'reply preserved in audit without extra schema columns')
 bad('TV3_FEEDBACK_PROCESS',{'feedbackId':feedback,'newStatus':'DA_XU_LY','staffReply':'Overwrite'},label='stale processed feedback cannot be overwritten')
 rep=good('TV3_STATISTICS',{'fromDate':'2026-11-01','toDate':'2026-12-31'},manager,'statistics independent aggregates')
 check(rep['totalCollectedReceiptsInPeriod']==164000 and rep['receiptCountInPeriod']==1,'statistics do not duplicate receipt money across fine items or discard voided receipts')
 bad('TV3_STATISTICS',{'fromDate':'2026-12-01','toDate':'2026-11-01'},manager,'invalid statistics interval rejected')
 bad('TV3_HISTORY',{'keyword':"' OR 1=1 --",'statusFilter':'BAD'},label='invalid history enum rejected')
 check(good('TV3_HISTORY',{'keyword':"' OR 1=1 --",'statusFilter':'TAT_CA'})==[],'history search parameterized')
 # A role/account change is observed for an already issued session.
 sql(f'USE ThuVienDAU;UPDATE TaiKhoan SET DangHoatDong=0 WHERE IdTaiKhoan={actor};')
 bad('TV3_HISTORY',{'statusFilter':'TAT_CA'},label='account locked after login loses circulation access')
 # Accounts without a linked reader profile must not receive staff-level feedback access.
 reader_account=next(x['id'] for x in accounts if x['role']=='DOC_GIA')
 sql(f'USE ThuVienDAU;UPDATE TaiKhoan SET IdDocGia=NULL WHERE IdTaiKhoan={reader_account};')
 bad('TV3_FEEDBACK_LIST',{},reader,'unlinked reader account denied instead of seeing all feedback')
 sql(f'USE ThuVienDAU;UPDATE TaiKhoan SET IdDocGia=1 WHERE IdTaiKhoan={reader_account};')
 (r/'kiem_tra/TV3_TCP_ket_qua.txt').write_text('\n'.join(logs)+'\n','utf8')
 print('TOTAL PASS:',len(logs),flush=True)
finally:
 if server is not None:server.terminate();server.wait(timeout=10)
 if dbproc is not None:
  run([mysqlbin/'mysqladmin.exe','--no-defaults','--protocol=TCP','-h127.0.0.1',f'-P{pdb}','-uroot','shutdown'])
  try:dbproc.wait(timeout=15)
  except subprocess.TimeoutExpired:dbproc.terminate();dbproc.wait(timeout=10)
 print('Stopped isolated TV3 fixtures.',flush=True)
