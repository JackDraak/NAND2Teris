using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hacksemmbler
/*  For project 6 of part I of "From Nand to Tetris", my take on the Hack Assembler: Hacksemmbler
 *  Be warned, my programming is not industrial-strength; I'm just doing this for fun and self-
 *  development. @JackDraak (passes unit-tests as of 16 March 2018)
 *  
 *  Primary specification reference: http://nand2tetris.org/course.php (topic: assembler)
 *  
 *  For better or worse, I decided to do this in Windows7 with the Visual Studio IDE using C#.
 *  At first I was thinking C++, but then I felt C# would give me some handy shortcuts for handling
 *  strings and lists and the like.  Because I'm developing this in Windows7 and using it in 
 *  Windows7, that's where I would suggest you tinker with it. Better-yet, just do it your own
 *  way, but if you really want to play with, or simply review my code: I'm on free (read: open)
 *  GitHub so.... have at it!
 *  
 *  Design methodology: create program to generate Hack Machine Language from Hack Assembly Language
 *  First, process simple .asm lacking comments and symbols, then adapt for symbol integration.
 *  Order of operations: 
 *      - read and parse .asm input
 *      - generate instruction sequence free of comments/whitespace
 *      - record labels in symbol table
 *      - allocate pointers and record in symbol table
 *      - encode addresses into machine language
 *          - using symbol table as needed
 *      - encode C-instructions and @_instructions to machine language
 * */
{
    class Program
    {
        static bool encodeHeader = false;

        struct C_code
        {
            public string cmp, dst, jmp;
        };

        private class SymbolEntry
        {
            bool isData;
            int address;
            string id;

            public bool IsData()            { return isData; }
            public int GetAddress()         { return address; }
            public string GetID()           { return id; }
            public void IsCode(bool torf)   { isData = !torf; }
            public void SetAddress(int num) { address = num; }
            public void SetID(string str)   { id = str; }

            public SymbolEntry()
            {
                isData = false;
                address = 0;
                id = "";
            }
        }

        static void Main(string[] args)
        {
            // Simple sanity checks, this could positively be more robust if extended for a broader audience.
            if (args.Length == 0 || args[0] == "?" || args[0] == "help" || !LooksLikeValidAsm(args[0])) 
            {
                PrintUsage();
                return;
            }

            // Track which argument [input file] we're processing (this allows for batch-processing).
            int argument = 0;
            while(argument < args.Length)
            {
                // Initialize setup for each input-file in the batch.
                List<string> instructionList, debugLog;
                List<SymbolEntry> symbolTable = new List<SymbolEntry>();
                String thisProgram = GetProgramName(args, argument);
                int nextOpenRegister;
                InitEncode(out nextOpenRegister, out instructionList, out encodeHeader, out symbolTable, out debugLog);

                // Pre-parse input-stream of instructions into a handy-dandy List... let's call it: instructionList.
                // (Get rid of whitespace, including blank lines and comments, that's for humans, not machines.)
                PreParse(args, argument, instructionList);

                // Output pre-parsed instructions for humans.
                DebugParsed($"_{thisProgram}.preparse", instructionList, debugLog);

                // On first-pass, assign requisite symbol table entries. 
                AssignSymbols(instructionList, debugLog, symbolTable);

                // Second-pass, link symbol table with variables.
                // TODO: deal with anaochronisitic return of nextOpenRegister... at this point, we no longer need it
                nextOpenRegister = LinkVariables(instructionList, debugLog, nextOpenRegister, symbolTable);

                // "Third-pass", Parse variables into absolute addresses.
                ParseVariables(instructionList, symbolTable);

                // Output pre-encoded but fully parsed instructions, again for humans.
                DebugParsed($"_{thisProgram}.postparse", instructionList, debugLog);

                // Parse and encode instructionList for machines* which are Hack-compliant. 
                // TODO: deal with anaochronisitic use of nextOpenRegister... at this point, we no longer need it [thanks to "pass three"]
                List<string> encodedInstructions = DoEncode(instructionList, debugLog, ref nextOpenRegister, symbolTable);

                // Debug output: dump symbol table for humans. 
                DebugSymbols(debugLog, symbolTable, thisProgram, true);

                // Output encoded data for machines*.
                string programName = OutputProgram(args, argument, encodedInstructions);

                // Output debug stream for humans.
                OutputDebug(debugLog, programName);

                // Chill until user hits enter or return, then exit (or continue batch).
                argument = ContinueOrExit(args, argument, programName);
            }
        }
        #region Internal Methods
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Internal methods // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        // Setup symbolTable from assembly instructionList. First pass, assign symbols to symbol table by offset.
        private static void AssignSymbols(List<string> instructionList, List<string> debugLog,  List<SymbolEntry> symbolTable)
        {
            int symbolOffset = 0;
            bool isSymbol = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                String labelOrNot = instructionList[i];
                if (LineIsSymbolReference(labelOrNot))
                {
                    isSymbol = true;
                    bool inTable = false;
                    foreach (var symbolEntry in symbolTable)
                    {
                        if (symbolEntry.GetID() == labelOrNot)
                        {
                            inTable = true;
                            continue;
                        }
                    }
                    if (!inTable)
                    {
                        SymbolEntry thisSymbol = new SymbolEntry();
                        thisSymbol.SetID(GetLabel(labelOrNot));
                        thisSymbol.SetAddress(symbolOffset);
                        thisSymbol.IsCode(true);
                        symbolTable.Add(thisSymbol);
                    }
                }
                if (!isSymbol) symbolOffset++; 
                isSymbol = false;
            }
        }

        // Strip '@' from the front of address instructions.
        private static string CleanAddress(string strIn)
        {
            try { return Regex.Replace(strIn, @"@", "", RegexOptions.None, TimeSpan.FromSeconds(0.3)); }
            catch (RegexMatchTimeoutException) {
                Console.WriteLine($"FAIL: CleanAddress({strIn}) exception");
                return String.Empty;
            }
        }

        // Called after each input file is processed; eventually this will exit with a -1, 0, or 1 perhaps.
        private static int ContinueOrExit(string[] args, int argument, string programName)
        {
            Console.WriteLine($"...\nprogram: '{programName}' parsed and encoded.");
            if (argument + 1 < args.Length) Console.WriteLine($"Press <Enter> to encode {args[argument + 1]}");
            else Console.WriteLine("Press <Enter> to exit");
            argument++; Console.ReadLine();
            return argument;
        }

        // Generate parsed-instruction-list debug output.
        private static void DebugParsed(String thisName, List<string> thisList, List<string> debugLog)
        {
            bool header = false;
            int localOffset = 0;
            List<string> fileOut = new List<string>();
            for (int i = 0; i < thisList.Count; i++)
            {
                if (!header)
                {
                    header = true;
                    fileOut.Add($"Line#\tParsed Instruction Set");
                    fileOut.Add($"-----\t----------------------");
                }

                if (LineIsSymbolReference(thisList[i]))
                {
                    fileOut.Add($"---)\t\t{thisList[i]}");
                }
                else
                {
                    fileOut.Add($"{localOffset})\t\t{thisList[i]}");
                    localOffset++;
                }
            }
            File.WriteAllText(thisName, ListAsString(fileOut), System.Text.Encoding.Unicode);
        }

        // Generate symbolTable debug output.
        private static void DebugSymbols(List<string> debugLog, List<SymbolEntry> symbolTable, string thisName, bool outToFile)
        {
            if (outToFile)
            {
                String outName = $"_{thisName}.symbolTable";
                List<string> thisTable = new List<string>();
                foreach(var symbolEntry in symbolTable)
                {
                    var thisID = symbolEntry.GetID();
                    var thisAddress = symbolEntry.GetAddress();
                    var isData = symbolEntry.IsData();

                    thisTable.Add($"{thisAddress}\t{isData:is Data}\t{thisID}");           
                }
                File.WriteAllText(outName, ListAsString(thisTable), System.Text.Encoding.Unicode);
            }
        }

        // Wrapper for encoding operation, to help keep Main tidy.
        private static List<string> DoEncode(List<string> instructionList, List<string> debugLog, 
                                             ref int nextOpenRegister, List<SymbolEntry> symbolTable)
        {
            List<String> encodedInstructions = new List<String>();
            int offset = 0;
            foreach (string instruction in instructionList)
            {
                if (instruction.Length > 0 && !LineIsSymbolReference(instruction))
                {
                    encodedInstructions.Add(EncodeDirective(debugLog, ref nextOpenRegister, symbolTable, offset, instruction));
                    offset++;
                }
            }
            return encodedInstructions;
        }

        // Encode 16-bit binary address from decimal address.
        private static string Encode16BitAddress(string address)
        {
            int addressAsInteger;
            bool success = int.TryParse(address, out addressAsInteger);
            if (!success)
            {
                Console.WriteLine($"FAIL: Encode16BitAddress({address})");
            }
            int place = 0;
            int remainder = 0;
            bool resolved = false;
            String binaryAddress = " ";
            int addressToConvert = addressAsInteger;
            while (!resolved)
            {
                Math.DivRem(addressToConvert, 2, out remainder);
                addressToConvert = addressToConvert / 2;
                binaryAddress = Prepend(binaryAddress, remainder.ToString());
                place++;
                if (addressToConvert == 0 | place == 15) resolved = true;
            }
            while (place < 16)
            {
                binaryAddress = Prepend(binaryAddress, "0");
                place++;
            }
            return StripWhitespace(binaryAddress);
        }

        // Parse and encode C-instruction.
        private static string EncodeC(List<string> debugLog, string instruction)
        {
            string encodedDirective;
            C_code cInst;
            cInst.cmp = "acccccc";
            cInst.dst = "ddd";
            cInst.jmp = "000";

            String[] splitstruction = Regex.Split(instruction, @";(...)");
            String jump = "";
            if (splitstruction.Length >= 2)
            {
                jump = splitstruction[1];
                cInst.jmp = EncodeJump(jump);
            }
            String destComp = GetJump(instruction);
            String dest = GetDest(destComp);
            String comp = GetComp(destComp);
            cInst.dst = EncodeDest(dest);
            cInst.cmp = EncodeComp(comp);
            if (!encodeHeader)
            {
                debugLog.Add($"Inst\tDest\tComp\tJump");
                debugLog.Add($"----\t----\t----\t----");
                encodeHeader = true;
            }
            debugLog.Add($"{instruction}\t{dest}\t{comp}\t{jump}\t111  {cInst.cmp}  {cInst.dst}  {cInst.jmp}");
            encodedDirective = "111" + cInst.cmp + cInst.dst + cInst.jmp;
            return encodedDirective;
        }

        // Encode @-instructions and C-instructions.
        private static string EncodeDirective(List<string> debugLog, ref int nextOpenRegister, List<SymbolEntry> symbolTable,
                                              int offset, string instruction)
        {
            List<string> fileLog = new List<string>();
            char test = instruction[0];
            string encodedDirective;
            if (test == '@')
            {
                String address = CleanAddress(instruction);
                int addressAsInteger;
                bool success = int.TryParse(address, out addressAsInteger);
                if (!success)
                {

                }
                encodedDirective = Encode16BitAddress(address);
            }
            else
            {
                encodedDirective = EncodeC(debugLog, instruction);
            }
            return encodedDirective;
        }

        // Return a SymboLEntry with the supplied values.
        private static SymbolEntry EnterSymbol(string name, int address, bool isCode)
        {
            SymbolEntry se = new SymbolEntry();
            se.SetID(name);
            se.SetAddress(address);
            se.IsCode(isCode);
            return se;
        }

        // Isolate & return computation directive.
        private static string GetComp(string strIn)
        {
            int l = strIn.Length;
            int delimiter = strIn.IndexOf("=");
            if (delimiter >= 0) return StripEq(strIn.Substring(delimiter, l - delimiter));
            return StripEq(strIn);
        }

        // Isolate & return destination directive.
        private static string GetDest(string strIn)
        {
            int delimiter = strIn.IndexOf("=");
            if (delimiter > 0) return strIn.Substring(0, delimiter);
            else return ""; 
        }

        // Isolate & return jump directive.
        private static string GetJump(string strIn)
        {
            int delimiter = strIn.IndexOf(";");
            if (delimiter == 0) return strIn;
            if (delimiter > 0) return strIn.Substring(0, delimiter);
            return strIn;
        }

        // Isolate & return label identity.
        private static string GetLabel(string strIn)
        {
            return strIn.Substring(1, strIn.Length - 2);
        }

        // Isolate & return program name from string.
        private static string GetName(string strIn)
        {
            int delimiter = strIn.IndexOf(".");
            return strIn.Substring(0, delimiter);
        }

        // Return the name of the selected program in the que.
        private static string GetProgramName(string[] args, int argument)
        {
            String inName = GetName(args[argument]);
            return inName;
        }

        // For batch processing, re-init before each input file: Lists, tables, debug & netOpenRegister...
        private static void InitEncode(out int nextOpenRegister, out List<string> instructionList, out bool encodeHeader,
                                       out List<SymbolEntry> symbolTable, out List<string> outDebug)
        {
            encodeHeader = false;
            instructionList = new List<String>();
            nextOpenRegister = 16;
            outDebug = new List<String>();
            symbolTable = new List<SymbolEntry>();
            symbolTable = PredefineSymbols(symbolTable);
        }

        // Identify symbolic references.
        private static bool LineIsSymbolReference(string strIn)
        {
            return strIn.StartsWith("(") && strIn.EndsWith(")");
        }

        // Parse @variables, link into symbol table.
        private static int LinkVariables(List<string> instructionList, List<string> debugLog, int nextOpenRegister, List<SymbolEntry> symbolTable)
        {
            for (int i = 0; i < instructionList.Count; i++)
            {
                char test = instructionList[i][0];
                if (test == '@')
                {
                    String labelOrNot = CleanAddress(instructionList[i]);
                    int addressAsInteger;
                    bool success = int.TryParse(labelOrNot, out addressAsInteger);
                    if (!success)
                    {
                        bool inTable = false;
                        int thisAddress;
                        foreach (var symbolEntry in symbolTable)
                        {
                            if (symbolEntry.GetID() == labelOrNot)
                            {
                                inTable = true;
                                thisAddress = symbolEntry.GetAddress();
                                Console.WriteLine($"LOOKUP: {labelOrNot}\t{thisAddress}");
                                instructionList[i] = $"@{thisAddress}";
                                continue;
                            }
                        }
                        if (!inTable & LineIsSymbolReference(labelOrNot))
                        {
                            Console.WriteLine("ERROR how did a symbol get here?");
                        }
                        else if (!inTable)
                        {
                            SymbolEntry thisSymbol = new SymbolEntry();
                            Console.WriteLine($"Pass 2 VAR: {labelOrNot}\t{nextOpenRegister}");
                            thisSymbol.SetID(labelOrNot);
                            thisSymbol.SetAddress(nextOpenRegister);
                            thisSymbol.IsCode(false);
                            symbolTable.Add(thisSymbol);
                            debugLog.Add($"variable: {labelOrNot}\t{nextOpenRegister}");
                            nextOpenRegister++;
                        }
                    }
                }
            }
            return nextOpenRegister;
        }

        // Convert a List<String> into a compound string for output.
        private static string ListAsString(List<String> listIn)
        {
            using (StringWriter streamOut = new StringWriter())
            {
                string line;
                int i = 0;
                while (i < listIn.Count)
                {
                    line = listIn[i];
                    streamOut.Write(line + "\r\n");
                    i++;
                }
                return streamOut.ToString();
            }
        }

        // Validate input assembly file(s).
        private static bool LooksLikeValidAsm(string strIn)
        {
            var probe = Regex.Match(strIn, @".[aA][sS][mM]");
            var test = probe.Captures;
            if (test.Count > 0) return true;
            return false;
        }

        // Output debugging information.
        private static void OutputDebug(List<string> debugLog, string inName)
        {
            String debugOut = $"_{inName}.debug";
            File.WriteAllText(debugOut, ListAsString(debugLog), System.Text.Encoding.Unicode);
        }
        
        // Output machine-encoded program.
        private static string OutputProgram(string[] args, int argument, List<string> encodedInstructions)
        {
            String inName = GetName(args[argument]);
            String fileOut = $"{inName}.hack";
            File.WriteAllText(fileOut, ListAsString(encodedInstructions), System.Text.Encoding.ASCII);
            return inName;
        }

        // Parse @variables into absolute references.
        private static void ParseVariables(List<string> instructionList, List<SymbolEntry> symbolTable)
        {
            for (int i = 0; i < instructionList.Count; i++)
            {
                String labelOrNot = instructionList[i];
                char test = labelOrNot[0];
                if (test == '@')
                {
                    labelOrNot = CleanAddress(labelOrNot);
                    int addressAsInteger;
                    bool success = int.TryParse(labelOrNot, out addressAsInteger);
                    if (!success)
                    {
                        foreach (var symbolEntry in symbolTable)
                        {
                            if (symbolEntry.GetID() == labelOrNot)
                            {
                                addressAsInteger = symbolEntry.GetAddress();
                                instructionList[i] = $"@{addressAsInteger}";
                                continue;
                            }
                        }
                    }
                }
            }
        }

        // Pre-parse input datastream (remove comments and whitespace).
        private static void PreParse(string[] args, int argument, List<string> instructionList)
        {
            foreach (string inputLine in File.ReadAllLines(args[argument]))
            {
                String thisLine = StripComments(inputLine);
                if (!string.IsNullOrWhiteSpace(thisLine))
                {
                    thisLine = StripWhitespace(thisLine);
                    instructionList.Add(thisLine);
                }
            }
        }

        // Prepend string: 'strIn' with string: 'prefix'.
        private static string Prepend(string strIn, string prefix)
        {
            bool first = true;
            using (StringWriter streamOut = new StringWriter())
            using (StringReader streamIn = new StringReader(strIn))
            {
                string line;
                while ((line = streamIn.ReadLine()) != null)
                {
                    if (!first) streamOut.WriteLine();
                    streamOut.Write(prefix + line); first = false;
                }
                return streamOut.ToString();
            }
        }

        // Display the usage statement.
        private static void PrintUsage()
        {
            Console.WriteLine("\nUSAGE: Hacksemmbler fileOne.asm [fileTwo.asm ... fileEn.asm]\n");
        }

        // Return string sans comments.
        private static string StripComments(string strIn)
        {
            int delimiter = strIn.IndexOf("/");
            if (delimiter == 0) return "";
            if (delimiter > 0) return strIn.Substring(0, delimiter);
            return strIn;
        }

        // Return string sans equal-sign(s).
        private static string StripEq(string strIn)
        {
            try { return Regex.Replace(strIn, @"=", "", RegexOptions.None, TimeSpan.FromSeconds(0.3)); }
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        // Return string sans whitespace.
        private static string StripWhitespace(string strIn)
        {
            try { return Regex.Replace(strIn, @"\s", "", RegexOptions.None, TimeSpan.FromSeconds(0.3)); }
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        // Report if symbol exists.
        private static bool SymbolInTable(string labelOrNot, List<SymbolEntry> symbolTable)
        {
            for (int i = 0; i < symbolTable.Count; i++)
            {
                string thisID = symbolTable[i].GetID();
                if (labelOrNot == thisID) return true;
            }
            return false;
        }
        #endregion
        #region Encoding Tables
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Encoding tables  // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        // Computaion-bits encoding lookup table.
        private static string EncodeComp(string strIn)
        {
            switch (strIn)
            {
                case "0":   return "0101010";
                case "1":   return "0111111";
                case "-1":  return "0111010";
                case "D":   return "0001100";
                case "A":   return "0110000";
                case "!D":  return "0001101";
                case "!A":  return "0110001";
                case "-D":  return "0001111";
                case "-A":  return "0110011";
                case "D+1": return "0011111";
                case "A+1": return "0110111";
                case "D-1": return "0001110";
                case "A-1": return "0110010";
                case "D+A": return "0000010";
                case "A+D": return "0000010";
                case "D-A": return "0010011";
                case "A-D": return "0000111";
                case "D&A": return "0000000";
                case "D|A": return "0010101";
                case "A&D": return "0000000";
                case "A|D": return "0010101";
                case "M":   return "1110000";
                case "!M":  return "1110001";
                case "-M":  return "1110011";
                case "M+1": return "1110111";
                case "M-1": return "1110010";
                case "D+M": return "1000010";
                case "M+D": return "1000010";
                case "D-M": return "1010011";
                case "M-D": return "1000111";
                case "D&M": return "1000000";
                case "D|M": return "1010101";
                case "M&D": return "1000000";
                case "M|D": return "1010101";
                default:    return "0000000";
            }
        }

        // Destination-bits encoding lookup table.
        private static string EncodeDest(string strIn)
        {
            switch (strIn)
            {
                case "M":   return "001";
                case "D":   return "010";
                case "MD":  return "011";
                case "DM":  return "011";
                case "A":   return "100";
                case "AM":  return "101";
                case "AD":  return "110";
                case "MA":  return "101";
                case "DA":  return "110";
                case "AMD": return "111";
                case "ADM": return "111";
                case "DAM": return "111";
                case "DMA": return "111";
                case "MDA": return "111";
                case "MAD": return "111";
                default:    return "000";
            }
        }

        // Jump-bits encoding lookup table.
        private static string EncodeJump(string strIn)
        {
            switch(strIn)
            {
                case "JGT": return "001";
                case "JEQ": return "010";
                case "JGE": return "011";
                case "JLT": return "100";
                case "JNE": return "101";
                case "JLE": return "110";
                case "JMP": return "111";
                default:    return "000";
            }
        }

        // Populate symbol table with predefined values as per Hack specification.
        private static List<SymbolEntry> PredefineSymbols(List<SymbolEntry> symTable)
        {
            symTable.Add(EnterSymbol("SP", 0, true));
            symTable.Add(EnterSymbol("LCL", 1, true));
            symTable.Add(EnterSymbol("ARG", 2, true));
            symTable.Add(EnterSymbol("THIS", 3, true));
            symTable.Add(EnterSymbol("THAT", 4, true));
            symTable.Add(EnterSymbol("SCREEN", 16384, true));
            symTable.Add(EnterSymbol("KBD", 24576, true));
            symTable.Add(EnterSymbol("R0", 0, true));
            symTable.Add(EnterSymbol("R1", 1, true));
            symTable.Add(EnterSymbol("R2", 2, true));
            symTable.Add(EnterSymbol("R3", 3, true));
            symTable.Add(EnterSymbol("R4", 4, true));
            symTable.Add(EnterSymbol("R5", 5, true));
            symTable.Add(EnterSymbol("R6", 6, true));
            symTable.Add(EnterSymbol("R7", 7, true));
            symTable.Add(EnterSymbol("R8", 8, true));
            symTable.Add(EnterSymbol("R9", 9, true));
            symTable.Add(EnterSymbol("R10", 10, true));
            symTable.Add(EnterSymbol("R11", 11, true));
            symTable.Add(EnterSymbol("R12", 12, true));
            symTable.Add(EnterSymbol("R13", 13, true));
            symTable.Add(EnterSymbol("R14", 14, true));
            symTable.Add(EnterSymbol("R15", 15, true));
            return symTable;
        }
        #endregion
    }
}
