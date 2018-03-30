#!/usr/bin/env python
#
#	@author	JackDraak
#	@date	26 March 2018
# 
#	PySembler.py : This small application processes one, or a batch,
#	of Hack* assembly files, generating Hack machine language as 
#	ASCII output; based on my C# implementation of March 2018.
#
#	*Hack-compliant as defined by the book --
#		The Elements of Computing Systems: 
#		Building a Modern Computer from First Principles
#		by Noam Nisan & Shimon Schocken
#
import platform, string, sys, time, threading
HAPTIC_INTERVAL = 0.6
PLATFORM_BASE_REGISTER = 16
PLATFORM_BIT_WIDTH = 16
PLATFORM_FIRST_ARG = 1
VERSION = "0.9.3"

def Main():
	# Initialize que.
	argument = PLATFORM_FIRST_ARG
	queSize = len(sys.argv)

	# Encode batch member.
	while argument < queSize:
		# Select (next) assembly from the que.
		qued = sys.argv[argument]

		# Benchmark setup.
		benchmark = time.clock()

		# ID qued ROM.
		romName = GetName(argument)

		# User-feedback.
		print(romName + " encoding...") # info
		progressIndicator = HapticCursor()
		progressIndicator.start()

		# Parse input (remove comments and whitespace).
		thisRom = Preparse(open(qued,'r'))

		# Pass one (link labels in symTable).
		symTable = InitSymbols()
		symTable = LinkLabels(thisRom, symTable)

		# Pass two (link variables in symTable & encode directives).
		symTable = LinkVariables(thisRom, symTable)
		machineCode = EncodeInstructions(thisRom, symTable)

		# Output machine-language file.
		GenHack(machineCode, romName)
		progressIndicator.stop()

		# Benchmarking:
		benchTime = str(time.clock() - benchmark)[0:6]
		benchOut = "{} in: {}, with: {}, on: {}({}), at: {}" .format(romName, benchTime, VERSION, platform.system(), platform.release(), str(time.asctime()))
		try:
			print(romName + " encoded in: " + benchTime) # info
			benchFile = open(romName + ".bench", 'a')
			benchFile.write(str(benchTime + "\n")) # NB fix this
			benchFile.close()
		except: print(romName + " WARNING: unable to record benchmark.")
		argument += 1

def AsInteger(strIn):
	if strIn.isdigit():	return int(strIn)
	return -1

def EncodeInstructions(thisList, symTable):
	instructionList = []
	romAddress = 0
	zip = "0" * PLATFORM_BIT_WIDTH
	for line in thisList:
		if line[0] == '@':
			address = line[1:]
			addressAsInteger = AsInteger(address)
			if addressAsInteger >= 0: address = addressAsInteger
			else: address = symTable[address]
			encodedAddress = str(bin(address))[2:]
			addressBits = len(encodedAddress)
			if addressBits < len(zip):
				offset = len(zip) - addressBits
				encodedAddress = zip[0:offset] + encodedAddress
			instructionList.append(encodedAddress)
			romAddress += 1
		elif not line[0] == '(':
			jCode = "000"
			dcCode = line
			jump =  line.find(';')
			if jump > 0:
				dcCode, j = line.split(';')
				jCode = EncodeJump(j)
				print(j + " " + jCode)

			assignment = dcCode.find('=')
			if assignment > 0:
				cCode = EncodeComp(dcCode[assignment + 1:])
				dCode = EncodeDest(dcCode[0:assignment])
			else:
				cCode = EncodeComp(dcCode[assignment + 1:])
				dCode = "000"
			instructionList.append("111" + cCode + dCode + jCode)
			romAddress += 1
	return instructionList

def GenHack(machineCode, progName):
	outFile = open(progName + ".hack", 'w')
	for instruction in machineCode: outFile.write(instruction + "\n")
	outFile.close()

def GetName(arg):
	delimiter = '.'
	thisArg = sys.argv[arg]
	success = thisArg.find(delimiter)
	if success >= 1:
		return thisArg[0:success]

def LinkLabels(thisList, symTable):
	romAddress = 0
	for line in thisList:
		isSymbol = False
		if line[0] == '(':
			isSymbol = True
			label = line[1:-1]
			if not label in symTable.keys(): symTable[label] = romAddress
		if not isSymbol: romAddress += 1
	return symTable

def LinkVariables(thisList, symTable):
	nextOpenRegister = PLATFORM_BASE_REGISTER
	for line in thisList:
		if line[0] == '@':
			address = line[1:]
			addressAsInteger = AsInteger(address)
			if addressAsInteger < 0:
				if not address in symTable.keys():
					symTable[address] = nextOpenRegister
					nextOpenRegister += 1
	return symTable

def Preparse(inFile):
	directiveList = []
	rawInput = inFile.readlines()
	for directive in rawInput:
		if '//' in directive:
			directive, remark = directive.split('//')
		directive = directive.strip().replace('\t', '')
		directive = directive.replace(' ', '')
		if len(directive) > 0: directiveList.append(directive)
	return directiveList

def PrintUsage(exitCode):
	print ("\nUSAGE: PySembler.py fileOne.asm [fileTwo.asm ... fileEn.asm]\n")
	sys.exit(exitCode)

def Start():
	if len(sys.argv) <= 1 or sys.argv[1] == "help": 
		PrintUsage(1)
	try: Main()
	except:
		print("FAIL -- unable to parse input: " + str(sys.argv))
		PrintUsage(9)

class HapticCursor:
	interval = HAPTIC_INTERVAL
	active = False
	@staticmethod
	def current_cursor():
		while True:
			for cursor in '\\-/|': yield cursor

	def __init__(self, interval=None):
		self.cursor = self.current_cursor()
		if interval and float(interval): self.interval = float(interval)

	def haptic_task(self):
		while self.active:
			sys.stdout.write(next(self.cursor))
			sys.stdout.flush()
			time.sleep(self.interval)
			sys.stdout.write('\b')
			sys.stdout.flush()

	def start(self):
		self.active = True
		threading.Thread(target=self.haptic_task).start()

	def stop(self):
		self.active = False
		time.sleep(self.interval)

## DATA TABLES
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

def InitSymbols():
	table =  {"SP":0, "LCL":1, "ARG":2, "THIS":3, "THAT":4, "SCREEN":16384,
		  "KBD":24576, "R0":0, "R1":1, "R2":2, "R3":3, "R4":4, "R5":5,
		  "R6":6, "R7":7, "R8":8, "R9":9, "R10":10, "R11":11, "R12":12,
		  "R13":13, "R14":14, "R15":15 }
	return table

Start()
