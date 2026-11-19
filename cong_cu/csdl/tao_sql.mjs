import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {bang,ddl} from './cau_truc.mjs';
import {faculties,majors,ddc,orientations} from './du_lieu_ddc23.mjs';
import {marcFields,marcSubfields} from './du_lieu_marc21.mjs';
import {cauHinh} from './cau_hinh_du_an.mjs';
const here=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(here,'../..');
const q=x=>x==null?'NULL':"'"+String(x).replaceAll("'","''")+"'";
const put=(n,s)=>{fs.mkdirSync(path.dirname(path.join(root,n)),{recursive:true});fs.writeFileSync(path.join(root,n),s,'utf8');};
let seeds='\n/* Danh muc khoi tao; khong co tai khoan hay dau sach gia. */\n';
for(const [ma,ten]of faculties)seeds+=`INSERT INTO Khoa(MaKhoa,TenKhoa) VALUES(${q(ma)},${q(ten)});\n`;
for(const [ma,khoa,ten]of majors)seeds+=`INSERT INTO NganhDaoTao(MaNganh,IdKhoa,TenNganh) SELECT ${q(ma)},IdKhoa,${q(ten)} FROM Khoa WHERE MaKhoa=${q(faculties[khoa][0])};\n`;
for(const [ma,cha,ten,trang]of ddc)seeds+=`INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao) VALUES(${q(ma)},${q(cha)},${q(ten)},${q('https://www.oclc.org/content/dam/oclc/dewey/ddc23-summaries.pdf#page='+trang)});\n`;
const rules=fs.readFileSync(path.join(here,'quy_tac.sql'),'utf8')
 .replaceAll('__SV_TOIDA__',String(cauHinh.Muon.SinhVien.SoDauSachToiDa))
 .replaceAll('__CB_TOIDA__',String(cauHinh.Muon.CanBoGiangVien.SoDauSachToiDa))
 .replaceAll('__NHAN_KHONG_LAP__',marcFields.filter(x=>x[3]===0).map(x=>q(x[0])).join(','))
 .replaceAll('__NHAN_DA_BIET__',marcFields.map(x=>q(x[0])).join(','))
 .replaceAll('__TRUONGCON_KHONG_LAP__',marcSubfields.filter(x=>x[3]===0).map(x=>`(t.NhanTruong=${q(x[0])} AND t.MaTruongCon=${q(x[1])})`).join(' OR '));
const sql=ddl()+seeds+'\n'+rules;
if(bang.length!==20||/__[A-Z_]+__/.test(sql))throw new Error('Unexpected model/template');
put('csdl/01_Tao_CSDL.sql',sql);
put('csdl/Cau_hinh_du_an.json',JSON.stringify(cauHinh,null,2)+'\n');
put('tai_lieu/Dinh_huong_DDC_nganh.json',JSON.stringify({LuuY:'49 đề xuất tham khảo, chưa đối chiếu/duyệt; không tự gán mã cho sách.',DinhHuong:majors.map(([ma,khoa,ten])=>({MaNganh:ma,TenNganh:ten,Khoa:faculties[khoa][1],MaDDC:orientations[ma][0],CanCu:orientations[ma][1],DaDuyet:false}))},null,2)+'\n');
put('csdl/Cau_hinh_MARC21.json',JSON.stringify({hoSo:'THU_MUC_CO_BAN_DAU_1',phamVi:'MARC21 Bibliographic, danh mục chọn lọc',nguon:'https://www.loc.gov/marc/bibliographic/',truong:marcFields,truongCon:marcSubfields},null,2)+'\n');
let dict='# Từ điển CSDL thư viện DAU — bản gọn\n\n20 bảng. Tên tiếng Việt không dấu. BieuGhiMARC.Truong giữ các trường và trường con theo thứ tự. DauTaiLieu.DanhSachTep giữ danh sách tệp đính kèm. Schema tạo mới: ThuVienDAU.\n\n## Danh sách bảng\n\n';
for(const b of bang)dict+=`- ${b.ten} (${b.cot.length} cột)\n`;
for(const b of bang){
 dict+=`\n## ${b.ten}\n\n| Cột | Kiểu dữ liệu và ràng buộc |\n|---|---|\n`;
 for(const c of b.cot){const split=c.indexOf(' ');dict+=`| ${c.slice(0,split)} | ${c.slice(split+1).replaceAll('|','\\|')} |\n`;}
 const refs=b.rb.filter(r=>r.startsWith('FOREIGN KEY'));
 if(b.ten==='DocGia')refs.push('FOREIGN KEY (IdNguoiDoiQuyen) REFERENCES TaiKhoan(IdTaiKhoan)');
 if(refs.length)dict+='\nKhóa ngoại:\n\n'+refs.map(r=>'- '+r).join('\n')+'\n';
}
dict+='\n## View, trigger và thủ tục\n\n| Loại | Tên |\n|---|---|\n';
for(const m of rules.matchAll(/CREATE (VIEW|TRIGGER|PROCEDURE) (\w+)/g))dict+=`| ${m[1]} | ${m[2]} |\n`;
put('tai_lieu/Tu_dien_CSDL.md',dict);
export const stats={tables:bang.length,columns:bang.reduce((n,b)=>n+b.cot.length,0),foreignKeys:[...sql.matchAll(/FOREIGN KEY/g)].length,views:[...rules.matchAll(/CREATE VIEW/g)].length,triggers:[...rules.matchAll(/CREATE TRIGGER/g)].length,procedures:[...rules.matchAll(/CREATE PROCEDURE/g)].length};
console.log(JSON.stringify(stats));
