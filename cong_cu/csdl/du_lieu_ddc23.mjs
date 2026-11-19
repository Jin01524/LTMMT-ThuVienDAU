// Faculty/degree names and codes are supplied by the user on 2026-11-08.
export const faculties=[
 ['KHOA_KIEN_TRUC','Khoa Kiến trúc'],['KHOA_TK_NGHE_THUAT','Khoa Thiết kế & Nghệ thuật'],
 ['KHOA_XAY_DUNG','Khoa Xây dựng'],['KHOA_CAU_DUONG','Khoa Cầu đường'],['KHOA_CNTT','Khoa Công nghệ Thông tin'],
 ['KHOA_DIEN_DIEN_TU','Khoa Kỹ thuật Điện - Điện tử'],['KHOA_KINH_TE','Khoa Kinh tế'],
 ['KHOA_NGOAI_NGU','Khoa Ngoại ngữ'],['KHOA_DU_LICH','Khoa Du lịch']
];
export const majors=[
 ['7580101',0,'Kiến trúc'],['7580105',0,'Quy hoạch vùng và đô thị'],['7580108',0,'Thiết kế nội thất'],
 ['7210403',1,'Thiết kế đồ họa'],['7580201',2,'Kỹ thuật xây dựng'],['7580302',2,'Quản lý xây dựng'],
 ['7580205',3,'Kỹ thuật xây dựng công trình giao thông'],['7580210',3,'Kỹ thuật cơ sở hạ tầng'],
 ['7480201',4,'Công nghệ thông tin'],['7510301',5,'Công nghệ kỹ thuật Điện – Điện tử'],
 ['7340101',6,'Quản trị kinh doanh'],['7340201',6,'Tài chính – Ngân hàng'],['7340301',6,'Kế toán'],
 ['7510605',6,'Logistics và Quản lý chuỗi cung ứng'],['7220201',7,'Ngôn ngữ Anh'],['7220204',7,'Ngôn ngữ Trung Quốc'],
 ['7810103',8,'Quản trị dịch vụ du lịch và lữ hành'],['7810201',8,'Quản trị khách sạn']
];
// Selected public-summary numbers, NOT the full schedules or an authorized Vietnamese translation.
// Labels are short local paraphrases. PDF positions are physical PDF page numbers (1-based).
export const ddc=[
 ['000',null,'Tin học, thông tin, tổng quát',6],['100',null,'Triết học, tâm lý',6],['200',null,'Tôn giáo',6],
 ['300',null,'Xã hội',6],['400',null,'Ngôn ngữ',6],['500',null,'Khoa học',6],['600',null,'Công nghệ',6],
 ['700',null,'Nghệ thuật, giải trí',6],['800',null,'Văn học',6],['900',null,'Lịch sử, địa lý',6],
 ['330','300','Kinh tế',7],['380','300','Thương mại, vận tải',7],['420','400','Tiếng Anh',7],
 ['490','400','Ngôn ngữ khác',7],['620','600','Kỹ thuật',7],['640','600','Gia đình',7],
 ['650','600','Quản trị',7],['690','600','Xây dựng nhà',7],['710','700','Quy hoạch, cảnh quan',7],
 ['720','700','Kiến trúc',7],['740','700','Thiết kế, nghệ thuật',7],['910','900','Địa lý, du hành',7],
 ['004','000','Tin học',8],['005','000','Lập trình, dữ liệu',8],['006','000','Phương pháp tin học',8],
 ['332','330','Tài chính',11],['388','380','Vận tải',11],['495','490','Ngôn ngữ Đông/Đông Nam Á',12],
 ['621','620','Vật lý ứng dụng',14],['624','620','Công trình dân dụng',14],['625','620','Đường bộ, đường sắt',14],
 ['627','620','Thủy công',14],['628','620','Môi trường, vệ sinh',14],['641','640','Ăn uống',14],
 ['647','640','Quản lý cơ sở phục vụ chung',14],['657','650','Kế toán',14],['658','650','Quản lý chung',14],
 ['711','710','Quy hoạch khu vực',15],['741','740','Vẽ',15],['747','740','Trang trí nội thất',15]
];
// These are project proposals for discovery, NOT OCLC mappings or final classifications.
export const orientations={
 '7580101':[['720','624','690'],'Tài liệu kiến trúc có thể liên quan kết cấu và thi công; chọn mã theo nội dung từng cuốn.'],
 '7580105':[['711','710','388'],'Phân biệt quy hoạch không gian, cảnh quan và nghiên cứu vận tải.'],
 '7580108':[['747','720','740'],'Phân biệt trang trí, tổ chức không gian và thiết kế; không quy mọi tài liệu nội thất vào một mã.'],
 '7210403':[['740','741','006'],'Chọn lớp rộng để định hướng thiết kế/vẽ và công cụ số; mã chi tiết cần tra bản in.'],
 '7580201':[['624','690','620'],'Phân biệt kỹ thuật công trình với quy trình xây dựng nhà.'],
 '7580302':[['690','658','624'],'Phân biệt quản lý dự án/nguồn lực với nội dung kỹ thuật xây dựng.'],
 '7580205':[['625','624','388'],'Phân biệt thiết kế hạ tầng đường với hoạt động vận tải.'],
 '7580210':[['624','627','628','625'],'Hạ tầng gồm nhiều chuyên đề; tra nội dung công trình, nước, môi trường và đường.'],
 '7480201':[['004','005','006'],'Phân biệt cơ sở tin học, phần mềm/dữ liệu và phương pháp ứng dụng.'],
 '7510301':[['621','620','004'],'621 chỉ là định hướng rộng; điện/điện tử cần tra phân mục chi tiết trong bộ bản in.'],
 '7340101':[['658','330'],'Phân biệt quản lý tổ chức với lý thuyết kinh tế.'],
 '7340201':[['332','330'],'Phân biệt nội dung tài chính/ngân hàng với kinh tế học tổng quát.'],
 '7340301':[['657','332'],'Chọn theo trọng tâm kế toán hay tài chính của tài liệu.'],
 '7510605':[['658','388','380'],'Chuỗi cung ứng có góc độ quản trị, vận tải và thương mại; không có ánh xạ một-một.'],
 '7220201':[['420','400'],'Phạm vi định hướng là ngôn ngữ; tác phẩm văn học cần tra phân loại riêng.'],
 '7220204':[['495','490'],'495 rộng hơn tiếng Trung; tra bản in để chọn phân mục phù hợp, không dùng làm mã riêng của ngành.'],
 '7810103':[['910','658'],'Tài liệu lữ hành có thể thiên về điểm đến hoặc quản trị; các góc độ kinh tế khác phải tra thêm.'],
 '7810201':[['647','658','641'],'Phân biệt điều hành cơ sở lưu trú, quản trị doanh nghiệp và dịch vụ ăn uống.']
};
