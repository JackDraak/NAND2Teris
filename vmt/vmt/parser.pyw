#!/usr/bin/env python
#
#	@author	JackDraak
#	
import operator, string, sys

class Parser:
	iStream = []
	iLength = 0
	index = -1

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
		directive = operator.itemgetter(index)(iStream)
		if ' ' in args: 
			try: directive, arg1, arg2 = directive.split(' ')
			except: print(".commandType() exception")
		return commandTable(directive)

	# Should not be called if the current command is C_RETURN.
	def arg1():
		thisType = commandType()
		if thisType is "C_RETURN":
			print("unexpected error #1 @" + index)
			sys.exit(1)
		arg1 = operator.itemgetter(index)(iStream)
		if ' ' in arg1: 
			try: directive, arg1, arg2 = arg1.split(' ')
			except: print(".arg1() exception @" + index)
		return arg1

	# Should be called only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.
	def arg2():
		thisType = commandType()
		if thisType is "C_PUSH" or "C_POP" or "C_FUNCTION" or "C_CALL":
			arg2 = operator.itemgetter(index)(iStream)
			try: directive, arg1, arg2 = arg2.split(' ')
			except: print(".arg2() exception @" + index)
			return arg2
		print("arg2 contracall @" + index)
		sys.exit(2)

	def commandTable(strIn):
		m = strIn
		commands = {"add":m, "sub":m, "neg":m, "eq":m, "gt":m, "lt":m, "and":m, "or":m, "not":m,
			 "pop":"C_POP", "push":"C_PUSH", "label":"C_LABEL", "goto":"C_GOTO",
			 "if-goto":"C_IF", "function":"C_FUNCTION", "call":"C_CALL",
			  "return":"C_RETURN",}
		return commands.get(strIn, None)
	