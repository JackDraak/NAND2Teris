#!/usr/bin/env python
#
#	@author JackDraak
# 
#	PySembler.py : This small application processes one, or a batch,
#	of Hack* assembly files to generate Hack machine language as 
#	ASCII output.
#
#	*Hack-compliant as defined by the book...
#
#				The Elements of Computing Systems: 
#	Building a Modern Computer from First Principles (Kindle Edition)
#				 by Noam Nisan & Shimon Schocken
#
import re
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
def AsDigit(strIn):
	strAsInteger = int()
	if strIn.isdigit():
		return int(strIn)
	try: 
		strAsInteger = int(strIn)
	except:
		return -1
	return int(strAsInteger)

def CleanTable(symTable):
	symTable.clear()

def DebugSymbols():
	for key in symTable:
		print("Key: " + str(key) + ",\tValue: " + str(symTable[key]))

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

## Program entry-point:
# Initialization.
Sanity()
symTable = dict()

# Main program loop.
while argument < len(sys.argv):
	fullList = []
	symTable.clear()
	symTable = PredefineSymbols() 
	progName = GetName()
	nextOpenRegister = 16
	if (progName): print ("Processing: " + progName) # debug

	# Read input.
	inputFile = open(sys.argv[argument],'r')
	rawInput = inputFile.readlines()
	# Remove comments.
	for item in rawInput:
		delimiter = '/'
		success = item.find(delimiter)
		if success >= 0: 
			item = item[0:success] 
	# Remove whitespace (space, tab, return, linefeed).
		if item:
			item = item.translate({ord(thisChar): None for thisChar in ' \t\r\n'})
			if len(item) > 0:
				fullList.append(item)

	# Assign symbols to symTable.
	symbolOffset = 0
	isSymbol = False
	for line in fullList:
		# If line is symbol...
		if line[0] == '(' and line[-1] == ')':
			isSymbol = True
			inTable = False
			# Try to find symbol in table...
			for key in symTable:
				if key == line:
					inTable = True
					continue
			# Add new symbols.
			if not inTable:
				symTable[line[1:-1]] = symbolOffset
		# Manage offset.
		if not isSymbol: 
			symbolOffset += 1
		isSymbol = False

	# Link variables into symbol table.
	instructionOffset = 0
	for line in fullList:
		if line[0] == '@':
			address = line[1:]
			addressAsInteger = AsDigit(address)
			if addressAsInteger >= 0:
				line = "@" + str(addressAsInteger) # TODO fix this?
			else:
				inTable = False
				for key in symTable:
					if key == address:
						address = symTable[key]
						print("symbol in table " + str(address) + " : " + str(key)) # debug
						inTable = True
						line = "@" + str(address) # TODO Why doesn't this work?
						continue	
						
				if not inTable: 
					print("symbol not in table " + str(address) + " : " + str(nextOpenRegister)) # debug
					symTable[address] = nextOpenRegister
					line = "@" + str(nextOpenRegister) # TODO ditto...
					nextOpenRegister += 1

				if not line[0] == '(':
					instructionOffset += 1

	# Encoding section:
	outList = []
	zip = "0000000000000000"
	for line in fullList:
		# Decode addresses.
		if line[0] == '@':
			address = line[1:]
			#print("decode: " + address + " from " + line) # debug
			addressAsInteger = AsDigit(address)
			if addressAsInteger >= 0:
				address = addressAsInteger
				#print("address resolved " + str(line) + " : " + str(address)) # debug
			else:
				inTable = False
				for key in symTable:
					if key == address:
						address = symTable[key]
						#print("symbol resolved " + str(address) + " : " + str(key)) # debug
						inTable = True
						continue

			# Encode addresses.
			encodedAddress = str(bin(address))
			encodedAddress = encodedAddress[2:]
			diff = len(encodedAddress)
			# Pad address to 16-bits.
			if diff < 16:
				offset = 16 - diff
				encodedAddress = zip[0:offset] + encodedAddress

			outList.append(encodedAddress)
			print("encode @ " + str(address) + " as\t" + str(encodedAddress)) # debug

		# Encode instructions.
		elif not line[0] == '(':
			# Encode jump bits.
			#print("C-code: " + line) # debug
			delimiter = ';'
			success = line.find(delimiter)
			if success >= 0: 
				jCode = line[success + 1:]
				#print ("preJCODE: " + jCode) # debug
				jCode = EncodeJump(jCode)
			else:
				jCode = "000"
			#print ("postJCODE: " + jCode) # debug

			# Encode dest bits.

			# Encode comp bits.

			# Add composite instruction to outList

	#DebugSymbols() # debug
	#print (fullList) # debug

	# End of main loop.
	argument += 1
	if argument == len(sys.argv): break 
