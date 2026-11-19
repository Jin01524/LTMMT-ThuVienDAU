// Dựng từ cùng mô hình sinh SQL. Cần @viz-js/viz và sharp.
// ERD_NODE_MODULES có thể trỏ tới node_modules của runtime đi kèm Codex.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {bang} from './cau_truc.mjs';
const here=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(here,'../..');
const require=createRequire(process.env.ERD_NODE_MODULES
  ? path.resolve(process.env.ERD_NODE_MODULES,'../package.json') : import.meta.url);
const {instance}=require('@viz-js/viz');
const sharp=require('sharp');
const xml=s=>s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const groups=[
 ['HỒ SƠ & TÀI KHOẢN','#156785',['Khoa','NganhDaoTao','DocGia','TaiKhoan']],
 ['TÀI LIỆU & KHO','#245cb5',['MucPhanLoaiDDC','DauTaiLieu','ThanhVienDeTai','ViTriLuuTru','BanTaiLieu']],
 ['MARC21','#6a47ad',['BieuGhiMARC']],
 ['MƯỢN / TRẢ & PHÒNG ĐỌC','#157b65',['PhieuMuon','ChiTietPhieuMuon','LuotVaoPhongDoc']],
 ['VI PHẠM & THU TIỀN','#b45a19',['ViPham','PhieuThu']],
 ['KIỂM KÊ & THANH LÝ','#a13e68',['KiemKe','ChiTietKiemKe','ThanhLy']],
 ['PHẢN HỒI & NHẬT KÝ','#526078',['PhanHoiDocGia','NhatKy']]
];
const keys=b=>b.rb.filter(x=>x.startsWith('FOREIGN KEY')).map(x=>{
 const m=x.match(/^FOREIGN KEY \(([^)]+)\) REFERENCES (\w+)\(([^)]+)\)$/);
 if(!m)throw new Error(x);
 return {child:b.ten,cols:m[1].split(',').map(x=>x.trim()),parent:m[2],refs:m[3].split(',').map(x=>x.trim())};
});
const edges=bang.flatMap(keys);
edges.push({child:'DocGia',cols:['IdNguoiDoiQuyen'],parent:'TaiKhoan',refs:['IdTaiKhoan']});
if(bang.length!==20||edges.length!==45)throw new Error('Model changed: review diagram layout/counts');
const names=new Set(bang.map(b=>b.ten));
if(groups.flatMap(g=>g[2]).length!==names.size||groups.flatMap(g=>g[2]).some(n=>!names.has(n)))throw new Error('Missing diagram table');
let dot=`digraph ERD {
 graph [rankdir=TB, bgcolor="#f5f7fb", pad=0.45, nodesep=0.50, ranksep=0.80,
  splines=spline, overlap=false, outputorder=edgesfirst, fontname="Arial", fontsize=30,
  labelloc=t, labeljust=l,
  label="THƯ VIỆN DAU  /  SƠ ĐỒ QUAN HỆ CSDL\n20 bảng · 160 cột · 45 khóa ngoại · MySQL 8.0\nDDC23 bản in tiếng Việt rút gọn · MARC21 Bibliographic\n "];
 node [shape=plain, fontname="Arial"];
 edge [fontname="Arial", fontsize=10, color="#7a8da7", penwidth=1.2, arrowsize=0.85, dir=both];
`;
for(const [label,color,tableNames] of groups){
 dot+=`subgraph cluster_${tableNames[0]} { label="${label}"; fontsize=18; fontcolor="${color}"; color="#d5ddea"; style="rounded"; bgcolor="#ffffff"; margin=18;\n`;
 for(const name of tableNames){
  const b=bang.find(x=>x.ten===name);
  const fks=new Set(edges.filter(e=>e.child===name).flatMap(e=>e.cols));
  const pks=new Set(b.cot.filter(c=>c.includes('PRIMARY KEY')).map(c=>c.split(' ')[0]));
  for(const r of b.rb){const m=r.match(/^PRIMARY KEY \(([^)]+)\)/);if(m)m[1].split(',').forEach(c=>pks.add(c.trim()));}
  dot+=`${name} [label=<<TABLE BORDER="0" CELLBORDER="0" CELLSPACING="0" CELLPADDING="5" BGCOLOR="#ffffff" COLOR="#cbd5e1">
   <TR><TD COLSPAN="3" BGCOLOR="${color}" ALIGN="LEFT" CELLPADDING="11"><FONT COLOR="white" POINT-SIZE="17"><B>${name}</B></FONT></TD></TR>\n`;
  for(const [i,c] of b.cot.entries()){
   const col=c.split(' ')[0],type=c.split(' ')[1];
   const tags=[pks.has(col)?'PK':'',fks.has(col)?'FK':'',c.includes(' UNIQUE')?'UQ':''].filter(Boolean).join('/');
   dot+=`<TR><TD WIDTH="48" BGCOLOR="${i%2?'#f0f4f9':'#f8fafd'}" ALIGN="LEFT"><FONT POINT-SIZE="10" COLOR="${pks.has(col)?'#9b650c':'#537393'}">${tags||' '}</FONT></TD><TD PORT="${col}" ALIGN="LEFT"><FONT POINT-SIZE="13" COLOR="#172b45">${pks.has(col)?'<B>':''}${xml(col)}${pks.has(col)?'</B>':''}</FONT></TD><TD ALIGN="LEFT"><FONT POINT-SIZE="11" COLOR="#607187">${xml(type)}${c.includes(' NOT NULL')||pks.has(col)?'':' ?'}${c.includes('GENERATED ALWAYS')?' *':''}</FONT></TD></TR>\n`;
  }
  dot+='</TABLE>>];\n';
 }
 dot+='}\n';
}
for(const e of edges){
 const child=bang.find(b=>b.ten===e.child);
 const nullable=e.cols.some(col=>!child.cot.find(c=>c.startsWith(col+' ')).includes('NOT NULL'));
 const unique=e.cols.length===1&&child.cot.some(c=>c.startsWith(e.cols[0]+' ')&&(c.includes('PRIMARY KEY')||c.includes('UNIQUE')))
   ||child.rb.some(r=>/^(PRIMARY KEY|UNIQUE)/.test(r)&&r.slice(r.indexOf('(')+1,r.indexOf(')')).split(',').map(x=>x.trim()).sort().join(',')===[...e.cols].sort().join(','));
 const actor=e.parent==='TaiKhoan';
 dot+=`${e.parent} -> ${e.child} [arrowtail="${nullable?'teeodot':'teetee'}",arrowhead="${unique?'teeodot':'crowodot'}",${actor?'style=dashed,color="#b0bacb",constraint=false,penwidth=0.85,':'color="#6e829c",'}tooltip="${e.child}(${e.cols.join(', ')}) → ${e.parent}(${e.refs.join(', ')})"];\n`;
}
dot+=`legend [label=<<TABLE BORDER="0" CELLBORDER="0" CELLSPACING="0" CELLPADDING="7" BGCOLOR="#eaf0f8">
 <TR><TD ALIGN="LEFT"><B>CHÚ GIẢI</B></TD></TR>
 <TR><TD ALIGN="LEFT">PK: khóa chính · FK: khóa ngoại · UQ: duy nhất một cột</TD></TR>
 <TR><TD ALIGN="LEFT">? : cho phép NULL · * : cột được tính tự động</TD></TR>
 <TR><TD ALIGN="LEFT">Một gạch kép: 1 · vòng tròn + gạch: 0..1 · vòng tròn + chân quạ: 0..N</TD></TR>
 <TR><TD ALIGN="LEFT">Nét đứt: tài khoản xử lý/nhật ký · Mọi khóa ngoại đều được vẽ</TD></TR>
 <TR><TD ALIGN="LEFT">Khóa ghép và CHECK chi tiết: xem 01_Tao_CSDL.sql / Tu_dien_CSDL.md</TD></TR>
 <TR><TD ALIGN="LEFT">Nguồn: cong_cu/csdl/cau_truc.mjs · 09/11/2026</TD></TR>
 </TABLE>>];
}`;
const viz=await instance();
const svg=viz.renderString(dot,{format:'svg',engine:'dot'});
const dir=path.join(root,'so_do');fs.mkdirSync(dir,{recursive:true});
fs.writeFileSync(path.join(dir,'So_do_CSDL.svg'),svg,'utf8');
const png=await sharp(Buffer.from(svg),{density:110,limitInputPixels:150000000}).png().toBuffer();
fs.writeFileSync(path.join(dir,'So_do_CSDL.png'),png);
console.log(JSON.stringify({tables:bang.length,columns:bang.reduce((n,b)=>n+b.cot.length,0),foreignKeys:edges.length,png:await sharp(png).metadata()}));
