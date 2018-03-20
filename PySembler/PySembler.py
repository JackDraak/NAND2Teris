#!/usr/bin/env python

# @author jackdraak

import sys

def Sanity():
	if len(sys.argv) <= 1 : Usage()
	elif (sys.argv[1] == "help") : Usage() 


def Usage():
	print ("\nUSAGE: Hacksemmbler fileOne.asm [fileTwo.asm ... fileEn.asm]\n")


Sanity()