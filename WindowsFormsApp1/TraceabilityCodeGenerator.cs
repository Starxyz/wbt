using System;

namespace WindowsFormsApp1
{
    public static class TraceabilityCodeGenerator
    {
        private static readonly DateTime factoryStartDate = DateTime.Parse("2022-01-01");

        public static string GenerateTraceabilityCode()
        {
            // Get current date
            DateTime currentDate = DateTime.Now;

            // 1. First character: Factory code (fixed as 'N')
            char factoryCode = 'N';

            // 2. Second character: First digit of the day
            int dayFirstDigit = currentDate.Day / 10;
            char[] dayFirstDigitMap = { 'A', 'B', 'C', 'D' }; // 0:A, 1:B, 2:C, 3:D
            char mappedDayFirstDigit = dayFirstDigitMap[dayFirstDigit];

            // 3. Third character: Last digit of the day
            int dayLastDigit = currentDate.Day % 10;
            char mappedDayLastDigit = (char)('A' + dayLastDigit); // 0:A, 1:B, ..., 9:J

            // 4. Fourth character: Years of factory operation
            int yearsOfOperation = currentDate.Year - factoryStartDate.Year;
            if (currentDate < factoryStartDate.AddYears(yearsOfOperation))
            {
                yearsOfOperation--; // Adjust if we haven't reached the anniversary date yet
            }
            // Ensure the value is between 1 and 10
            yearsOfOperation = Math.Max(1, Math.Min(10, yearsOfOperation));
            char mappedYearsOfOperation = (char)('A' + (yearsOfOperation - 1)); // 1:A, 2:B, ..., 10:J

            // 5. Fifth character: Month of production date
            int month = currentDate.Month;
            char mappedMonth = (char)('A' + (month - 1)); // 1:A, 2:B, ..., 12:L

            // Combine all characters to form the traceability code
            string traceabilityCode = new string(new[] {
                factoryCode,
                mappedDayFirstDigit,
                mappedDayLastDigit,
                mappedYearsOfOperation,
                mappedMonth
            });

            return traceabilityCode;
        }
    }
}
