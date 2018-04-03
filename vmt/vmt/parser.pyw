#!/usr/bin/env python
#
#	@author	JackDraak
#	
'''	Notes from the Chapter 7 Slides available online at nand2tetris.orc (see: course)

PARSER: Handles the parsing of a single .vm file, and encapsulates access to the input code. It reads VM commands, parses them, and
provides convenient access to their components. In addition, it removes all white space and comments.
Routine				Arguments				Returns				Function
-------				---------				-------				--------
Constructor			Input file / stream		--					Opens the input file/stream and gets ready to parse it.

hasMoreCommands		--						boolean				Are there more commands in the input?

advance				--						--					Reads the next command from the input and makes it the current command. 
																Should be called only if hasMoreCommands is true. Initially there is no
																current command.

commandType			--						C_ARITHMETIC,		Returns the type of the current VM command. C_ARITHMETIC is returned 
											C_PUSH, C_POP,		for all the arithmetic commands.
											C_LABEL, C_GOTO, 
											C_IF, C_FUNCTION,
											C_RETURN, C_CALL

arg1				--						string				Returns the first arg. of the current command. In the case of 
																C_ARITHMETIC, the command itself (add, sub, etc.) is returned. Should
																not	be called if the current command is C_RETURN.

arg2				--						int					Returns the second argument of the current command. Should be called
																only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.
'''
import operator, string, sys

class Parser:
	iStream = []
	iLength = 0
	index = 0

	def Constructor(vmFile):
		rawInput = vmFile.readlines()
		for directive in rawInput:
			if '//' in directive:
				directive, remark = directive.split('//')
			directive = directive.strip().replace('\t' ' ')
			if len(directive) > 0: 
				iStream.append(directive)
				iLength += 1

	def hasMoreCommands():
		return index < iLength

	def advance():
		if hasMoreCommands:
			index += 1

	def commandType():
		args = operator.itemgetter(index)(iStream)

		return "C_ARITHMETIC"

	# Should not be called if the current command is C_RETURN.
	def arg1():
		args = operator.itemgetter(index)(iStream)
		if ' ' in arg1: directive, arg1, arg2 = directive.split(' ')
		return arg1

	# Should be called only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.
	def arg2():
		args = operator.itemgetter(index)(iStream)
		directive, arg1, arg2 = directive.split(' ')
		return arg2

	def commandTable(strIn):
		maths = "C_ARITHMETIC"
		commands = {"add":maths, "sub":maths, "neg":maths, "eq":maths, 
			 "gt":maths, "lt":maths, "and":maths, "or":maths, "not":maths,
			 "pop":"C_POP", "push":"C_PUSH", "label":"C_LABEL", "goto":"C_GOTO",
			 "if-goto":"C_IF", "function":"C_FUNCTION", "call":"C_CALL",
			  "return":"C_RETURN",}
		return jumpCodes.get(strIn, "000")


class MemoryAccess:
	def __init__(self, a = "", b = "", c = 0):
		self.directive = a
		self.segment = b
		self.index = c
		# segments: static, this, local, argument, that, constant, pointer, temp
	