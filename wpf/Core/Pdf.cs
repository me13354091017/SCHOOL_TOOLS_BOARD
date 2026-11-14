using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace InkBoard
{
    /// <summary>PDF 导入：用系统自带 Windows.Data.Pdf 渲染成页面底图（无需第三方库）</summary>
    public static class PdfImport
    {
        public const int MaxPages = 200;

        public static async Task<int> ImportAsync(string path, Doc doc, Action<string> status)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var pdf = await PdfDocument.LoadFromFileAsync(file);

            int count = Math.Min((int)pdf.PageCount, MaxPages);
            int startIndex = doc.Pages.Count == 1 && doc.Pages[0].Objects.Count == 0 ? 0 : doc.Pages.Count;

            for (uint i = 0; i < count; i++)
            {
                if (status != null) status(string.Format("正在导入第 {0}/{1} 页…", i + 1, count));
                BitmapSource bmp = await RenderPage(pdf, i);
                Page page;
                if (i == 0 && startIndex == 0)
                {
                    page = doc.Pages[0];
                }
                else
                {
                    page = new Page();
                    doc.Pages.Add(page);
                }
                page.BackImage = bmp;
                page.Bg = "plain";
                page.Color = "#ffffff";
            }

            if (count > 0 && doc.Pages.Count > 0)
                doc.Current = startIndex;
            return count;
        }

        static async Task<BitmapSource> RenderPage(PdfDocument pdf, uint index)
        {
            using (var page = pdf.GetPage(index))
            {
                var ras = new InMemoryRandomAccessStream();
                var opt = new PdfPageRenderOptions { DestinationWidth = (uint)Math.Min(2400, (int)page.Size.Width * 2) };
                await page.RenderToStreamAsync(ras, opt);

                ras.Seek(0);
                uint size = (uint)ras.Size;
                var reader = new DataReader(ras.GetInputStreamAt(0));
                await reader.LoadAsync(size);
                var bytes = new byte[size];
                reader.ReadBytes(bytes);
                reader.Dispose();

                var ms = new MemoryStream(bytes);
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }
    }
}
