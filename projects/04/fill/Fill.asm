// make a buffer size declaration
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
	M=D		// size of buffer

(SCAN)
	@KBD
	D=M		// check keyboard buffer
	@SCAN
	D;JEQ	// scan until input detected, else:

	@SCREEN
	D=A
	@pos
	M=D		// initialize procedure
(SET)
	@pos
	D=M
	@current
	M=D		// current = pos
	A=D
	@A		
	M=-1
	@pos
	D=M+1	// increment register
	M=D
	@top
	D-M
	@SET
	D;JLT	// continue until finished
	@SCAN
	0;JMP

(END)
	@END
	0;JMP

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