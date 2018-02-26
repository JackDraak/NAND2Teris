// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.
// File name: projects/04/Fill.asm

// Runs an infinite loop that listens to the keyboard input.
// When a key is pressed (any key), the program blackens the screen,
// i.e. writes "black" in every pixel;
// the screen should remain fully black as long as the key is pressed. 
// When no key is pressed, the program clears the screen, i.e. writes
// "white" in every pixel;
// the screen should remain fully clear as long as no key is pressed.

// prepare a buffer size declaration
	@SCREEN
	D=A		// get address of screen buffer
	@bottom
	M=D		// bottom address of screen buffer
	@KBD
	D=A		// get address of keyboard buffer
	@top
	M=D		// top address of screen buffer
	@bottom
	D=D-M	// buff=top-bottom
	@buff
	M=D		// size of screen buffer

(INIT)
	@pos
	M=0

(SCAN)
	@KBD
	D=M		// check keyboard buffer
	@OFF
	D;JEQ	// off until input detected, else:

(ON)
	@pos
	D=M
	@SCREEN
	A=A+D	// select register using @pos
	M=-1	// set all bits in register to "black"
	@pos
	D=M+1	
	M=D		// increment position register
	@buff
	D=D-M
	@ON
	D;JLT	// continue until finished
	@INIT
	0;JMP

(OFF)
	@pos
	D=M
	@SCREEN
	A=A+D	// select register using @pos
	M=0		// set all bits in register to "white"
	@pos
	D=M+1	
	M=D		// increment position register
	@buff
	D=D-M
	@OFF
	D;JLT	// continue until finished
	@INIT
	0;JMP

(END)
	@END
	0;JMP