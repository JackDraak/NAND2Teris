using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hacksemmbler
/*  For project 6 of part I of "From Nand to Tetris", my take on the Hack Assembler: Hacksemmbler
 *  Be warned, my programming is not industrial-strength; I'm just doing this for fun and self-
 *  development. @JackDraak
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

        static void Main(string[] args)
        {
            // Simple sanity checks, could positively be more robust if extended for a broader audience.
            if (args.Length == 0 || args[0] == "?" || args[0] == "help" || !LooksLikeValidAsm(args[0])) 
            {
                PrintUsage();
                return; // No this doesn't 'return' from the IF, it will exit Main gracefully.
            }

            // Track which argument [input file] we're processing (this allows for batch-processing).
            int argument = 0;
            while(argument < args.Length)
            {
                // Initialize setup for each input-file in the batch.
                String thisProgram = GetProgramName(args, argument);
                List<string> instructionList, debugLog;
                int nextOpenRegister;
                Dictionary<string, int> symbolTable;
                InitEncode(out nextOpenRegister, out instructionList, out encodeHeader, out symbolTable, out debugLog);

                // Pre-parse input-stream of instructions into a handy-dandy List... let's call it: instructionList.
                PreParse(args, argument, instructionList);

                // Output parsed instructions.
                DebugPreParsed(thisProgram, true, instructionList, debugLog);

                // On first-pass, build requisite symbol table.
                BuildSymbolTable(instructionList, debugLog, symbolTable);

                // Parse and encode instructionList. 
                List<string> encodedInstructions = DoEncode(instructionList, debugLog, ref nextOpenRegister, symbolTable);

                // Debug output: symbol table.
                DebugSymbols(debugLog, symbolTable, thisProgram, true);

                // Output encoded data.
                string programName = OutputProgram(args, argument, encodedInstructions);

                // Output debug stream.
                OutputDebug(debugLog, programName);

                // Chill until user hits enter or return, then exit (or continue batch).
                argument = ContinueOrExit(args, argument, programName);
            }
        }

        #region Internal Methods
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Internal methods // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        // Setup symbolTable from assembly instructionList.
        private static void BuildSymbolTable(List<string> instructionList, List<string> debugLog, Dictionary<string, int> symbolTable)
        {
            ///bool header = false;
            int symbolOffset = 0;
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (symbolOffset == 144 | symbolOffset == 145)
                {
                    Console.WriteLine($"{symbolOffset}\tInstr: {instructionList[i]}");
                }
                if (LineIsSymbolReference(instructionList[i]))
                {
                    String label = GetLabel(instructionList[i]);
                    if (symbolOffset == 144 | symbolOffset == 145)
                    {
                        Console.WriteLine($"{symbolOffset}\tLabel: {label}");
                    }
                    if (!symbolTable.ContainsKey(label))
                    {
                        if (symbolOffset == 144 | symbolOffset == 145)
                        {
                            Console.WriteLine($"New: {label} = {symbolOffset}");
                        }
                        symbolTable.Add(label, symbolOffset);
                        //instructionList.RemoveAt(i); // Maybe this isnt the best thing to do?
                    }
            /*        if (!header)
                    {
                        debugLog.Add($"------\t------\t\t-------------");
                        debugLog.Add($"Offset\tSymbol\t\tDetail");
                        debugLog.Add($"------\t------\t\t-------------");
                        header = true;
                    }
                    debugLog.Add($"{symbolOffset})\t{instructionList[i]}\t\t: Line Is Symbol Reference");
            */
                }
                if (!LineIsSymbolReference(instructionList[i])) symbolOffset++; // NB
            }
        }

        // Strip '@' from the front of address instructions.
        private static string CleanAddress(string strIn)
        {
            try { return Regex.Replace(strIn, @"@", "", RegexOptions.None, TimeSpan.FromSeconds(0.3)); }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException) { return String.Empty; }
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
        private static void DebugPreParsed(String thisName, bool outToFile, List<string> thisList, List<string> debugLog)
        {
            bool header = false;
            int localOffset = 0;
            List<string> fileOut = new List<string>();
            for (int i = 0; i < thisList.Count; i++)
            {
                if (!header)
                {
                    header = true;
                    if (outToFile)
                    {
                        fileOut.Add($"Line#\tPre-Parsed Instruction Set");
                        fileOut.Add($"-----\t--------------------------");
                    }
                }
                if (outToFile)
                {
                    // symbol lines
                    if (LineIsSymbolReference(thisList[i]))
                    {
                        fileOut.Add($"---)\t\t{thisList[i]}");
                    }
                    else
                    // regular lines
                    {
                        fileOut.Add($"{localOffset})\t\t{thisList[i]}");
                        localOffset++;
                    }
                }
            }
            
            if (outToFile)
            {
                String outName = $"_{thisName}.preparsed";
                File.Delete(outName);
                File.WriteAllText(outName, ListAsString(fileOut), System.Text.Encoding.Unicode);
            }
        }

        // Generate symbolTable debug output.
        private static void DebugSymbols(List<string> debugLog, Dictionary<string, int> symbolTable, string thisName, bool outToFile)
        {
            if (outToFile)
            {
                String outName = $"_{thisName}.symbolTable";
                File.Delete(outName);
                List<string> thisTable = new List<string>();
                foreach(var thisPair in symbolTable)
                {
                    var thisVal = thisPair.Value;
                    List<string> keyList = new List<string>();
                    List<int> valList = new List<int>();
                    foreach (var myPair in symbolTable)
                    {
                        var myKey = myPair.Key;
                        var myVal = myPair.Value;
                        if (myVal == thisVal)
                        {
                            keyList.Add(myKey);
                        }
                    }
                    thisTable.Add($"Value: {thisVal}\t{ListOfValues(keyList)}");           
                }
                List<string> fileOut = new List<string>();
                /*foreach (var thisPair in symbolTable) // dont think this did squat.
                {
                    var thisKey = thisPair.Key;
                    foreach (string str in thisTable)
                    {
                        if (str.Contains(thisKey))
                        {
                            fileOut.Add(str);
                        }
                    }
                } */
                //..thisTable.Sort();
                //int symbolCount = thisTable.Count;
                //..List<string> unique = thisTable.Distinct().ToList(); // NB trimming dupes for now.
                //int symbolCountUnique = unique.Count;
                //thisTable.Add($"Full Table Count: {symbolCount}\tDupes:{symbolCount - symbolCountUnique}");
                //fileOut.Sort();
                File.WriteAllText(outName, ListAsString(thisTable), System.Text.Encoding.Unicode);
            }
        }

        // Wrapper for encoding operation, to help keep Main tidy.
        private static List<string> DoEncode(List<string> instructionList, List<string> debugLog, 
                                             ref int nextOpenRegister, Dictionary<string, int> symbolTable)
        {
            List<String> encodedInstructions = new List<String>();
            int offset = 0;
            foreach (string instruction in instructionList)
            {
                if (instruction.Length > 0 && !LineIsSymbolReference(instruction))
                {
                    // Convert @-instructions and C-instructions to hack machine language encoding.
                    encodedInstructions.Add(EncodeDirective(debugLog, ref nextOpenRegister, symbolTable, offset, instruction));
                    offset++;
                }
                else
                {
                    // handle symbols
                    String address = CleanAddress(instruction);
                    int addressAsInteger;
                    bool success = int.TryParse(address, out addressAsInteger);
                    if (!success)
                    {
                        if (symbolTable.TryGetValue(address, out int value))
                        {
                            string logString = $"({offset})\tConversion:\t{address}={value}";
                            address = value.ToString();
                            debugLog.Add(logString);
                            //fileLog.Add(logString);
                        }
                        else
                        {
                            symbolTable.Add(address, nextOpenRegister);
                            string logString = $"({offset})\tNew label:\t{address}={nextOpenRegister}";
                            debugLog.Add(logString);
                            //fileLog.Add(logString);
                            address = nextOpenRegister.ToString();
                            nextOpenRegister++;
                        }
                    }
                }
            }
            return encodedInstructions;
        }

        // Encode binary address from decimal address.
        private static string Encode16BitAddress(string address)
        {
            int addressAsInteger;
            bool success = int.TryParse(address, out addressAsInteger);
            if (!success)
            {
                // This should never happen; addresses should have been converted from labels to numbers before encoding.
                Console.WriteLine("FAIL: convert address to integer error: Encode16BitAddress");
            }
            int place = 0;
            int remainder = 0;
            bool resolved = false;
            String binaryAddress = " "; // NB - return StripWhitespace(binaryAddress); The fix for Issue #3.
            int addressToConvert = addressAsInteger;
            // Employing "divide by 2" technique [recursive: modulo to next significant bit, until zero].
            while (!resolved)
            {
                Math.DivRem(addressToConvert, 2, out remainder);
                addressToConvert = addressToConvert / 2;
                binaryAddress = Prepend(binaryAddress, remainder.ToString());
                place++;
                // Truncate out of range numbers (they're likely to produce unexpexcted results with bad 
                // references, in any case, but this will prevent such from masquerading as C-instructions).
                if (addressToConvert == 0 | place == 15) resolved = true;
            }

            // Pad any remaining bits with zeros.
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
        private static string EncodeDirective(List<string> debugLog, ref int nextOpenRegister, Dictionary<string, int> symbolTable,
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
                    GetSymbolOrUpdateTable(debugLog, ref nextOpenRegister, symbolTable, offset, fileLog, ref address);
                }
                encodedDirective = Encode16BitAddress(address);
            }
            else
            {
             /*   if (LineIsSymbolReference(instruction))
                {
                    String address = instruction;
                    int addressAsInteger;
                    bool success = int.TryParse(instruction, out addressAsInteger);
                    GetSymbolOrUpdateTable(debugLog, ref nextOpenRegister, symbolTable, offset, fileLog, ref address);
                }
                else
                {
                }
                */
                    encodedDirective = EncodeC(debugLog, instruction);
            }
            return encodedDirective;
        }

        // Convert symbol or assign label stuff
        private static void GetSymbolOrUpdateTable(List<string> debugLog, ref int nextOpenRegister, Dictionary<string, int> symbolTable, 
                                                   int offset, List<string> fileLog, ref string address)
        {
            if (symbolTable.TryGetValue(address, out int value))
            {
                string logString = $"({offset})\tConversion:\t{address}={value}";
                address = value.ToString();
                debugLog.Add(logString);
                fileLog.Add(logString);
            }
            else
            {
                symbolTable.Add(address, nextOpenRegister);
                string logString = $"({offset})\tNew label:\t{address}={nextOpenRegister}";
                debugLog.Add(logString);
                fileLog.Add(logString);
                address = nextOpenRegister.ToString();
                nextOpenRegister++;
            }
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
            if (delimiter <= 0) return ""; // NB - If there is no assignment (no = sign), return empty string.
            if (delimiter > 0) return strIn.Substring(0, delimiter);
            return strIn;
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

        // Isolate & return program name from argument(s).
        private static string GetName(string strIn)
        {
            int delimiter = strIn.IndexOf(".");
            return strIn.Substring(0, delimiter);
        }

        // Return name of program in the que.
        private static string GetProgramName(string[] args, int argument)
        {
            String inName = GetName(args[argument]);
            return inName;
        }

        // For batch processing, re-init before each input file: Lists, tables, debug & netOpenRegister.
        private static void InitEncode(out int nextOpenRegister, out List<string> instructionList, out bool encodeHeader,
                                       out Dictionary<string, int> symbolTable, out List<string> outDebug)
        {
            nextOpenRegister = 16;
            encodeHeader = false;
            instructionList = new List<String>();
            outDebug = new List<String>();
            symbolTable = new Dictionary<String, int>();
            symbolTable = PredefineSymbols(symbolTable);
        }

        // Identify symbolic references.
        private static bool LineIsSymbolReference(string strIn)
        {
            return strIn.StartsWith("(") && strIn.EndsWith(")");
        }

        // Convert a List<String> into a continuous string for output to a file.
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

        // Convert a List<string> into a continuous string for output to a file.
        private static string ListOfValues(List<string> listIn)
        {
            using (StringWriter streamOut = new StringWriter())
            {
                string thisVal;
                int i = 0;
                while (i < listIn.Count)
                {
                    thisVal = listIn[i];
                    streamOut.Write(thisVal + ", ");
                    i++;
                }
                return streamOut.ToString();
            }
        }

        // Convert a List<int> into a continuous string for output to a file.
        private static string ListOfValues(List<int> listIn)
        {
            using (StringWriter streamOut = new StringWriter())
            {
                int thisVal;
                int i = 0;
                while (i < listIn.Count)
                {
                    thisVal = listIn[i];
                    streamOut.Write(thisVal + ", ");
                    i++;
                }
                return streamOut.ToString();
            }
        }

        // Validate input assembly file(s).
        private static bool LooksLikeValidAsm(string strIn)
        {
            // For now, this simply checks for "*.asm" : .[aA][sS][mM]
            var probe = Regex.Match(strIn, @".[aA][sS][mM]");
            var test = probe.Captures;
            if (test.Count > 0) return true;
            return false;
        }

        // Output debugging information.
        private static void OutputDebug(List<string> debugLog, string inName)
        {
            String debugOut = $"_{inName}.debug";
            File.Delete(debugOut);
            File.WriteAllText(debugOut, ListAsString(debugLog), System.Text.Encoding.Unicode);
        }
        
        // Output machine-encoded program.
        private static string OutputProgram(string[] args, int argument, List<string> encodedInstructions)
        {
            String inName = GetName(args[argument]);
            String fileOut = $"{inName}.hack";
            File.Delete(fileOut);
            File.WriteAllText(fileOut, ListAsString(encodedInstructions), System.Text.Encoding.ASCII);
            return inName;
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

        // Return string sans equal-sign.
        private static string StripEq(string strIn)
        {
            try { return Regex.Replace(strIn, @"=", "", RegexOptions.None, TimeSpan.FromSeconds(0.3)); }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        // Return string sans whitespace.
        private static string StripWhitespace(string strIn)
        {
            try { return Regex.Replace(strIn, @"\s", "", RegexOptions.None, TimeSpan.FromSeconds(0.3)); }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        #endregion

        #region Encoding Tables
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Encoding tables  // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        // Comp encoding lookup table.
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

        // Destination encoding lookup table.
        // TODO: target for simplification... why not handle each bit, (A, D, M), individually?
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

        // Jump encoding lookup table.
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

        // Populate symbol table with predefined values as per specification.
        private static Dictionary<String, int> PredefineSymbols(Dictionary<String, int> symTable)
        {
            symTable.Add("SP",      0);
            symTable.Add("LCL",     1);
            symTable.Add("ARG",     2);
            symTable.Add("THIS",    3);
            symTable.Add("THAT",    4);
            symTable.Add("SCREEN",  16384);
            symTable.Add("KBD",     24576);
            symTable.Add("R0",      0);
            symTable.Add("R1",      1);
            symTable.Add("R2",      2);
            symTable.Add("R3",      3);
            symTable.Add("R4",      4);
            symTable.Add("R5",      5);
            symTable.Add("R6",      6);
            symTable.Add("R7",      7);
            symTable.Add("R8",      8);
            symTable.Add("R9",      9);
            symTable.Add("R10",     10);
            symTable.Add("R11",     11);
            symTable.Add("R12",     12);
            symTable.Add("R13",     13);
            symTable.Add("R14",     14);
            symTable.Add("R15",     15);
            return symTable;
        }
        #endregion
    }
}
