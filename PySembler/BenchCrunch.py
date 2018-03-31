#!/usr/bin/env python
#
#	@author	JackDraak
#
import platform, string, sys, time, threading
HAPTIC_INTERVAL = 0.6
PLATFORM_FIRST_ARG = 1
VERSION = "0.0.3"

def Main():
	argument = PLATFORM_FIRST_ARG
	queSize = len(sys.argv)
	while argument < queSize:
		qued = sys.argv[argument]
		fileHanlde = open(qued,'r')
		nextBench = TallyBenchmarks(fileHanlde, argument)
		argument += 1

def GetName(arg):
	delimiter = '.'
	thisArg = sys.argv[int(arg)]
	success = thisArg.find(delimiter)
	if success >= 1:
		return thisArg[0:success]

def PrintUsage(exitCode):
	print ("\nUSAGE: BenchCrunch.py fileOne.bench [fileTwo.bench ... fileEn.bench]\n")
	sys.exit(exitCode)

def Start():
	if len(sys.argv) <= 1 or sys.argv[1] == "help": 
		PrintUsage(1)
	try: Main()
	except:
		print("FAIL -- unable to parse input: " + str(sys.argv))
		PrintUsage(9)

def TallyBenchmarks(inFile, arg):
	timesBenched = timeTotal = timeSlow = 0 
	timeFast = 99
	benchName = GetName(arg)
	rawInput = inFile.readlines()
	for benchmark in rawInput:
		timesBenched += 1
		timeTotal = float(timeTotal) + float(benchmark)
		if float(benchmark) > timeSlow: timeSlow = float(benchmark)
		if float(benchmark) < timeFast: timeFast = float(benchmark)
	timeAverage = timeTotal / timesBenched
	print(benchName + "\taverage: " + str(timeAverage)[:7] + ", " + str(timeFast) + 
	   "-" + str(timeSlow) + "\t(" + str(timesBenched) + " trials)")

Start()
