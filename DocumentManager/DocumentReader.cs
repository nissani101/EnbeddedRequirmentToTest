using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentManager
{
    public class DocumentReader
    {
        public string[,] ReadExcelToTwoDimensionalArray(string filePath)
        {
            try
            {
                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
                {
                    WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart!;
                    Sheet? sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault();
                    if (sheet == null) return new string[0, 0];

                    WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                    IEnumerable<Row> rows = sheetData.Elements<Row>();

                    int maxRowIndex = 0;
                    int maxColIndex = 0;

                    foreach (var row in rows)
                    {
                        int rowIndex = (int)row.RowIndex!.Value;
                        if (rowIndex > maxRowIndex) maxRowIndex = rowIndex;

                        var cells = row.Elements<Cell>();
                        if (cells.Any())
                        {
                            var lastCell = cells.Last();
                            int colIndex = GetColumnIndex(lastCell.CellReference!) + 1;
                            if (colIndex > maxColIndex) maxColIndex = colIndex;
                        }
                    }

                    if (maxRowIndex == 0 || maxColIndex == 0) return new string[0, 0];

                    string[,] data = new string[maxRowIndex, maxColIndex];
                    SharedStringTablePart? sharedStringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();

                    foreach (Row row in rows)
                    {
                        int r = (int)row.RowIndex!.Value - 1;
                        foreach (Cell cell in row.Elements<Cell>())
                        {
                            int c = GetColumnIndex(cell.CellReference!);
                            if (r < maxRowIndex && c < maxColIndex)
                            {
                                data[r, c] = GetCellValue(cell, sharedStringTablePart);
                            }
                        }
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading Excel file: " + ex.Message);
                return new string[0, 0];
            }
        }

        private int GetColumnIndex(string cellReference)
        {
            string columnReference = Regex.Replace(cellReference, @"[\d]", "");
            int columnIndex = 0;
            int factor = 1;

            for (int i = columnReference.Length - 1; i >= 0; i--)
            {
                columnIndex += (columnReference[i] - 'A' + 1) * factor;
                factor *= 26;
            }

            return columnIndex - 1;
        }

        private string GetCellValue(Cell cell, SharedStringTablePart? sharedStringTablePart)
        {
            if (cell.CellValue == null) return string.Empty;

            string value = cell.CellValue.InnerText;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString && sharedStringTablePart != null)
            {
                return sharedStringTablePart.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
            }

            return value;
        }

        public List<TestData> ReadTestDataFromWordDocument(string filePath)
        {
            var testDataList = new List<TestData>();

            try
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                {
                    var mainPart = wordDoc.MainDocumentPart;
                    if (mainPart == null || mainPart.Document == null)
                    {
                        Console.WriteLine("Error: Document structure is invalid.");
                        return testDataList;
                    }

                    DocumentFormat.OpenXml.Wordprocessing.Table? table = null;
                    if (mainPart.Document.Body != null)
                    {
                        table = mainPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>().FirstOrDefault();
                    }

                    if (table == null)
                    {
                        Console.WriteLine("Error: Table not found. Ensure the file contains a standard table.");
                        return testDataList;
                    }

                    var rows = table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>().ToList();
                    if (rows.Count <= 1) return testDataList;

                    foreach (var row in rows.Skip(1))
                    {
                        var cells = row.Descendants<TableCell>().ToList();

                        if (cells.Count >= 5)
                        {
                            testDataList.Add(new TestData
                            {
                                Min = ParseDoubleSafely(cells[0].InnerText),
                                Max = ParseDoubleSafely(cells[1].InnerText),
                                Input1 = ParseDoubleSafely(cells[2].InnerText),
                                Input2 = ParseDoubleSafely(cells[3].InnerText),
                                Expected = cells[4].InnerText.Trim().ToLower().Contains("true")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical error reading file: " + ex.Message);
            }
            return testDataList;
        }

        private double ParseDoubleSafely(string text)
        {
            string cleaned = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            double.TryParse(cleaned, out double result);
            return result;
        }
    }
}
