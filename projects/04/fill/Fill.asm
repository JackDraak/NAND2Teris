// make a buffer size declaration
	@SCREEN
	D=A
	@bottom
	M=D // bottom of screen buffer
	@KBD
	D=A
	@top
	M=D // top of screen buffer
	@bottom
	D=D-M
	@buff
	M=D // size of buffer

(SCAN)
	@KBD
	D=M
	@SCAN
	D;JEQ // scan until input detected, else:

	@pos
	M=0
(SET)
	@pos
	D=M
	@current
	M=D //+@bottom
	@bottom
	D=M
	@current
	M=D+M
	D=M
	@D
	M=-1
	@pos
	D=M+1
	M=D
	@top
	D-M
	@SCAN
	D;JEQ
	@SET
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