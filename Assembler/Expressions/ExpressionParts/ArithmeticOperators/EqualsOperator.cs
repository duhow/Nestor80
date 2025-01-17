﻿using Konamiman.Nestor80.Assembler.Relocatable;

namespace Konamiman.Nestor80.Assembler.Expressions.ExpressionParts.ArithmeticOperators
{
    internal class EqualsOperator : BinaryOperator
    {
        public static EqualsOperator Instance = new();

        public override int Precedence => 5;

        public override string Name => "EQ";

        protected override Address OperateCore(Address value1, Address value2)
        {
            // Both addresses must be in the same mode

            if (!value1.SameModeAs(value2))
            {
                throw new InvalidExpressionException($"EQ: Both values must be in the same mode (attempted {value1.Type} EQ {value2.Type})");
            }

            return value1.Value == value2.Value ? AbsoluteMinusOne : AbsoluteZero;
        }
    }
}
