using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hacksemmbler
/*  For project 6 of part I of From Nand to Tetris, the Hack Assembler: Hacksemmbler
 *  
 *  design methodology: create program to generate Hack Machine Language from Hack Assembly Language
 *  First, process simple .asm lacking comments and symbols, then adapt for symbol integration.
 *  Order of operations: 
 *      - read and parse .asm input
 *      - generate instruction sequence free of comments/whitespace
 *      - record labels in symbol table
 *      - allocate pointers and record in symbol table
 *      - convert instructions to machine code, using symbol table as needed
 * */
{
    class Program
    {
        struct c_instruction
        {
            public string compB;
            public string destB;
            public string jumpB;
            public string compT;
            public string destT;
            public string jumpT;
        };


        static void Main(string[] args)
        {
            // debug output
            //for(int i = 0; i < args.Length; i++) { Console.WriteLine($"Arg {i} : {args[i]}"); }

            // count which argument [input file] we're processing; <<setting-up to allow for batch-processing>>
            int argument = 0;

            while(argument < args.Length)
            {
                // initialize some variables
                int inFile_length = 0;
                int significant_lines = 0; 

                // discover length of input filestream
                foreach (string instruction in File.ReadAllLines(args[argument]))
                {
                    inFile_length++;
                    ///Console.WriteLine($"{lines}. Instruction: {instruction}"); // debug
                    // <<ADD CODE HERE:>> if instruction is !comment or whitespace, increment significant_lines counter
                    significant_lines++; // get # of lines that have symbols or directives
                }
                // initialize code buffer
                string[] inputFile_stripped = new string[significant_lines];

                // strip non-instruction lines, build inputFile_stripped array:
                int filesize_offset = 0;
                foreach (string instruction in File.ReadAllLines(args[argument]))
                {
                    // <<ADD CODE HERE:>> if instruction is !comment or whitespace, add to inputFile_stripped[]
                    inputFile_stripped[filesize_offset] = instruction; // for now, let's focus on a simple .asm input, however.
                    filesize_offset++;
                }

                // parse input file
                int stripped_offset = 0;
                string encoded_instruction = "\0";
                c_instruction cInst;
                List<String> outStream = new List<String>();
                foreach (string instruction in inputFile_stripped)
                {
                    if (instruction.Length > 0)
                    {
                        char test = instruction[0];
                        // convert @ instructions to hack machine language addresses (16-bit, two-'s complement binary format)
                        if (test == '@')
                        {
                            encoded_instruction = Encode16BitAddress(stripped_offset, instruction);
                            ///Console.WriteLine($"stripped line number: {stripped_offset} @ddress as 16-bit binary: {encoded_instruction}"); // debug output             
                        }
                        // parse C-instructions <<and later, handle symbols and pointers.... here, or prior to here>>
                        else
                        {
                            // C-instruction format
                            //      in:     [dest=comp;jump]
                            //      out:    1-1-1-a  c-c-c-c  c-c-d-d  d-j-j-j
                            cInst = ParseInstruction(instruction);
                            Console.WriteLine($"{cInst.jumpB} {instruction}");
                            encoded_instruction = "111" + cInst.compB + cInst.destB + cInst.jumpB;
                            ///encoded_instruction = EncodeCInstruction(compDestJump);
                        }
                        // stack lines in list for dumping as output filestream
                        outStream.Add(encoded_instruction);
                    }
                    stripped_offset++;
                }

                //debug: output what we have so far
                for (int i = 0; i < outStream.Count; i++)
                {
                    Console.WriteLine($"offset: {i}\t\tencoded: {outStream.ElementAt(i)}");
                }

                ///Console.WriteLine($"lines: {significant_lines} vs. offset: {stripped_offset}"); // debug output

                // End of program, eventually this will exit with a -1, 0, or 1 perhaps.
                // Chill until user hits enter or return, then exit.
                Console.WriteLine($"...\n{args[argument]} parsed. Press <Enter> to continue/close window.");
                Console.ReadLine();
                argument++;
            }
        }

        //
        // // // // Internal methods // // // //
        //

        /* private static string EncodeCInstruction(c_instruction cInst)
        {
            String encoded_directive = "Code me.\0";
            return encoded_directive;
        } */

        private static string Encode16BitAddress(int offset, string instruction)
        {
            // extract @ddress (as string, then convert to integer, then to binary)
            String address = CleanAddress(instruction);

            // convert to integer
            int address_integer;
            bool success = int.TryParse(address, out address_integer);
            if (!success) Console.WriteLine("FAIL: convert address to integer.");

            // convert address to binary
            int place = 0;
            int remainder = 0;
            bool resolved = false;
            String binary_address = "\0";
            int address_to_convert = address_integer;
            
            // use divide by 2 technique [recursive: modulo to next significant bit, until zero]
            while (!resolved)
            {
                Math.DivRem(address_to_convert, 2, out remainder);
                address_to_convert = address_to_convert / 2;
                binary_address = Prepend(binary_address, remainder.ToString());
                place++;
                if (address_to_convert == 0) resolved = true;
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
        static string Prepend(string strIn, string prefix)
        {
            bool first = true;
            using (StringWriter stream_out = new StringWriter())
            using (StringReader stream_in = new StringReader(strIn))
            {
                string line;
                while ((line = stream_in.ReadLine()) != null)
                {
                    if (!first) stream_out.WriteLine();
                    // prepend prefix on first pass
                    stream_out.Write(prefix + line); first = false;
                }
                return stream_out.ToString();
            }
        }

        // strip @ from the front of address instructions
        static string CleanAddress(string strIn)
        {
            // Replace @ character with empty strings.
            try
            {
                return Regex.Replace(strIn, @"@", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, we return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        // parse C-instructions [comp, dest, jump]
        static c_instruction ParseInstruction(string strIn)
        {
            c_instruction thisInstruction;
            // intitialize binary component
            thisInstruction.compB = "0000000";
            thisInstruction.destB = "000";
            thisInstruction.jumpB = "000";
            // initialize symbolic component
            thisInstruction.compT = "";
            thisInstruction.destT = "";
            thisInstruction.jumpT = "";
            // isolate jump directive
            String[] jumpT = Regex.Split(strIn, ";");
            if (jumpT.Length > 1)
            {
                foreach(string str in jumpT)
                {
                    thisInstruction.jumpB = EncodeJump(str);
                }
            }
            return thisInstruction;
        }

        static string EncodeJump(string strIn)
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
    }
}
