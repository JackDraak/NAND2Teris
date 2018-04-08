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

import string, sys#, parser, codewriter
VERSION = "0.0.1"

class CodeWriter:
	"""VM -> Assembly encoder"""
	global fileName
	global oFile
	global oStream

	def __init__(self, fileName, oFile, oStream):
		self.fileName = fileName
		self.oFile = oFile
		self.oStream = oStream

	def setFilename(strIn):
		global fileName
		fileName = strIn

	def Constructor(fileName):
		global oFile
		oFile = open(fileName, 'w')

	def Close():
		global oFile
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
		oFile.write(commands.get(directive, None))
		#oFile.write(c_add)

class Parser:
	"""Parser of VM code"""
	def __init__(self, iStream, iLength, index):
		self.iStream = iStream
		self.iLength = iLength
		self.index = index
	
	#global iStream
	#global iLength
	#global index
	self.iStream = []
	self.iLength = 0
	self.index = -1

	#@staticmethod
	def Constructor(vmFile):
		#global iLength
		fh = open(vmFile, 'r')
		rawInput = fh.readlines()
		for directive in rawInput:
			if '//' in directive:
				directive, remark = directive.split('//')
			directive = directive.strip()
			if len(directive) > 0: 
				directive = directive.replace('\t', ' ')
				self.iStream.append(directive)
				self.iLength += 1

	def hasMoreCommands():
		global index
		global iLength
		return index < iLength

	def advance():
		global index

		if Parser.hasMoreCommands():
			index += 1

	def commandType():
		directive = operator.itemgetter(index)(iStream)
		if ' ' in args: 
			try: directive, arg1, arg2 = directive.split(' ')
			except: print(".commandType() exception")
		return commandTable(directive)

	# Should not be called if the current command is C_RETURN.
	def arg1():
		thisType = commandType()
		if thisType is "C_RETURN":
			print("unexpected error #1 @" + index)
			sys.exit(1)
		arg1 = operator.itemgetter(index)(iStream)
		if ' ' in arg1: 
			try: directive, arg1, arg2 = arg1.split(' ')
			except: print(".arg1() exception @" + index)
		return arg1

	# Should be called only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.
	def arg2():
		thisType = commandType()
		if thisType is "C_PUSH" or "C_POP" or "C_FUNCTION" or "C_CALL":
			arg2 = operator.itemgetter(index)(iStream)
			try: directive, arg1, arg2 = arg2.split(' ')
			except: print(".arg2() exception @" + index)
			return arg2
		print("arg2 contracall @" + index)
		sys.exit(2)

	def commandTable(strIn):
		m = strIn
		commands = {"add":m, "sub":m, "neg":m, "eq":m, "gt":m, "lt":m, "and":m, "or":m, "not":m,
			 "pop":"C_POP", "push":"C_PUSH", "label":"C_LABEL", "goto":"C_GOTO",
			 "if-goto":"C_IF", "function":"C_FUNCTION", "call":"C_CALL",
			  "return":"C_RETURN",}
		return commands.get(strIn, None)

global cue
cue = 1
while cue <= len(sys.argv):
	qued = sys.argv[cue]
	name = "name_def"
	extenstion = "x"
	try:
		name, extension = qued.split('.')
	except:
		print("argument error")

	w = CodeWriter
	r = Parser

	w.setFilename(name)
	w.Constructor(fileName + ".asm")
	#w.writeArithmetic("add")

	r.Constructor(qued)
	if r.hasMoreCommands():
		print(r.commandType)
		r.advance()

else:
	print("Usage:")