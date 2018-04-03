#!/usr/bin/env python
#
#	@author	JackDraak
#	
'''	Notes from the Chapter 7 Slides available online at nand2tetris.orc (see: course)

CODEWRITER: Translates VM commands into Hack assembly code.
Routine				Arguments				Returns				Function
-------				---------				-------				--------
Constructor			Output file / stream	--					Opens the output file/stream and gets ready to write into it.

setFileName			fileName (string)		--					Informs the code writer that the translation of a new VM file is started.

writeArithmetic		cmd (string)			--					Writes the assembly code that is the translation of the given arithmetic
																command.

WritePushPop		cmd (C_PUSH|C_POP),		--					Writes the assembly code that is the translation of the given command,
					segment (string),							where command is either C_PUSH or C_POP.
					index (int)

Close				--						--					Closes the output file.

'''
import string, sys

def Start():
	if len(sys.argv) <= 1 or sys.argv[1] == "help": 
		PrintUsage(1)
	try: Main()
	except:
		print("FAIL -- unable to parse input: " + str(sys.argv))
		PrintUsage(9)
