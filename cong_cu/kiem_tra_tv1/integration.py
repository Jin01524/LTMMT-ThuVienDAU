"""Real TCP server + isolated MySQL; no production database/config mutations."""
from pathlib import Path
from datetime import datetime,timedelta,timezone
import subprocess,socket,json,struct,time,shutil,os,sys,secrets,hashlib
from concurrent.futures import ThreadPoolExecutor
r=Path(__file__).resolve().parents[2];work=r/'.tooling'/('tv1-merge-fixture-'+str(time.time_ns()));work.mkdir(parents=True);data=work/'mysql-data';data.mkdir();bindir=work/'server-fixture';shutil.copytree(r/'server/bin/Release/net10.0',bindir,ignore=shutil.ignore_patterns('appsettings.local.json'))
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
 server=subprocess.Popen(['dotnet',str(bindir/'ThuVienDAU.Server.dll'),'--port',str(pserver)],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,creationflags=flags)
 for _ in range(80):
  try:request('PING');break
  except OSError:time.sleep(.1)
 else:raise RuntimeError('TCP server startup timeout')
 tokens={role:login(role) for role in ['QUAN_TRI','QUAN_LY_THU_VIEN','BIEN_MUC','THU_THU','DOC_GIA']};check(len(tokens)==5,'all five login roles work over TCP')
 admin=tokens['QUAN_TRI'];manager=tokens['QUAN_LY_THU_VIEN'];bm=tokens['BIEN_MUC'];reader=tokens['DOC_GIA'];librarian=tokens['THU_THU']
 ranges={'from':'2026-01-01T00:00:00','to':'2026-12-31T00:00:00'}
 def good(cmd,payload,token,label):
  v=request(cmd,payload,token);check(v['success'],label);return v['data']
 def bad(cmd,payload,token,label):check(not request(cmd,payload,token)['success'],label)
 good('TV1_READER_SEARCH',{'query':'TEST-STUDENT'},bm,'reader search retained after merge')
 bad('TV1_READER_SEARCH',{'query':'TEST'},None,'reader search needs authenticated frame token')
 bad('TV1_READER_SEARCH',{'query':'TEST'},reader,'reader cannot browse staff directory')
 bad('TV1_READER_SEARCH',{'query':'x'},bm,'short reader query rejected')
 check(good('TV1_READER_SEARCH',{'query':"' OR 1=1 --"},bm,'reader search parameterized')['readers']==[],'injection term does not return all readers')
 good('TV1_CONFIGURATION',{},admin,'system configuration JSON copied beside console server')
 bad('TV1_CONFIGURATION',{},reader,'reader cannot read system configuration')
 good('TV1_AUDIT',ranges,admin,'audit API retained over TCP')
 bad('TV1_AUDIT',ranges,bm,'cataloger cannot browse system audit')
 bad('TV1_AUDIT',{'from':ranges['to'],'to':ranges['from']},admin,'invalid audit date interval rejected')
 report=good('TV1_READER_REPORT',ranges,manager,'reader report retained over TCP');check(report['totalReaders']==2 and sum(x['count'] for x in report['byType'])==2,'report totals match fixture rows')
 check(good('TV1_READER_REPORT',ranges|{'readerType':'SINH_VIEN'},manager,'reader report filter works')['totalReaders']==1,'report student filter count')
 bad('TV1_READER_REPORT',ranges,librarian,'librarian cannot access reader report')
 permission={'readerId':1,'canBorrow':False,'reason':'Fixture borrow lock'}
 for t,label in [(bm,'cataloger'),(admin,'admin'),(librarian,'librarian')]:bad('TV1_BORROW_PERMISSION',permission,t,label+' cannot lock borrowing')
 good('TV1_BORROW_PERMISSION',permission,manager,'manager locks borrowing')
 check(sql('USE ThuVienDAU;SELECT DangHoatDong FROM TaiKhoan WHERE IdDocGia=1')=='1','borrow lock leaves reader login active')
 good('TV1_BORROW_PERMISSION',permission|{'canBorrow':True},manager,'manager unlocks borrowing')
 bad('TV1_BORROW_PERMISSION',permission|{'sessionId':manager},bm,'payload cannot impersonate manager frame token')
 target=next(a['id'] for a in accounts if a['role']=='DOC_GIA')
 good('AUTH_READER_STATUS',{'accountId':target,'isActive':False,'reason':'Fixture login lock'},bm,'cataloger locks reader login')
 a=next(a for a in accounts if a['role']=='DOC_GIA');bad('AUTH_LOGIN',{'username':a['username'],'password':a['password']},None,'locked reader cannot log in')
 check(sql('USE ThuVienDAU;SELECT DuocMuon FROM DocGia WHERE IdDocGia=1')=='1','login lock does not change borrowing')
 good('AUTH_READER_STATUS',{'accountId':target,'isActive':True,'reason':'Fixture login unlock'},bm,'cataloger unlocks reader login')
 bad('AUTH_READER_STATUS',{'accountId':target,'isActive':False,'reason':'Forbidden'},manager,'manager cannot lock reader login')
 good('TV2_DDC_LIST',{},bm,'real login session opens merged TV2 DDC')
 bad('TV2_DDC_LIST',{},reader,'reader cannot open TV2 catalog')
 bm_id=next(a['id'] for a in accounts if a['role']=='BIEN_MUC')
 sql(f'USE ThuVienDAU;UPDATE TaiKhoan SET DangHoatDong=0 WHERE IdTaiKhoan={bm_id}')
 bad('TV1_READER_SEARCH',{'query':'TEST'},bm,'disabled existing session denied by current DB account state')
 sql(f'USE ThuVienDAU;UPDATE TaiKhoan SET DangHoatDong=1 WHERE IdTaiKhoan={bm_id}');bm=login('BIEN_MUC')
 manager_id=next(a['id'] for a in accounts if a['role']=='QUAN_LY_THU_VIEN')
 sql(f"USE ThuVienDAU;UPDATE TaiKhoan SET VaiTro='THU_THU' WHERE IdTaiKhoan={manager_id}")
 bad('TV1_READER_REPORT',ranges,manager,'existing session rechecks changed DB role')
 sql(f"USE ThuVienDAU;UPDATE TaiKhoan SET VaiTro='QUAN_LY_THU_VIEN' WHERE IdTaiKhoan={manager_id}")
 good('AUTH_LOGOUT',{},bm,'logout command works')
 bad('TV1_READER_SEARCH',{'query':'TEST'},bm,'logged out session cannot use TV1')
 bad('TV2_DDC_LIST',{},bm,'logged out session cannot use TV2')
 sql(f"USE ThuVienDAU;INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet) SELECT {next(a['id'] for a in accounts if a['role']=='QUAN_TRI')},'TEST_LARGE','Test','0',JSON_OBJECT('value',REPEAT('x',3000)) FROM MucPhanLoaiDDC a CROSS JOIN MucPhanLoaiDDC b LIMIT 400;")
 oversized=request('TV1_AUDIT',ranges,admin);check(not oversized['success'] and '1 MiB' in oversized['error'],'oversized audit returns bounded actionable TCP error')
 sql("USE ThuVienDAU;DELETE FROM NhatKy WHERE HanhDong='TEST_LARGE'")
 fixture=work/'client-fixture.json';fixture.write_text(json.dumps({'port':pserver,'accounts':accounts}),'utf8')
 v=run(['dotnet','run','--project',r/'cong_cu/kiem_tra_tv1_client','--',fixture],cwd=r,env=os.environ.copy());print(v.stdout.decode('utf8',errors='replace'),flush=True);assert v.returncode==0,v.stderr.decode('utf8',errors='replace')
 check(True,'actual client service suite passes against same isolated server')
 (r/'kiem_tra/TV1_Merge_TCP.txt').write_text('25/11/2026 - TV1 merge with main; isolated synthetic MySQL/TCP only\n'+'\n'.join(logs)+'\n','utf8')
 print(f'PASS {len(logs)} server checks; fixture={work.name}',flush=True)
finally:
 if server:server.kill();server.wait(timeout=10)
 if dbproc:run([mysqlbin/'mysqladmin.exe','--no-defaults','--protocol=TCP','-h127.0.0.1',f'-P{pdb}','-uroot','shutdown']);dbproc.wait(timeout=20)
