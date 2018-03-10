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

            // convert @ instructions to hack machine language addresses (16-bit, two-'s complement binary format)
            offset = 0;
            foreach (string instruction in File.ReadAllLines(args[argument]))
            {
                int size = instruction.Length;
                if (size > 0)
                {
                    char test = instruction[0];
                    //string[] address = new string[size];
                    String address;

                    // extract @ addresses (as strings, then convert to integer, then to binary)
                    if (test == '@')
                    {
                        // extract address
                        address = CleanAddress(instruction);

                        // convert to integer
                        int address_number;
                        bool success = int.TryParse(address, out address_number);
                        if (!success) Console.WriteLine("FAIL: convert address to integer.");
                        
                        // convert address to binary
                        int result = 0;
                        Math.DivRem(address_number, 2, out result); // get this into a loop

                        Console.WriteLine($"{success} -- line({offset}) -- integer: {address_number} from string: {address}"); // debug
                    }         
                }
                offset++;
            }

            Console.WriteLine($"lines: {lines} vs. offset: {offset}");

            // End of program, eventually this will exit with a -1, 0, or 1 perhaps.
            // Chill until user hits enter or return, then exit.
            Console.ReadLine();
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
