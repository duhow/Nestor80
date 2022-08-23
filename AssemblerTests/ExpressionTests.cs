﻿using NUnit.Framework;
using Konamiman.Nestor80.Assembler;

namespace Konamiman.Nestor80.AssemblerTests
{
    [TestFixture]
    public class ExpressionTests
    {
        static object[] TestNumberCases = {

            // Radix 10, no suffix
            new object[] { 10, "0", 0 },
            new object[] { 10, "01", 1 },
            new object[] { 10, "1234", 1234 },
            new object[] { 10, "9999", 9999 },

            // Radix 10, suffixes
            new object[] { 10, "01d", 1 },
            new object[] { 10, "1234d", 1234 },
            new object[] { 10, "9999d", 9999 },
            new object[] { 10, "1234D", 1234 },
            new object[] { 10, "9999D", 9999 },
            new object[] { 10, "101010b", 0b101010 },
            new object[] { 10, "101010B", 0b101010 },
            new object[] { 10, "777o", 511 },
            new object[] { 10, "777O", 511 },
            new object[] { 10, "1000q", 512 },
            new object[] { 10, "1000Q", 512 },
            new object[] { 10, "12EFh", 0x12EF },
            new object[] { 10, "12efH", 0x12EF },

            // Radix 2, no suffix
            new object[] { 2, "0", 0 },
            new object[] { 2, "01", 1 },
            new object[] { 2, "1010", 0b1010 },

            // Radix 2, suffixes
            new object[] { 2, "01d", 1 },
            new object[] { 2, "1234d", 1234 },
            new object[] { 2, "9999d", 9999 },
            new object[] { 2, "1234D", 1234 },
            new object[] { 2, "9999D", 9999 },
            new object[] { 2, "101010b", 0b101010 },
            new object[] { 2, "101010B", 0b101010 },
            new object[] { 2, "777o", 511 },
            new object[] { 2, "777O", 511 },
            new object[] { 2, "1000q", 512 },
            new object[] { 2, "1000Q", 512 },
            new object[] { 2, "12EFh", 0x12EF },
            new object[] { 2, "12efH", 0x12EF },

            // Radix 8, no suffix
            new object[] { 8, "0", 0 },
            new object[] { 8, "01", 1 },
            new object[] { 8, "777", 511 },
            new object[] { 8, "1000", 512 },

            // Radix 8, suffixes
            new object[] { 8, "01d", 1 },
            new object[] { 8, "1234d", 1234 },
            new object[] { 8, "9999d", 9999 },
            new object[] { 8, "1234D", 1234 },
            new object[] { 8, "9999D", 9999 },
            new object[] { 8, "101010b", 0b101010 },
            new object[] { 8, "101010B", 0b101010 },
            new object[] { 8, "777o", 511 },
            new object[] { 8, "777O", 511 },
            new object[] { 8, "1000q", 512 },
            new object[] { 8, "1000Q", 512 },
            new object[] { 8, "12EFh", 0x12EF },
            new object[] { 8, "12efH", 0x12EF },

            // Radix 16, no suffix
            new object[] { 16, "0", 0 },
            new object[] { 16, "01", 1 },
            new object[] { 16, "12ef", 0x12ef },
            new object[] { 16, "0EF12", 0xef12 },

            // Radix 16, suffixes
            new object[] { 16, "01d", 0x1d },     // not decimal 1 !
            new object[] { 16, "1234d", 0x234d }, // not decimal 1234 !
            new object[] { 16, "9999d", 0x999d }, // not decimal 9999 !
            new object[] { 16, "1234D", 0x234d }, // not decimal 1234 !
            new object[] { 16, "9999D", 0x999d }, // not decimal 9999 !
            new object[] { 16, "101010b", 0x010b }, // not binary 101010 !
            new object[] { 16, "101010B", 0x010b }, // not binary 101010 !
            new object[] { 16, "777o", 511 },
            new object[] { 16, "777O", 511 },
            new object[] { 16, "1000q", 512 },
            new object[] { 16, "1000Q", 512 },
            new object[] { 16, "12EFh", 0x12EF },
            new object[] { 16, "12efH", 0x12EF },

            // Arbitrary radixes, including suffixes

            new object[] { 5, "20101", 2*5*5*5*5 + 1*5*5 + 1 },
            new object[] { 11, "101b", 0b101 },
            new object[] { 12, "201b", 2*12*12*12 + 1*12 + 11 },
            new object[] { 13, "201b", 2*13*13*13 + 1*13 + 11 },
            new object[] { 14, "201b", 2*14*14*14 + 1*14 + 11 },
            new object[] { 15, "201b", 2*15*15*15 + 1*15 + 11 },
            new object[] { 13, "201d", 201 },
            new object[] { 14, "201d", 2*14*14*14 + 1*14 + 13 },
            new object[] { 15, "201d", 2*15*15*15 + 1*15 + 13 },

            // The x'nnnn' syntax

            new object[] {10, "x'12eF'", 0x12ef },
            new object[] {10, "X'1234eF'", 0x34ef },
            new object[] {10, "x''", 0 },

            // Overflow
            new object[] { 10, "99999", 0x869F }, // 99999 = 1869F
            new object[] { 10, "99999h", 0x9999 },
            new object[] { 10, "X'1234ABCD'", 0xabcd },
        };

        [TestCaseSource(nameof(TestNumberCases))]
        public void TestParsingNumber(int radix, string input, int output)
        {
            Expression.DefaultRadix = radix;
            AssertParsesToNumber(input, (ushort)output);
        }

        static object[] TestWrongNumberCases = {
            new object[] { 10, "123x", "Unexpected character found after number: x" },
            new object[] { 10, "102b", "Invalid number" },
            new object[] { 10, "x'12", "Invalid X'' number" },
            new object[] { 10, "x'12h'", "Invalid X'' number" },
            new object[] { 10, "x'12'h", "Unexpected character found after number: h" },
            new object[] { 2, "111x", "Unexpected character found after number: x" },
            new object[] { 2, "102", "Unexpected character found after number: 2" },
            new object[] { 3, "222x", "Unexpected character found after number: x" },
            new object[] { 3, "234", "Unexpected character found after number: 3" },
            new object[] { 4, "333x", "Unexpected character found after number: x" },
            new object[] { 4, "345", "Unexpected character found after number: 4" },
            new object[] { 5, "444x", "Unexpected character found after number: x" },
            new object[] { 5, "456", "Unexpected character found after number: 5" },
            new object[] { 6, "555x", "Unexpected character found after number: x" },
            new object[] { 6, "456", "Unexpected character found after number: 6" },
            new object[] { 7, "666x", "Unexpected character found after number: x" },
            new object[] { 7, "567", "Unexpected character found after number: 7" },
            new object[] { 8, "777x", "Unexpected character found after number: x" },
            new object[] { 8, "678", "Unexpected character found after number: 8" },
            new object[] { 9, "888x", "Unexpected character found after number: x" },
            new object[] { 9, "789", "Unexpected character found after number: 9" },
            new object[] { 11, "0aaax", "Unexpected character found after number: x" },
            new object[] { 11, "09ab", "Invalid number" },
            new object[] { 12, "0bbbx", "Unexpected character found after number: x" },
            new object[] { 12, "0abc", "Unexpected character found after number: c" },
            new object[] { 13, "0cccx", "Unexpected character found after number: x" },
            new object[] { 13, "0bcd", "Invalid number" },
            new object[] { 14, "0dddx", "Unexpected character found after number: x" },
            new object[] { 14, "0cde", "Unexpected character found after number: e" },
            new object[] { 15, "0eeex", "Unexpected character found after number: x" },
            new object[] { 15, "0def", "Unexpected character found after number: f" },
            new object[] { 16, "0fffx", "Unexpected character found after number: x" },
            new object[] { 16, "0efg", "Unexpected character found after number: g" },
        };

        [TestCaseSource(nameof(TestWrongNumberCases))]
        public void TestParsingInvalidNumber(int radix, string input, string exceptionMessage)
        {
            AssertThrowsExpressionError(radix, input, exceptionMessage);
        }

        private static void AssertParsesToNumber(string expressionString, ushort number) =>
            AssertIsNumber(Expression.Parse(expressionString), number);

        private static void AssertIsNumber(Expression expression, ushort number) =>
            AssertExpressionIs(expression, Address.Absolute(number));

        private static void AssertExpressionIs(Expression expression, params IExpressionPart[] parts) =>
            Assert.AreEqual(Expression.FromParts(parts), expression);

        private static void AssertThrowsExpressionError(int radix, string input, string? message = null) =>
            AssertThrowsExpressionError(() => { Expression.DefaultRadix = radix; Expression.Parse(input); }, message);

        private static void AssertThrowsExpressionError(Action code, string? message = null)
        {
            var ex = Assert.Throws<InvalidExpressionException>(new TestDelegate(code));
            if(message is not null)
                Assert.AreEqual(message, ex.Message);
        }
    }
}
