#!/usr/bin/env python
#
#	@author JackDraak
#	I'm ostensibly using Python 3 here, fwiw.
# 
#	PySembler.py : This small application processes one, or a
#	batch, of Hack* assembly files to generate Hack machine
#	language as ASCII output.
#
import string
import sys

## Variables
argument = 1

## Encoding Functions
def EncodeComp(strIn):
    if strIn == "0":     return "0101010"
    elif strIn == "1":   return "0111111"
    elif strIn == "-1":  return "0111010"
    elif strIn == "D":   return "0001100"
    elif strIn == "A":   return "0110000"
    elif strIn == "!D":  return "0001101"
    elif strIn == "!A":  return "0110001"
    elif strIn == "-D":  return "0001111"
    elif strIn == "-A":  return "0110011"
    elif strIn == "D+1": return "0011111"
    elif strIn == "A+1": return "0110111"
    elif strIn == "D-1": return "0001110"
    elif strIn == "A-1": return "0110010"
    elif strIn == "D+A": return "0000010"
    elif strIn == "A+D": return "0000010"
    elif strIn == "D-A": return "0010011"
    elif strIn == "A-D": return "0000111"
    elif strIn == "D&A": return "0000000"
    elif strIn == "D|A": return "0010101"
    elif strIn == "A&D": return "0000000"
    elif strIn == "A|D": return "0010101"
    elif strIn == "M":   return "1110000"
    elif strIn == "!M":  return "1110001"
    elif strIn == "-M":  return "1110011"
    elif strIn == "M+1": return "1110111"
    elif strIn == "M-1": return "1110010"
    elif strIn == "D+M": return "1000010"
    elif strIn == "M+D": return "1000010"
    elif strIn == "D-M": return "1010011"
    elif strIn == "M-D": return "1000111"
    elif strIn == "D&M": return "1000000"
    elif strIn == "D|M": return "1010101"
    elif strIn == "M&D": return "1000000"
    elif strIn == "M|D": return "1010101"
    return "0000000"

def EncodeDest(strIn):
	destA = destD = destM = "0"
	if strIn.Contains("A"): destA = "1" 
	if strIn.Contains("D"): destD = "1"
	if strIn.Contains("M"): destM = "1"
	return str(destA + destD + destM)

def EncodeJump(strIn):
    if strIn == "JGT":   return "001"
    elif strIn == "JEQ": return "010"
    elif strIn == "JGE": return "011"
    elif strIn == "JLT": return "100"
    elif strIn == "JNE": return "101"
    elif strIn == "JLE": return "110"
    elif strIn == "JMP": return "111"
    return "000"

## Other Functions
def CleanTable(symTable):
	symTable.clear()

def DebugSymbols():
	for key in symTable:
		print("Key: " + str(key) + ", Value: " + str(symTable[key]))

def GetName():
	delimiter = '.'
	thisArg = sys.argv[argument]
	success = thisArg.find(delimiter)
	if success >= 1: 
		progName = thisArg[0:success]
		return progName

def PredefineSymbols():
	symTable = dict()
	symTable["SP"] = 0
	symTable["LCL"] = 1
	symTable["ARG"] = 2
	symTable["THIS"] = 3
	symTable["THAT"] = 4
	symTable["SCREEN"] = 24576
	symTable["KBD"] = 16384
	symTable["R0"] = 0
	symTable["R1"] = 1
	symTable["R2"] = 2
	symTable["R3"] = 3
	symTable["R4"] = 4
	symTable["R5"] = 5
	symTable["R6"] = 6
	symTable["R7"] = 7
	symTable["R8"] = 8
	symTable["R9"] = 9
	symTable["R10"] = 10
	symTable["R11"] = 11
	symTable["R12"] = 12
	symTable["R13"] = 13
	symTable["R14"] = 14
	symTable["R15"] = 15
	return symTable

def Sanity():
	if len(sys.argv) <= argument or sys.argv[argument] == "help": 
		Usage()

def StripComments(strIn):
	delimiter = '/'
	success = strIn.find(delimiter)
	if success >= 1: 
		strOut = strIn[0:success]
		return strOut

def Usage():
	print ("\nUSAGE: PySembler fileOne.asm [fileTwo.asm ... fileEn.asm]\n")

## Program entry-point
# initialization
Sanity()
symTable = dict()
nextOpenRegister = 16

# main program loop
while argument < len(sys.argv):
	fullList = []
	truncList = []
	symTable.clear()
	symTable = PredefineSymbols() 
	progName = GetName()
	if (progName): print ("Processing: " + progName) # debug

	# read input
	line = 0
	inputFile = open(sys.argv[argument],'r')
	rawInput = inputFile.readlines()
	# remove comments
	for item in rawInput:
		delimiter = '/'
		success = item.find(delimiter)
		if success >= 0: 
			item = item[0:success] 
	# remove whitespace (space, tab, return, linefeed)
		if item:
			item = item.translate({ord(c): None for c in ' \t\r\n'})
			if len(item) > 0:
				fullList.append(item)
		line += 1

	print (fullList)

	"""
	unicode_line = unicode_line.translate({ord(c): None for c in '!@#$'})
	t = [] # implicit instantiation
	t = t.append(1)
	' hello  apple'.translate(None, ' \n\t\r')

	// On first-pass, assign requisite symbol table entries. 
	AssignSymbols(instructionList, debugLog, symbolTable);

	// Second-pass, link symbol table with variables.
	// TODO: deal with anaochronisitic return of nextOpenRegister... at this point, we no longer need it
	nextOpenRegister = LinkVariables(instructionList, debugLog, nextOpenRegister, symbolTable);

	// "Third-pass", Parse variables into absolute addresses.
	ParseVariables(instructionList, symbolTable);

	// Output pre-encoded but fully parsed instructions, again for humans.
	DebugParsed($"_{thisProgram}.postparse", instructionList, debugLog);

	// Parse and encode instructionList for machines* which are Hack-compliant. 
	// TODO: deal with anaochronisitic use of nextOpenRegister... at this point, we no longer need it [thanks to "pass three"]
	List<string> encodedInstructions = DoEncode(instructionList, debugLog, ref nextOpenRegister, symbolTable);
	"""
	# end of main loop
	argument += 1
	if argument == len(sys.argv): break 
