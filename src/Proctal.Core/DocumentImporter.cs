using System.IO.Compression;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
namespace Proctal.Core;
public sealed class DocumentImporter
{
    public async Task<ContextDocument> ImportAsync(string path)
    {
        var file=new FileInfo(path);if(file.Length>20*1024*1024)throw new InvalidDataException("Files must be smaller than 20 MB.");
        string text;
        switch(file.Extension.ToLowerInvariant())
        {
            case ".txt":case ".md":text=await File.ReadAllTextAsync(path);break;
            case ".docx":
                using(var zip=ZipFile.OpenRead(path))
                {
                    var entry=zip.GetEntry("word/document.xml")??throw new InvalidDataException("This DOCX has no document content.");
                    if(entry.Length>20*1024*1024)throw new InvalidDataException("The DOCX is too large when expanded.");
                    using var stream=entry.Open();var xml=XDocument.Load(stream);XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                    text=string.Join("\n",xml.Descendants(w+"p").Select(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))));
                }break;
            case ".pdf":
                text=await Task.Run(()=>{using var pdf=PdfDocument.Open(path);if(pdf.NumberOfPages>100)throw new InvalidDataException("Import PDFs with at most 100 pages.");return string.Join("\n",pdf.GetPages().Select(page=>ContentOrderTextExtractor.GetText(page)));});break;
            default:throw new InvalidDataException("Choose a PDF, DOCX, TXT, or Markdown file.");
        }
        if(string.IsNullOrWhiteSpace(text)||text.Count(char.IsLetter)<12)throw new InvalidDataException("No useful text was extracted. This may be a scanned document; use OCR or paste its text.");
        if(text.Length>150000)throw new InvalidDataException("This document is too long. Import a focused extract.");
        return new ContextDocument(Guid.NewGuid().ToString("N"),file.Name,text.Trim());
    }
}
