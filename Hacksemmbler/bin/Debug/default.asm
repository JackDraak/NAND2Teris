// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.
// File name: projects/04/Mult.asm

// Multiplies R0 and R1 and stores the result in R2, when R0 and R1 are positive.
// (R0, R1, R2 refer to RAM[0], RAM[1], and RAM[2], respectively.)

// Sanity checks
	@2		// @R2 is Sum register
	M=0		// set Sum to zero
	@0		// @ R0 is 'X'
	D=M		// Fetch 'X'
	@26
	D;JLE	// JMP to END if X <= 0
	@1		// @R1 is 'Y'
	D=M		// Fetch 'Y'
	@26
	D;JLE	//JMP to END if Y <= 0

// Prepare
	@1		// 'Y'
	D=M		// Fetch 'Y' into D
	@16	// @count temp variable for decrementing value of 'Y'
	M=D		// Store multiplier 'Y' in @count

// Add X+X, Y times:
(MULTI)
	@0
	D=M		// 'X' into D
	@2
	M=M+D	// Add 'X' to Sum
	@16
	D=M-1	// Decrement multiplier
	@26
	D;JLE	// if @count==0, JMP to END
	@16
	M=D		// Update @count
	@14
	0;JMP	// Repeat MULTI until jumped

(END)
	@26
	0;JMP  // Infinite loop when calculation complete (HACK requirement).

