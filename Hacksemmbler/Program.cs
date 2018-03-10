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

            // convert @ instructions to hack addresses
            offset = 0;
            foreach (string instruction in File.ReadAllLines(args[argument]))
            {
                int size = instruction.Length;
                if (size > 0)
                {
                    char test = instruction[0];
                    //string[] address = new string[size];
                    String address;

                    // extract @ addresses (as strings, then convert to integers, then to binary)
                    if (test == '@')
                    {
                        address = CleanAddress(instruction);
                        Console.WriteLine($"line({offset}) -- integer: {address} from string: {address}"); // debug
  
                    }
                    //int thisAddress;
                    //Int32.TryParse(address.ToString(), out thisAddress);
                    //Console.WriteLine($"line({offset}) -- integer: {thisAddress} from string: {address}"); // debug

                    //int fromBase = 10;
                    //int toBase = 2;
                    ///String result = Convert.ToString(Convert.ToInt16(address), toBase);
                    ///Console.WriteLine($"line({offset}) -- integer: {result} from string: {address}"); // debug
                    ///                
                }
                offset++;
            }

            Console.WriteLine($"lines: {lines} vs. offset: {offset}");

            // Chill until user hits enter or return, then exit.
            Console.ReadLine();
        }

        static string CleanAddress(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"@", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }
    }
}
