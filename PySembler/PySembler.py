#!/usr/bin/env python

# @author jackdraak
#
# 
import sys

## Variables
args = sys.argv
args_L = len(args)
argument = 1
symTable = dict()

## Functions
def Sanity():
	if args_L <= argument or args[argument] == "help": 
		Usage()

def Usage():
	print ("\nUSAGE: PySembler fileOne.asm [fileTwo.asm ... fileEn.asm]\n")

def GetName():
	delimiter = '.'
	thisArg = args[argument]
	success = thisArg.find(delimiter)
	if success >= 1: 
		progName = thisArg[0:success]
		return progName

## Main.c, as it were
Sanity()

	## main program loop
while argument < args_L :
#if argument < args_L : 
	progName = GetName()
	if (progName): print (progName)
	argument += 1

## instructionList, debugLog, symboLTable,

## Encoding
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
	destA = "0"
	destD = "0"
	destM = "0"
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

def PredefineSymbols(symTable):
    symTable.Add("SP", 0)
    symTable.Add("LCL", 1)
    symTable.Add("ARG", 2)
    symTable.Add("THIS", 3)
    symTable.Add("THAT", 4)
    symTable.Add("SCREEN", 16384)
    symTable.Add("KBD", 24576)
    symTable.Add("R0", 0)
    symTable.Add("R1", 1)
    symTable.Add("R2", 2)
    symTable.Add("R3", 3)
    symTable.Add("R4", 4)
    symTable.Add("R5", 5)
    symTable.Add("R6", 6)
    symTable.Add("R7", 7)
    symTable.Add("R8", 8)
    symTable.Add("R9", 9)
    symTable.Add("R10", 10)
    symTable.Add("R11", 11)
    symTable.Add("R12", 12)
    symTable.Add("R13", 13)
    symTable.Add("R14", 14)
    symTable.Add("R15", 15)
    return symTable
