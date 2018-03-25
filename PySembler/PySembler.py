#!/usr/bin/env python
#
#	@author JackDraak
# 
#	PySembler.py : This small application processes one, or a batch,
#	of Hack* assembly files to generate Hack machine language as 
#	ASCII output; based on my C# implementation of March 2018.
#
#	*Hack-compliant as defined by the book...
#
#		The Elements of Computing Systems: 
#		Building a Modern Computer from First Principles (Kindle Edition)
#			by Noam Nisan & Shimon Schocken
#
import re	# Regular Expressions library
import string	# Strings library
import sys	# I/O library
import time
import threading

def Main():
	argument = 1
	Sanity(argument) # TODO: make robust
	while argument < len(sys.argv):
		inFile = open(sys.argv[argument],'r')
		progName = GetName(argument)
		if (progName): print ("Processing: " + progName) # info
		progressIndicator = SpinHaptic()
		progressIndicator.start()
		thisProg = Preparse(inFile)
		symTable = PredefineSymbols() 
		symTable = LinkSymbols(thisProg, symTable)
		symTable = LinkVariables(thisProg, symTable)
		machineCode = EncodeInstructions(thisProg, symTable)
		GenHack(machineCode, progName)
		progressIndicator.stop()
		argument += 1
		if argument == len(sys.argv): break 

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
	if "A" in strIn : destA = "1" 
	if "D" in strIn : destD = "1"
	if "M" in strIn : destM = "1"
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

## Other Methods
def AsDigit(strIn):
	if strIn.isdigit():	return int(strIn)
	return -1

def EncodeInstructions(fullList, symTable):
	outList = []
	instructionCount = 0
	for line in fullList:
		if line[0] == '@':
			address = line[1:]
			addressAsInteger = AsDigit(address)
			if addressAsInteger >= 0: address = addressAsInteger
			else:
				inTable = False
				for key in symTable:
					if key == address:
						address = symTable[key]
						inTable = True
						continue
			encodedAddress = str(bin(address))
			encodedAddress = encodedAddress[2:]
			zip = "0000000000000000"
			diff = len(encodedAddress)
			if diff < len(zip):
				offset = len(zip) - diff
				encodedAddress = zip[0:offset] + encodedAddress
			outList.append(encodedAddress)
			instructionCount += 1
		elif not line[0] == '(':
			dcCode = line
			jumpInstruction = ';'
			success = line.find(jumpInstruction)
			if success >= 0: 
				dcCode = line[0:success]
				jCode = EncodeJump(line[success + 1:])
			else: jCode = "000"
			assignment = '='
			success = dcCode.find(assignment)
			if success > 0:
				cCode = EncodeComp(dcCode[success + 1:])
				dCode = EncodeDest(dcCode[0:success])
			else:
				cCode = EncodeComp(dcCode[success + 1:])
				dCode = "000"	
			outList.append("111" + cCode + dCode + jCode)
			instructionCount += 1
	return outList

def GenHack(machineCode, progName):
	outFile = open(progName + ".hack", 'w')
	for instruction in machineCode: outFile.write(instruction + "\n")
	outFile.close()

def GetName(arg):
	delimiter = '.'
	thisArg = sys.argv[arg]
	success = thisArg.find(delimiter)
	if success >= 1: 
		progName = thisArg[0:success]
		return progName

def LinkSymbols(thisList, symTable):
	symbolOffset = 0
	isSymbol = False
	for line in thisList:
		if line[0] == '(' and line[-1] == ')':
			isSymbol = True
			inTable = False
			for key in symTable:
				if key == line:
					inTable = True
					continue
			if not inTable: symTable[line[1:-1]] = symbolOffset
		if not isSymbol: symbolOffset += 1
		isSymbol = False
	return symTable

def LinkVariables(thisList, symTable):
	instructionOffset = 0
	nextOpenRegister = 16
	for line in thisList:
		if line[0] == '@':
			address = line[1:]
			addressAsInteger = AsDigit(address)
			if addressAsInteger < 0:
				inTable = False
				for key in symTable:
					if key == address:
						address = symTable[key]
						inTable = True
						continue							
				if not inTable: 
					symTable[address] = nextOpenRegister
					nextOpenRegister += 1	
				if not line[0] == '(': instructionOffset += 1
	return symTable

def PredefineSymbols():
	symTable = dict()
	symTable["SP"] = 0
	symTable["LCL"] = 1
	symTable["ARG"] = 2
	symTable["THIS"] = 3
	symTable["THAT"] = 4
	symTable["SCREEN"] = 16384
	symTable["KBD"] = 24576
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

def Preparse(inFile):
	thisList = []
	rawInput = inFile.readlines()
	for item in rawInput:
		whitespace = ' \t\r\n'
		remark = '/'
		success = item.find(remark)
		if success >= 0: item = item[0:success]
		if item:
			item = item.translate({ord(thisChar): None for thisChar in whitespace})
			if len(item) > 0: thisList.append(item)
	return thisList

def Sanity(arg):
	if len(sys.argv) <= arg or sys.argv[arg] == "help": Usage()

def StripComments(strIn):
	delimiter = '/'
	success = strIn.find(delimiter)
	if success >= 0: return strIn[0:success]

def Usage():
	print ("\nUSAGE: PySembler.py fileOne.asm [fileTwo.asm ... fileEn.asm]\n")

class SpinHaptic:
	busy = False
	delay = 0.1
	@staticmethod
	def haptic_cursor():
		while 1: 
			for cursor in '\\-/|': yield cursor

	def __init__(self, delay=None):
		self.spinner = self.haptic_cursor()
		if delay and float(delay): self.delay = delay

	def spinner_task(self):
		while self.busy:
			sys.stdout.write(next(self.spinner))
			sys.stdout.flush()
			time.sleep(self.delay)
			sys.stdout.write('\b')
			sys.stdout.flush()

	def start(self):
		self.busy = True
		threading.Thread(target=self.spinner_task).start()

	def stop(self):
		self.busy = False
		time.sleep(self.delay)

Main()
