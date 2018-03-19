#!/bin/python2

import sys

dctNANDCount = {'Nand' : 1, 'DFF': 4}

dctSubChipCount = {}
nChipTotal = 0

# Get the NANDs in a chip
def count(sChip, isTopLevel=False):
  
  # If we dont already know the count for this chip...
  if sChip not in dctNANDCount:      
    # scan the file, strip the trailing whitespace 
    with open(sChip + '.hdl') as f:
      arrContent = f.readlines()
    arrContent = [x.strip() for x in arrContent]
    
    iCount = 0
        
    # Find the "PARTS" line and iterate till the end
    idx = arrContent.index('PARTS:')
    for i in range(idx+1, len(arrContent)-1):
      sLine = arrContent[i]

      # Check if it starts with a letter (it must be a chip)
      if len(sLine):
        if sLine[0] >= 'A' and sLine[0] <= 'Z':
          sSubChip = sLine.split('(')[0]
          iCount = iCount + count(sSubChip)
          
          if isTopLevel:
            if sSubChip in dctSubChipCount:
              dctSubChipCount[sSubChip] = dctSubChipCount[sSubChip] + 1
            else:
              dctSubChipCount[sSubChip] = 1
    
    # Cache the value for later
    dctNANDCount[sChip] = iCount
  
  return dctNANDCount[sChip]
  
  
sChipName = sys.argv[1]      
nNANDs = count(sChipName, True)

arrChipNames = dctNANDCount.keys();
arrChipNames.sort();

if len(sys.argv) > 2 and sys.argv[2]=='all':
  for i in arrChipNames:
    if not i == 'Nand':
      print '%-20s%4s NANDs' % (i, dctNANDCount[i]); 
else:
  for i in arrChipNames:
    if i in dctSubChipCount:
      nChipTotal = nChipTotal + dctSubChipCount[i]
      s = '%s(%s)' % (i, dctSubChipCount[i])
      print '%-20s%4s NANDs' % (s, dctNANDCount[i]); 
  print '------------------------------'
  print '%-20s%4s NANDs in %s gates' % ('Total', nNANDs, nChipTotal)

