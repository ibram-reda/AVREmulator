## CPU Registers

### Program Counter Registe (PC)
The most important register in the AVR microcontroller is The PC. The program counter is used by CPU to point to the address of the next instruction to be executed. 

program counter is incremented automatically to point to the next instruction.

The Wider the program counter , the more memory locations (i.e., code memory) a CPU can access. the program counter width can be up to 22 bits wide


Controller       | PC size    |  Rom size               | code Address Range
-----------------|------------|-------------------------|----------------
Atiny 25         | 10 bit     | 1k   * 2byte   = 2kb    | 00000 - 003FF
Atmega 8         | 12 bit     | 4k   * 2bytes  = 8kb    | 00000 - 00FFF
atmega 32        | 14 bit     | 16K  * 2 bytes = 32kb   | 00000 - 03FFF
atmega 64        | 15 bit     | 32k  * 2 bytes = 64kb   | 00000 - 07FFF
Atmega 128       | 16 bit     | 64k  * 2bytes  = 1288kb | 00000 - 0FFFF
Atmega 256       | 17 bit     | 128k * 2bytes  = 256kb  | 00000 - 1FFFF


### Status Register (SREG)
also called (Flag Register) is an 8 bit register used manly to indicate arithmetic conditions (such as the carry bit), it used extensively by conditional branching instructions

 I   | T    | H   | S    | V   | N    | Z   | C   |
-----|------|-----|------|-----|------|-----|------|

- **C** Carry Flag: this flag is set whenever there is a carry out from the D7 bit.this flag bit is affectedd after an 8-bit addition or subtraction
- **Z** Zero Flag: The Zero flag reflects the results of an arithmetic or logic operation. If the Result is zero, then Z=1.Therefore, Z=0 if the result is not zero
- **N** Negative Flag:Binary representation of signed numbers uses D7 as the sign bit. The negative flag reflects the result of an arithmetic operation. If the D7 bit of the result is zero,then N=0 and the result is positive. If the D7 bit is one, then N=1 and the result is negative.The negative and V flag bit are used for the signed number arithmetic operations.
- V Two’s complement overflow indicator: This flag is set whenever the result of a signed number operation is too large,causing the high-order bit to overflow into the sign bit.In general,the carry flag is used to detect errors in unsigned arithmetic operations while the Tow's complement Overflow flag is used to detect errors in signed arithmetic operation. The v and N flag bits are used for signed number arithmetic operations.
- **S** N ⊕ V, for signed tests
- **H** Half Carry Flag : If there is acarry from D3 to D4 during an ADD or SUB Operations this bit is Set; otherwise it is cleared.
- **T** Transfer bit used by BLD and BST instructions
- **I** Global Interrupt Enable/Disable Flag

