using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
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
            Console.WriteLine("Hello World!"); // debug
            for(int i = 0; i < args.Length; i++) // debug
            {
                Console.WriteLine("Arg {0} : {1}", i, args[i]);
            }

            int lines = 0;
            foreach (string instruction in File.ReadAllLines(args[0]))
            {
                Console.WriteLine($"{lines}. Instruction: {instruction}"); // debug

                // CODE: if instruction is !comment or whitespace, increment lines counter
                lines++;
            }

            // strip comment lines, build inputFIle_stripped
            string[] inputFile_stripped = new string[lines];
            int offset = 0;
            foreach (string instruction in File.ReadAllLines(args[0]))
            {
                // if instruction is !comment or whitespace, add to program
                inputFile_stripped[offset] = instruction;
                offset++;
            }

            Console.WriteLine($"lines: {lines} vs. offset: {offset}");




            Console.ReadLine();
        }
    }
}
