from pathlib import Path
import subprocess,socket,json,struct,time,shutil,os,sys
r=Path(__file__).resolve().parents[2];work=r/'.tooling'/('tv2-fixture-'+str(time.time_ns()));work.mkdir(parents=True);data=work/'mysql-data';data.mkdir();bindir=work/'server-fixture';shutil.copytree(r/'server/bin/Release/net10.0',bindir,ignore=shutil.ignore_patterns('appsettings.local.json'))
mysqlbin=Path(sys.argv[1]) if len(sys.argv)>1 else Path('C:/Program Files/MySQL/MySQL Server 8.0/bin'); flags=subprocess.CREATE_NO_WINDOW
logs=[]
def port():
 with socket.socket() as s:s.bind(('127.0.0.1',0));return s.getsockname()[1]
def run(args,**kw):return subprocess.run(list(map(str,args)),capture_output=True,creationflags=flags,**kw)
pdb=port();pserver=port();dbproc=None;server=None
args=[mysqlbin/'mysql.exe','--no-defaults','--protocol=TCP','-h127.0.0.1',f'-P{pdb}','-uroot','--default-character-set=utf8mb4','--batch','--skip-column-names']
def sql(s):
 a=run(args,input=s.encode('utf8'))
 if a.returncode:raise RuntimeError(a.stderr.decode('utf8',errors='replace'))
 return a.stdout.decode('utf8',errors='replace').strip()
def check(ok,label):
 if not ok:raise AssertionError(label)
 logs.append('PASS '+label);print(logs[-1],flush=True)
def request(cmd,payload=None,token=None):
 req={'version':1,'requestId':str(time.time_ns()),'command':cmd,'payload':payload,'sessionToken':token}
 content=json.dumps(req,ensure_ascii=False).encode('utf8')
 with socket.create_connection(('127.0.0.1',pserver),timeout=15) as s:
  s.sendall(struct.pack('>i',len(content))+content)
  def exact(n):
   result=b''
   while len(result)<n:
    chunk=s.recv(n-len(result))
    if not chunk:raise EOFError('truncated response')
    result+=chunk
   return result
  length=struct.unpack('>i',exact(4))[0];return json.loads(exact(length))
try:
 a=run([mysqlbin/'mysqld.exe','--no-defaults','--initialize-insecure',f'--datadir={data}']);assert a.returncode==0,a.stderr
 dbproc=subprocess.Popen([str(mysqlbin/'mysqld.exe'),'--no-defaults',f'--datadir={data}',f'--port={pdb}','--bind-address=127.0.0.1','--mysqlx=OFF',f'--log-error={work}/mysql-fixture.log'],creationflags=flags)
 for i in range(120):
  if run(args+['-e','SELECT 1']).returncode==0:break
  time.sleep(.25)
 else:raise RuntimeError('fixture startup failed')
 sql((r/'csdl/01_Tao_CSDL.sql').read_text('utf8'))
 sql("USE ThuVienDAU;INSERT INTO TaiKhoan(TenDangNhap,MatKhauDaBam,VaiTro,DangHoatDong) VALUES('TV2_TEST',X'01','BIEN_MUC',1);CREATE USER 'fixture'@'127.0.0.1' IDENTIFIED BY 'test-only-password';GRANT ALL PRIVILEGES ON ThuVienDAU.* TO 'fixture'@'127.0.0.1';")
 account=int(sql('USE ThuVienDAU;SELECT IdTaiKhoan FROM TaiKhoan LIMIT 1;'))
 (bindir/'appsettings.local.json').write_text(json.dumps({'MySql':{'Server':'127.0.0.1','Port':pdb,'Database':'ThuVienDAU','User':'fixture','Password':'test-only-password','SslMode':'Preferred'}}),'utf8')
 server=subprocess.Popen(['dotnet',str(bindir/'ThuVienDAU.Server.dll'),'--port',str(pserver),'--tv2-demo-account',str(account)],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf8',creationflags=flags)
 first=server.stdout.readline();assert first.startswith('TV2 DEMO TOKEN'),first;token=first.split(': ',1)[1].strip()
 for i in range(50):
  try:request('PING');break
  except OSError:time.sleep(.1)
 check(not request('TV2_DDC_LIST',{},None)['success'],'unauthenticated catalog denied')
 check(not request('TV2_DDC_LIST',{},'0'*64)['success'],'forged session denied')
 rows=request('TV2_DDC_LIST',{'text':'','offset':0},token);check(rows['success'] and len(rows['data'])==40,'40 seeded DDC loaded without altering pending status')
 check(all(not x['daDoiChieu'] for x in rows['data']),'seed DDC not auto approved')
 check(request('TV2_DDC_LIST',{'text':"' OR 1=1 --",'offset':0},token)['data']==[],'parameterized search and literal matching')
 check(request('TV2_DDC_LIST',{'text':'','offset':100},token)['data']==[],'pagination end')
 ddc={'maDDC':'004.1','maCha':'004','tenMuc':'Muc THU NGHIEM','nguonThamKhao':'Du lieu gia lap, khong phai doi chieu sach that','daDoiChieu':False,'viTriTraBanIn':None,'cachGhepSo':None,'revision':''}
 a=request('TV2_DDC_SAVE',ddc,token);check(a['success'],'create pending DDC with leading zeros');ddc=a['data']
 base={'idDauTaiLieu':0,'nhanDe':'Sach thu nghiem tieng Viet','loaiTaiLieu':'SACH','maDDC':'004.1','trangThaiPhanLoai':'DA_XAC_NHAN','viTriTraDDC':'TRANG GIA LAP','dangHienThi':True,'phienBan':0}
 check(not request('TV2_TITLE_SAVE',base,token)['success'],'cannot confirm title using unverified DDC')
 bad=ddc|{'daDoiChieu':True};check(not request('TV2_DDC_SAVE',bad,token)['success'],'confirmation requires print location')
 a=request('TV2_DDC_SAVE',ddc|{'daDoiChieu':True,'viTriTraBanIn':'TRANG THU NGHIEM KHONG DUNG THAT'},token);check(a['success'],'DDC confirmation stores server actor/time');confirmed=a['data'];check(confirmed['idNguoiDoiChieu']==account and confirmed['thoiDiemDoiChieu'] is not None,'server owns approval metadata')
 check(not request('TV2_DDC_SAVE',ddc|{'tenMuc':'overwrite'},token)['success'],'stale DDC edit rejected')
 a=request('TV2_TITLE_SAVE',base,token);check(a['success'],'create classified title via TCP and real MySQL');title=a['data']
 a=request('TV2_TITLE_SAVE',title|{'nhanDe':'Da cap nhat'},token);check(a['success'] and a['data']['phienBan']>title['phienBan'],'title update increments version');updated=a['data']
 check(not request('TV2_TITLE_SAVE',title|{'nhanDe':'stale'},token)['success'],'stale title edit rejected')
 check(not request('TV2_DDC_SAVE',confirmed|{'daDoiChieu':False},token)['success'],'verified DDC used by title cannot be unapproved')
 check(not request('TV2_DDC_SAVE',confirmed|{'maCha':'004.1'},token)['success'],'self-parent DDC rejected')
 sql(f"USE ThuVienDAU;UPDATE TaiKhoan SET VaiTro='THU_THU' WHERE IdTaiKhoan={account};")
 check(not request('TV2_TITLE_SAVE',updated|{'nhanDe':'unauthorized'},token)['success'],'role checked in DB each command')
 sql(f"USE ThuVienDAU;UPDATE TaiKhoan SET VaiTro='BIEN_MUC',DangHoatDong=0 WHERE IdTaiKhoan={account};")
 check(not request('TV2_DDC_LIST',{},token)['success'],'inactive account rejected even with live token')
 check(sql("USE ThuVienDAU;SELECT NhanDe FROM DauTaiLieu LIMIT 1;")=='Da cap nhat','rejected commands do not change persisted title')
 check(int(sql("USE ThuVienDAU;SELECT COUNT(*) FROM NhatKy WHERE HanhDong IN ('TV2_DDC_SAVE','TV2_TITLE_SAVE');"))==4,'successful writes audited; failed transactions rolled back')
 check(sql("USE ThuVienDAU;SELECT JSON_UNQUOTE(JSON_EXTRACT(ChiTiet,'$.after.idNguoiDoiChieu')) FROM NhatKy WHERE HanhDong='TV2_DDC_SAVE' ORDER BY IdNhatKy DESC LIMIT 1;")==str(account),'DDC audit stores server identity rather than client metadata')
 sql(f"USE ThuVienDAU;UPDATE TaiKhoan SET DangHoatDong=1 WHERE IdTaiKhoan={account};")
 check(request('TV2_TITLE_LIST',{'text':'Da cap nhat','offset':0},token)['data'][0]['idDauTaiLieu']==title['idDauTaiLieu'],'staff title search returns saved data')
 # Development mode must never bind LAN.
 denied=run(['dotnet',bindir/'ThuVienDAU.Server.dll','--host','0.0.0.0','--tv2-demo-account',str(account)])
 check(denied.returncode!=0,'development identity forbidden on LAN')
 print('PASS all TV2 DB/TCP integration cases',flush=True)
 (r/'kiem_tra/TV2_TL01_TL02.txt').write_text('22/11/2026 - isolated temporary MySQL and copied server build; synthetic data only. No production config/data modified.\n'+'\n'.join(logs)+'\n','utf8')
finally:
 if server:server.kill();server.wait(timeout=10)
 if dbproc:
  run([mysqlbin/'mysqladmin.exe','--no-defaults','--protocol=TCP','-h127.0.0.1',f'-P{pdb}','-uroot','shutdown']);dbproc.wait(timeout=20)
