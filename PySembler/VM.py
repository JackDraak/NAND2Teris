#!/usr/bin/env python
#
#	@author	JackDraak
#	VM
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
	#print(str(argument) + " main " + str(queSize))
	while argument < queSize:
		#print("while")
		qued = sys.argv[argument]
		fileHanlde = open(qued,'r')
		nextVM = Parse(fileHanlde, argument)
		#print(nextVM)
		argument += 1

def GetName(arg):
	#print(str(arg) + " arg")
	delimiter = '.'
	thisArg = sys.argv[int(arg)]
	success = thisArg.find(delimiter)
	if success >= 1:
		return thisArg[0:success]

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
	#print(str(arg) + "parse")
	lines = []
	vmName = GetName(arg)
	print(vmName)
	rawInput = inFile.readlines()
	for vmLine in rawInput:
		try:
			aPart, bPart, cPart = vmLine.split(' ')
			try:
				print("memory access:\t" + aPart + "\t" + bPart  + "\t" + cPart)
			except:
				print("abc fail")
		except:
			print("directive or flow:" + vmLine)
	return lines

Start()
