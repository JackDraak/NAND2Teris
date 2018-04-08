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
'''
class SMSStore(object):
    def __init__(self):
        self.store = []
        self.message_count = 0

    def add_new_arrival(self,number,time,text):
        self.store.append(("From: "+number, "Recieved: "+time,"Msg: "+text))
        self.message_count += 1

    def delete(self, i):
        if i >= len(store):
            raise IndexError
        else:
            del self.store[i]
            self.message_count -= 1

sms_store = SMSStore()
sms_store.add_new_arrival("1234", "now", "lorem ipsum")
try:
    sms_store.delete(20)
except IndexError:
    print("Index does not exist")

print sms_store.store

# multiple separate stores
sms_store2 = SMSStore()
sms_store2.add_new_arrival("4321", "then", "lorem ipsum")
print sms_store2.store
'''
import string, sys, operator#, parser, codewriter
VERSION = "0.0.1"

class CodeWriter(object):
	"""VM -> Assembly encoder"""
	def __init__(self):
		self.oStream = []
		self.oFile = ""
		self.fileName = ""

	def setFilename(self, strIn):
		self.fileName = strIn

	def Constructor(self):
		self.oFile = open(self.fileName + "asm", 'w')

	def Close(self):
		self.oFile.close()

	def writePushPop(self, command, segment, index):
		if command is "C_PUSH":
			self.oFile.write("@" + segment + "." + str(index))
		# TODO: more stuff here -- POP

	def writeArithmetic(self, directive):
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
		self.oFile.write(commands.get(directive, None))
		#oFile.write(c_add)

class Parser(object):
	"""Parser of VM code"""
	def __init__(self):
		self.iLength = 0
		self.index = -1
		self.iStream = []

	def Constructor(self, vmFile):
		self.iLength = 0
		self.index = -1
		self.iStream = []
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

	def hasMoreCommands(self):
		return self.index < self.iLength

	def advance(self):
		if self.hasMoreCommands(self):
			self.index += 1
			print(self.index)

	def commandType(self):
		directive = operator.itemgetter(self.index)(self.iStream)
		if ' ' in directive: 
			try: directive, arg1, arg2 = directive.split(' ')
			except: print(".commandType() exception")
		return self.commandTable(directive)

	# Should not be called if the current command is C_RETURN.
	def arg1(self):
		thisType = self.commandType()
		if thisType is "C_RETURN":
			print("unexpected error #1 @" + self.index)
			sys.exit(1)
		thisArg = operator.itemgetter(self.index)(self.iStream)
		if ' ' in thisArg: 
			try: directive, ar1, ar2 = thisArg.split(' ')
			except: print(".arg1() exception @" + self.index)
		return ar1

	# Should be called only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.
	def arg2(self):
		thisType = self.commandType()
		if thisType is "C_PUSH" or "C_POP" or "C_FUNCTION" or "C_CALL":
			thisArg = operator.itemgetter(self.index)(self.iStream)
			try: directive, ar1, ar2 = thisArg.split(' ')
			except: print(".arg2() exception @" + self.index)
			return ar2
		print("arg2 contracall @" + self.index)
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

	w.setFilename(w, name)
	w.Constructor(w)
	#w.writeArithmetic("add")

	r.Constructor(r, qued)
	if r.hasMoreCommands(r):
		print(r.commandType(r))
		r.advance(r)

else:
	print("Usage:")