#!/usr/bin/env python
#
#	@author	JackDraak
#
import string, sys
VERSION = "0.0.1"

class CodeWriter:
	fileName = None
	oFile = None
	oStream = []

	def setFilename(strIn):
		fileName = strIn

	def Constructor():
		oFile = open(fileName, 'w')

	def Close():
		oFile.close()

	def writePushPop(command, segment, index):
		if command is "C_PUSH":
			oFile.write("@" + segment + "." + str(index))
		# TODO: more stuff here -- POP

	def writeArithmetic(directive):
		c_add = """// add
		@SP
		A=M		// Fetch SP pointer
		M=A-1	// Decrement SP
		D=M		// Fetch top of stack (Y)
		@SP
		M=M+D	// X+Y, left in top of stack
		"""
		c_sub = """// sub
		@SP 
		A=M		// Fetch SP pointer
		M=A-1	// Decrement SP
		D=M		// Fetch top of stack (Y)
		@SP
		M=M-D	// X-Y, left in top of stack
		"""
		# TODO: more stuff here -- add, sub, neg, eq, gt, lt, and, or, not
		commands = {"add":c_add, "sub":c_sub}
		oFile.write(commands.get(strIn, None))


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