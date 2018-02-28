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

// declare initial state, for cleanliness
	@state	
	M=0		// state-awareness: 0=OFF, 1=ON
			// While I'm happy with this optimization, I later learned 
			// that it can be done much more elegantly... Perhaps I'll 
			// need to revisit this design when I have the time.

// prepare a buffer size declaration -- I've gone slightly overboard here, this can be optimized.
	@SCREEN
	D=A		// get address of screen buffer
	@bottom
	M=D		// set bottom address of screen buffer
	@KBD
	D=A		// get address of keyboard buffer
	@top
	M=D		// set top address of screen buffer
	@bottom
	D=D-M	// calc: buff=top-bottom
	@buff
	M=D		// set size of screen buffer

(INIT)		// called after any transition.
	@pos
	M=0		// initialize current position in screen buffer -- after completing my design I saw a clever
			// design that is bi-directional, and doesn't need to re-initialize the screen buffer offset
			// (i.e. it turns on pixels walking one way through memory, then  clears them walking the
			// other direction. If I work on this further I should try to do something similar.  That design
			// also made my 'statefulness' look like child's-play with it's grace and "simplicity".)

// reduced SCAN loop to eliminate screen buffer thrashing
(SCAN)
	@KBD
	D=M		// check keyboard buffer
	@SWITCHOFF
	D;JEQ	// off until input detected, else:

// screen buffer is "state-aware", will only refresh when needed
(SWITCHON)
	@state
	D=M
	@SCAN
	D-1;JEQ	// if already "ON", rescan
	@ON
	0;JMP	// else refresh "ON" state

(SWITCHOFF)
	@state
	D=M
	@SCAN
	D;JEQ	// if already "OFF", rescan
	@OFF
	0;JMP	// else refresh "OFF" state

// set entire screen buffer to "black"
(ON)
	@pos
	D=M
	@SCREEN
	A=A+D	// select current register using @pos
	M=-1	// set all bits in register to "black"
	@pos
	D=M+1	
	M=D		// increment position register
	@buff
	D=D-M
	@ON
	D;JLT	// continue until finished
	@state
	M=1
	@INIT
	0;JMP

// set entire screen buffer to "white"
(OFF)
	@pos
	D=M
	@SCREEN
	A=A+D	// select current register using @pos
	M=0		// set all bits in register to "white"
	@pos
	D=M+1	
	M=D		// increment position register
	@buff
	D=D-M
	@OFF
	D;JLT	// continue until finished
	@state
	M=0
	@INIT
	0;JMP
