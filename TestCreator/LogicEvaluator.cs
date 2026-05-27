using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TestCreator
{
    public class LogicEvaluator
    {
        public bool Evaluate(string expression, Dictionary<string, string> values)
        {
            // 1. Pre-process expression: replace Param_X with their values
            string processed = expression;
            foreach (var kvp in values)
            {
                // Use regex to replace exact param name only (avoid Param_1 matching Param_10)
                processed = Regex.Replace(processed, $@"\b{kvp.Key}\b", QuoteValue(kvp.Value));
            }

            // 2. Tokenize and Evaluate
            try
            {
                return EvaluateExpression(processed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating expression: {ex.Message}");
                return false;
            }
        }

        private string QuoteValue(string value)
        {
            if (double.TryParse(value, out _)) return value;
            return $"\"{value}\"";
        }

        private bool EvaluateExpression(string expression)
        {
            // Handle logical OR
            var parts = SplitByOperator(expression, "OR");
            if (parts.Count > 1)
            {
                bool result = false;
                foreach (var part in parts)
                    result |= EvaluateExpression(part);
                return result;
            }

            // Handle logical XOR
            parts = SplitByOperator(expression, "XOR");
            if (parts.Count > 1)
            {
                bool result = EvaluateExpression(parts[0]);
                for (int i = 1; i < parts.Count; i++)
                    result ^= EvaluateExpression(parts[i]);
                return result;
            }

            // Handle logical AND
            parts = SplitByOperator(expression, "AND");
            if (parts.Count > 1)
            {
                bool result = true;
                foreach (var part in parts)
                    result &= EvaluateExpression(part);
                return result;
            }

            // Handle logical NOT
            expression = expression.Trim();
            if (expression.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase))
            {
                return !EvaluateExpression(expression.Substring(4));
            }

            // Handle Parentheses
            if (expression.StartsWith("(") && expression.EndsWith(")"))
            {
                return EvaluateExpression(expression.Substring(1, expression.Length - 2));
            }

            // Handle Boolean Literals
            if (bool.TryParse(expression, out bool boolVal)) return boolVal;

            // Handle Comparisons
            return EvaluateComparison(expression);
        }

        private bool EvaluateComparison(string expression)
        {
            var match = Regex.Match(expression, @"(.*?)\s*(==|!=|>=|<=|>|<)\s*(.*)");
            if (!match.Success) return false;

            string left = match.Groups[1].Value.Trim().Trim('"');
            string op = match.Groups[2].Value;
            string right = match.Groups[3].Value.Trim().Trim('"');

            double lVal, rVal;
            bool isLeftNum = double.TryParse(left, out lVal);
            bool isRightNum = double.TryParse(right, out rVal);

            if (isLeftNum && isRightNum)
            {
                switch (op)
                {
                    case "==": return lVal == rVal;
                    case "!=": return lVal != rVal;
                    case ">": return lVal > rVal;
                    case "<": return lVal < rVal;
                    case ">=": return lVal >= rVal;
                    case "<=": return lVal <= rVal;
                }
            }
            else
            {
                // String comparison
                int cmp = string.Compare(left, right);
                switch (op)
                {
                    case "==": return cmp == 0;
                    case "!=": return cmp != 0;
                    case ">": return cmp > 0;
                    case "<": return cmp < 0;
                    case ">=": return cmp >= 0;
                    case "<=": return cmp <= 0;
                }
            }

            return false;
        }

        private List<string> SplitByOperator(string expression, string op)
        {
            var result = new List<string>();
            int parenLevel = 0;
            int lastIndex = 0;

            for (int i = 0; i < expression.Length; i++)
            {
                if (expression[i] == '(') parenLevel++;
                else if (expression[i] == ')') parenLevel--;
                else if (parenLevel == 0)
                {
                    if (i + op.Length <= expression.Length && 
                        expression.Substring(i, op.Length).Equals(op, StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if it's a whole word
                        bool isWord = true;
                        if (i > 0 && char.IsLetterOrDigit(expression[i - 1])) isWord = false;
                        if (i + op.Length < expression.Length && char.IsLetterOrDigit(expression[i + op.Length])) isWord = false;

                        if (isWord)
                        {
                            result.Add(expression.Substring(lastIndex, i - lastIndex));
                            lastIndex = i + op.Length;
                            i = lastIndex - 1;
                        }
                    }
                }
            }

            result.Add(expression.Substring(lastIndex));
            return result;
        }
    }
}
