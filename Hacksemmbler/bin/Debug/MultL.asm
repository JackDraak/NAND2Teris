// multiply two positive integers (in R0 & R1, placing result in R2).
	@19269	// testing 'bad address' decoding
	@38713	// testing 'bad address' decoding
	@52021	// testing 'bad address' decoding
	@77654	// testing 'bad address' decoding
	@92126	// testing 'bad address' decoding
	@121473	// testing 'bad address' decoding
@2
M=0
@0
D=M
@26
D;JLE
@1
D=M
@26
D;JLE
@1
D=M
@16
M=D
@0
D=M
@2
M=M+D
@16
D=M-1
@26
D;JLE
@26
M=D
@14
0;JMP
@26
0;JMP  
// multiply two positive integers (in R0 & R1, placing result in R2).

