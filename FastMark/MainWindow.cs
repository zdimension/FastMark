using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfiumViewer;
using Image = System.Drawing.Image;
using PdfDocument = PdfiumViewer.PdfDocument;

namespace FastMark
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnPDF_Click(object sender, EventArgs e)
        {
            Process(false);
        }

        private void btnJPEG_Click(object sender, EventArgs e)
        {
            Process(true);
        }

        private const int PadWidth = 3;
        private static readonly string PadString = new string('X', PadWidth);

        private void Process(bool jpeg)
        {
            var wmText = txtWatermark.Text.Trim();
            var wmTextPad = wmText + PadString;
            var brush = new SolidBrush(Color.FromArgb(20, 0, 0, 0));
            
            foreach (var filename in Environment.GetCommandLineArgs()[1..])
            {
                try
                {
                    var ext = Path.GetExtension(filename).ToUpperInvariant();
                    Image[] images;
                    switch (ext)
                    {
                        case ".PDF":
                            var doc = PdfDocument.Load(filename);

                            images = Enumerable
                                .Range(0, doc.PageCount)
                                .Select(i => doc.Render(i, 120, 120,
                                    PdfRenderFlags.ForPrinting | PdfRenderFlags.CorrectFromDpi))
                                .ToArray();

                            break;
                        case ".JPEG" or ".JPG" or ".PNG":
                            images = new[] { Image.FromFile(filename) };
                            break;
                        default:
                            TaskDialog.ShowDialog(
                                this,
                                new TaskDialogPage()
                                {
                                    Icon = TaskDialogIcon.Error,
                                    Caption = "Unsupported file type",
                                    Text = "Unsupported file type, only PDFs, PNGs and JPEGs are supported."
                                });
                            return;
                    }

                    foreach (var image in images)
                    {
                        using var g = Graphics.FromImage(image);

                        var font = new System.Drawing.Font(FontFamily.GenericSansSerif, g.DpiX / 4);
                        var sz = g.MeasureString(wmTextPad, font);
                        var szY = sz.Height * 1.5f;

                        var hypot = (float) Math.Sqrt(image.Width * image.Width + image.Height * image.Height);
                        var countX = hypot / sz.Width;
                        var countY = hypot / szY;

                        g.RotateTransform(45);

                        var x = 0f;
                        var y = -(countY / 2) * szY;
                        for (var j = 0; j <= countY; j++, x = -sz.Width * 0.5f * (j % 2), y += szY)
                        {
                            for (var i = 0; i <= countX; i++, x += sz.Width)
                            {
                                g.DrawString(wmText, font, brush, x, y);
                            }
                        }

                    }

                    var outName = Path.GetFileNameWithoutExtension(filename) + ".wf";

                    if (jpeg)
                    {
                        var fmt = $"{{0:D{images.Length.ToString().Length}}}";
                        if (images.Length > 1)
                        {
                            for (var i = 0; i < images.Length; i++)
                            {
                                images[i].Save(outName + $".page{string.Format(fmt, i + 1)}.jpg");
                            }
                        }
                        else
                        {
                            images[0].Save(outName + ".jpg");
                        }
                    }
                    else
                    {
                        using var fp = File.Create(outName + ".pdf");
                        using var doc = new Document();
                        doc.SetMargins(0, 0, 0, 0);
                        using var pw = PdfWriter.GetInstance(doc, fp);
                        doc.Open();

                        foreach (var img in images)
                        {
                            using var ms = new MemoryStream();
                            img.Save(ms, ImageFormat.Jpeg);
                            ms.Position = 0;
                            var simg = iTextSharp.text.Image.GetInstance(ms.ToArray());
                            simg.ScaleToFit(doc.PageSize);
                            doc.Add(simg);
                        }

                        doc.Close();
                    }
                }
                catch (Exception e)
                {
                    TaskDialog.ShowDialog(
                        this,
                        new TaskDialogPage()
                        {
                            Icon = TaskDialogIcon.Error,
                            Caption = "Processing error",
                            Text = $"An error occurred while processing the file '{filename}'.",
                            Expander = new TaskDialogExpander(e.ToString())
                            {
                                CollapsedButtonText = "View error information",
                                ExpandedButtonText = "Hide error information"
                            },
                        });
                }
            }
            
            Close();
        }
    }
}