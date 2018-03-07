// Set specific instructions for modeling nominal CPU

(INIT)
	@myRegister		// at first variable register
	MD=-1			// set all bits in register on

	@13				// at register 13
	M=A				// set register value to 13

	@8192			// set A to a constant
	MD=A			// set register to it's own value & copy to D
	@counter		// at next variable register
	M=D				// set register (counter) to 8192	

	@3				// set A to a constant
	MD=A			// set register to it's own value & copy to D
	@offset			// at next variable register
	M=D				// set register (offset) to 3

(LOOP)
	@LOOP
	0;JMP