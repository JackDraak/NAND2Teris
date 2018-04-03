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

import string, sys, parser, codewriter
VERSION = "0.0.1"

w = codewriter
r = parser

w.CodeWriter.setFilename("fileName_test")
w.CodeWriter.Constructor("oFile_test")
w.CodeWriter.writeArithmetic("C_PUSH")

#r.Parser.Constructor(vmFile)