using ConnectionManager;
using DocumentManager;
using TestCreator;
using EnbeddedRequirmentToTest;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace RangeChecker
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "gen")
            {
                CreateDocx();
                return;
            }

            string requirementsPath = @"Assets\Requirements.docx";
            string excelPath = @"Assets\TestTable.xlsx";
            string outputPath = @"Assets\GeneratedTestTable.xlsx";

            Console.WriteLine("Generating Test Table...");
            ExcelTestTableCreator creator = new ExcelTestTableCreator();
            creator.CreatePermutationTable(requirementsPath, excelPath, outputPath);
            Console.WriteLine($"Test Table generated successfully at: {outputPath}");
        }

        static void CreateDocx()
        {
            string path = "OnePager.docx";
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                AddPara(body, "Test Engine & Generation Suite - One Pager", true, 24);
                AddPara(body, "Overview", true, 16);
                AddPara(body, "The application is a sophisticated test generation and verification suite designed to automate the lifecycle of embedded and logic-based requirements. It bridges the gap between static specifications (Word) and live system validation (TCP/UDP networking).", false, 11);

                AddPara(body, "Core Features", true, 16);
                AddPara(body, "- Advanced Logic Engine: Parsers complex conditions (AND, OR, XOR, NOT) from Word documents.", false, 11);
                AddPara(body, "- Automated Permutations: Generates Cartesian product test tables from Excel parameter sets.", false, 11);
                AddPara(body, "- Dual Protocol Support: Real-time UDP and TCP echo verification for test data.", false, 11);
                AddPara(body, "- Modern Dashboard: A high-end WPF interface inspired by industry-leading design standards.", false, 11);
                AddPara(body, "- Persistent Settings: Remembers user configurations and database connection strings.", false, 11);

                AddPara(body, "Technical Stack", true, 16);
                AddPara(body, "- Framework: .NET 8 / WPF", false, 11);
                AddPara(body, "- Libraries: OpenXML, SQL Client, System.Text.Json", false, 11);
                AddPara(body, "- Architecture: Modular multi-project solution.", false, 11);

                mainPart.Document.Save();
            }
            Console.WriteLine("OnePager.docx Created.");
        }

        static void AddPara(Body body, string text, bool bold, int fontSize)
        {
            Paragraph para = body.AppendChild(new Paragraph());
            Run run = para.AppendChild(new Run());
            RunProperties rp = new RunProperties();
            if (bold) rp.AppendChild(new Bold());
            rp.AppendChild(new FontSize { Val = (fontSize * 2).ToString() });
            run.AppendChild(rp);
            run.AppendChild(new Text(text));
        }
    }
}
