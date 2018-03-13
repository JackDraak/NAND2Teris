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
        struct C_instruction
        {
            public string compB; public string destB; public string jumpB;
            public string compT; public string destT; public string jumpT;
        };

        static void Main(string[] args)
        {
            // track which argument [input file] we're processing (allows for batch-processing);
            int argument = 0;
            while(argument < args.Length)
            {
                // begin parsing input datastream
                int inFile_length = 0;
                int significant_lines = 0;
                List<String> instruction_list = new List<String>();
                Dictionary<String, int> symbolTable = new Dictionary<String, int>();
                symbolTable = PredefineSymbols(symbolTable);
                foreach (string input_line in File.ReadAllLines(args[argument]))
                {
                    inFile_length++;
                    String thisInstruction = StripComments(input_line);
                    if (!string.IsNullOrWhiteSpace(thisInstruction))
                    {
                        thisInstruction = StripWhitespace(thisInstruction);
                        instruction_list.Add(thisInstruction);
                        ///Console.WriteLine($"{significant_lines}) instruction_list.Add({thisInstruction})"); // debug output
                        significant_lines++; // get # of lines that have symbols or directives
                    }
                    // TODO: first-pass, build symbol table
                    int symbol_offset = 0;
                    foreach (string instruction in instruction_list)
                    {
                        symbol_offset++;
                        if (LineIsSymbolReference(instruction))
                        {
                            if (!symbolTable.ContainsKey(instruction))
                            {
                                symbolTable.Add(instruction, symbol_offset);
                            }
                            symbol_offset--;
                        }
                    }
                }
                // debug output: symbol table
                for (int i = 0; i < symbolTable.Count; i++)
                {
                    Console.WriteLine($"Symbol: {symbolTable.Keys.ElementAt(i)} at address: {symbolTable.Values.ElementAt(i)}");
                }

                // parse instructions
                int stripped_offset = 0;
                string encoded_directive = "\0";
                C_instruction cInst;
                List<String> outStream = new List<String>();
                foreach (string instruction in instruction_list)
                {
                    if (instruction.Length > 0)
                    {
                        char test = instruction[0];
                        // convert @ instructions to hack machine language addresses (16-bit, two-'s complement binary format)
                        if (test == '@')
                        {
                            String address = CleanAddress(instruction);
                            Console.WriteLine($"address: {address}\t{instruction}"); // debug output

                            int address_integer;
                            bool success = int.TryParse(address, out address_integer);
                            if (!success)
                            {
                               // symbol_list.Contains(SymbolEntry = );
                            }

                            // TODO: convert labels to addresses
                            encoded_directive = Encode16BitAddress(address);            
                        }
                        // parse C-instructions
                        else
                        {
                            // C-instruction format
                            //      in:     [dest=comp;jump]
                            //      out:    1-1-1-a  c-c-c-c  c-c-d-d  d-j-j-j
                            cInst = ParseInstruction(instruction); // TODO: finish parser level 1 & level 2
                            Console.WriteLine($"{cInst.jumpB} {instruction}"); // debug output
                            encoded_directive = "111" + cInst.compB + cInst.destB + cInst.jumpB;
                        }
                        // stack lines in list for dumping as output filestream
                        outStream.Add(encoded_directive);
                    }
                    stripped_offset++;
                }

                // output encoded data
                String fileOut = $"_{args[argument]}.hack";
                File.Delete(fileOut);

                // convert from list to string
                string dataOut = "";
                for (int i = 0; i < outStream.Count; i++)
                {
                    dataOut = (dataOut + outStream.ElementAt(i) + "\r\n");
                }
                System.IO.File.WriteAllText(fileOut, dataOut);

                // End of program, eventually this will exit with a -1, 0, or 1 perhaps.
                // Chill until user hits enter or return, then exit (or continue batch).
                Console.WriteLine($"...\n{args[argument]} parsed. Press <Enter> to continue/close window.");
                argument++;
                Console.ReadLine();
            }
        }

        #region Internal Methods
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Internal methods // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        // convert decmial address to binary address
        private static string Encode16BitAddress(string address)
        {
            int address_integer;
            bool success = int.TryParse(address, out address_integer);
            if (!success)
            {
                Console.WriteLine("FAIL: convert address to integer.");
            }
            int place = 0;
            int remainder = 0;
            bool resolved = false;
            String binary_address = "\0";
            int address_to_convert = address_integer;
            // employing "divide by 2" technique [recursive: modulo to next significant bit, until zero]
            while (!resolved)
            {
                Math.DivRem(address_to_convert, 2, out remainder);
                address_to_convert = address_to_convert / 2;
                binary_address = Prepend(binary_address, remainder.ToString());
                place++;
                // truncate out of range numbers (likely to produce unexpexcted results with bad address
                // references, in any case, but this will prevent them masquerading as C-instructions.)
                if (address_to_convert == 0 | place == 15) resolved = true; 
            }

            // pad any remaining bits with zeros
            while (place < 16)
            {
                binary_address = Prepend(binary_address, "0");
                place++;
            }
            return binary_address;
        }

        // prepend string 'str' with string 'prefix' 
        private static string Prepend(string strIn, string prefix)
        {
            bool first = true;
            using (StringWriter stream_out = new StringWriter())
            using (StringReader stream_in = new StringReader(strIn))
            {
                string line;
                while ((line = stream_in.ReadLine()) != null)
                {
                    if (!first) stream_out.WriteLine();
                    stream_out.Write(prefix + line); first = false;
                }
                return stream_out.ToString();
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

        // parse C-instructions [dest=comp;jump]
        private static C_instruction ParseInstruction(string strIn)
        {
            C_instruction thisInstruction;
            // intitialize binary component(s)
            thisInstruction.compB = "acccccc";
            thisInstruction.destB = "ddd";
            thisInstruction.jumpB = "jjj";
            // initialize symbolic component
            thisInstruction.compT = "";
            thisInstruction.destT = "";
            thisInstruction.jumpT = "";

            // isolate jump directive
            String[] jumpT = Regex.Split(strIn, ";");

            for (int i = 0; i < jumpT.Length; i++)
            {
                thisInstruction.jumpB = EncodeJump(jumpT[i]);
                // debug output
                Console.WriteLine($"({i}) symbol: {jumpT[i]} resolves to {thisInstruction.jumpB}");
            }

            // isolate dest and comp directives
            String[] destT = Regex.Split(strIn, "=");
            foreach (string str in destT)
            {
                thisInstruction.destB = EncodeDest(str);
            }
            return thisInstruction;
        }

        // strip whitespace
        private static string StripWhitespace(string strIn)
        { 
            try { return Regex.Replace(strIn, @"\s", "", RegexOptions.None, TimeSpan.FromSeconds(0.5)); }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException) { return String.Empty; }
        }

        // identify symbolic label directives
        private static bool LineIsSymbolReference(string strIn)
        {
            return strIn.StartsWith("(") && strIn.EndsWith(")");
        }

        // isolate symbolic directive label
        private static string GetSymbol(string strIn)
        {
            return strIn.Substring(1, strIn.Length -2);
        }

        // copy of StripComments... depreciated?
        private static string TagSymbols(string strIn)
        {
            int symbol_id = strIn.IndexOf("/"); // NB? wth?
            if (symbol_id == 0) return "";
            if (symbol_id > 0) return strIn.Substring(0, symbol_id);
            return strIn;
        }

        // strip comments out of instructions
        private static string StripComments(string strIn)
        {
            int commentLocation = strIn.IndexOf("/");
            if (commentLocation == 0) return "";
            if (commentLocation > 0) return strIn.Substring(0, commentLocation);
            return strIn;
        }
        #endregion

        #region Encoding Tables
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Encoding tables  // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

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
                default: return "-D-";
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
                default: return "-J-";
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
