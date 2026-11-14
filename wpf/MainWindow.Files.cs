using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace InkBoard
{
    /// <summary>文件读写与导出</summary>
    public partial class MainWindow
    {
        /* ------------------------------ 文件 ------------------------------ */
        void OpenDoc()
        {
            var dlg = new OpenFileDialog { Filter = "白板工程 (*.json)|*.json|所有文件|*.*" };
            if (dlg.ShowDialog() != true) return;
            LoadDocFrom(dlg.FileName);
        }

        void LoadDocFrom(string path)
        {
            try
            {
                var text = File.ReadAllText(path);
                var d = JsonSerializer.Deserialize<Doc>(text, History.Json);
                if (d == null || d.Pages.Count == 0) { MessageBox.Show("文件内容无效"); return; }
                _doc = d;
                _board.SetDoc(d);
                _history.Reset(d);
                _board.FitPage();
                RefreshPages();
            }
            catch (Exception ex) { MessageBox.Show("打开失败：" + ex.Message); }
        }

        void SaveDoc()
        {
            var dlg = new SaveFileDialog { Filter = "白板工程 (*.json)|*.json", FileName = "板书.json" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(_doc, History.Json));
            }
            catch (Exception ex) { MessageBox.Show("保存失败：" + ex.Message); }
        }

        void ExportPng()
        {
            var dlg = new SaveFileDialog { Filter = "PNG 图片 (*.png)|*.png", FileName = "板书.png" };
            if (dlg.ShowDialog() != true) return;
            var bmp = _board.ExportPage(_doc.Cur, 2.0);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(dlg.FileName)) enc.Save(fs);
        }

        void ExportPdf()
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;
            var bmp = _board.ExportPage(_doc.Cur, 2.0);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
                dc.DrawImage(bmp, new Rect(0, 0, dlg.PrintableAreaWidth, dlg.PrintableAreaHeight));
            dlg.PrintVisual(dv, "School Tools Board");
        }

        async void ImportPdf()
        {
            var dlg = new OpenFileDialog { Filter = "PDF 文件 (*.pdf)|*.pdf" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                int n = await PdfImport.ImportAsync(dlg.FileName, _doc, s => Title = "School Tools Board · " + s);
                _board.SetDoc(_doc);
                _board.FitPage();
                RefreshPages();
                _history.Reset(_doc);
                MessageBox.Show(string.Format("已导入 {0} 页", n));
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF 导入失败：" + ex.Message + "\n（需要 Windows 10 1809 及以上系统）");
                RefreshPages();
            }
        }

        void OnDropFile(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            var f = files[0];
            if (f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _pendingPdf = f;
                DoPendingPdf();
            }
            else LoadDocFrom(f);
        }

        string _pendingPdf;

        async void DoPendingPdf()
        {
            if (_pendingPdf == null) return;
            try
            {
                int n = await PdfImport.ImportAsync(_pendingPdf, _doc, s => Title = "School Tools Board · " + s);
                _pendingPdf = null;
                _board.SetDoc(_doc);
                _board.FitPage();
                RefreshPages();
                _history.Reset(_doc);
            }
            catch (Exception ex) { MessageBox.Show("PDF 导入失败：" + ex.Message); }
        }

    }
}
