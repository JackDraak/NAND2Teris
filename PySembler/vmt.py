#!/usr/bin/env python
#
#	@author	JackDraak
#	VMTranslator. Suggested: Parser, CodeWriter, Main
#
#	Phase 1: 
#		Arithmetic: add, sub (x-y), neg (-y), eq, gt (x>y), lt (x<y), and, or, not ('y) 
#		Memory Access: push, pop
#		Program Flow: label, goto, if-goto
#
#	Example:
#		push constant 6
#		push constant 5
#		add
#
#	Additional Notes:
#		The Stack: mapped to RAM[256..2047], pointer stored in RAM[SP]
#		Static Segments: mapped to RAM[16..255]
#		local, argument, this, that: RAM[2048..+], 
#			pointer (to base) stored in RAM[]:, LCL, ARG, THIS, THAT
#			therefore argument.7 is accessed as RAM[ARG + 7]

'''	Notes from the Chapter 7 Slides available online at nand2tetris.orc (see: course)

PARSER: Handles the parsing of a single .vm file, and encapsulates access to the input code. It reads VM commands, parses them, and
provides convenient access to their components. In addition, it removes all white space and comments.
Routine				Arguments				Returns				Function
-------				---------				-------				--------
Constructor			Input file / stream		--					Opens the input file/stream and gets ready to parse it.

hasMoreCommands		--						boolean				Are there more commands in the input?

advance				--						--					Reads the next command from the input and makes it the current command. 
																Should be called only if hasMoreCommands is true. Initially there is no
																current command.

commandType			--						C_ARITHMETIC,		Returns the type of the current VM command. C_ARITHMETIC is returned 
											C_PUSH, C_POP,		for all the arithmetic commands.
											C_LABEL, C_GOTO, 
											C_IF, C_FUNCTION,
											C_RETURN, C_CALL

arg1				--						string				Returns the first arg. of the current command. In the case of 
																C_ARITHMETIC, the command itself (add, sub, etc.) is returned. Should
																not	be called if the current command is C_RETURN.

arg2				--						int					Returns the second argument of the current command. Should be called
																only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.

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
PLATFORM_FIRST_ARG = 1
VERSION = "0.0.1"

def Main():
	argument = PLATFORM_FIRST_ARG
	queSize = len(sys.argv)
	while argument < queSize:
		qued = sys.argv[argument]
		fileHanlde = open(qued,'r')
		nextVM, vmName = Parse(fileHanlde, argument)
		PrintList(nextVM) # debug
		GenAsm(nextVM, vmName)
		argument += 1

def GenAsm(vmCode, progName):
	outFile = open(progName + ".asm", 'w')
	for instruction in vmCode: outFile.write(instruction + "\n")
	outFile.close()

def GetName(arg):
	delimiter = '.'
	thisArg = sys.argv[int(arg)]
	success = thisArg.find(delimiter)
	if success >= 1:
		return thisArg[0:success]

def PrintList(aList):
	for l in aList:
		print(l)

def PrintUsage(exitCode):
	print ("\nUSAGE: VM.py fileOne.vm [fileTwo.vm ... fileEn.vm]\n")
	sys.exit(exitCode)

def Start():
	if len(sys.argv) <= 1 or sys.argv[1] == "help": 
		PrintUsage(1)
	try: Main()
	except:
		print("FAIL -- unable to parse input: " + str(sys.argv))
		PrintUsage(9)

def Parse(inFile, arg):
	lines = []
	vmName = GetName(arg)
	print(vmName + ": VM image being processed....") # debug
	rawInput = inFile.readlines()
	for directive in rawInput:
		directive = directive.strip().replace("\r", "").replace("\n", "").replace("\t", " ") # make input uniform
		if "//" in directive:
			directive, remark = directive.split("//") # isolate directive
		if len(directive) > 0: # if there's anything left to process
			lines.append("// " + directive)
			try:
				ma = MemoryAccess()
				ma.directive, ma.segment, ma.index =  directive.split(' ')
				try:
					lines.append("// memory access:\t" + ma.directive + "\t" + ma.segment  + "\t" + ma.index)
					asmLines = CodeWriter(ma)
					for eachNewLine in asmLines:
						lines.append(eachNewLine)
				except:
					print("abc fail")
			except:
				lines.append("// directive or flow:" + directive)
	return lines, vmName

class MemoryAccess:
	def __init__(self, a = "", b = "", c = 0):
		self.directive = a
		self.segment = b
		self.index = c
		# segments: static, this, local, argument, that, constant, pointer, temp

	
def CodeWriter(maIn):
	thisASM = []
	if "push" in maIn.directive:
		thisASM.append("PushMe\nPushMe\nPushMe\nPushMe\nPush\n")
	return thisASM

Start()
