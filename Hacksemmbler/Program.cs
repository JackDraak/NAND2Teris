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
        static void Main(string[] args)
        {
            // output for debug purposes, primarilly
            for(int i = 0; i < args.Length; i++)
            {
                Console.WriteLine($"Arg {i} : {args[i]}");
            }

            // count which argument we're processing; allows for batch-processing
            int argument = 0;

            // initialize some variable we need for our nexr process
            int inFile_length = 0;
            int lines = 0; 

            foreach (string instruction in File.ReadAllLines(args[argument]))
            {
                inFile_length++;
                ///Console.WriteLine($"{lines}. Instruction: {instruction}"); // debug
                // ADD CODE HERE: if instruction is !comment or whitespace, increment lines counter
                lines++;
            }

            // // Strip comment lines, build inputFile_stripped array:
            // initialize code buffer
            string[] inputFile_stripped = new string[lines];
            int offset = 0;

            foreach (string instruction in File.ReadAllLines(args[argument]))
            {
                // ADD CODE HERE: if instruction is !comment or whitespace, add to inputFile_stripped[]
                inputFile_stripped[offset] = instruction; // for now, let's focus on a simple .asm input, however.
                offset++;
            }

            // parse input file
            offset = 0;
            foreach (string instruction in File.ReadAllLines(args[argument]))
            {
                int size = instruction.Length;
                if (size > 0)
                {
                    char test = instruction[0];

                    // convert @ instructions to hack machine language addresses (16-bit, two-'s complement binary format)
                    if (test == '@')
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
                        while (!resolved)
                        {
                            Math.DivRem(address_to_convert, 2, out remainder);
                            address_to_convert = address_to_convert / 2;
                            binary_address = StringPrepender(binary_address, remainder.ToString());
                            place++;
                            if (address_to_convert == 0) resolved = true;
                        }
                        while (place < 15)
                        {
                            binary_address = StringPrepender(binary_address, "0");
                            place++;
                        }

                        Console.WriteLine($"{success} -- " +
                                            $"line({offset}) -- " +
                                            $"integer: {address_integer} " +
                                            $"from string: {address} " +
                                            $"as binary: {binary_address}"); // debug
                    }         
                }
                offset++;
            }

            Console.WriteLine($"lines: {lines} vs. offset: {offset}");

            // End of program, eventually this will exit with a -1, 0, or 1 perhaps.
            // Chill until user hits enter or return, then exit.
            Console.ReadLine();
        }

        // prepend string 'str' with string 'prefix' 
        static string StringPrepender(string str, string prefix)
        {
            bool first = true;
            using (StringWriter writer = new StringWriter())
            using (StringReader reader = new StringReader(str))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!first)
                        writer.WriteLine();
                    writer.Write(prefix + line);
                    first = false;
                }
                return writer.ToString();
            }
        }

        // strip @ from the front of address instructions
        static string CleanAddress(string strIn)
        {
            // Replace invalid characters with empty strings.
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
    }
}
