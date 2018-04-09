#!/usr/bin/env python
#
#  @author	JackDraak
#  VMTranslator. Suggested: Parser, CodeWriter, Main
#
#  Phase 1: 
#     Arithmetic: add, sub (x-y), neg (-y), eq, gt (x>y), lt (x<y), and, or, not ('y) 
#     Memory Access: push, pop
#     Program Flow: label, goto, if-goto
#
#  Example:
#     push constant 6
#     push constant 5
#     add
#
#  Additional Notes:
#     The Stack: mapped to RAM[256..2047], pointer stored in RAM[SP]
#     Static Segments: mapped to RAM[16..255]
#     local, argument, this, that: RAM[2048..+], 
#        pointer (to base) stored in RAM[]:, LCL, ARG, THIS, THAT
#        therefore argument.7 is accessed as RAM[ARG + 7]
#
import string, sys, operator
VERSION = "0.0.11"

class CodeWriter(object):
   """VM -> Assembly encoder"""
   def __init__(self):
      self.oStream = []
      self.oFile = ""
      self.fileName = ""

   def setFilename(self, strIn):
      self.fileName = strIn

   def Constructor(self):
      self.oFile = open(self.fileName + ".asm", 'w')

   def Close(self):
      self.oFile.close()

   def writePushPop(self, command, segment, index):
      if command is "C_PUSH":
         self.oFile.write("@" + segment + "." + str(index))
		# TODO: more stuff here -- POP

   def writeArithmetic(self, directive):
      c_add = """// writeArithmetic(add)
      @SP      // set A to @StackPointer
      A=M      // Fetch SP pointer reference
      M=A-1    // Decrement @SP
      D=M      // Fetch top of stack (Y)
      @SP      // set A to @StackPointer
      M=M+D    // X+Y, left in top of stack (@SP)
      """
      c_sub = """// sub
      @SP      // set A to @StackPointer
      A=M      // Fetch SP pointer reference
      M=A-1    // Decrement @SP
      D=M      // Fetch top of stack (Y)
      @SP      // set A to @StackPointer
      M=M-D    // X-Y, left in top of stack (@SP)
      """
      c_neg = "todo c_neg"
      c_eq = "todo c_eq"
      c_gt = "todo c_gt"
      c_lt = "todo c_lt"
      c_and = "todo c_and"
      c_or = "todo c_or"
      c_not = "todo c_not"

		# TODO: more stuff here -- add, sub, neg, eq, gt, lt, and, or, not
      commands = {"add":c_add, "sub":c_sub, "neg":c_neg, "eq":c_eq, "gt":c_gt, 
                  "lt":c_lt, "and":c_and, "or":c_or, "not":c_not}
      self.oFile.write(commands.get(directive, None))

class Parser(object):
   """Parser of VM code"""
   def __init__(self):
      self.iLength = 0
      self.index = -1
      self.iStream = []

   def Constructor(self, vmFile):
      try:
         print(self.index)
      except:
         self.iLength = 0
         self.index = -1
         self.iStream = []
         fh = open(vmFile, 'r')
         rawInput = fh.readlines()
         for directive in rawInput:
            if '//' in directive:
               directive, remark = directive.split('//')
            directive = directive.strip()
            if len(directive) > 0: 
               directive = directive.replace('\t', ' ')
               self.iStream.append(directive)
               self.iLength += 1
            print(self.iLength) # debug
      return self

   def hasMoreCommands(self):
      return self.index <= self.iLength

   def advance(self):
      if self.hasMoreCommands(self):
         self.index += 1
      return self.index

   def commandType(self):
      if self.hasMoreCommands(self):
         print("ctype index: " + str(self.index)) # debug
         directive = operator.itemgetter(self.index)(self.iStream)
         if ' ' in directive: 
            try: directive, arg1, arg2 = directive.split(' ')
            except: print(".commandType() exception")
      return self.commandTable(directive)

	# Should not be called if the current command is C_RETURN.
   def arg1(self):
      thisType = self.commandType(self)
      if thisType is "C_RETURN":
         print("unexpected error #1 @" + self.index)
      sys.exit(1)
      thisArg = operator.itemgetter(self.index)(self.iStream)
      if ' ' in thisArg: 
         try: directive, ar1, ar2 = thisArg.split(' ')
         except: print(".arg1() exception @" + self.index)
      return ar1

	# Should be called only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.
   def arg2(self):
      thisType = self.commandType(self)
      if thisType is "C_PUSH" or "C_POP" or "C_FUNCTION" or "C_CALL":
         thisArg = operator.itemgetter(self.index)(self.iStream)
         try: directive, ar1, ar2 = thisArg.split(' ')
         except: print(".arg2() exception @" + self.index)
         return ar2
      print("arg2 contracall @" + self.index)
      sys.exit(2)

   def commandTable(strIn):
      m = strIn
      commands = {"add":m, "sub":m, "neg":m, "eq":m, "gt":m, "lt":m, "and":m, "or":m, "not":m,
                  "pop":"C_POP", "push":"C_PUSH", "label":"C_LABEL", "goto":"C_GOTO",
                  "if-goto":"C_IF", "function":"C_FUNCTION", "call":"C_CALL",
                  "return":"C_RETURN",}
      return commands.get(strIn, None)

global cue
cue = 1
while cue <= len(sys.argv):
   qued = sys.argv[cue]
   name = "default_name"
   extenstion = "x"
   try:
      name, extension = qued.split('.')
   except:
      print("argument error")

   w = CodeWriter
   r = Parser

   w.setFilename(w, name)
   w.Constructor(w)
   r.Constructor(r, qued)

   if r.hasMoreCommands(r):
      print(r.commandType(r)) # debug
      thisLine = r.commandType(r)
      if thisLine[0] is "C":
         print(thisLine + " @" + r.arg1(r) + "." + r.arg2(r)) # debug
         w.writePushPop(w, thisLine, r.arg1(r), r.arg2(r))
	      # TODO: do more stuff here
      elif thisLine is "add" or "sub" or "neg" or "eq" or "gt" or "lt" or "and" or "or" or "not":
         w.writeArithmetic(w, thisLine)	
      r.advance(r)
   #w.Close(w)
else:
   print("Usage: .....")