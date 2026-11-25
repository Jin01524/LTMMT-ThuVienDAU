using System.Text.Json;
using ThuVienDAU.Catalog;
using ZXing;
using ZXing.Common;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();var logs=new List<string>();var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));var output=Path.Combine(root,".tooling/tv2-client-review");Directory.CreateDirectory(output);
        void Check(bool ok,string message){if(!ok)throw new Exception(message);logs.Add("PASS "+message);Console.WriteLine(logs[^1]);}
        using var doc=JsonDocument.Parse("""[{"NhanTruong":"008","NoiDungDieuKhien":"                                        "},{"NhanTruong":"650","ChiThi1":" ","ChiThi2":"4","TruongCon":[{"MaTruongCon":"a","NoiDung":"Chủ đề A"},{"MaTruongCon":"x","NoiDung":"Chi tiết"}]},{"NhanTruong":"650","ChiThi1":" ","ChiThi2":"4","TruongCon":[{"MaTruongCon":"a","NoiDung":"Chủ đề B"}]}]""");
        Check(JsonElement.DeepEquals(doc.RootElement,MarcEditorCodec.Encode(MarcEditorCodec.Decode(doc.RootElement))),"MARC editor preserves field/subfield order, repeats, blank indicators and fixed spaces");
        var rows=MarcEditorCodec.Decode(doc.RootElement);rows.Add(rows[1]);try{MarcEditorCodec.Encode(rows);throw new Exception("Expected invalid noncontiguous field group");}catch(InvalidOperationException){Check(true,"MARC editor rejects ambiguous noncontiguous groups");}
        foreach(var code in new[]{"TV2-BC01","abc-0123456789","CODE !@$%","1234567890123456789012345678901234567890"})
        {
            using var bitmap=new Bitmap(1200,450);using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.ScaleTransform(3,3);LabelRenderer.Draw(g,new CopyItem(1,1,"DK-001",code,1,"004.1 BINH","SAN_SANG","TOT",100000,null),new(0,0,370,140));}
            var rgb=new byte[bitmap.Width*bitmap.Height*3];int offset=0;for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){var c=bitmap.GetPixel(x,y);rgb[offset++]=c.R;rgb[offset++]=c.G;rgb[offset++]=c.B;}
            var reader=new BarcodeReaderGeneric{Options=new DecodingOptions{TryHarder=true,PossibleFormats=new[]{BarcodeFormat.CODE_128}}};var found=reader.Decode(new RGBLuminanceSource(rgb,bitmap.Width,bitmap.Height,RGBLuminanceSource.BitmapFormat.RGB24));
            Check(found?.Text==code,"printed barcode decodes exactly: "+code);bitmap.Save(Path.Combine(output,"label-"+logs.Count+".png"));
        }
        foreach(var invalid in new[]{"",new string('A',41),"Sách-01"})try{LabelRenderer.Encode(invalid);throw new Exception("Expected invalid barcode");}catch(InvalidOperationException){Check(true,"unsupported barcode rejected before print");}
        var forms=new Form[]{new CatalogWorkspaceForm(),new CatalogWorkspaceForm(new ThuVienDAU.Models.SessionInfo("REAL-SESSION-TEST",1,null,"fixture","BIEN_MUC","Bien muc fixture")),new MarcForm("test-token"),new ProjectForm("test-token"),new StocktakeForm("test-token"),new DisposalForm("test-token"),new StockReportForm("test-token"),new LabelForm("test-token")};
        foreach(var form in forms)
        {
            using(form)
            {
                // Construct actual native controls off-screen. Suppress initial API loading only;
                // this render check deliberately does not pretend to exercise user interaction.
                var events=(System.ComponentModel.EventHandlerList)typeof(System.ComponentModel.Component).GetProperty("Events",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(form)!;
                foreach(var field in typeof(Form).GetFields(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Where(f=>f.Name.Contains("shown",StringComparison.OrdinalIgnoreCase)))
                {var key=field.GetValue(null);if(key is not null&&events[key] is Delegate handler)events.RemoveHandler(key,handler);}
                form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();form.PerformLayout();
                if(form is CatalogWorkspaceForm)
                {
                    var panel=form.Controls.OfType<FlowLayoutPanel>().Single();
                    var token=panel.Controls.OfType<TextBox>().Single();
                    if(token.Text=="REAL-SESSION-TEST")
                    {
                        Check(!token.Visible,"authenticated TV2 workspace hides manual demo token field");
                        Check(panel.Controls.OfType<Label>().Any(x=>x.Text.Contains("BIEN_MUC")),"authenticated TV2 workspace shows current login identity");
                        Check(panel.Controls.OfType<Button>().Count()==10,"authenticated workspace retains all 10 screens for 9 TV2 modules");
                    }
                }
                using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(Path.Combine(output,form.GetType().Name+".png"));form.Hide();Check(form.Controls.Count>0,"construct and render "+form.GetType().Name);
            }
        }
        File.WriteAllLines(Path.Combine(root,"kiem_tra/TV2_Client.txt"),new[]{"25/11/2026 - model roundtrip, real barcode image decode, form construction/render only; no manual UI or hardware claim"}.Concat(logs));Console.WriteLine($"PASS {logs.Count} client checks");
    }
}
