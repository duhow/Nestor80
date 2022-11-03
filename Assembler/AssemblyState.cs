﻿using Konamiman.Nestor80.Assembler.Expressions;
using Konamiman.Nestor80.Assembler.Output;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Konamiman.Nestor80.Assembler
{
    internal class AssemblyState
    {
        public AssemblyState(AssemblyConfiguration configuration, Stream sourceStream, Encoding sourceStreamEncoding)
        {
            this.Configuration = configuration;
            this.sourceStreamEncoding = sourceStreamEncoding;
            this.SourceStreamReader = new StreamReader(sourceStream, sourceStreamEncoding, true, 4096);
        }

        public string CurrentSourceLineText {get;set;}

        private Encoding sourceStreamEncoding;

        private readonly List<AssemblyError> Errors = new();

        public AssemblyConfiguration Configuration { get; init; }

        public StreamReader SourceStreamReader { get; private set; }

        public Encoding DefaultOutputStringEncoding {get;set;}

        public bool InPass2 { get; private set; } = false;

        public bool InPass1 => !InPass2;

        public bool HasErrors => Errors.Any(e => !e.IsWarning);

        public int CurrentLineNumber { get; private set; } = 1;

        public List<ProcessedSourceLine> ProcessedLines { get; private set; } = new();

        public Dictionary<string, NamedMacroDefinitionLine> NamedMacros { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterPendingExpression(
            ProcessedSourceLine line, 
            Expression expression, 
            int location = 0,
            CpuInstrArgType argumentType = CpuInstrArgType.None,
            bool isNegativeIxy = false)
        {
            if(!ExpressionsPendingEvaluation.ContainsKey(line)) {
                ExpressionsPendingEvaluation[line] = new List<ExpressionPendingEvaluation>();
            }

            ExpressionsPendingEvaluation[line].Add(new ExpressionPendingEvaluation() { Expression = expression, LocationInOutput = location, ArgumentType = argumentType, IsNegativeIxy = isNegativeIxy } );
        }

        public void UnregisterPendingExpressions(ProcessedSourceLine line)
        {
            if(ExpressionsPendingEvaluation.ContainsKey(line)) {
                ExpressionsPendingEvaluation.Remove(line);
            }
        }

        public Dictionary<ProcessedSourceLine, List<ExpressionPendingEvaluation>> ExpressionsPendingEvaluation { get; } = new();

        public Address EndAddress { get; private set; }

        public void End(Address address)
        {
            if(address is null)
                throw new ArgumentNullException(nameof(address));

            EndAddress = address;
        }

        public bool EndReached => EndAddress is not null;

        public void SwitchToPass2()
        {
            InPass2 = true;
            CurrentLineNumber = 1;
            CurrentSourceLineText = null;
            CurrentPhasedLocationPointer = null;
            EndAddress = null;
            CurrentModule = null;
            currentRootSymbols = null;
            modules.Clear();

            LocationPointersByArea[AddressType.CSEG] = 0;
            LocationPointersByArea[AddressType.DSEG] = 0;
            LocationPointersByArea[AddressType.ASEG] = 0;
        }

        private readonly Dictionary<AddressType, ushort> LocationPointersByArea = new() {
            {AddressType.CSEG, 0},
            {AddressType.DSEG, 0},
            {AddressType.ASEG, 0}
        };

        private readonly Dictionary<AddressType, ushort> AreaSizes = new() {
            {AddressType.CSEG, 0},
            {AddressType.DSEG, 0},
            {AddressType.ASEG, 0}
        };

        private AddressType locationAreaBeforePhase;
        private AddressType _CurrentLocationArea;
        public AddressType CurrentLocationArea 
        {
            get => IsCurrentlyPhased ? AddressType.ASEG : _CurrentLocationArea;
            private set
            {
                _CurrentLocationArea = value;
            }
        }

        public void EnterPhase(ushort address)
        {
            if(IsCurrentlyPhased) {
                throw new InvalidOperationException($"{nameof(EnterPhase)} isn't intended to be called while already in .PHASE mode");
            }

            locationAreaBeforePhase = _CurrentLocationArea;
            CurrentPhasedLocationPointer = address;
        }

        public void ExitPhase()
        {
            if(!IsCurrentlyPhased) {
                throw new InvalidOperationException($"{nameof(ExitPhase)} isn't intended to be called while not in .PHASE mode");
            }

            CurrentPhasedLocationPointer = null;
            _CurrentLocationArea = locationAreaBeforePhase;
        }

        private ushort currentDephasedLocationPointer;
        public ushort CurrentLocationPointer {
            get => CurrentPhasedLocationPointer.GetValueOrDefault(currentDephasedLocationPointer);
            private set
            {
                currentDephasedLocationPointer = value;
            }
        }

        public ushort? CurrentPhasedLocationPointer { get; private set; } = null;

        public bool IsCurrentlyPhased => CurrentPhasedLocationPointer is not null;

        public Address GetCurrentLocation() => new(CurrentLocationArea, CurrentLocationPointer);

        public void SwitchToArea(AddressType area)
        {
            if(IsCurrentlyPhased && area is not AddressType.ASEG) {
                throw new InvalidOperationException($"{nameof(SwitchToArea)} isn't intended to be executed while in .PHASE mode");
            }

            //TODO: Handle sizes of commons
            if(area == CurrentLocationArea)
                return;

            if(area == AddressType.COMMON) {
                CurrentLocationPointer = 0;
            }
            else {
                AreaSizes[CurrentLocationArea] = Math.Max(AreaSizes[CurrentLocationArea], CurrentLocationPointer);
                LocationPointersByArea[CurrentLocationArea] = CurrentLocationPointer;
                CurrentLocationPointer = LocationPointersByArea[area];
            }

            CurrentLocationArea = area;
        }

        public void SwitchToLocation(ushort location)
        {
            if(IsCurrentlyPhased) {
                throw new InvalidOperationException($"{nameof(SwitchToLocation)} isn't intended to be executed while in .PHASE mode");
            }

            //TODO: Handle commons
            if(location != CurrentLocationPointer) {
                CurrentLocationPointer = location;
                AreaSizes[CurrentLocationArea] = Math.Max(AreaSizes[CurrentLocationArea], CurrentLocationPointer);
            }
        }

        public ushort GetLocationPointer(AddressType area)
        {
            //TODO: Handle commons
            if(area != AddressType.COMMON) {
                return LocationPointersByArea[area];
            }

            return 0;
        }

        public ushort GetAreaSize(AddressType area)
        {
            //TODO: Handle commons
            if(area != AddressType.COMMON) {
                return AreaSizes[area];
            }

            return 0;
        }

        public void IncreaseLocationPointer(int amount)
        {
            currentDephasedLocationPointer += (ushort)amount;
            if(IsCurrentlyPhased) {
                CurrentPhasedLocationPointer += (ushort)amount;
            }
        }

        public void IncreaseLineNumber() => CurrentLineNumber++;

        public void AddError(AssemblyError error) => Errors.Add(error);

        public AssemblyError AddError(AssemblyErrorCode code, string message, bool withLineNumber = true)
        {   //TODO: Include macro name and line
            var error = new AssemblyError(code, message, withLineNumber ? CurrentLineNumber : null, withLineNumber ? CurrentSourceLineText : null,  CurrentIncludeFilename);
            AddError(error);
            return error;
        }

        public AssemblyError[] GetErrors() => Errors.ToArray();

        private readonly Dictionary<string, SymbolInfo> Symbols = new(StringComparer.InvariantCultureIgnoreCase);

        public SymbolInfo[] GetSymbols() => Symbols.Values.ToArray();

        public SymbolInfo[] GetSymbolsOfUnknownType() => Symbols.Values.Where(s => !s.IsOfKnownType || (!s.IsExternal && !s.HasKnownValue)).ToArray();

        public bool HasSymbol(string symbol) => Symbols.ContainsKey(symbol);

        public bool SymbolIsKnown(string symbol) => Symbols.ContainsKey(symbol) && Symbols[symbol].HasKnownValue;

        public bool SymbolIsOfKnownType(string symbol) => Symbols.ContainsKey(symbol) && Symbols[symbol].IsOfKnownType;

        public void AddSymbol(string name, SymbolType type, Address value = null, bool isPublic = false) =>
            Symbols.Add(name, new SymbolInfo() { Name = name, Type = type, Value = value, IsPublic = isPublic });

        public void WrapUp()
        {
            //TODO: Handle sizes of commons
            if(CurrentLocationArea != AddressType.COMMON) {
                AreaSizes[CurrentLocationArea] = Math.Max(AreaSizes[CurrentLocationArea], CurrentLocationPointer);
                LocationPointersByArea[CurrentLocationArea] = CurrentLocationPointer;
            }
        }

        public SymbolInfo GetSymbol(string name)
        {
            return Symbols.ContainsKey(name) ? Symbols[name] : null;
        }

        public char? MultiLineCommandDelimiter { get; set; }

        public bool InsideMultiLineComment => MultiLineCommandDelimiter.HasValue;

        public Stack<ConditionalBlockType> conditionalBlocksStack = new();

        public ConditionalBlockType CurrentConditionalBlockType { get; private set; }

        public bool InTrueConditional => CurrentConditionalBlockType is ConditionalBlockType.TrueIf or ConditionalBlockType.TrueElse;

        public bool InFalseConditional =>
            (CurrentConditionalBlockType is ConditionalBlockType.FalseIf or ConditionalBlockType.FalseElse) ||
            (conditionalBlocksStack.Any(b => b is ConditionalBlockType.FalseIf or ConditionalBlockType.FalseElse));

        public bool InMainConditionalBlock => CurrentConditionalBlockType is ConditionalBlockType.TrueIf or ConditionalBlockType.FalseIf;

        public bool InElseBlock => CurrentConditionalBlockType is ConditionalBlockType.TrueElse or ConditionalBlockType.FalseElse;

        public bool InConditionalBlock => CurrentConditionalBlockType is not ConditionalBlockType.None;

        public void PushAndSetConditionalBlock(ConditionalBlockType blockType)
        {
            if(CurrentConditionalBlockType is not ConditionalBlockType.None)
                conditionalBlocksStack.Push(CurrentConditionalBlockType);

            CurrentConditionalBlockType = blockType;
        }

        public void SetConditionalBlock(ConditionalBlockType blockType)
        {
            CurrentConditionalBlockType = blockType;
        }

        public void PopConditionalBlock()
        {
            if(conditionalBlocksStack.Count == 0) {
                if(InConditionalBlock) {
                    CurrentConditionalBlockType = ConditionalBlockType.None;
                }
                else {
                    throw new InvalidOperationException("Attempted to exit a conditional block when none was in progress");
                }
            }
            else {
                CurrentConditionalBlockType = conditionalBlocksStack.Pop();
            }
        }

        private Stack<IncludeState> includeStates = new();

        public string CurrentIncludeFilename { get; private set; } = null;

        public void PushIncludeState(Stream newStream, IncludeLine includeLine)
        {
            var previousState = new IncludeState() {
                PreviousFileName = CurrentIncludeFilename,
                ProcessedLine = includeLine, 
                PreviousLineNumber = CurrentLineNumber,
                PreviousLines = ProcessedLines, 
                PreviousSourceStreamReader = SourceStreamReader
            };

            includeStates.Push(previousState);
            InsideIncludedFile = true;

            CurrentIncludeFilename = includeLine.FileName;
            SourceStreamReader = newStream is null ? null : new StreamReader(newStream, sourceStreamEncoding, true, 4096);
            CurrentLineNumber = 0; //0 because the line number will be increased right after this method

            //Don't just clear the existing list, we really need a new one!
            ProcessedLines = new List<ProcessedSourceLine>();
        }

        public void PopIncludeState()
        {
            if(!InsideIncludedFile) {
                throw new InvalidOperationException("Can't exit included file because we aren't in one");
            }

            SourceStreamReader?.Dispose();

            var previousState = includeStates.Pop();

            if(SourceStreamReader is not null) {
                previousState.ProcessedLine.Lines = ProcessedLines.ToArray();
            }

            CurrentLineNumber = previousState.PreviousLineNumber + (InPass2 ? 0 : 1); //+1 in pass 1 because line number was increased after the call to PushIncludeState
            SourceStreamReader = previousState.PreviousSourceStreamReader;
            ProcessedLines = previousState.PreviousLines;
            CurrentIncludeFilename = previousState.PreviousFileName;

            InsideIncludedFile = includeStates.Count > 0;
        }

        public bool InsideIncludedFile { get; private set; }

        public int CurrentIncludesDeepLevel => includeStates.Count;

        public Dictionary<ProcessedSourceLine, (InstructionPendingSelection[], Expression)> InstructionsPendingSelection { get; set; } = new();

        public void RegisterInstructionsPendingSelection(ProcessedSourceLine line, InstructionPendingSelection[] choices, Expression selectorExpression)
        {
            InstructionsPendingSelection.Add(line, (choices, selectorExpression));
        }

        public void UnregisterInstructionsPendingSelection(ProcessedSourceLine line)
        {
            if(InstructionsPendingSelection.ContainsKey(line)) {
                InstructionsPendingSelection.Remove(line);
            }
        }

        private Stack<(string, HashSet<string>)> modules = new();

        public string CurrentModule { get; private set; } = null;

        private HashSet<string> currentRootSymbols = null;

        public void EnterModule(string name)
        {
            modules.Push((CurrentModule, currentRootSymbols));
            CurrentModule = CurrentModule is null ? name : $"{CurrentModule}.{name}";
            currentRootSymbols = new HashSet<string>(
                currentRootSymbols is null ? Array.Empty<string>() : currentRootSymbols,
                StringComparer.OrdinalIgnoreCase);
        }

        public void ExitModule()
        {
            if(CurrentModule is null) {
                throw new InvalidOperationException($"{nameof(ExitModule)} called while not in a module");
            }

            (CurrentModule, currentRootSymbols) = modules.Pop();
        }

        public void RegisterRootSymbols(IEnumerable<string> symbols)
        {
            if(currentRootSymbols is null) {
                throw new InvalidOperationException($"{nameof(RegisterRootSymbols)} called while not in a module");
            }

            foreach(var symbol in symbols) {
                currentRootSymbols.Add(symbol);
            }
        }

        public string Modularize(string symbol)
        {
            return CurrentModule is null || currentRootSymbols.Contains(symbol) ? symbol : $"{CurrentModule}.{symbol}";
        }

        private MacroExpansionState currentMacroExpansionState = null;

        private Stack<MacroExpansionState> previousExpansionStates = new();

        public void RegisterNamedMacroDefinitionStart(NamedMacroDefinitionLine definitionLine, NamedMacroDefinitionLine processedLine)
        {
            if(MacroDefinitionState.DefiningMacro) {
                throw new InvalidOperationException($"{nameof(RegisterNamedMacroDefinitionStart)} is not supposed to be called while already in macro definition mode");
            }

            MacroDefinitionState.StartDefinition(MacroType.Named, processedLine);
        }

        public void RegisterMacroExpansionStart(MacroExpansionLine expansionLine)
        {
            if(expansionLine.MacroType is MacroType.Named) {
                if(NamedMacros.ContainsKey(expansionLine.Name)) {
                    throw new InvalidOperationException($"{nameof(RegisterMacroExpansionStart)}: unknown named macro '{expansionLine.Name}'");
                }

                if(currentMacroExpansionState is not null) {
                    previousExpansionStates.Push(currentMacroExpansionState);
                }

                var macroDefinition = NamedMacros[expansionLine.Name];
                currentMacroExpansionState = new NamedMacroExpansionState(expansionLine, macroDefinition.LineTemplates, macroDefinition.Arguments.Length, expansionLine.Parameters, CurrentLineNumber);
            }
            else if(MacroDefinitionState.DefiningMacro) {
                throw new InvalidOperationException($"{nameof(RegisterMacroExpansionStart)} is not supposed to be called while already in macro definition mode");
            }
            else {
                MacroDefinitionState.StartDefinition(expansionLine.MacroType, expansionLine);
            }
        }

        public void RegisterMacroDefinitionLine(string sourceLine, bool isMacroDefinitionOrExpansionInstruction)
        {
            if(isMacroDefinitionOrExpansionInstruction) {
                MacroDefinitionState.IncreaseDepth();
            }
            MacroDefinitionState.AddLine(sourceLine);
        }

        public void RegisterMacroEnd()
        {
            if(CurrentMacroMode is MacroMode.Definition) {
                if(MacroDefinitionState.Depth > 1) {
                    MacroDefinitionState.DecreaseDepth();
                }
                else if(MacroDefinitionState.ProcessedLine is NamedMacroDefinitionLine nmdl) {
                    nmdl.LineTemplates = MacroDefinitionState.GetLines();
                    MacroDefinitionState.EndDefinition();
                }
                else {
                    var macroExpansionLine = (MacroExpansionLine)MacroDefinitionState.ProcessedLine;
                    MacroExpansionState expansionState;
                    if(macroExpansionLine.MacroType is MacroType.ReptWithCount) {
                        expansionState = new ReptWithCountExpansionState(macroExpansionLine, MacroDefinitionState.GetLines(), macroExpansionLine.RepetitionsCount, CurrentLineNumber);
                    }
                    else {
                        expansionState = new ReptWithParamsExpansionState(macroExpansionLine, MacroDefinitionState.GetLines(), macroExpansionLine.Parameters, CurrentLineNumber);
                    }

                    if(currentMacroExpansionState is not null) {
                        previousExpansionStates.Push(currentMacroExpansionState);
                    }

                    currentMacroExpansionState = expansionState;
                    MacroDefinitionState.EndDefinition();
                }
            }
            else if(CurrentMacroMode is MacroMode.Expansion) {
                //TODO: this never reached?
                currentMacroExpansionState.ExpansionProcessedLine.Lines = currentMacroExpansionState.ProcessedLines.ToArray();
                if(previousExpansionStates.Count == 0) {
                    currentMacroExpansionState = null;
                }
                else {
                    currentMacroExpansionState = previousExpansionStates.Pop();
                }
            }
            else {
                AddError(AssemblyErrorCode.EndMacroOutOfScope, $"ENDM found outside of a macro definition or macro expansion");
            }
        }

        public string GetNextMacroExpansionLine()
        {
            if(currentMacroExpansionState is null) {
                return null;
            }

            if(!currentMacroExpansionState.HasMore) {
                var initialEndmLine = currentMacroExpansionState.ProcessedLines[0];
                string line;

                currentMacroExpansionState.ExpansionProcessedLine.Lines = currentMacroExpansionState.ProcessedLines.Skip(1).ToArray();
                if(previousExpansionStates.Count == 0) {
                    currentMacroExpansionState = null;
                    RegisterProcessedLine(initialEndmLine);
                    line = null;
                }
                else {
                    currentMacroExpansionState = previousExpansionStates.Pop();
                    RegisterProcessedLine(initialEndmLine);
                    line = GetNextMacroExpansionLine();
                }

                return line;
            }

            return currentMacroExpansionState.GetNextSourceLine();
        }

        internal void RegisterProcessedLine(ProcessedSourceLine processedLine)
        {
            if(currentMacroExpansionState is not null) {
                currentMacroExpansionState.ProcessedLines.Add(processedLine);
            }
            else { 
                ProcessedLines.Add(processedLine);
            }
        }

        public MacroMode CurrentMacroMode =>
            MacroDefinitionState.DefiningMacro ? MacroMode.Definition :
            currentMacroExpansionState is not null ? MacroMode.Expansion :
            MacroMode.None;
    }
}
