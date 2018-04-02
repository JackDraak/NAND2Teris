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
