using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
//using System.Text;
//using System.Threading.Tasks;

namespace Hacksemmbler
/*  For project 6 of part I of "From Nand to Tetris", my take on the Hack Assembler: Hacksemmbler
 *  Be warned, my programming is not industrial-strength; I'm just doing this for fun and self-
 *  development. 
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
 *      - convert C-instructions and @_instructions to machine code
 *          - using symbol table as needed
 * */
{
    class Program
    {
        struct C_code
        {
            public string cmp, dst, jmp;
        };

        static void Main(string[] args)
        {
            // track which argument [input file] we're processing (this allows for batch-processing);
            int argument = 0;
            while(argument < args.Length)
            {
                // initialize setup for each input-file in the batch
                List<string> instructionList;
                List<string> debugLog;
                int nextOpenRegister;
                Dictionary<string, int> symbolTable;
                InitEncode(out nextOpenRegister, out instructionList, out symbolTable, out debugLog);

                // pre-parse input datastream of instructions into a handy-dandy List... let's call it, instructionList
                PreParse(args, argument, instructionList);

                // first-pass, build symbol table
                BuildSymbolTable(instructionList, debugLog, symbolTable);

                // debug output: symbol table
                DebugSymbols(debugLog, symbolTable);

                // parse instructions
                string encodedDirective = "\0";
                List<String> outStream = new List<String>();
                foreach (string instruction in instructionList)
                {
                    if (instruction.Length > 0)
                    {
                        char test = instruction[0];
                        // convert @-instructions to hack machine language addresses
                        encodedDirective = EncodeDirective(debugLog, ref nextOpenRegister, symbolTable, instruction, test);
                        // stack lines in list for dumping as output filestream
                        outStream.Add(encodedDirective);
                    }
                }

                // output encoded data
                String inFile = $"{args[argument]}";
                String inName = GetName(inFile);
                String fileOut = $"{inName}.hack";
                File.Delete(fileOut);
                File.WriteAllText(fileOut, ListAsString(outStream), System.Text.Encoding.Default);

                // output debug stream
                String debugOut = $"_{inName}.debug";
                File.Delete(debugOut);
                File.WriteAllText(debugOut, ListAsString(debugLog), System.Text.Encoding.Unicode);

                // End of program, eventually this will exit with a -1, 0, or 1 perhaps.
                // Chill until user hits enter or return, then exit (or continue batch).
                Console.WriteLine($"...\nprogram: '{inName}' parsed."); // Press <Enter> to continue/close window.");
                if (argument + 1 < args.Length) Console.WriteLine($"Press <Enter> to process {args[argument + 1]}");
                else Console.WriteLine("Press <Enter> to exit");
                argument++; Console.ReadLine();
            }
        }

        #region Internal Methods
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Internal methods // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        // setup symbol table
        private static void BuildSymbolTable(List<string> instructionList, List<string> debugLog, Dictionary<string, int> symbolTable)
        {
            int symbolOffset = 0;
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (LineIsSymbolReference(instructionList[i]))
                {
                    String label = GetLabel(instructionList[i]);
                    if (!symbolTable.ContainsKey(label))
                    {
                        symbolTable.Add(label, symbolOffset);
                        instructionList.RemoveAt(i);
                    }
                    debugLog.Add($"{instructionList[i]}: LineIsSymbolReference");
                }
                symbolOffset++;
            }
        }

        // strip @ from the front of address instructions
        private static string CleanAddress(string strIn)
        {
            // Remove @ character
            try { return Regex.Replace(strIn, @"@", "", RegexOptions.None, TimeSpan.FromSeconds(0.5)); }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        // generate symboltable debug output
        private static void DebugSymbols(List<string> debugLog, Dictionary<string, int> symbolTable)
        {
            for (int i = 0; i < symbolTable.Count; i++)
            {
                debugLog.Add($"Symbol: {symbolTable.Keys.ElementAt(i)} at address: {symbolTable.Values.ElementAt(i)}");
            }
        }

        // convert decmial address to binary address
        private static string Encode16BitAddress(string address)
        {
            int addressAsInteger;
            bool success = int.TryParse(address, out addressAsInteger);
            if (!success)
            {
                Console.WriteLine("FAIL: convert address to integer error: Encode16BitAddress");
            }
            int place = 0;
            int remainder = 0;
            bool resolved = false;
            String binaryAddress = "\0";
            int addressToConvert = addressAsInteger;
            // employing "divide by 2" technique [recursive: modulo to next significant bit, until zero]
            while (!resolved)
            {
                Math.DivRem(addressToConvert, 2, out remainder);
                addressToConvert = addressToConvert / 2;
                binaryAddress = Prepend(binaryAddress, remainder.ToString());
                place++;
                // truncate out of range numbers (likely to produce unexpexcted results with bad address
                // references, in any case, but this will prevent them masquerading as C-instructions.)
                if (addressToConvert == 0 | place == 15) resolved = true;
            }

            // pad any remaining bits with zeros
            while (place < 16)
            {
                binaryAddress = Prepend(binaryAddress, "0");
                place++;
            }
            return binaryAddress;
        }

        // encode @-instructions and C-instructions
        private static string EncodeDirective(List<string> debugLog, ref int nextOpenRegister, Dictionary<string, int> 
                                             symbolTable, string instruction, char test)
        {
            string encodedDirective;
            if (test == '@')
            {
                String address = CleanAddress(instruction);
                int addressAsInteger;
                bool success = int.TryParse(address, out addressAsInteger);
                if (!success)
                {
                    if (symbolTable.TryGetValue(address, out int value))
                    {
                        debugLog.Add($"convert label address: {address}\tto the value: {value}");
                        address = value.ToString();
                    }
                    else
                    {
                        debugLog.Add($"symbol-lookup failure: {address}");
                        debugLog.Add($"adding new label to table: {address}\t{nextOpenRegister}");
                        symbolTable.Add(address, nextOpenRegister);
                        address = nextOpenRegister.ToString();
                        nextOpenRegister++;
                    }
                }
                encodedDirective = Encode16BitAddress(address);
            }
            else
            {
                encodedDirective = ParseCInstruction(debugLog, instruction);
            }

            return encodedDirective;
        }

        // isolate computation directive
        private static string GetComp(string strIn)
        {
            int l = strIn.Length;
            int delimiter = strIn.IndexOf("=");
            //TODO: depreciate: if (delimiter == 0) return "";
            if (delimiter == l) return StripEq(strIn.Substring(delimiter, l - delimiter));
            if (delimiter > 0) return StripEq(strIn.Substring(delimiter, l - delimiter));
            return StripEq(strIn);
        }

        // isolate destination directive
        private static string GetDest(string strIn)
        {
            int delimiter = strIn.IndexOf("=");
            if (delimiter == 0) return "";
            if (delimiter > 0) return strIn.Substring(0, delimiter);
            return strIn;
        }

        // isolate jump directive
        private static string GetJump(string strIn)
        {
            int delimiter = strIn.IndexOf(";");
            if (delimiter == 0) return strIn;
            if (delimiter > 0) return strIn.Substring(0, delimiter);
            return strIn;
        }

        // isolate label
        private static string GetLabel(string strIn)
        {
            return strIn.Substring(1, strIn.Length - 2);
        }

        // isolate program name
        private static string GetName(string strIn)
        {
            int delimiter = strIn.IndexOf(".");
            return strIn.Substring(0, delimiter);
        }

        // for batch processing, re-init before each input file
        private static void InitEncode(out int nextOpenRegister, out List<string> instructionList,
                                       out Dictionary<string, int> symbolTable, out List<string> outDebug)
        {
            nextOpenRegister = 16;
            instructionList = new List<String>();
            outDebug = new List<String>();
            symbolTable = new Dictionary<String, int>();
            symbolTable = PredefineSymbols(symbolTable);
        }

        // convert List of words into a string for output to a file
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

        // identify symbolic label references
        private static bool LineIsSymbolReference(string strIn)
        {
            return strIn.StartsWith("(") && strIn.EndsWith(")");
        }

        // parse C-instruction
        private static string ParseCInstruction(List<string> debugLog, string instruction)
        {
            string encodedDirective;
            C_code cInst;
            // intitialize binary component(s) with dummy values
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
            debugLog.Add($"Instruction: {instruction}\t{dest}\t{comp}\t{jump}\t111{cInst.cmp}{cInst.dst}{cInst.jmp}");
            encodedDirective = "111" + cInst.cmp + cInst.dst + cInst.jmp;
            return encodedDirective;
        }

        // pre-parse input datastream
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

        // prepend string 'str' with string 'prefix' 
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

        // strip comments from instructions
        private static string StripComments(string strIn)
        {
            int delimiter = strIn.IndexOf("/");
            if (delimiter == 0) return "";
            if (delimiter > 0) return strIn.Substring(0, delimiter);
            return strIn;
        }

        // strip equal-sign
        private static string StripEq(string strIn)
        {
            try { return Regex.Replace(strIn, @"=", "", RegexOptions.None, TimeSpan.FromSeconds(0.5)); }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        // strip whitespace
        private static string StripWhitespace(string strIn)
        {
            try { return Regex.Replace(strIn, @"\s", "", RegexOptions.None, TimeSpan.FromSeconds(0.5)); }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        #endregion

        #region Encoding Tables
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Encoding tables  // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        // Comp encoding table
        private static string EncodeComp(string strIn)
        {
            switch (strIn)
            {
                case "0": return "0101010";
                case "1": return "0111111";
                case "-1": return "0111010";
                case "D": return "0001100";
                case "A": return "0110000";
                case "!D": return "0001101";
                case "!A": return "0110001";
                case "-D": return "0001111";
                case "-A": return "0110011";
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
                case "M": return "1110000";
                case "!M": return "1110001";
                case "-M": return "1110011";
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
                default: return "0000000";
            }
        }

        // Dest encoding table
        private static string EncodeDest(string strIn)
        {
            switch (strIn)
            {
                case "M": return "001";
                case "D": return "010";
                case "MD": return "011";
                case "DM": return "011";
                case "A": return "100";
                case "AM": return "101";
                case "AD": return "110";
                case "MA": return "101";
                case "DA": return "110";
                case "AMD": return "111";
                case "ADM": return "111";
                case "DAM": return "111";
                case "DMA": return "111";
                case "MDA": return "111";
                case "MAD": return "111";
                default: return "000";
            }
        }

        // Jump encoding table
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
                default: return "000";
            }
        }

        // Populate symbol table with predefined values
        private static Dictionary<String, int> PredefineSymbols(Dictionary<String, int> symTable)
        {
            symTable.Add("SP", 0);
            symTable.Add("LCL", 1);
            symTable.Add("ARG", 2);
            symTable.Add("THIS", 3);
            symTable.Add("THAT", 4);
            symTable.Add("SCREEN", 16384);
            symTable.Add("KBD", 24576);
            symTable.Add("R0", 0);
            symTable.Add("R1", 1);
            symTable.Add("R2", 2);
            symTable.Add("R3", 3);
            symTable.Add("R4", 4);
            symTable.Add("R5", 5);
            symTable.Add("R6", 6);
            symTable.Add("R7", 7);
            symTable.Add("R8", 8);
            symTable.Add("R9", 9);
            symTable.Add("R10", 10);
            symTable.Add("R11", 11);
            symTable.Add("R12", 12);
            symTable.Add("R13", 13);
            symTable.Add("R14", 14);
            symTable.Add("R15", 15);
            return symTable;
        }
        #endregion
    }
}
