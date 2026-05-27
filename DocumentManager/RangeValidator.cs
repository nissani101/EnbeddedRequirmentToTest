namespace DocumentManager
{
    public class RangeValidator
    {
        public bool AreBothInNumbersInRange(double val1, double val2, double min, double max)
        {
            return (val1 >= min && val1 <= max) && (val2 >= min && val2 <= max);
        }

        public bool ValidateRange(double firstParam, double secondParam)
        {
            const double MinValue = 10.0;
            const double MaxValue = 100.0;

            if (firstParam != 0 && secondParam != 0)
            {
                bool isInRange = (firstParam >= MinValue && firstParam <= MaxValue) &&
                                 (secondParam >= MinValue && secondParam <= MaxValue);

                Console.WriteLine("\nAre both values in range? " + isInRange);
                return isInRange;
            }
            else
            {
                Console.WriteLine("\nError: One or more of the entered values are not valid numbers.");
                return false;
            }
        }
    }
}
