﻿namespace Konamiman.Nestor80.Assembler.Output
{
    public enum AssemblyErrorCode : byte
    {
        None = 0,
        NoEndStatement,
        UnexpectedContentAtEndOfLine,

        FirstError = 64,
        InvalidExpression,
        MisssingOperand,

        FirstFatal = 128,
        UnexpectedError,
        SourceLineTooLong,
    }
}