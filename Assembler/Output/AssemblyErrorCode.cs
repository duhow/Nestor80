﻿namespace Konamiman.Nestor80.Assembler.Output
{
    public enum AssemblyErrorCode : byte
    {
        None = 0,
        NoEndStatement = 1,
        UnexpectedContentAtEndOfLine,
        DollarAsLabel,
        LineHasNoEffect,
        UnterminatedComment,
        StringHasBytesWithHighBitSet,
        InvalidListingPageSize,
        SymbolWithCpuRegisterName,
        ConfusingOffset,
        IgnoredForAbsoluteOutput,
        UnterminatedConditional,
        PhaseWithoutArgument,
        DephaseWithoutPhase,
        UserWarning,

        FirstError = 64,
        InvalidExpression = 64,
        InvalidArgument,
        MissingValue,
        InvalidLabel,
        DuplicatedSymbol,
        UnknownStringEncoding,
        InvalidCpuInstruction,
        InvalidForAbsoluteOutput,
        ConditionalOutOfScope,
        UnknownSymbol,
        InvalidForRelocatable,
        InvalidNestedPhase,
        InvalidInPhased,
        UserError,

        FirstFatal = 128,
        UnexpectedError = 128,
        SourceLineTooLong,
        UnsupportedCpu,
        CantInclude,
        TooManyNestedIncludes,
        UserFatal
    }
}
