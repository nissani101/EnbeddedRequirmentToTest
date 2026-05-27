using ConnectionManager;
using DocumentManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnbeddedRequirmentToTest
{
    public class PreRequisites
    {
        public void ReadWordFile()
        {
            // Solve encoding issues
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;

            string docPath = @"C:\Temp\Req.docx";

            // Both classes are now recognized in their respective namespaces
            DocumentReader reader = new DocumentReader();
            RangeValidator validator = new RangeValidator();

            Console.WriteLine("Loading data from Word document and performing checks...");
            Console.WriteLine();

            var tests = reader.ReadTestDataFromWordDocument(docPath);

            if (tests.Count == 0)
            {
                Console.WriteLine("No data found in table or file not found.");
            }
            else
            {
                Console.WriteLine($"{"Input1",-8} | {"Input2",-8} | {"Result",-8} | {"Expected",-8} | {"Status"}");
                Console.WriteLine(new string('-', 60));

                foreach (var test in tests)
                {
                    bool actual = validator.AreBothInNumbersInRange(test.Input1, test.Input2, test.Min, test.Max);
                    string status = (actual == test.Expected) ? "PASS" : "FAIL";

                    Console.WriteLine($"{test.Input1,-8} | {test.Input2,-8} | {actual,-8} | {test.Expected,-8} | {status}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        public void OpenUdpSocket()
        {
            UdpSocketManager udpManager = new UdpSocketManager();
            // Open local socket on port 5001 (e.g., "127.0.0.1")
            udpManager.StartListening("0.0.0.0", 5001);
        }
    }
}
