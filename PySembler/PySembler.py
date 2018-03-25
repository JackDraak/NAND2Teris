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
import string, sys, time, threading
PLATFORM_BIT_WIDTH = 16
PLATFORM_BASE_REGISTER = 16

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
	compCodes = {"0":"0101010", "1":"0111111", "-1":"0111010", "D":"0001100", 
			"A":"0110000", "!D":"0001101", "!A":"0110001", "-D":"0001111",
			"-A":"0110011", "D+1":"0011111", "A+1":"0110111", "D-1":"0001110",
			"A-1":"0110010", "D+A":"0000010", "A+D":"0000010", "D-A":"0010011", 
			"A-D":"0000111", "D&A":"0000000", "D|A":"0010101", "A&D":"0000000",
			"A|D":"0010101", "M":"1110000", "!M":"1110001", "-M":"1110011",
			"M+1":"1110111", "M-1":"1110010", "D+M":"1000010", "M+D":"1000010",
			"D-M":"1010011", "M-D":"1000111", "D&M":"1000000", "D|M":"1010101",
			"M&D":"1000000", "M|D":"1010101"}
	return compCodes.get(strIn, "0000000")

def EncodeDest(strIn):
	destA = destD = destM = "0"
	if "A" in strIn: destA = "1" 
	if "D" in strIn: destD = "1"
	if "M" in strIn: destM = "1"
	return str(destA + destD + destM)

def EncodeJump(strIn):
	jumpCodes = {"JGT":"001", "JEQ":"010",  "JGE":"011",  "JLT":"100",  
		  "JNE":"101",  "JLE":"110",  "JMP":"111"}
	return jumpCodes.get(strIn, "000")

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
			zip = "0" * PLATFORM_BIT_WIDTH
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
			# TODO fix how labels are stored, as-is, this could cause erros w/ duplicate labels, no?
			if not line in symTable.keys(): symTable[line[1:-1]] = symbolOffset 
		if not isSymbol: symbolOffset += 1
		isSymbol = False
	return symTable

def LinkVariables(thisList, symTable):
	instructionOffset = 0
	nextOpenRegister = PLATFORM_BASE_REGISTER
	for line in thisList:
		if line[0] == '@':
			address = line[1:]
			addressAsInteger = AsDigit(address)
			if addressAsInteger < 0:						
				if not address in symTable.keys(): 
					symTable[address] = nextOpenRegister
					nextOpenRegister += 1	
				if not line[0] == '(': instructionOffset += 1
	return symTable

def PredefineSymbols():
	table =  {"SP":0, "LCL":1, "ARG":2, "THIS":3, "THAT":4, "SCREEN":16384, 
		  "KBD":24576, "R0":0, "R1":1, "R2":2, "R3":3, "R4":4, "R5":5, 
		  "R6":6, "R7":7, "R8":8, "R9":9, "R10":10, "R11":11, "R12":12, 
		  "R13":13, "R14":14, "R15":15 }
	return table

def Preparse(inFile):
	thisList = []
	rawInput = inFile.readlines()
	for item in rawInput:
		voids = ' \t\r\n'
		remark = '//'
		success = item.find(remark)
		if success >= 0: item = item[0:success]
		if item:
			item = item.translate({ord(thisChar): None for thisChar in voids})
			if len(item) > 0: thisList.append(item)
	return thisList

def Sanity(arg):
	if len(sys.argv) <= arg or sys.argv[arg] == "help": Usage()

def Usage():
	print ("\nUSAGE: PySembler.py fileOne.asm [fileTwo.asm ... fileEn.asm]\n")

class SpinHaptic:
	busy = False
	delay = 0.03
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
