using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentManager;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TestCreator
{
    public class ExcelTestTableCreator
    {
        public void CreatePermutationTable(string docPath, string excelPath, string outputPath)
        {
            // 1. Read Requirements
            string conditionText = ReadConditionFromDoc(docPath);
            List<string> participatingParams = ExtractParamNames(conditionText);

            // 2. Read Parameter Values from Excel
            DocumentReader reader = new DocumentReader();
            string[,] excelData = reader.ReadExcelToTwoDimensionalArray(excelPath);
            var paramValuesMap = GetValuesForParams(excelData, participatingParams);

            // 3. Generate and Write
            ProcessAndWriteResults(conditionText, paramValuesMap, participatingParams, outputPath);
        }

        public void CreatePermutationTable(string docPath, Dictionary<string, List<string>> paramValues, string outputPath)
        {
            // 1. Read Requirements
            string conditionText = ReadConditionFromDoc(docPath);
            List<string> participatingParams = ExtractParamNames(conditionText);

            // 2. Filter provided values to only those participating
            var filteredValues = paramValues
                .Where(kvp => participatingParams.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // 3. Generate and Write
            ProcessAndWriteResults(conditionText, filteredValues, participatingParams, outputPath);
        }

        private void ProcessAndWriteResults(string conditionText, Dictionary<string, List<string>> paramValuesMap, List<string> participatingParams, string outputPath)
        {
            // Generate Permutations
            var permutations = GeneratePermutations(paramValuesMap.ToList(), 0);

            // Create Results
            var results = new List<Dictionary<string, string>>();
            LogicEvaluator evaluator = new LogicEvaluator();

            foreach (var perm in permutations)
            {
                bool result = evaluator.Evaluate(CleanCondition(conditionText), perm);
                var row = new Dictionary<string, string>(perm);
                row["Result"] = result.ToString();
                results.Add(row);
            }

            // Write to Excel
            WriteResultsToExcel(outputPath, results, participatingParams);
        }

        private string ReadConditionFromDoc(string path)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(path, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                return body?.InnerText ?? "";
            }
        }

        private string CleanCondition(string condition)
        {
            string cleaned = condition;
            // Remove "If " from the beginning
            if (cleaned.StartsWith("If ", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(3);

            // Remove " then ..." part if it exists (we evaluate the condition part)
            int thenIndex = cleaned.IndexOf(" then", StringComparison.OrdinalIgnoreCase);
            if (thenIndex > 0)
                cleaned = cleaned.Substring(0, thenIndex);

            return cleaned.Trim();
        }

        private List<string> ExtractParamNames(string text)
        {
            var matches = Regex.Matches(text, @"Param_\d+");
            return matches.Cast<Match>().Select(m => m.Value).Distinct().ToList();
        }

        private Dictionary<string, List<string>> GetValuesForParams(string[,] data, List<string> paramNames)
        {
            var result = new Dictionary<string, List<string>>();
            for (int r = 0; r < data.GetLength(0); r++)
            {
                string pName = data[r, 0];
                if (paramNames.Contains(pName))
                {
                    string valuesStr = data[r, 1];
                    var values = valuesStr.Split(',').Select(v => v.Trim()).ToList();
                    result[pName] = values;
                }
            }
            return result;
        }

        private List<Dictionary<string, string>> GeneratePermutations(List<KeyValuePair<string, List<string>>> paramList, int index)
        {
            if (index == paramList.Count)
            {
                return new List<Dictionary<string, string>> { new Dictionary<string, string>() };
            }

            var result = new List<Dictionary<string, string>>();
            var currentParam = paramList[index];
            var subPermutations = GeneratePermutations(paramList, index + 1);

            foreach (var val in currentParam.Value)
            {
                foreach (var sub in subPermutations)
                {
                    var newDict = new Dictionary<string, string>(sub);
                    newDict[currentParam.Key] = val;
                    result.Add(newDict);
                }
            }

            return result;
        }

        private void WriteResultsToExcel(string path, List<Dictionary<string, string>> results, List<string> headers)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                SheetData sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                Sheets sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = document.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "TestResults" };
                sheets.Append(sheet);

                uint rowIndex = 1;
                // Header
                Row headerRow = new Row { RowIndex = rowIndex };
                for (int c = 0; c < headers.Count; c++)
                {
                    headerRow.Append(CreateTextCell(headers[c], c, rowIndex));
                }
                headerRow.Append(CreateTextCell("Expected Result", headers.Count, rowIndex));
                sheetData.Append(headerRow);

                // Data
                foreach (var res in results)
                {
                    rowIndex++;
                    Row dataRow = new Row { RowIndex = rowIndex };
                    for (int c = 0; c < headers.Count; c++)
                    {
                        dataRow.Append(CreateTextCell(res[headers[c]], c, rowIndex));
                    }
                    dataRow.Append(CreateTextCell(res["Result"], headers.Count, rowIndex));
                    sheetData.Append(dataRow);
                }

                workbookPart.Workbook.Save();
            }
        }

        private Cell CreateTextCell(string text, int columnIndex, uint rowIndex)
        {
            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(text ?? ""),
                CellReference = GetColumnName(columnIndex) + rowIndex
            };
        }

        private string GetColumnName(int index)
        {
            int dividend = index + 1;
            string columnName = string.Empty;
            int modifier;

            while (dividend > 0)
            {
                modifier = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modifier).ToString() + columnName;
                dividend = (int)((dividend - modifier) / 26);
            }

            return columnName;
        }
    }
}
