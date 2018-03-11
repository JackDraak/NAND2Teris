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
            // track which argument [input file] we're processing (allows for batch-processing);
            int argument = 0;
            while(argument < args.Length)
            {
                // begin parsing input datastream
                int inFile_length = 0;
                int significant_lines = 0;
                List<String> instruction_list = new List<String>();
                foreach (string instruction in File.ReadAllLines(args[argument]))
                {
                    inFile_length++;
                    String thisInstruction = StripComments(instruction);
                    if (!string.IsNullOrWhiteSpace(thisInstruction))
                    {
                        instruction_list.Add(thisInstruction);
                        significant_lines++; // get # of lines that have symbols or directives
                    }
                }

                // parse instructions
                int stripped_offset = 0;
                string encoded_instruction = "\0";
                c_instruction cInst;
                List<String> outStream = new List<String>();
                foreach (string instruction in instruction_list)
                {
                    if (instruction.Length > 0)
                    {
                        char test = instruction[0];
                        // convert @ instructions to hack machine language addresses (16-bit, two-'s complement binary format)
                        if (test == '@')
                        {
                            encoded_instruction = Encode16BitAddress(stripped_offset, instruction);            
                        }
                        // parse C-instructions <<and later, handle symbols and pointers.... here, or prior to here>>
                        else
                        {
                            // C-instruction format
                            //      in:     [dest=comp;jump]
                            //      out:    1-1-1-a  c-c-c-c  c-c-d-d  d-j-j-j
                            cInst = ParseInstruction(instruction); // TODO: finish parser level 1 & level 2
                            Console.WriteLine($"{cInst.jumpB} {instruction}"); // debug output
                            encoded_instruction = "111" + cInst.compB + cInst.destB + cInst.jumpB;
                        }
                        // stack lines in list for dumping as output filestream
                        outStream.Add(encoded_instruction);
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

        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 
        // \\ // \\  Internal methods // \\ // \\ 
        // \\ // \\ // \\ // \\ // \\ // \\ // \\ 

        private static string Encode16BitAddress(int offset, string instruction)
        {
            // extract @ddress as string...
            String address = CleanAddress(instruction);

            // convert string to integer...
            int address_integer;
            bool success = int.TryParse(address, out address_integer);
            if (!success) Console.WriteLine("FAIL: convert address to integer.");

            // convert integer address to binary...
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
                    // prepend prefix on first pass
                    stream_out.Write(prefix + line); first = false;
                }
                return stream_out.ToString();
            }
        }

        // strip @ from the front of address instructions
        private static string CleanAddress(string strIn)
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
        private static c_instruction ParseInstruction(string strIn)
        {
            c_instruction thisInstruction;
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

        private static string StripComments(string strIn)
        {
            int commentLocation = strIn.IndexOf("//");
            if (commentLocation > 0) return strIn.Substring(0, commentLocation);
            return strIn;
        }

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
                default: return ">D<";
            }
        }

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
                default: return ">J<";
            }
        }
    }
}
