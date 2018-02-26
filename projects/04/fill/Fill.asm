// make a buffer size declaration
@KBD
D=A
@top
M=D // top of memory range
@SCREEN
D=A
@bottom
M=D // bottom of memory range
@top
D=M
@bottom
D=D-M
@buff
M=D // size of buffer
	
(SCAN)
	@KBD	// pointer to keyboard input
	D=M		// read keyboard into D
	@DARK
	D;JNE	// on input, JMP to DARK 
	@SCAN	// else, reSCAN
	0;JMP
(DARK)
	@screenpos
	M=0		// init position 0
(SET)
	// get and check offset
	@screenpos
	D=M
	@buff
	D-M
	@SCAN
	D;JLE // SCAN if offset beyond bounds

	@screenpos
	D=M		// D = memory offset
	@SCREEN+D
	M=-1
	@screenpos
	M=D+1	// increment memory offset
	@SET	// repeat until jumped 
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