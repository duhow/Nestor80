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
        UnterminatedPhase,
        SameEffectiveExternal,
        UnterminatedModule,
        RootWithoutModule,
        TruncatedRequestFilename,
        MissingDelimiterInMacroArgsList,
        UserWarning,
        LastWarning = UserWarning,

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
        DifferentPassValues,
        SameEffectivePublic,
        UnknownInstruction,
        EndModuleOutOfScope,
        EndMacroOutOfScope,
        UnterminatedMacro,
        DuplicatedMacro,
        NestedMacro,
        ExitmOutOfScope,
        UserError,
        LasetError = UserError,

        FirstFatal = 128,
        UnexpectedError = 128,
        SourceLineTooLong,
        UnsupportedCpu,
        CantInclude,
        TooManyNestedIncludes,
        IncludeInPass2Only,
        MaxErrorsReached,
        UserFatal,
        LastFatal = UserFatal,
    }
}
